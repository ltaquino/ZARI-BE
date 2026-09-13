#!/usr/bin/env bash
# ZARI full-cycle integration test — master data creation through a 5 backdated day cycle
# covering procurement, POS selling, AR selling, returns, price variance, partial payment,
# a manual journal entry, BIR period-close enforcement, and every BIR "books of accounts" /
# financial report endpoint this codebase exposes.
#
# Day 0 (setup):   create a brand-new Branch's full master-data ecosystem from scratch — Branch,
#                  FiscalYear, GL accounts, CostCenter, Warehouse, two Items (plain + serialized),
#                  ItemBranchSettings, Supplier, two Customers (walk-in + AR), BankAccount,
#                  POS Terminal, a BIR-OR DocumentSequence, and two Users (an encoder on the Staff
#                  role, an approver on the Manager role) — everything idempotent (safe to re-run;
#                  looks each thing up by its natural key first and only creates what's missing).
# Day 1 (procure): Purchase Request -> Purchase Order -> Goods Receipt -> AP Invoice, at cost.
#                  Receives a plain Avg-costed item and one serialized unit.
# Day 2 (sell):    two POS checkouts sell what Day 1 received, plus a direct (non-POS) AR Sales
#                  Invoice to the new branch's regular Customer -- collected via Customer Payment.
#                  Day 1's AP Invoice is paid in full.
# Day 3 (return):  a Sales Return reverses part of each POS sale, then X-/Z-Reading close the day.
# Day 4 (variance):a second procurement cycle where the AP Invoice is billed at a HIGHER unit cost
#                  than the PO/GRPO -- exercises the Purchase Price Variance GL posting -- paid
#                  across two Outgoing Payments (a partial payment, then the remaining balance).
# Day 5 (close):   a Manual Journal Entry (accrual), a live proof that a CLOSED fiscal year blocks
#                  posting, then a full sweep of every BIR book / financial report endpoint,
#                  finishing with a whole-cycle trial-balance tie-out (total debit == total credit
#                  across every journal this run posted).
#
# Requires: a running ZARI.Api dev instance (default https://localhost:7200), curl, grep/sed/awk
# (no jq/python -- see this session's own notes on Windows path mapping breaking those against
# Git-Bash paths). Run from anywhere; self-contained; safe to re-run against the same dev DB.

set -u
BASE_URL="${BASE_URL:-https://localhost:7200}"
CURL="curl -sk"
PASS=0
FAIL=0

# --- tiny assertion / JSON helpers (same naive, dependency-free style as full-cycle-3day-test.sh) -
ok()   { PASS=$((PASS+1)); echo "  [PASS] $1"; }
bad()  { FAIL=$((FAIL+1)); echo "  [FAIL] $1"; }
num()  { awk -v n="${1:-0}" 'BEGIN{printf "%.4f", n+0}'; }
add()  { awk -v a="${1:-0}" -v b="${2:-0}" 'BEGIN{printf "%.4f", a+b}'; }
sub()  { awk -v a="${1:-0}" -v b="${2:-0}" 'BEGIN{printf "%.4f", a-b}'; }
mul()  { awk -v a="${1:-0}" -v b="${2:-0}" 'BEGIN{printf "%.4f", a*b}'; }
assert_eq() {
  local e a; e=$(num "$2"); a=$(num "$3")
  if [ "$e" = "$a" ]; then ok "$1 (= $e)"; else bad "$1 (expected $e, got $a)"; fi
}
assert_status() {
  if echo "$2" | grep -q "$3"; then ok "$1"; else bad "$1 -- body: $(echo "$2" | head -c 300)"; fi
}
assert_not() {
  if echo "$2" | grep -q "$3"; then bad "$1 -- unexpectedly matched: $(echo "$2" | head -c 300)"; else ok "$1"; fi
}
jget() { # naive single-level JSON field extractor (first match only)
  local json="$1" field="$2" sval
  sval=$(echo "$json" | grep -o "\"$field\":\"[^\"]*\"" | head -1)
  if [ -n "$sval" ]; then echo "$sval" | sed "s/\"$field\":\"//;s/\"\$//"; return; fi
  echo "$json" | grep -o "\"$field\":[^,}]*" | head -1 | sed "s/\"$field\"://"
}
find_id_by() { # $1=json_array $2=field $3=value -> id of first flat object where field==value
  echo "$1" | grep -o "{[^{}]*\"$2\":\"$(printf '%s' "$3" | sed 's/[&/\]/\\&/g')\"[^{}]*}" | head -1 \
    | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//'
}
extract_array() { # $1=json $2=field -> the raw "[...]" array literal (flat string arrays only)
  echo "$1" | grep -o "\"$2\":\[[^]]*\]" | sed "s/\"$2\"://" | head -1
}
# Sum of every debitAmount/creditAmount for a given accountId inside a GlJournalResponse's lines[]
# array -- exploits System.Text.Json's stable declaration-order serialization: each line renders as
# ..."accountId":"<id>","costCenterId":...,"debitAmount":X,"creditAmount":Y... so grabbing the
# window right after the accountId match reliably lands on that same line's two amounts.
sum_journal_side() { # $1=journal_json $2=accountId $3=debitAmount|creditAmount
  echo "$1" | grep -o "\"accountId\":\"$2\"[^}]*\"$3\":[0-9.]*" \
    | grep -o "\"$3\":[0-9.]*" | sed "s/\"$3\"://" \
    | awk '{s+=$1} END{printf "%.4f", s+0}'
}
# GET /api/gl-journals ignores every query string -- confirmed by reading GlJournalEndpoints.cs,
# it always returns the whole table -- so isolating "this one document's own journal" out of that
# flat array (each element itself holds a nested lines[] array, so braces are NOT single-level) has
# to be done by hand: walk the raw JSON character by character, tracking brace depth, and hand back
# whichever depth-0-to-depth-1 span contains our needle. No jq/python in this environment (Windows
# path mapping breaks both against Git-Bash paths) -- pure awk, no external JSON tooling required.
extract_toplevel_object() { # $1=json_array $2=needle_substring -> the one whole element containing it
  awk -v needle="$2" '
  { text = $0; n = length(text); depth = 0; start = 0
    for (i = 1; i <= n; i++) {
      c = substr(text, i, 1)
      if (c == "{") { if (depth == 0) start = i; depth++ }
      else if (c == "}") {
        depth--
        if (depth == 0 && start > 0) {
          obj = substr(text, start, i - start + 1)
          if (index(obj, needle) > 0) { print obj; exit }
          start = 0
        }
      }
    }
  }' <<< "$1"
}
journal_for() { # $1=sourceReferenceTable $2=sourceReferenceId -> that document's own GlJournal, or ""
  # Always re-fetches -- this run posts only a few dozen journals, so the extra round trips are
  # cheap and it's never at risk of reading a stale list from before a later posting.
  extract_toplevel_object "$($CURL "$BASE_URL/api/gl-journals" "${ADM_AUTH[@]}")" "\"sourceReferenceTable\":\"$1\",\"sourceReferenceId\":\"$2\""
}

echo "=== ZARI full-cycle 5-day test (master data -> 5 backdated days) against $BASE_URL ==="

# --- login as admin for master-data setup, then the two branch-specific test users for the actual
# transaction cycle (self-approval guard requires two distinct people throughout this codebase) ---
login() {
  local body; body=$($CURL -X POST "$BASE_URL/api/identity/login" -H "Content-Type: application/json" -d "{\"email\":\"$1\",\"password\":\"$2\"}")
  echo "$(jget "$body" jwToken) $(jget "$body" id)"
}
read -r ADM_TOKEN ADM_ID < <(login "admin@zari.coop" "zari123")
[ -n "$ADM_TOKEN" ] && ok "Logged in as admin (master data setup)" || { bad "admin login -- aborting"; exit 1; }
ADM_AUTH=(-H "Authorization: Bearer $ADM_TOKEN")
JSON=(-H "Content-Type: application/json")

echo ""
echo "=== DAY 0 -- master data, created from scratch (idempotent: safe to re-run) ==="

# 1. Branch -----------------------------------------------------------------------------------
BR_LIST=$($CURL "$BASE_URL/api/branches" "${ADM_AUTH[@]}")
BRANCH_ID=$(find_id_by "$BR_LIST" code "TB")
if [ -z "$BRANCH_ID" ]; then
  BR_BODY=$($CURL -X POST "$BASE_URL/api/branches" "${ADM_AUTH[@]}" "${JSON[@]}" -d '{
    "name":"Test Branch (5-Day Cycle)","code":"TB","city":"Cainta","address":"Unit 9, Test Complex, Cainta, Rizal",
    "phone":"+63 2 8000 0000","status":"active","isHeadOffice":false,
    "birBranchCode":"00099","posPermitNumber":"FP-TB-000001","posPermitDateIssued":"2026-01-01T00:00:00Z",
    "machineIdentificationNumber":"MIN-TB-0001","machineSerialNumber":"SN-TB-0001"}')
  BRANCH_ID=$(jget "$BR_BODY" id)
  [ -n "$BRANCH_ID" ] && ok "Created Branch TB (BIR branch code 00099, MIN MIN-TB-0001)" || bad "Create Branch -- $BR_BODY"
