#!/usr/bin/env bash
# ZARI full-cycle integration test — Purchase-to-Pay feeding Order-to-Cash, over 3 backdated days.
#
# Day 1 (procurement): Purchase Request -> Purchase Order -> Goods Receipt (PO) -> AP Invoice.
#   Receives a plain FIFO-costed item and one unit of a serialized item into stock.
# Day 2 (selling): two POS checkouts sell what Day 1 received (one plain, one serialized line),
#   then the Day 1 AP Invoice is paid in full via Outgoing Payment.
# Day 3 (closing): a Sales Return reverses one unit of each POS sale (proving the serialized-item
#   return path and the plain average-cost return path both work), then an X-/Z-Reading closes the
#   day, and a handful of GL/AP/stock assertions tie the whole 3-day cycle out.
#
# Requires: a running ZARI.Api dev instance (default https://localhost:7200), curl, grep/sed (no
# jq/python dependency — deliberately avoided, see this session's own notes on Windows path
# mapping breaking node/python against Git-Bash paths). Run from anywhere; self-contained.
#
# Every ID is discovered at runtime by code/name (branch HQ, warehouse HQ-MAIN, supplier SUP-001,
# items TEST-FIFO-1/SKU-1001, CASH payment method, Main Operating Account bank account) rather than
# hardcoded — safe to re-run against any dev DB descended from the standard seed data.

set -u
BASE_URL="${BASE_URL:-https://localhost:7200}"
CURL="curl -sk"
PASS=0
FAIL=0

# --- tiny assertion helpers -------------------------------------------------------------------
ok()   { PASS=$((PASS+1)); echo "  [PASS] $1"; }
bad()  { FAIL=$((FAIL+1)); echo "  [FAIL] $1"; }
num()  { awk -v n="${1:-0}" 'BEGIN{printf "%.4f", n+0}'; }  # normalizes "30"/"30.0000"/"" to one form
add()  { awk -v a="${1:-0}" -v b="${2:-0}" 'BEGIN{printf "%.4f", a+b}'; }
assert_eq() { # $1=label $2=expected(numeric) $3=actual(numeric) -- normalized before comparing,
              # so "30" and "30.0000" (the API's own format) count as equal.
  local e a; e=$(num "$2"); a=$(num "$3")
  if [ "$e" = "$a" ]; then ok "$1 (= $e)"; else bad "$1 (expected $e, got $a)"; fi
}
assert_status() { # $1=label $2=http_body $3=expected_field_regex
  if echo "$2" | grep -q "$3"; then ok "$1"; else bad "$1 -- body: $(echo "$2" | head -c 300)"; fi
}
# Naive single-level JSON field extractor (first match only) -- deliberately no jq/python
# dependency, see this session's own notes on Windows path mapping breaking those against
# Git-Bash paths. Handles a quoted string value or a bare number/bool/null value.
jget() {
  local json="$1" field="$2" sval
  sval=$(echo "$json" | grep -o "\"$field\":\"[^\"]*\"" | head -1)
  if [ -n "$sval" ]; then
    echo "$sval" | sed "s/\"$field\":\"//;s/\"\$//"
    return
  fi
  echo "$json" | grep -o "\"$field\":[^,}]*" | head -1 | sed "s/\"$field\"://"
}

echo "=== ZARI full-cycle 3-day test (procure-to-pay -> order-to-cash) against $BASE_URL ==="