else
  ok "Branch TB already exists, reusing"
fi

# 1b. Every branch-scoped create below (CostCenter, Warehouse, Customer, BankAccount, PosTerminal,
#     POS closing...) is gated not just by the Admin role's permission grant but also by whether
#     the calling user is actually linked to that branch -- and admin@zari.coop, being seeded
#     against the original 4 branches, isn't linked to a brand-new branch until we say so. Link it.
ADMIN_USER=$($CURL "$BASE_URL/api/users/$ADM_ID" "${ADM_AUTH[@]}")
ADMIN_BRANCH_IDS=$(extract_array "$ADMIN_USER" branchIds)
if ! echo "$ADMIN_BRANCH_IDS" | grep -q "\"$BRANCH_ID\""; then
  ADMIN_ROLE_IDS=$(extract_array "$ADMIN_USER" roleIds)
  NEW_ADMIN_BRANCH_IDS=$(echo "$ADMIN_BRANCH_IDS" | sed "s/\]\$/,\"$BRANCH_ID\"]/")
  $CURL -X PUT "$BASE_URL/api/users/$ADM_ID" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"email\":\"$(jget "$ADMIN_USER" email)\",\"firstName\":\"$(jget "$ADMIN_USER" firstName)\",
    \"lastName\":\"$(jget "$ADMIN_USER" lastName)\",\"phone\":null,\"status\":\"$(jget "$ADMIN_USER" status)\",
    \"roleIds\":$ADMIN_ROLE_IDS,\"branchIds\":$NEW_ADMIN_BRANCH_IDS}" > /dev/null
  ADMIN_CHECK=$($CURL "$BASE_URL/api/users/$ADM_ID" "${ADM_AUTH[@]}")
  echo "$(extract_array "$ADMIN_CHECK" branchIds)" | grep -q "\"$BRANCH_ID\"" \
    && ok "Linked admin to branch TB (required for master-data creation below)" \
    || bad "Link admin to branch TB -- $ADMIN_CHECK"
else
  ok "Admin already linked to branch TB"
fi

# 2. FiscalYear ---------------------------------------------------------------------------------
FY_LIST=$($CURL "$BASE_URL/api/fiscal-years" "${ADM_AUTH[@]}")
FY_ID=$(echo "$FY_LIST" | grep -o "{[^{}]*\"yearName\":\"FY 2026\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$FY_ID" ]; then
  FY_BODY=$($CURL -X POST "$BASE_URL/api/fiscal-years" "${ADM_AUTH[@]}" "${JSON[@]}" -d '{
    "yearName":"FY 2026","startDate":"2026-01-01T00:00:00Z","endDate":"2026-12-31T23:59:59Z","status":"OPEN"}')
  FY_ID=$(jget "$FY_BODY" id)
  [ -n "$FY_ID" ] && ok "Created FiscalYear FY 2026 (OPEN)" || bad "Create FiscalYear -- $FY_BODY"
else
  ok "FiscalYear FY 2026 already exists, reusing"
fi

# 3. Two new GL accounts for the Day 5 manual accrual (the rest of the chart -- Cash, AR, Inventory,
#    AP, GRNI, Revenue, COGS, Purchase Price Variance -- is already seeded) -----------------------
GL_LIST=$($CURL "$BASE_URL/api/gl-accounts" "${ADM_AUTH[@]}")
ensure_gl_account() { # $1=code $2=name $3=type $4=normalBalance -> echoes id
  local id; id=$(find_id_by "$GL_LIST" code "$1")
  if [ -n "$id" ]; then echo "$id"; return; fi
  local body; body=$($CURL -X POST "$BASE_URL/api/gl-accounts" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{\"code\":\"$1\",\"name\":\"$2\",\"accountType\":\"$3\",\"normalBalance\":\"$4\",\"parentAccountId\":null,\"status\":\"active\"}")
  GL_LIST=$($CURL "$BASE_URL/api/gl-accounts" "${ADM_AUTH[@]}")
  jget "$body" id
}
UTIL_EXP_ID=$(ensure_gl_account "6100" "Utilities Expense" "Expense" "Debit")
ACCR_PAY_ID=$(ensure_gl_account "2300" "Accrued Expenses Payable" "Liability" "Credit")
[ -n "$UTIL_EXP_ID" ] && [ -n "$ACCR_PAY_ID" ] && ok "GL accounts 6100 Utilities Expense / 2300 Accrued Expenses Payable ready" || bad "GL account setup -- util=$UTIL_EXP_ID accr=$ACCR_PAY_ID"
CASH_ON_HAND_ID=$(find_id_by "$GL_LIST" code "1000")
AP_CONTROL_ID=$(find_id_by "$GL_LIST" code "2000")
GRNI_ID=$(find_id_by "$GL_LIST" code "2100")
PPV_ID=$(find_id_by "$GL_LIST" code "5200")

# 4. CostCenter -----------------------------------------------------------------------------------
CC_LIST=$($CURL "$BASE_URL/api/cost-centers" "${ADM_AUTH[@]}")
COST_CENTER_ID=$(find_id_by "$CC_LIST" code "CC-TB")
if [ -z "$COST_CENTER_ID" ]; then
  CC_BODY=$($CURL -X POST "$BASE_URL/api/cost-centers" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{\"branchId\":\"$BRANCH_ID\",\"code\":\"CC-TB\",\"name\":\"Test Branch Cost Center\",\"status\":\"active\"}")
  COST_CENTER_ID=$(jget "$CC_BODY" id)
  [ -n "$COST_CENTER_ID" ] && ok "Created CostCenter CC-TB" || bad "Create CostCenter -- $CC_BODY"
else
  ok "CostCenter CC-TB already exists, reusing"
fi

# 5. Warehouse ------------------------------------------------------------------------------------
WH_LIST=$($CURL "$BASE_URL/api/warehouses" "${ADM_AUTH[@]}")
WAREHOUSE_ID=$(find_id_by "$WH_LIST" code "TB-MAIN")
if [ -z "$WAREHOUSE_ID" ]; then
  WH_BODY=$($CURL -X POST "$BASE_URL/api/warehouses" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{\"branchId\":\"$BRANCH_ID\",\"code\":\"TB-MAIN\",\"name\":\"Test Branch Main Warehouse\",\"warehouseType\":\"Main\",\"status\":\"active\"}")
  WAREHOUSE_ID=$(jget "$WH_BODY" id)
  [ -n "$WAREHOUSE_ID" ] && ok "Created Warehouse TB-MAIN" || bad "Create Warehouse -- $WH_BODY"
else
  ok "Warehouse TB-MAIN already exists, reusing"
fi

# 6. UOM (reuse the already-seeded PCS unit, no need to create a new one) -------------------------
UOM_PCS_ID=$(find_id_by "$($CURL "$BASE_URL/api/uoms" "${ADM_AUTH[@]}")" code "PCS")
[ -n "$UOM_PCS_ID" ] && ok "Resolved UOM PCS" || bad "Resolve UOM PCS"

# 7. Items ----------------------------------------------------------------------------------------
IT_LIST=$($CURL "$BASE_URL/api/items" "${ADM_AUTH[@]}")
ITEM_A_ID=$(find_id_by "$IT_LIST" code "TB-ITEM-A")
if [ -z "$ITEM_A_ID" ]; then
  A_BODY=$($CURL -X POST "$BASE_URL/api/items" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"code\":\"TB-ITEM-A\",\"name\":\"Test Branch Item A (Avg-costed)\",\"description\":null,\"categoryId\":null,
    \"baseUomId\":\"$UOM_PCS_ID\",\"itemType\":\"FinishedGood\",\"costingMethod\":\"Avg\",
    \"isSerialized\":false,\"isBatchTracked\":false,\"isSold\":true,\"isPurchased\":true,\"isStocked\":true,
    \"salesAccountId\":null,\"purchaseAccountId\":null,\"inventoryAccountId\":null,\"cogsAccountId\":null,
    \"vatType\":\"VATABLE\",\"status\":\"active\"}")
  ITEM_A_ID=$(jget "$A_BODY" id)
  [ -n "$ITEM_A_ID" ] && ok "Created Item TB-ITEM-A (plain, Avg-costed)" || bad "Create Item A -- $A_BODY"
else
  ok "Item TB-ITEM-A already exists, reusing"
fi
ITEM_B_ID=$(find_id_by "$IT_LIST" code "TB-ITEM-B")
if [ -z "$ITEM_B_ID" ]; then
  B_BODY=$($CURL -X POST "$BASE_URL/api/items" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"code\":\"TB-ITEM-B\",\"name\":\"Test Branch Item B (Serialized)\",\"description\":null,\"categoryId\":null,
    \"baseUomId\":\"$UOM_PCS_ID\",\"itemType\":\"FinishedGood\",\"costingMethod\":\"Avg\",
    \"isSerialized\":true,\"isBatchTracked\":false,\"isSold\":true,\"isPurchased\":true,\"isStocked\":true,
    \"salesAccountId\":null,\"purchaseAccountId\":null,\"inventoryAccountId\":null,\"cogsAccountId\":null,
    \"vatType\":\"VATABLE\",\"status\":\"active\"}")
  ITEM_B_ID=$(jget "$B_BODY" id)
  [ -n "$ITEM_B_ID" ] && ok "Created Item TB-ITEM-B (serialized)" || bad "Create Item B -- $B_BODY"
else
  ok "Item TB-ITEM-B already exists, reusing"
fi

# 8. ItemBranchSetting (the AUTHORITATIVE selling price + default warehouse per branch) -----------
IBS_LIST=$($CURL "$BASE_URL/api/item-branch-settings" "${ADM_AUTH[@]}")
ensure_ibs() { # $1=itemId $2=sellingPrice -> "existing"/"created:<id>"/"FAILED:<body>"
  local existing; existing=$(echo "$IBS_LIST" | grep -o "{[^{}]*\"itemId\":\"$1\"[^{}]*\"branchId\":\"$BRANCH_ID\"[^{}]*}")
  if [ -n "$existing" ]; then echo "existing"; return; fi
  local body; body=$($CURL -X POST "$BASE_URL/api/item-branch-settings" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"itemId\":\"$1\",\"branchId\":\"$BRANCH_ID\",\"defaultWarehouseId\":\"$WAREHOUSE_ID\",
    \"reorderPoint\":5,\"minStock\":0,\"maxStock\":1000,\"sellingPrice\":$2,\"markupPct\":null,\"status\":\"active\"}")
  local id; id=$(jget "$body" id)
  if [ -n "$id" ]; then echo "created:$id"; else echo "FAILED:$body"; fi
}
IBS_A=$(ensure_ibs "$ITEM_A_ID" "25.00")
IBS_B=$(ensure_ibs "$ITEM_B_ID" "750.00")
case "$IBS_A" in FAILED:*) bad "ItemBranchSetting for TB-ITEM-A -- ${IBS_A#FAILED:}";; *) ok "ItemBranchSetting for TB-ITEM-A ready ($IBS_A, price 25.00)";; esac
case "$IBS_B" in FAILED:*) bad "ItemBranchSetting for TB-ITEM-B -- ${IBS_B#FAILED:}";; *) ok "ItemBranchSetting for TB-ITEM-B ready ($IBS_B, price 750.00)";; esac

# 9. Supplier ---------------------------------------------------------------------------------------
SUP_LIST=$($CURL "$BASE_URL/api/suppliers" "${ADM_AUTH[@]}")
SUPPLIER_ID=$(find_id_by "$SUP_LIST" code "SUP-TB")
if [ -z "$SUPPLIER_ID" ]; then
  SUP_BODY=$($CURL -X POST "$BASE_URL/api/suppliers" "${ADM_AUTH[@]}" "${JSON[@]}" -d '{
    "code":"SUP-TB","name":"Test Branch Supplier Co.","taxId":"111-222-333-000","paymentTermsDays":30,
    "currencyId":null,"apAccountId":null,"address":"456 Supplier Ave.","contactPerson":"Maria Santos",
    "contactNumber":"+63 917 111 2222","email":"supplier.tb@example.com","status":"active"}')
  SUPPLIER_ID=$(jget "$SUP_BODY" id)
  [ -n "$SUPPLIER_ID" ] && ok "Created Supplier SUP-TB" || bad "Create Supplier -- $SUP_BODY"
else
  ok "Supplier SUP-TB already exists, reusing"
fi

# 10. Customers: a Walk-in (for POS/returns) and a regular AR customer (for the direct Sales Invoice
#     + Customer Payment on Day 2, to exercise the Cash Receipts Book) ------------------------------
CUST_LIST=$($CURL "$BASE_URL/api/customers" "${ADM_AUTH[@]}")
WALKIN_CUSTOMER_ID=$(echo "$CUST_LIST" | grep -o "{[^{}]*\"name\":\"Walk-in Customer\"[^{}]*\"branchId\":\"$BRANCH_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$WALKIN_CUSTOMER_ID" ]; then
  WI_BODY=$($CURL -X POST "$BASE_URL/api/customers" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"name\":\"Walk-in Customer\",\"type\":\"individual\",\"email\":\"walkin.tb@zari.coop\",\"phone\":\"0000000000\",
    \"branchId\":\"$BRANCH_ID\",\"status\":\"active\",\"owner\":\"System\",\"address\":\"N/A\",\"notes\":null,
    \"arAccountId\":null,\"paymentTermsDays\":null,\"standingDiscountPct\":null,\"memberNo\":null}")
  WALKIN_CUSTOMER_ID=$(jget "$WI_BODY" id)
  [ -n "$WALKIN_CUSTOMER_ID" ] && ok "Created Walk-in Customer for TB" || bad "Create Walk-in Customer -- $WI_BODY"
else
  ok "Walk-in Customer for TB already exists, reusing"
fi
AR_CUSTOMER_ID=$(echo "$CUST_LIST" | grep -o "{[^{}]*\"name\":\"Test Branch Business Customer\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$AR_CUSTOMER_ID" ]; then
  AR_BODY=$($CURL -X POST "$BASE_URL/api/customers" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"name\":\"Test Branch Business Customer\",\"type\":\"business\",\"email\":\"arcustomer.tb@example.com\",\"phone\":\"+63 917 333 4444\",
    \"branchId\":\"$BRANCH_ID\",\"status\":\"active\",\"owner\":\"Test Encoder\",\"address\":\"789 Business Park\",\"notes\":null,
    \"arAccountId\":null,\"paymentTermsDays\":15,\"standingDiscountPct\":null,\"memberNo\":\"MB-TB-0001\"}")
  AR_CUSTOMER_ID=$(jget "$AR_BODY" id)
  [ -n "$AR_CUSTOMER_ID" ] && ok "Created AR Customer 'Test Branch Business Customer'" || bad "Create AR Customer -- $AR_BODY"
else
  ok "AR Customer 'Test Branch Business Customer' already exists, reusing"
fi

# 11. PaymentMethod (reuse the global seeded CASH method -- PaymentMethod isn't branch-scoped) -----
CASH_METHOD_ID=$(find_id_by "$($CURL "$BASE_URL/api/payment-methods" "${ADM_AUTH[@]}")" code "CASH")
[ -n "$CASH_METHOD_ID" ] && ok "Resolved PaymentMethod CASH" || bad "Resolve PaymentMethod CASH"

# 12. BankAccount -------------------------------------------------------------------------------
BA_LIST=$($CURL "$BASE_URL/api/bank-accounts" "${ADM_AUTH[@]}")
BANK_ACCOUNT_ID=$(find_id_by "$BA_LIST" accountNumber "TB-0001")
if [ -z "$BANK_ACCOUNT_ID" ]; then
  BA_BODY=$($CURL -X POST "$BASE_URL/api/bank-accounts" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"branchId\":\"$BRANCH_ID\",\"accountName\":\"Test Branch Operating Account\",\"accountNumber\":\"TB-0001\",
    \"bankName\":\"BDO\",\"glAccountId\":\"$CASH_ON_HAND_ID\",\"currencyId\":null}")
  BANK_ACCOUNT_ID=$(jget "$BA_BODY" id)
  [ -n "$BANK_ACCOUNT_ID" ] && ok "Created BankAccount TB-0001" || bad "Create BankAccount -- $BA_BODY"
else
  ok "BankAccount TB-0001 already exists, reusing"
fi

# 13. POS Terminal --------------------------------------------------------------------------------
PT_LIST=$($CURL "$BASE_URL/api/pos-terminals" "${ADM_AUTH[@]}")
TERMINAL_ID=$(find_id_by "$PT_LIST" code "TB-POS-1")
if [ -z "$TERMINAL_ID" ]; then
  PT_BODY=$($CURL -X POST "$BASE_URL/api/pos-terminals" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"branchId\":\"$BRANCH_ID\",\"code\":\"TB-POS-1\",\"name\":\"Test Branch Front Counter\",
    \"machineIdentificationNumber\":\"MIN-TB-POS1\",\"machineSerialNumber\":\"SN-TB-POS1\",
    \"posPermitNumber\":\"FP-TB-000001\",\"posPermitDateIssued\":\"2026-01-01T00:00:00Z\",\"status\":\"active\"}")
  TERMINAL_ID=$(jget "$PT_BODY" id)
  [ -n "$TERMINAL_ID" ] && ok "Created POS Terminal TB-POS-1" || bad "Create POS Terminal -- $PT_BODY"