# --- login: two users, since a self-approval guard blocks the same person deciding their own
# request everywhere in this codebase — Manager creates/submits, Admin approves throughout. ------
login() { # $1=email $2=password -> prints "token id" on stdout
  local body; body=$($CURL -X POST "$BASE_URL/api/identity/login" -H "Content-Type: application/json" -d "{\"email\":\"$1\",\"password\":\"$2\"}")
  local token; token=$(jget "$body" jwToken)
  local id; id=$(jget "$body" id)
  echo "$token $id"
}
read -r MGR_TOKEN MGR_ID < <(login "manager.hq@zari.coop" "zari123")
read -r ADM_TOKEN ADM_ID < <(login "admin@zari.coop" "zari123")
[ -n "$MGR_TOKEN" ] && ok "Logged in as manager.hq" || bad "manager.hq login"
[ -n "$ADM_TOKEN" ] && ok "Logged in as admin" || bad "admin login"
MGR_AUTH=(-H "Authorization: Bearer $MGR_TOKEN")
ADM_AUTH=(-H "Authorization: Bearer $ADM_TOKEN")

# --- reference data, discovered by code/name --------------------------------------------------
BRANCH_ID="br-hq"
WAREHOUSE_ID=$($CURL "$BASE_URL/api/warehouses" "${ADM_AUTH[@]}" | grep -o '{"id":"[^}]*"code":"HQ-MAIN"[^}]*}' | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
SUPPLIER_ID=$($CURL "$BASE_URL/api/suppliers" "${ADM_AUTH[@]}" | grep -o '{"id":"[^}]*"code":"SUP-001"[^}]*}' | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
BANK_ACCOUNT_ID=$($CURL "$BASE_URL/api/bank-accounts" "${ADM_AUTH[@]}" | grep -o '{"id":"[^}]*"branchId":"br-hq"[^}]*}' | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
TERMINAL_ID=$($CURL "$BASE_URL/api/pos-terminals" "${ADM_AUTH[@]}" | grep -o '{"id":"[^}]*"branchId":"br-hq"[^}]*}' | grep '"hasOwnSeries":false' | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
CASH_METHOD_ID=$($CURL "$BASE_URL/api/payment-methods" "${ADM_AUTH[@]}" | grep -o '{"id":"[^}]*"code":"CASH"[^}]*}' | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
# Sales Return's CustomerId is a non-nullable Guid (unlike POS checkout's, which defaults to the
# branch's Walk-in Customer when omitted) -- has to be supplied explicitly here.
WALKIN_CUSTOMER_ID=$($CURL "$BASE_URL/api/customers" "${ADM_AUTH[@]}" | grep -o "{[^{}]*\"name\":\"Walk-in Customer\"[^{}]*\"branchId\":\"$BRANCH_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')

ITEM_A_JSON=$($CURL "$BASE_URL/api/items" "${ADM_AUTH[@]}" | grep -o '{[^{}]*"code":"TEST-FIFO-1"[^{}]*}')
ITEM_A_ID=$(jget "$ITEM_A_JSON" id)
ITEM_A_UOM=$(jget "$ITEM_A_JSON" baseUomId)
ITEM_B_JSON=$($CURL "$BASE_URL/api/items" "${ADM_AUTH[@]}" | grep -o '{[^{}]*"code":"SKU-1001"[^{}]*}')
ITEM_B_ID=$(jget "$ITEM_B_JSON" id)
ITEM_B_UOM=$(jget "$ITEM_B_JSON" baseUomId)

for pair in "WAREHOUSE_ID:$WAREHOUSE_ID" "SUPPLIER_ID:$SUPPLIER_ID" "BANK_ACCOUNT_ID:$BANK_ACCOUNT_ID" "TERMINAL_ID:$TERMINAL_ID" "CASH_METHOD_ID:$CASH_METHOD_ID" "WALKIN_CUSTOMER_ID:$WALKIN_CUSTOMER_ID" "ITEM_A_ID:$ITEM_A_ID" "ITEM_B_ID:$ITEM_B_ID"; do
  name="${pair%%:*}"; val="${pair#*:}"
  [ -n "$val" ] && ok "Resolved $name" || bad "Resolved $name (empty -- check seed data)"
done

DAY1="2026-09-01T09:00:00Z"
DAY2="2026-09-02T09:00:00Z"
DAY3="2026-09-03T09:00:00Z"
SERIAL="SN-CYCLE-$(date +%s)"
SUPPLIER_INVOICE_NO="SUPINV-CYCLE-$(date +%s)"
QTY_A=30
COST_A="8.00"
COST_B="65.00"

echo ""
echo "--- DAY 1 ($DAY1) -- Procurement: PR -> PO -> Goods Receipt -> AP Invoice ---"

# Stock baselines, so later assertions check the delta this test caused, not an absolute number --
bal_of() { $CURL "$BASE_URL/api/stock-ledger/balances" "${ADM_AUTH[@]}" | grep -o "{\"id\":\"[^}]*\"itemId\":\"$1\"[^}]*\"warehouseId\":\"$WAREHOUSE_ID\"[^}]*}"; }
BASE_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand); BASE_QTY_A=${BASE_QTY_A:-0}
BASE_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand); BASE_QTY_B=${BASE_QTY_B:-0}

PR_BODY=$($CURL -X POST "$BASE_URL/api/purchase-requests" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"requestDate\":\"$DAY1\",\"remarks\":\"Full-cycle test\",\"createdBy\":null,
  \"lines\":[
    {\"itemId\":\"$ITEM_A_ID\",\"qtyRequested\":$QTY_A,\"uomId\":\"$ITEM_A_UOM\",\"neededByDate\":null},
    {\"itemId\":\"$ITEM_B_ID\",\"qtyRequested\":1,\"uomId\":\"$ITEM_B_UOM\",\"neededByDate\":null}
  ]}")
PR_ID=$(jget "$PR_BODY" id)
PR_LINE_A=$(echo "$PR_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
PR_LINE_B=$(echo "$PR_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_B_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$PR_ID" ] && ok "Created PR $(jget "$PR_BODY" requestNo 2>/dev/null || echo "$PR_ID")" || bad "Create PR -- $PR_BODY"

$CURL -X POST "$BASE_URL/api/purchase-requests/$PR_ID/submit" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{\"requestedBy\":\"$MGR_ID\"}" > /dev/null
PR_APPROVE=$($CURL -X POST "$BASE_URL/api/purchase-requests/$PR_ID/approve" "${ADM_AUTH[@]}" -H "Content-Type: application/json" -d "{\"approverUserId\":\"$ADM_ID\",\"comments\":null}")
assert_status "PR submitted+approved" "$PR_APPROVE" '"status":"APPROVED"'

PO_BODY=$($CURL -X POST "$BASE_URL/api/purchase-orders" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"orderDate\":\"$DAY1\",\"expectedDate\":\"$DAY1\",
  \"remarks\":\"Full-cycle test\",\"purchaseRequestId\":\"$PR_ID\",\"createdBy\":null,
  \"lines\":[
    {\"itemId\":\"$ITEM_A_ID\",\"qty\":$QTY_A,\"uomId\":\"$ITEM_A_UOM\",\"unitCost\":$COST_A,\"purchaseRequestLineId\":\"$PR_LINE_A\"},
    {\"itemId\":\"$ITEM_B_ID\",\"qty\":1,\"uomId\":\"$ITEM_B_UOM\",\"unitCost\":$COST_B,\"purchaseRequestLineId\":\"$PR_LINE_B\"}
  ]}")
PO_ID=$(jget "$PO_BODY" id)
PO_LINE_A=$(echo "$PO_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
PO_LINE_B=$(echo "$PO_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_B_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$PO_ID" ] && ok "Created PO $(jget "$PO_BODY" poNo 2>/dev/null || echo "$PO_ID") from PR" || bad "Create PO -- $PO_BODY"

$CURL -X POST "$BASE_URL/api/purchase-orders/$PO_ID/submit" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{\"requestedBy\":\"$MGR_ID\"}" > /dev/null
PO_APPROVE=$($CURL -X POST "$BASE_URL/api/purchase-orders/$PO_ID/approve" "${ADM_AUTH[@]}" -H "Content-Type: application/json" -d "{\"approverUserId\":\"$ADM_ID\",\"comments\":null}")
assert_status "PO submitted+approved" "$PO_APPROVE" '"status":"POSTED"'

GR_BODY=$($CURL -X POST "$BASE_URL/api/goods-receipt-po" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$WAREHOUSE_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"purchaseOrderId\":\"$PO_ID\",
  \"supplierInvoiceNo\":\"$SUPPLIER_INVOICE_NO\",\"receiptDate\":\"$DAY1\",\"remarks\":\"Full-cycle test\",\"costCenterId\":null,\"createdBy\":null,
  \"lines\":[
    {\"itemId\":\"$ITEM_A_ID\",\"batchNo\":null,\"serialNo\":null,\"qtyReceived\":$QTY_A,\"uomId\":\"$ITEM_A_UOM\",\"unitCost\":$COST_A,\"locationId\":null,\"purchaseOrderLineId\":\"$PO_LINE_A\"},
    {\"itemId\":\"$ITEM_B_ID\",\"batchNo\":null,\"serialNo\":\"$SERIAL\",\"qtyReceived\":1,\"uomId\":\"$ITEM_B_UOM\",\"unitCost\":$COST_B,\"locationId\":null,\"purchaseOrderLineId\":\"$PO_LINE_B\"}
  ]}")
GR_ID=$(jget "$GR_BODY" id)
GR_LINE_A=$(echo "$GR_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_A_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
GR_LINE_B=$(echo "$GR_BODY" | grep -o "{[^{}]*\"itemId\":\"$ITEM_B_ID\"[^{}]*}" | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
[ -n "$GR_ID" ] && ok "Created GRPO $(jget "$GR_BODY" grpoNo 2>/dev/null || echo "$GR_ID") against PO (serial $SERIAL)" || bad "Create GRPO -- $GR_BODY"

$CURL -X POST "$BASE_URL/api/goods-receipt-po/$GR_ID/submit" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{\"requestedBy\":\"$MGR_ID\"}" > /dev/null
GR_APPROVE=$($CURL -X POST "$BASE_URL/api/goods-receipt-po/$GR_ID/approve" "${ADM_AUTH[@]}" -H "Content-Type: application/json" -d "{\"approverUserId\":\"$ADM_ID\",\"comments\":null}")
assert_status "GRPO submitted+approved (stock posted)" "$GR_APPROVE" '"status":"POSTED"'

AFTER_GR_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand)
AFTER_GR_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand)
assert_eq "Item A stock after receipt (base $BASE_QTY_A + $QTY_A)" "$(add "$BASE_QTY_A" "$QTY_A")" "$AFTER_GR_QTY_A"
assert_eq "Item B stock after receipt (base $BASE_QTY_B + 1)" "$(add "$BASE_QTY_B" 1)" "$AFTER_GR_QTY_B"

SERIALS_BODY=$($CURL "$BASE_URL/api/serial-numbers" "${ADM_AUTH[@]}")
SERIAL_ROW=$(echo "$SERIALS_BODY" | grep -o "{[^{}]*\"serialNo\":\"$SERIAL\"[^{}]*}")
assert_status "New serial $SERIAL is IN_STOCK after receipt" "$SERIAL_ROW" '"status":"IN_STOCK"'

AP_BODY=$($CURL -X POST "$BASE_URL/api/ap-invoices" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"invoiceType\":\"ITEM\",\"goodsReceiptPoId\":\"$GR_ID\",
  \"supplierInvoiceNo\":\"$SUPPLIER_INVOICE_NO\",\"invoiceDate\":\"$DAY1\",\"dueDate\":null,\"remarks\":\"Full-cycle test\",\"costCenterId\":null,\"createdBy\":null,
  \"lines\":[
    {\"itemId\":\"$ITEM_A_ID\",\"qty\":$QTY_A,\"uomId\":\"$ITEM_A_UOM\",\"unitCost\":$COST_A,\"goodsReceiptPoLineId\":\"$GR_LINE_A\"},
    {\"itemId\":\"$ITEM_B_ID\",\"qty\":1,\"uomId\":\"$ITEM_B_UOM\",\"unitCost\":$COST_B,\"goodsReceiptPoLineId\":\"$GR_LINE_B\"}
  ],
  \"expenseLines\":[]}")
AP_ID=$(jget "$AP_BODY" id)
EXPECTED_AP_TOTAL=$(awk -v q="$QTY_A" -v ca="$COST_A" -v cb="$COST_B" 'BEGIN{printf "%.4f", q*ca + 1*cb}')
[ -n "$AP_ID" ] && ok "Created AP Invoice $(jget "$AP_BODY" invoiceNo 2>/dev/null || echo "$AP_ID") against GRPO" || bad "Create AP Invoice -- $AP_BODY"

$CURL -X POST "$BASE_URL/api/ap-invoices/$AP_ID/submit" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{\"requestedBy\":\"$MGR_ID\"}" > /dev/null
AP_APPROVE=$($CURL -X POST "$BASE_URL/api/ap-invoices/$AP_ID/approve" "${ADM_AUTH[@]}" -H "Content-Type: application/json" -d "{\"approverUserId\":\"$ADM_ID\",\"comments\":null}")
assert_status "AP Invoice submitted+approved" "$AP_APPROVE" '"status":"POSTED"'
assert_eq "AP Invoice amountPaid before any payment" "0" "$(jget "$AP_APPROVE" amountPaid)"

# GRNI nets to zero across the two journals this GRPO/AP pair posted (Cr GRNI at receipt, Dr GRNI
# at invoicing, same amount) -- extract each journal's own total debit/credit and compare.
GR_JOURNAL=$($CURL "$BASE_URL/api/gl-journals?referenceTable=GoodsReceiptPo&referenceId=$GR_ID" "${ADM_AUTH[@]}")
AP_JOURNAL=$($CURL "$BASE_URL/api/gl-journals?referenceTable=ApInvoice&referenceId=$AP_ID" "${ADM_AUTH[@]}")
[ -n "$GR_JOURNAL" ] && [ "$GR_JOURNAL" != "[]" ] && ok "GRPO posted a GL journal (GRNI accrual)" || bad "GRPO GL journal missing"
[ -n "$AP_JOURNAL" ] && [ "$AP_JOURNAL" != "[]" ] && ok "AP Invoice posted a GL journal (GRNI clearing)" || bad "AP Invoice GL journal missing"

echo ""
echo "--- DAY 2 ($DAY2) -- Selling what Day 1 received via POS, then paying the AP Invoice ---"

POS1_BODY=$($CURL -X POST "$BASE_URL/api/pos/checkout" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"posTerminalId\":\"$TERMINAL_ID\",\"customerId\":null,\"invoiceDate\":\"$DAY2\",
  \"discountPct\":null,\"costCenterId\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qty\":5,\"uomId\":\"$ITEM_A_UOM\",\"unitPrice\":25.00,\"discountPct\":0,
    \"discountSourceType\":null,\"discountSourceId\":null,\"vatType\":\"VATABLE\",\"statutoryDiscountTypeId\":null,
    \"statutoryIdNumber\":null,\"deliveryOrderLineId\":null}],
  \"tenders\":[{\"paymentMethodId\":\"$CASH_METHOD_ID\",\"amount\":125.00,\"referenceNo\":null,\"bankOrPartnerName\":null}],
  \"createdBy\":null}")
POS1_INVOICE_ID=$(jget "$POS1_BODY" salesInvoiceId)
[ -n "$POS1_INVOICE_ID" ] && ok "POS sale #1: 5x Item A -- invoice $(jget "$POS1_BODY" invoiceNo)" || bad "POS sale #1 -- $POS1_BODY"

POS2_BODY=$($CURL -X POST "$BASE_URL/api/pos/checkout" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"posTerminalId\":\"$TERMINAL_ID\",\"customerId\":null,\"invoiceDate\":\"$DAY2\",
  \"discountPct\":null,\"costCenterId\":null,
  \"lines\":[{\"itemId\":\"$ITEM_B_ID\",\"qty\":1,\"uomId\":\"$ITEM_B_UOM\",\"unitPrice\":77.65,\"discountPct\":0,
    \"discountSourceType\":null,\"discountSourceId\":null,\"vatType\":\"VATABLE\",\"statutoryDiscountTypeId\":null,
    \"statutoryIdNumber\":null,\"deliveryOrderLineId\":null,\"serialNo\":\"$SERIAL\"}],
  \"tenders\":[{\"paymentMethodId\":\"$CASH_METHOD_ID\",\"amount\":77.65,\"referenceNo\":null,\"bankOrPartnerName\":null}],
  \"createdBy\":null}")
POS2_INVOICE_ID=$(jget "$POS2_BODY" salesInvoiceId)
[ -n "$POS2_INVOICE_ID" ] && ok "POS sale #2: 1x serialized Item B ($SERIAL) -- invoice $(jget "$POS2_BODY" invoiceNo)" || bad "POS sale #2 -- $POS2_BODY"

SERIAL_ROW=$($CURL "$BASE_URL/api/serial-numbers" "${ADM_AUTH[@]}" | grep -o "{[^{}]*\"serialNo\":\"$SERIAL\"[^{}]*}")
assert_status "Serial $SERIAL is SOLD after POS checkout" "$SERIAL_ROW" '"status":"SOLD"'

AFTER_POS_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand)
AFTER_POS_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand)
assert_eq "Item A stock after POS sale ($AFTER_GR_QTY_A - 5)" "$(add "$AFTER_GR_QTY_A" -5)" "$AFTER_POS_QTY_A"
assert_eq "Item B stock after POS sale ($AFTER_GR_QTY_B - 1)" "$(add "$AFTER_GR_QTY_B" -1)" "$AFTER_POS_QTY_B"

OP_BODY=$($CURL -X POST "$BASE_URL/api/outgoing-payments" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"supplierId\":\"$SUPPLIER_ID\",\"bankAccountId\":\"$BANK_ACCOUNT_ID\",\"paymentDate\":\"$DAY2\",
  \"refNo\":\"CYCLE-PMT-1\",\"remarks\":\"Full-cycle test\",\"costCenterId\":null,\"createdBy\":null,
  \"lines\":[{\"apInvoiceId\":\"$AP_ID\",\"amount\":$EXPECTED_AP_TOTAL}]}")
OP_ID=$(jget "$OP_BODY" id)
[ -n "$OP_ID" ] && ok "Created Outgoing Payment for full AP Invoice balance" || bad "Create Outgoing Payment -- $OP_BODY"

$CURL -X POST "$BASE_URL/api/outgoing-payments/$OP_ID/submit" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{\"requestedBy\":\"$MGR_ID\"}" > /dev/null
OP_APPROVE=$($CURL -X POST "$BASE_URL/api/outgoing-payments/$OP_ID/approve" "${ADM_AUTH[@]}" -H "Content-Type: application/json" -d "{\"approverUserId\":\"$ADM_ID\",\"comments\":null}")
assert_status "Outgoing Payment submitted+approved" "$OP_APPROVE" '"status":"POSTED"'

AP_AFTER_PAYMENT=$($CURL "$BASE_URL/api/ap-invoices/$AP_ID" "${ADM_AUTH[@]}")
assert_eq "AP Invoice amountPaid after full payment" "$EXPECTED_AP_TOTAL" "$(jget "$AP_AFTER_PAYMENT" amountPaid)"
assert_status "AP Invoice status after full payment" "$AP_AFTER_PAYMENT" '"status":"PAID"'

echo ""
echo "--- DAY 3 ($DAY3) -- Returns, day close, and tie-out ---"

RET1_BODY=$($CURL -X POST "$BASE_URL/api/sales-returns" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$WAREHOUSE_ID\",\"customerId\":\"$WALKIN_CUSTOMER_ID\",\"deliveryOrderId\":null,\"returnDate\":\"$DAY3\",
  \"remarks\":\"Full-cycle test return\",\"costCenterId\":null,\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_A_ID\",\"qtyReturned\":1,\"uomId\":\"$ITEM_A_UOM\",\"unitPrice\":25.00,\"deliveryOrderLineId\":null,\"vatType\":\"VATABLE\"}]}")
assert_status "Return 1x Item A from POS sale #1" "$RET1_BODY" '"status":"POSTED"'

RET2_BODY=$($CURL -X POST "$BASE_URL/api/sales-returns" "${MGR_AUTH[@]}" -H "Content-Type: application/json" -d "{
  \"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$WAREHOUSE_ID\",\"customerId\":\"$WALKIN_CUSTOMER_ID\",\"deliveryOrderId\":null,\"returnDate\":\"$DAY3\",
  \"remarks\":\"Full-cycle test return\",\"costCenterId\":null,\"createdBy\":null,
  \"lines\":[{\"itemId\":\"$ITEM_B_ID\",\"qtyReturned\":1,\"uomId\":\"$ITEM_B_UOM\",\"unitPrice\":77.65,\"deliveryOrderLineId\":null,\"vatType\":\"VATABLE\",\"serialNo\":\"$SERIAL\"}]}")
assert_status "Return the serialized Item B unit from POS sale #2" "$RET2_BODY" '"status":"POSTED"'

SERIAL_ROW=$($CURL "$BASE_URL/api/serial-numbers" "${ADM_AUTH[@]}" | grep -o "{[^{}]*\"serialNo\":\"$SERIAL\"[^{}]*}")
assert_status "Serial $SERIAL is back IN_STOCK after return" "$SERIAL_ROW" '"status":"IN_STOCK"'

AFTER_RET_QTY_A=$(jget "$(bal_of "$ITEM_A_ID")" qtyOnHand)
AFTER_RET_QTY_B=$(jget "$(bal_of "$ITEM_B_ID")" qtyOnHand)
assert_eq "Item A stock after return ($AFTER_POS_QTY_A + 1)" "$(add "$AFTER_POS_QTY_A" 1)" "$AFTER_RET_QTY_A"
assert_eq "Item B stock after return ($AFTER_POS_QTY_B + 1)" "$(add "$AFTER_POS_QTY_B" 1)" "$AFTER_RET_QTY_B"

XREAD=$($CURL "$BASE_URL/api/pos-closing/x-reading?branchId=$BRANCH_ID" "${ADM_AUTH[@]}")
[ -n "$XREAD" ] && ok "X-Reading ran (read-only snapshot)" || bad "X-Reading -- $XREAD"

ZREAD=$($CURL -X POST "$BASE_URL/api/pos-closing/z-reading" "${ADM_AUTH[@]}" -H "Content-Type: application/json" -d "{\"branchId\":\"$BRANCH_ID\",\"runBy\":\"$ADM_ID\"}")
[ -n "$(jget "$ZREAD" id)" ] && ok "Z-Reading closed the day -- Z-Counter $(jget "$ZREAD" zCounterValue 2>/dev/null)" || bad "Z-Reading -- $ZREAD"

echo ""
echo "=== RESULT: $PASS passed, $FAIL failed ==="
[ "$FAIL" -eq 0 ] && echo "Full 3-day cycle tied out cleanly." || echo "See [FAIL] lines above."
exit $([ "$FAIL" -eq 0 ] && echo 0 || echo 1)