else
  ok "POS Terminal TB-POS-1 already exists, reusing"
fi

# 14. DocumentSequence for the branch's BIR Official Receipt series ------------------------------
DS_LIST=$($CURL "$BASE_URL/api/document-sequences" "${ADM_AUTH[@]}")
DS_EXISTS=$(echo "$DS_LIST" | grep -o "{[^{}]*\"branchId\":\"$BRANCH_ID\"[^{}]*\"docType\":\"BIR-OR\"[^{}]*}")
if [ -z "$DS_EXISTS" ]; then
  DS_BODY=$($CURL -X POST "$BASE_URL/api/document-sequences" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{\"branchId\":\"$BRANCH_ID\",\"docType\":\"BIR-OR\",\"prefix\":\"TB-OR-\",\"nextNumber\":1,\"paddingLength\":6}")
  [ -n "$(jget "$DS_BODY" id)" ] && ok "Created BIR-OR DocumentSequence for TB (TB-OR-000001...)" || bad "Create BIR-OR DocumentSequence -- $DS_BODY"
else
  ok "BIR-OR DocumentSequence for TB already exists, reusing"
fi

# 15. Users: an encoder (Staff role -- create/submit only) and an approver (Manager role) ---------
ROLE_LIST=$($CURL "$BASE_URL/api/roles" "${ADM_AUTH[@]}")
STAFF_ROLE_ID=$(echo "$ROLE_LIST" | grep -o '"id":"[^"]*","name":"Staff"' | head -1 | grep -o '"id":"[^"]*"' | sed 's/"id":"//;s/"//')
MANAGER_ROLE_ID=$(echo "$ROLE_LIST" | grep -o '"id":"[^"]*","name":"Manager"' | head -1 | grep -o '"id":"[^"]*"' | sed 's/"id":"//;s/"//')
USER_LIST=$($CURL "$BASE_URL/api/users" "${ADM_AUTH[@]}")
ENCODER_EMAIL="tb.encoder@zari.coop"
APPROVER_EMAIL="tb.approver@zari.coop"
ENCODER_ID=$(echo "$USER_LIST" | grep -o "{[^{}]*\"email\":\"$ENCODER_EMAIL\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$ENCODER_ID" ]; then
  ENC_BODY=$($CURL -X POST "$BASE_URL/api/users" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"email\":\"$ENCODER_EMAIL\",\"firstName\":\"Test Branch\",\"lastName\":\"Encoder\",\"phone\":null,
    \"status\":\"active\",\"password\":\"Zari@12345\",\"roleIds\":[\"$STAFF_ROLE_ID\"],\"branchIds\":[\"$BRANCH_ID\"]}")
  ENCODER_ID=$(jget "$ENC_BODY" id)
  [ -n "$ENCODER_ID" ] && ok "Created User tb.encoder (Staff role, TB branch)" || bad "Create encoder user -- $ENC_BODY"
else
  ok "User tb.encoder already exists, reusing"
fi
APPROVER_ID=$(echo "$USER_LIST" | grep -o "{[^{}]*\"email\":\"$APPROVER_EMAIL\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$APPROVER_ID" ]; then
  APR_BODY=$($CURL -X POST "$BASE_URL/api/users" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{
    \"email\":\"$APPROVER_EMAIL\",\"firstName\":\"Test Branch\",\"lastName\":\"Approver\",\"phone\":null,
    \"status\":\"active\",\"password\":\"Zari@12345\",\"roleIds\":[\"$MANAGER_ROLE_ID\"],\"branchIds\":[\"$BRANCH_ID\"]}")
  APPROVER_ID=$(jget "$APR_BODY" id)
  [ -n "$APPROVER_ID" ] && ok "Created User tb.approver (Manager role, TB branch)" || bad "Create approver user -- $APR_BODY"
else
  ok "User tb.approver already exists, reusing"
fi

read -r ENC_TOKEN ENC_ID2 < <(login "$ENCODER_EMAIL" "Zari@12345")
read -r APR_TOKEN APR_ID2 < <(login "$APPROVER_EMAIL" "Zari@12345")
[ -n "$ENC_TOKEN" ] && ok "Logged in as tb.encoder" || bad "tb.encoder login"
[ -n "$APR_TOKEN" ] && ok "Logged in as tb.approver" || bad "tb.approver login"
ENC_AUTH=(-H "Authorization: Bearer $ENC_TOKEN")
APR_AUTH=(-H "Authorization: Bearer $APR_TOKEN")

for pair in "BRANCH_ID:$BRANCH_ID" "WAREHOUSE_ID:$WAREHOUSE_ID" "SUPPLIER_ID:$SUPPLIER_ID" "BANK_ACCOUNT_ID:$BANK_ACCOUNT_ID" "TERMINAL_ID:$TERMINAL_ID" "CASH_METHOD_ID:$CASH_METHOD_ID" "WALKIN_CUSTOMER_ID:$WALKIN_CUSTOMER_ID" "AR_CUSTOMER_ID:$AR_CUSTOMER_ID" "ITEM_A_ID:$ITEM_A_ID" "ITEM_B_ID:$ITEM_B_ID" "ENCODER_ID:$ENCODER_ID" "APPROVER_ID:$APPROVER_ID"; do
  name="${pair%%:*}"; val="${pair#*:}"
  [ -n "$val" ] && ok "Resolved $name" || bad "Resolved $name (empty)"
done

DAY1="2026-09-11T09:00:00Z"
DAY2="2026-09-12T09:00:00Z"
DAY3="2026-09-13T09:00:00Z"
DAY4="2026-09-14T09:00:00Z"
DAY5="2026-09-15T09:00:00Z"
SERIAL="SN-TB-$(date +%s)"
SUPINV1="SUPINV-TB-D1-$(date +%s)"
SUPINV2="SUPINV-TB-D4-$(date +%s)"

# Running whole-cycle trial-balance accumulators (every journal this run posts gets folded in) ----
TOTAL_DR="0.0000"
TOTAL_CR="0.0000"
accumulate_journal() { # $1=journal_json -- sums every line's debitAmount/creditAmount into the totals
  local d c
  d=$(echo "$1" | grep -o '"debitAmount":[0-9.]*' | sed 's/"debitAmount"://' | awk '{s+=$1} END{printf "%.4f", s+0}')
  c=$(echo "$1" | grep -o '"creditAmount":[0-9.]*' | sed 's/"creditAmount"://' | awk '{s+=$1} END{printf "%.4f", s+0}')
  TOTAL_DR=$(add "$TOTAL_DR" "$d")
  TOTAL_CR=$(add "$TOTAL_CR" "$c")
}

echo ""
echo "--- DAY 1 ($DAY1) -- Procurement: PR -> PO -> Goods Receipt -> AP Invoice (at cost, no variance) ---"

bal_of() { $CURL "$BASE_URL/api/stock-ledger/balances" "${ADM_AUTH[@]}" | grep -o "{\"id\":\"[^}]*\"itemId\":\"$1\"[^}]*\"warehouseId\":\"$WAREHOUSE_ID\"[^}]*}"; }
BASE_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand); BASE_QTY_A=${BASE_QTY_A:-0}
BASE_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand); BASE_QTY_B=${BASE_QTY_B:-0}

QTY_A1=40; COST_A1="10.00"
QTY_B1=1;  COST_B1="500.00"

PR1_BODY=$($CURL -X POST "$BASE_URL/api/purchase-requests" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"requestDate\":\"$DAY1\",\"remarks\":\"5-day cycle Day1\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qtyRequested\":$QTY_A1,\"uomId\":\"$UOM_PCS_ID\",\"neededByDate\":null},
             {\"itemId\":\"$ITEM_B_ID\",\"qtyRequested\":$QTY_B1,\"uomId\":\"$UOM_PCS_ID\",\"neededByDate\":null}]}")
PR1_ID=$(jget "$PR1_BODY" id)
PR1_LINE_A=$(echo "$PR1_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
PR1_LINE_B=$(echo "$PR1_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_B_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$PR1_ID" ] && ok "Created PR $(jget "$PR1_BODY" requestNo)" || bad "Create PR1 -- $PR1_BODY"
$CURL -X POST "$BASE_URL/api/purchase-requests/$PR1_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
PR1_APPROVE=$($CURL -X POST "$BASE_URL/api/purchase-requests/$PR1_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "PR1 submitted+approved" "$PR1_APPROVE" '"status":"APPROVED"'

PO1_BODY=$($CURL -X POST "$BASE_URL/api/purchase-orders" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"orderDate\":\"$DAY1\",\"expectedDate\":\"$DAY1\",
  \"remarks\":\"5-day cycle Day1\",\"purchaseRequestId\":\"$PR1_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qty\":$QTY_A1,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$COST_A1,\"purchaseRequestLineId\":\"$PR1_LINE_A\"},
             {\"itemId\":\"$ITEM_B_ID\",\"qty\":$QTY_B1,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$COST_B1,\"purchaseRequestLineId\":\"$PR1_LINE_B\"}]}")
PO1_ID=$(jget "$PO1_BODY" id)
PO1_LINE_A=$(echo "$PO1_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
PO1_LINE_B=$(echo "$PO1_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_B_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$PO1_ID" ] && ok "Created PO $(jget "$PO1_BODY" poNo) from PR1" || bad "Create PO1 -- $PO1_BODY"
$CURL -X POST "$BASE_URL/api/purchase-orders/$PO1_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
PO1_APPROVE=$($CURL -X POST "$BASE_URL/api/purchase-orders/$PO1_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "PO1 submitted+approved" "$PO1_APPROVE" '"status":"POSTED"'

GR1_BODY=$($CURL -X POST "$BASE_URL/api/goods-receipt-po" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$WAREHOUSE_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"purchaseOrderId\":\"$PO1_ID\",
  \"supplierInvoiceNo\":\"$SUPINV1\",\"receiptDate\":\"$DAY1\",\"remarks\":\"5-day cycle Day1\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"batchNo\":null,\"serialNo\":null,\"qtyReceived\":$QTY_A1,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$COST_A1,\"locationId\":null,\"purchaseOrderLineId\":\"$PO1_LINE_A\"},
             {\"itemId\":\"$ITEM_B_ID\",\"batchNo\":null,\"serialNo\":\"$SERIAL\",\"qtyReceived\":$QTY_B1,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$COST_B1,\"locationId\":null,\"purchaseOrderLineId\":\"$PO1_LINE_B\"}]}")
GR1_ID=$(jget "$GR1_BODY" id)
GR1_LINE_A=$(echo "$GR1_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
GR1_LINE_B=$(echo "$GR1_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_B_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$GR1_ID" ] && ok "Created GRPO $(jget "$GR1_BODY" grpoNo) (serial $SERIAL)" || bad "Create GRPO1 -- $GR1_BODY"
$CURL -X POST "$BASE_URL/api/goods-receipt-po/$GR1_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
GR1_APPROVE=$($CURL -X POST "$BASE_URL/api/goods-receipt-po/$GR1_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "GRPO1 submitted+approved (stock posted)" "$GR1_APPROVE" '"status":"POSTED"'

AFTER_GR1_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand)
AFTER_GR1_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand)
assert_eq "Item A stock after Day1 receipt" "$(add "$BASE_QTY_A" "$QTY_A1")" "$AFTER_GR1_QTY_A"
assert_eq "Item B stock after Day1 receipt" "$(add "$BASE_QTY_B" "$QTY_B1")" "$AFTER_GR1_QTY_B"
assert_status "Serial $SERIAL is IN_STOCK after receipt" "$($CURL "$BASE_URL/api/serial-numbers" "${ADM_AUTH[@]}" | grep -o "{[^{}]*\"serialNo\":\"$SERIAL\"[^{}]*}")" '"status":"IN_STOCK"'

AP1_BODY=$($CURL -X POST "$BASE_URL/api/ap-invoices" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"invoiceType\":\"ITEM\",\"goodsReceiptPoId\":\"$GR1_ID\",
  \"supplierInvoiceNo\":\"$SUPINV1\",\"invoiceDate\":\"$DAY1\",\"dueDate\":null,\"remarks\":\"5-day cycle Day1\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qty\":$QTY_A1,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$COST_A1,\"goodsReceiptPoLineId\":\"$GR1_LINE_A\"},
             {\"itemId\":\"$ITEM_B_ID\",\"qty\":$QTY_B1,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$COST_B1,\"goodsReceiptPoLineId\":\"$GR1_LINE_B\"}],
  \"expenseLines\":[]}")
AP1_ID=$(jget "$AP1_BODY" id)
AP1_TOTAL=$(awk -v q="$QTY_A1" -v c="$COST_A1" -v qb="$QTY_B1" -v cb="$COST_B1" 'BEGIN{printf "%.4f", q*c + qb*cb}')
[ -n "$AP1_ID" ] && ok "Created AP Invoice $(jget "$AP1_BODY" invoiceNo) against GRPO1 (total $AP1_TOTAL)" || bad "Create AP1 -- $AP1_BODY"
$CURL -X POST "$BASE_URL/api/ap-invoices/$AP1_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
AP1_APPROVE=$($CURL -X POST "$BASE_URL/api/ap-invoices/$AP1_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "AP1 submitted+approved" "$AP1_APPROVE" '"status":"POSTED"'
assert_eq "AP1 amountPaid before any payment" "0" "$(jget "$AP1_APPROVE" amountPaid)"

GR1_JOURNAL=$(journal_for "GoodsReceiptPo" "$GR1_ID")
AP1_JOURNAL=$(journal_for "ApInvoice" "$AP1_ID")
[ -n "$GR1_JOURNAL" ] && ok "GRPO1 GL journal found" || bad "GRPO1 GL journal missing"
[ -n "$AP1_JOURNAL" ] && ok "AP1 GL journal found" || bad "AP1 GL journal missing"
GRNI_CR_GR1=$(sum_journal_side "$GR1_JOURNAL" "$GRNI_ID" creditAmount)
GRNI_DR_AP1=$(sum_journal_side "$AP1_JOURNAL" "$GRNI_ID" debitAmount)
assert_eq "Day1 GRNI nets to zero (GR credit = AP debit, no variance)" "$GRNI_CR_GR1" "$GRNI_DR_AP1"
accumulate_journal "$GR1_JOURNAL"; accumulate_journal "$AP1_JOURNAL"

echo ""
echo "--- DAY 2 ($DAY2) -- Selling: 2 POS checkouts + a direct AR Sales Invoice, then paying AP1 in full ---"

POS1_BODY=$($CURL -X POST "$BASE_URL/api/pos/checkout" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"posTerminalId\":\"$TERMINAL_ID\",\"customerId\":null,\"invoiceDate\":\"$DAY2\",
  \"discountPct\":null,\"costCenterId\":\"$COST_CENTER_ID\",
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qty\":10,\"uomId\":\"$UOM_PCS_ID\",\"unitPrice\":25.00,\"discountPct\":0,
    \"discountSourceType\":null,\"discountSourceId\":null,\"vatType\":\"VATABLE\",\"statutoryDiscountTypeId\":null,
    \"statutoryIdNumber\":null,\"deliveryOrderLineId\":null}],
  \"tenders\":[{\"paymentMethodId\":\"$CASH_METHOD_ID\",\"amount\":250.00,\"referenceNo\":null,\"bankOrPartnerName\":null}],
  \"createdBy\":null}")
POS1_INVOICE_ID=$(jget "$POS1_BODY" salesInvoiceId)
[ -n "$POS1_INVOICE_ID" ] && ok "POS sale #1: 10x Item A -- invoice $(jget "$POS1_BODY" invoiceNo)" || bad "POS sale #1 -- $POS1_BODY"

POS2_BODY=$($CURL -X POST "$BASE_URL/api/pos/checkout" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"posTerminalId\":\"$TERMINAL_ID\",\"customerId\":null,\"invoiceDate\":\"$DAY2\",
  \"discountPct\":null,\"costCenterId\":\"$COST_CENTER_ID\",
  \"lines\":[{\"itemId\":\"$ITEM_B_ID\",\"qty\":1,\"uomId\":\"$UOM_PCS_ID\",\"unitPrice\":750.00,\"discountPct\":0,
    \"discountSourceType\":null,\"discountSourceId\":null,\"vatType\":\"VATABLE\",\"statutoryDiscountTypeId\":null,
    \"statutoryIdNumber\":null,\"deliveryOrderLineId\":null,\"serialNo\":\"$SERIAL\"}],
  \"tenders\":[{\"paymentMethodId\":\"$CASH_METHOD_ID\",\"amount\":750.00,\"referenceNo\":null,\"bankOrPartnerName\":null}],
  \"createdBy\":null}")
POS2_INVOICE_ID=$(jget "$POS2_BODY" salesInvoiceId)
[ -n "$POS2_INVOICE_ID" ] && ok "POS sale #2: 1x serialized Item B ($SERIAL) -- invoice $(jget "$POS2_BODY" invoiceNo)" || bad "POS sale #2 -- $POS2_BODY"
assert_status "Serial $SERIAL is SOLD after POS checkout" "$($CURL "$BASE_URL/api/serial-numbers" "${ADM_AUTH[@]}" | grep -o "{[^{}]*\"serialNo\":\"$SERIAL\"[^{}]*}")" '"status":"SOLD"'

AFTER_POS_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand)
AFTER_POS_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand)
assert_eq "Item A stock after POS sale" "$(add "$AFTER_GR1_QTY_A" -10)" "$AFTER_POS_QTY_A"
assert_eq "Item B stock after POS sale" "$(add "$AFTER_GR1_QTY_B" -1)" "$AFTER_POS_QTY_B"
POS1_JOURNAL=$(journal_for "SalesInvoice" "$POS1_INVOICE_ID")
POS2_JOURNAL=$(journal_for "SalesInvoice" "$POS2_INVOICE_ID")
accumulate_journal "$POS1_JOURNAL"; accumulate_journal "$POS2_JOURNAL"

# Direct (non-POS) AR Sales Invoice to the regular customer -- no delivery order, no stock effect by
# design (SalesInvoice.cs's own documented invariant) -- purely to put something real through the
# Cash Receipts Book via a genuine Customer Payment collection below.
SINV_BODY=$($CURL -X POST "$BASE_URL/api/sales-invoices" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"customerId\":\"$AR_CUSTOMER_ID\",\"deliveryOrderId\":null,\"invoiceDate\":\"$DAY2\",
  \"dueDate\":null,\"remarks\":\"5-day cycle Day2 AR sale\",\"discountPct\":null,\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qty\":5,\"uomId\":\"$UOM_PCS_ID\",\"unitPrice\":25.00,\"discountPct\":0,
    \"discountSourceType\":null,\"discountSourceId\":null,\"vatType\":\"VATABLE\",\"statutoryDiscountTypeId\":null,
    \"statutoryIdNumber\":null,\"deliveryOrderLineId\":null}]}")
SINV_ID=$(jget "$SINV_BODY" id)
[ -n "$SINV_ID" ] && ok "Created direct AR Sales Invoice $(jget "$SINV_BODY" invoiceNo) (125.00)" || bad "Create AR Sales Invoice -- $SINV_BODY"
# Company.SalesInvoiceQuickPostEnabled is on in this dev DB and there's no discount to breach the
# unapproved-discount threshold, so CreateSalesInvoiceCommandHandler posts straight to POSTED right
# here -- no submit/approve step exists to call, exactly like Sales Return below.
assert_status "AR Sales Invoice quick-posted at creation" "$SINV_BODY" '"status":"POSTED"'
SINV_JOURNAL=$(journal_for "SalesInvoice" "$SINV_ID")
accumulate_journal "$SINV_JOURNAL"

CP_BODY=$($CURL -X POST "$BASE_URL/api/customer-payments" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"customerId\":\"$AR_CUSTOMER_ID\",\"paymentDate\":\"$DAY2\",\"remarks\":\"5-day cycle Day2 collection\",
  \"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,\"lines\":[{\"salesInvoiceId\":\"$SINV_ID\",\"amountApplied\":125.00}],
  \"paymentMethod\":\"CASH\",\"cashAccountId\":\"$CASH_ON_HAND_ID\",\"referenceNo\":null}")
CP_ID=$(jget "$CP_BODY" id)
[ -n "$CP_ID" ] && ok "Created Customer Payment collecting the AR Sales Invoice in full" || bad "Create Customer Payment -- $CP_BODY"
# Company.CustomerPaymentQuickPostEnabled is unconditionally on -- posts straight to POSTED here too.
assert_status "Customer Payment quick-posted at creation" "$CP_BODY" '"status":"POSTED"'
CP_JOURNAL=$(journal_for "CustomerPayment" "$CP_ID")
accumulate_journal "$CP_JOURNAL"

OP1_BODY=$($CURL -X POST "$BASE_URL/api/outgoing-payments" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"bankAccountId\":\"$BANK_ACCOUNT_ID\",\"paymentDate\":\"$DAY2\",
  \"refNo\":\"TB-PMT-1\",\"remarks\":\"5-day cycle Day2 -- pay AP1 in full\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"apInvoiceId\":\"$AP1_ID\",\"amount\":$AP1_TOTAL}]}")
OP1_ID=$(jget "$OP1_BODY" id)
[ -n "$OP1_ID" ] && ok "Created Outgoing Payment for AP1's full balance ($AP1_TOTAL)" || bad "Create OP1 -- $OP1_BODY"
$CURL -X POST "$BASE_URL/api/outgoing-payments/$OP1_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
OP1_APPROVE=$($CURL -X POST "$BASE_URL/api/outgoing-payments/$OP1_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "OP1 submitted+approved" "$OP1_APPROVE" '"status":"POSTED"'
AP1_AFTER=$($CURL "$BASE_URL/api/ap-invoices/$AP1_ID" "${ADM_AUTH[@]}")
assert_eq "AP1 amountPaid after full payment" "$AP1_TOTAL" "$(jget "$AP1_AFTER" amountPaid)"
assert_status "AP1 status after full payment" "$AP1_AFTER" '"status":"PAID"'
OP1_JOURNAL=$(journal_for "OutgoingPayment" "$OP1_ID")
accumulate_journal "$OP1_JOURNAL"

echo ""
echo "--- DAY 3 ($DAY3) -- Returns + day close ---"

RET1_BODY=$($CURL -X POST "$BASE_URL/api/sales-returns" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$WAREHOUSE_ID\",\"customerId\":\"$WALKIN_CUSTOMER_ID\",\"deliveryOrderId\":null,\"returnDate\":\"$DAY3\",
  \"remarks\":\"5-day cycle Day3 return\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qtyReturned\":2,\"uomId\":\"$UOM_PCS_ID\",\"unitPrice\":25.00,\"deliveryOrderLineId\":null,\"vatType\":\"VATABLE\"}]}")
assert_status "Return 2x Item A from POS sale #1" "$RET1_BODY" '"status":"POSTED"'
RET1_ID=$(jget "$RET1_BODY" id)
accumulate_journal "$(journal_for "SalesReturn" "$RET1_ID")"

RET2_BODY=$($CURL -X POST "$BASE_URL/api/sales-returns" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$WAREHOUSE_ID\",\"customerId\":\"$WALKIN_CUSTOMER_ID\",\"deliveryOrderId\":null,\"returnDate\":\"$DAY3\",
  \"remarks\":\"5-day cycle Day3 return\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_B_ID\",\"qtyReturned\":1,\"uomId\":\"$UOM_PCS_ID\",\"unitPrice\":750.00,\"deliveryOrderLineId\":null,\"vatType\":\"VATABLE\",\"serialNo\":\"$SERIAL\"}]}")
assert_status "Return the serialized Item B unit" "$RET2_BODY" '"status":"POSTED"'
RET2_ID=$(jget "$RET2_BODY" id)
accumulate_journal "$(journal_for "SalesReturn" "$RET2_ID")"
assert_status "Serial $SERIAL is back IN_STOCK after return" "$($CURL "$BASE_URL/api/serial-numbers" "${ADM_AUTH[@]}" | grep -o "{[^{}]*\"serialNo\":\"$SERIAL\"[^{}]*}")" '"status":"IN_STOCK"'

AFTER_RET_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand)
AFTER_RET_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand)
assert_eq "Item A stock after return" "$(add "$AFTER_POS_QTY_A" 2)" "$AFTER_RET_QTY_A"
assert_eq "Item B stock after return" "$(add "$AFTER_POS_QTY_B" 1)" "$AFTER_RET_QTY_B"

XREAD=$($CURL "$BASE_URL/api/pos-closing/x-reading?branchId=$BRANCH_ID" "${ADM_AUTH[@]}")
[ -n "$XREAD" ] && ok "X-Reading ran" || bad "X-Reading -- $XREAD"
ZREAD=$($CURL -X POST "$BASE_URL/api/pos-closing/z-reading" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{\"branchId\":\"$BRANCH_ID\",\"runBy\":\"$APPROVER_ID\"}")
[ -n "$(jget "$ZREAD" id)" ] && ok "Z-Reading closed Day3 -- Z-Counter $(jget "$ZREAD" zCounterValue)" || bad "Z-Reading -- $ZREAD"

echo ""
echo "--- DAY 4 ($DAY4) -- A second procurement cycle: Purchase Price Variance + a 2-installment partial payment ---"

QTY_A2=20; PO_COST_A2="10.00"; INVOICE_COST_A2="11.50"

PR2_BODY=$($CURL -X POST "$BASE_URL/api/purchase-requests" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"requestDate\":\"$DAY4\",\"remarks\":\"5-day cycle Day4 (variance)\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qtyRequested\":$QTY_A2,\"uomId\":\"$UOM_PCS_ID\",\"neededByDate\":null}]}")
PR2_ID=$(jget "$PR2_BODY" id)
PR2_LINE_A=$(echo "$PR2_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$PR2_ID" ] && ok "Created PR2 $(jget "$PR2_BODY" requestNo)" || bad "Create PR2 -- $PR2_BODY"
$CURL -X POST "$BASE_URL/api/purchase-requests/$PR2_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
PR2_APPROVE=$($CURL -X POST "$BASE_URL/api/purchase-requests/$PR2_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "PR2 submitted+approved" "$PR2_APPROVE" '"status":"APPROVED"'

PO2_BODY=$($CURL -X POST "$BASE_URL/api/purchase-orders" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"orderDate\":\"$DAY4\",\"expectedDate\":\"$DAY4\",
  \"remarks\":\"5-day cycle Day4 (variance)\",\"purchaseRequestId\":\"$PR2_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qty\":$QTY_A2,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$PO_COST_A2,\"purchaseRequestLineId\":\"$PR2_LINE_A\"}]}")
PO2_ID=$(jget "$PO2_BODY" id)
PO2_LINE_A=$(echo "$PO2_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$PO2_ID" ] && ok "Created PO2 $(jget "$PO2_BODY" poNo) at $PO_COST_A2/unit" || bad "Create PO2 -- $PO2_BODY"
$CURL -X POST "$BASE_URL/api/purchase-orders/$PO2_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
PO2_APPROVE=$($CURL -X POST "$BASE_URL/api/purchase-orders/$PO2_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "PO2 submitted+approved" "$PO2_APPROVE" '"status":"POSTED"'

GR2_BODY=$($CURL -X POST "$BASE_URL/api/goods-receipt-po" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$WAREHOUSE_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"purchaseOrderId\":\"$PO2_ID\",
  \"supplierInvoiceNo\":\"$SUPINV2\",\"receiptDate\":\"$DAY4\",\"remarks\":\"5-day cycle Day4\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"batchNo\":null,\"serialNo\":null,\"qtyReceived\":$QTY_A2,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$PO_COST_A2,\"locationId\":null,\"purchaseOrderLineId\":\"$PO2_LINE_A\"}]}")
GR2_ID=$(jget "$GR2_BODY" id)
GR2_LINE_A=$(echo "$GR2_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$GR2_ID" ] && ok "Created GRPO2 $(jget "$GR2_BODY" grpoNo), received at $PO_COST_A2/unit (matches PO)" || bad "Create GRPO2 -- $GR2_BODY"
$CURL -X POST "$BASE_URL/api/goods-receipt-po/$GR2_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
GR2_APPROVE=$($CURL -X POST "$BASE_URL/api/goods-receipt-po/$GR2_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "GRPO2 submitted+approved" "$GR2_APPROVE" '"status":"POSTED"'
AFTER_GR2_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand)
assert_eq "Item A stock after Day4 receipt" "$(add "$AFTER_RET_QTY_A" "$QTY_A2")" "$AFTER_GR2_QTY_A"

AP2_BODY=$($CURL -X POST "$BASE_URL/api/ap-invoices" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"invoiceType\":\"ITEM\",\"goodsReceiptPoId\":\"$GR2_ID\",
  \"supplierInvoiceNo\":\"$SUPINV2\",\"invoiceDate\":\"$DAY4\",\"dueDate\":null,\"remarks\":\"5-day cycle Day4 -- billed above receipt cost\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qty\":$QTY_A2,\"uomId\":\"$UOM_PCS_ID\",\"unitCost\":$INVOICE_COST_A2,\"goodsReceiptPoLineId\":\"$GR2_LINE_A\"}],
  \"expenseLines\":[]}")
AP2_ID=$(jget "$AP2_BODY" id)
AP2_TOTAL=$(mul "$QTY_A2" "$INVOICE_COST_A2")
RECEIVED_VALUE2=$(mul "$QTY_A2" "$PO_COST_A2")
EXPECTED_VARIANCE=$(sub "$AP2_TOTAL" "$RECEIVED_VALUE2")
[ -n "$AP2_ID" ] && ok "Created AP Invoice $(jget "$AP2_BODY" invoiceNo) at $INVOICE_COST_A2/unit (total $AP2_TOTAL, vs. received value $RECEIVED_VALUE2)" || bad "Create AP2 -- $AP2_BODY"
$CURL -X POST "$BASE_URL/api/ap-invoices/$AP2_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
AP2_APPROVE=$($CURL -X POST "$BASE_URL/api/ap-invoices/$AP2_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "AP2 submitted+approved (unfavorable price variance)" "$AP2_APPROVE" '"status":"POSTED"'

AP2_JOURNAL=$(journal_for "ApInvoice" "$AP2_ID")
GR2_JOURNAL=$(journal_for "GoodsReceiptPo" "$GR2_ID")
GRNI_CR_GR2=$(sum_journal_side "$GR2_JOURNAL" "$GRNI_ID" creditAmount)
GRNI_DR_AP2=$(sum_journal_side "$AP2_JOURNAL" "$GRNI_ID" debitAmount)
assert_eq "Day4 GRNI still nets to zero (variance is isolated to PPV, not GRNI)" "$GRNI_CR_GR2" "$GRNI_DR_AP2"
assert_eq "GRNI cleared at the GRPO's own received value" "$RECEIVED_VALUE2" "$GRNI_DR_AP2"
PPV_DR_AP2=$(sum_journal_side "$AP2_JOURNAL" "$PPV_ID" debitAmount)
assert_eq "Purchase Price Variance posted (unfavorable, billed above receipt cost)" "$EXPECTED_VARIANCE" "$PPV_DR_AP2"
accumulate_journal "$GR2_JOURNAL"; accumulate_journal "$AP2_JOURNAL"

# Installment 1: a genuine partial payment (well under the balance) ------------------------------
PARTIAL_AMT="100.00"
OP2_BODY=$($CURL -X POST "$BASE_URL/api/outgoing-payments" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"bankAccountId\":\"$BANK_ACCOUNT_ID\",\"paymentDate\":\"$DAY4\",
  \"refNo\":\"TB-PMT-2\",\"remarks\":\"5-day cycle Day4 -- partial payment 1 of 2\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"apInvoiceId\":\"$AP2_ID\",\"amount\":$PARTIAL_AMT}]}")
OP2_ID=$(jget "$OP2_BODY" id)
[ -n "$OP2_ID" ] && ok "Created partial Outgoing Payment #1 ($PARTIAL_AMT of $AP2_TOTAL)" || bad "Create OP2 -- $OP2_BODY"
$CURL -X POST "$BASE_URL/api/outgoing-payments/$OP2_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
OP2_APPROVE=$($CURL -X POST "$BASE_URL/api/outgoing-payments/$OP2_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "OP2 (partial) submitted+approved" "$OP2_APPROVE" '"status":"POSTED"'
AP2_AFTER_PARTIAL=$($CURL "$BASE_URL/api/ap-invoices/$AP2_ID" "${ADM_AUTH[@]}")
assert_eq "AP2 amountPaid after partial payment" "$PARTIAL_AMT" "$(jget "$AP2_AFTER_PARTIAL" amountPaid)"
assert_not "AP2 status is NOT yet PAID (partially paid only)" "$AP2_AFTER_PARTIAL" '"status":"PAID"'
accumulate_journal "$(journal_for "OutgoingPayment" "$OP2_ID")"

# Installment 2: pay the remaining balance, closing AP2 out fully ---------------------------------
REMAINING_AMT=$(sub "$AP2_TOTAL" "$PARTIAL_AMT")
OP3_BODY=$($CURL -X POST "$BASE_URL/api/outgoing-payments" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"bankAccountId\":\"$BANK_ACCOUNT_ID\",\"paymentDate\":\"$DAY4\",
  \"refNo\":\"TB-PMT-3\",\"remarks\":\"5-day cycle Day4 -- partial payment 2 of 2 (closes AP2)\",\"costCenterId\":\"$COST_CENTER_ID\",\"createdBy\":null,
  \"lines\":[{\"apInvoiceId\":\"$AP2_ID\",\"amount\":$REMAINING_AMT}]}")
OP3_ID=$(jget "$OP3_BODY" id)
[ -n "$OP3_ID" ] && ok "Created Outgoing Payment #2 for the remaining balance ($REMAINING_AMT)" || bad "Create OP3 -- $OP3_BODY"
$CURL -X POST "$BASE_URL/api/outgoing-payments/$OP3_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
OP3_APPROVE=$($CURL -X POST "$BASE_URL/api/outgoing-payments/$OP3_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "OP3 submitted+approved" "$OP3_APPROVE" '"status":"POSTED"'
AP2_AFTER_FULL=$($CURL "$BASE_URL/api/ap-invoices/$AP2_ID" "${ADM_AUTH[@]}")
assert_eq "AP2 amountPaid after both installments" "$AP2_TOTAL" "$(jget "$AP2_AFTER_FULL" amountPaid)"
assert_status "AP2 status is PAID once both installments landed" "$AP2_AFTER_FULL" '"status":"PAID"'
accumulate_journal "$(journal_for "OutgoingPayment" "$OP3_ID")"

echo ""
echo "--- DAY 5 ($DAY5) -- Manual Journal Entry, period-close enforcement, and the BIR books / reports sweep ---"

MJE_BODY=$($CURL -X POST "$BASE_URL/api/manual-journal-entries" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"entryDate\":\"$DAY5\",\"remarks\":\"September utilities accrual -- 5-day cycle test\",\"createdBy\":null,
  \"lines\":[{\"glAccountId\":\"$UTIL_EXP_ID\",\"costCenterId\":\"$COST_CENTER_ID\",\"memo\":\"Sept utilities\",\"debitAmount\":3000.00,\"creditAmount\":0},
             {\"glAccountId\":\"$ACCR_PAY_ID\",\"costCenterId\":\"$COST_CENTER_ID\",\"memo\":null,\"debitAmount\":0,\"creditAmount\":3000.00}]}")
MJE_ID=$(jget "$MJE_BODY" id)
[ -n "$MJE_ID" ] && ok "Created Manual Journal Entry (Dr Utilities Expense 3000 / Cr Accrued Expenses Payable 3000)" || bad "Create MJE -- $MJE_BODY"
$CURL -X POST "$BASE_URL/api/manual-journal-entries/$MJE_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
MJE_APPROVE=$($CURL -X POST "$BASE_URL/api/manual-journal-entries/$MJE_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "MJE submitted+approved (posted to GL)" "$MJE_APPROVE" '"status":"POSTED"'
MJE_JOURNAL=$(journal_for "ManualJournalEntry" "$MJE_ID")
[ -n "$MJE_JOURNAL" ] && ok "MJE GL journal found" || bad "MJE GL journal missing"
accumulate_journal "$MJE_JOURNAL"

# Period-close enforcement -- a throwaway, already-past FiscalYear that never overlaps the dates
# this script actually transacts on, so closing it has zero effect on this or any future re-run. ---
CLOSED_FY_LIST=$($CURL "$BASE_URL/api/fiscal-years" "${ADM_AUTH[@]}")
CLOSED_FY_ID=$(echo "$CLOSED_FY_LIST" | grep -o "{[^{}]*\"yearName\":\"FY 2020-01 (closed-period test)\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$CLOSED_FY_ID" ]; then
  CLOSED_FY_BODY=$($CURL -X POST "$BASE_URL/api/fiscal-years" "${ADM_AUTH[@]}" "${JSON[@]}" -d '{
    "yearName":"FY 2020-01 (closed-period test)","startDate":"2020-01-01T00:00:00Z","endDate":"2020-01-31T23:59:59Z","status":"OPEN"}')
  CLOSED_FY_ID=$(jget "$CLOSED_FY_BODY" id)
fi
[ -n "$CLOSED_FY_ID" ] && ok "Throwaway FiscalYear ready (Jan 2020, isolated from this run's real dates)" || bad "Create throwaway FiscalYear"
$CURL -X PUT "$BASE_URL/api/fiscal-years/$CLOSED_FY_ID" "${ADM_AUTH[@]}" "${JSON[@]}" -d "{\"id\":\"$CLOSED_FY_ID\",\"yearName\":\"FY 2020-01 (closed-period test)\",\"startDate\":\"2020-01-01T00:00:00Z\",\"endDate\":\"2020-01-31T23:59:59Z\",\"status\":\"CLOSED\"}" > /dev/null
BLOCKED_MJE_BODY=$($CURL -X POST "$BASE_URL/api/manual-journal-entries" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{
  \"branchId\":\"$BRANCH_ID\",\"entryDate\":\"2020-01-15T00:00:00Z\",\"remarks\":\"Should be blocked by period close\",\"createdBy\":null,
  \"lines\":[{\"glAccountId\":\"$UTIL_EXP_ID\",\"costCenterId\":null,\"memo\":null,\"debitAmount\":10,\"creditAmount\":0},
             {\"glAccountId\":\"$ACCR_PAY_ID\",\"costCenterId\":null,\"memo\":null,\"debitAmount\":0,\"creditAmount\":10}]}")
BLOCKED_MJE_ID=$(jget "$BLOCKED_MJE_BODY" id)
$CURL -X POST "$BASE_URL/api/manual-journal-entries/$BLOCKED_MJE_ID/submit" "${ENC_AUTH[@]}" "${JSON[@]}" -d "{\"requestedBy\":\"$ENCODER_ID\"}" > /dev/null
BLOCKED_APPROVE=$($CURL -X POST "$BASE_URL/api/manual-journal-entries/$BLOCKED_MJE_ID/approve" "${APR_AUTH[@]}" "${JSON[@]}" -d "{\"approverUserId\":\"$APPROVER_ID\",\"comments\":null}")
assert_status "Posting into the CLOSED Jan-2020 period is rejected" "$BLOCKED_APPROVE" 'PeriodClosed'

echo ""
echo "--- BIR books of accounts / financial reports sweep (this run's own documents) ---"

PB_LIST=$($CURL "$BASE_URL/api/ap-invoices" "${ADM_AUTH[@]}")
assert_status "Purchase Book (GET /api/ap-invoices) contains AP1" "$PB_LIST" "\"id\":\"$AP1_ID\""
assert_status "Purchase Book contains AP2" "$PB_LIST" "\"id\":\"$AP2_ID\""

SB_LIST=$($CURL "$BASE_URL/api/sales-invoices" "${ADM_AUTH[@]}")
assert_status "Sales Book (GET /api/sales-invoices) contains POS sale #1" "$SB_LIST" "\"id\":\"$POS1_INVOICE_ID\""
assert_status "Sales Book contains the direct AR Sales Invoice" "$SB_LIST" "\"id\":\"$SINV_ID\""

CRB_LIST=$($CURL "$BASE_URL/api/customer-payments" "${ADM_AUTH[@]}")
assert_status "Cash Receipts Book (GET /api/customer-payments) contains the AR collection" "$CRB_LIST" "\"id\":\"$CP_ID\""

CDB_LIST=$($CURL "$BASE_URL/api/outgoing-payments" "${ADM_AUTH[@]}")
assert_status "Cash Disbursements Book (GET /api/outgoing-payments) contains OP1" "$CDB_LIST" "\"id\":\"$OP1_ID\""
assert_status "Cash Disbursements Book contains both Day4 installments" "$CDB_LIST" "\"id\":\"$OP2_ID\""

SC_A=$($CURL "$BASE_URL/api/stock-ledger/entries?itemId=$ITEM_A_ID&warehouseId=$WAREHOUSE_ID" "${ADM_AUTH[@]}")
SC_A_COUNT=$(echo "$SC_A" | grep -o '"id":"[^"]*"' | wc -l | tr -d ' ')
[ "$SC_A_COUNT" -ge 4 ] && ok "Stock Card for Item A has movements from all 4 stock-affecting days ($SC_A_COUNT entries)" || bad "Stock Card for Item A -- only $SC_A_COUNT entries, expected >= 4"

AIL=$($CURL "$BASE_URL/api/stock-ledger/as-of?date=${DAY5}&branchId=$BRANCH_ID" "${ADM_AUTH[@]}")
AIL_QTY_A=$(echo "$AIL" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"qtyOnHand":[0-9.]*' | head -1 | sed 's/"qtyOnHand"://')
assert_eq "Annual Inventory List (as-of Day5) matches live Item A balance" "$AFTER_GR2_QTY_A" "$AIL_QTY_A"

# GRNI Reconciliation: the account should now be back to zero from this run's own postings (both
# GRPO/AP pairs cleared it exactly, variance or not) -- sum every journal this run touched.
GRNI_NET=$(awk -v a="$GRNI_CR_GR1" -v b="$GRNI_DR_AP1" -v c="$GRNI_CR_GR2" -v d="$GRNI_DR_AP2" 'BEGIN{printf "%.4f", (a-b)+(c-d)}')
assert_eq "GRNI Reconciliation: this run's GRPOs and AP Invoices net to zero on account 2100" "0" "$GRNI_NET"

# AP Aging: both AP invoices this run created should now show zero outstanding balance.
AP1_BAL=$(sub "$AP1_TOTAL" "$(jget "$AP1_AFTER" amountPaid)")
AP2_BAL=$(sub "$AP2_TOTAL" "$(jget "$AP2_AFTER_FULL" amountPaid)")
assert_eq "AP Aging: AP1 outstanding balance" "0" "$AP1_BAL"
assert_eq "AP Aging: AP2 outstanding balance" "0" "$AP2_BAL"

# Trial Balance tie-out: every journal line this run posted, total debit must equal total credit.
assert_eq "Whole 5-day cycle trial balance ties out (total debit = total credit)" "$TOTAL_DR" "$TOTAL_CR"

echo ""
echo "=== RESULT: $PASS passed, $FAIL failed ==="
[ "$FAIL" -eq 0 ] && echo "Full 5-day cycle (master data -> Day1-5) tied out cleanly." || echo "See [FAIL] lines above."
exit $([ "$FAIL" -eq 0 ] && echo 0 || echo 1)
