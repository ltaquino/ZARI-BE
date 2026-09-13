#!/usr/bin/env bash
# Creates the fixtures full-cycle-3day-test.sh assumes already exist but AppDbSeeder deliberately
# does not seed (they're test-only cruft, not real ERP master data — see docs/TESTING.md §2.3 for
# why). Idempotent: every step checks by code/name first and skips creation if already present, so
# it's safe to re-run against the same DB (a CI job's fresh ephemeral DB, or a long-lived local dev
# DB) without creating duplicates.
#
# Also flips the three Company quick-post toggles both regression scripts require (see
# docs/TESTING.md §2.4) so Sales Invoice / Customer Payment / Sales Return reach POSTED immediately
# instead of sitting in DRAFT waiting for a Submit/Approve the scripts never call.
#
# No jq/python dependency, matching the two regression scripts' own convention.

set -u
BASE_URL="${BASE_URL:-https://localhost:7200}"
CURL="curl -sk"

jget() {
  local json="$1" field="$2" sval
  sval=$(echo "$json" | grep -o "\"$field\":\"[^\"]*\"" | head -1)
  if [ -n "$sval" ]; then
    echo "$sval" | sed "s/\"$field\":\"//;s/\"\$//"
    return
  fi
  echo "$json" | grep -o "\"$field\":[^,}]*" | head -1 | sed "s/\"$field\"://"
}

echo "=== Seeding full-cycle-3day-test.sh fixtures against $BASE_URL ==="

body=$($CURL -X POST "$BASE_URL/api/identity/login" -H "Content-Type: application/json" -d '{"email":"admin@zari.coop","password":"zari123"}')
TOKEN=$(jget "$body" jwToken)
if [ -z "$TOKEN" ]; then
  echo "  [FAIL] admin login -- $body" >&2
  exit 1
fi
AUTH=(-H "Authorization: Bearer $TOKEN")
JSON=(-H "Content-Type: application/json")

PCS_UOM=$($CURL "$BASE_URL/api/uoms" "${AUTH[@]}" | grep -o '{[^{}]*"code":"PCS"[^{}]*}' | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
WAREHOUSE_ID=$($CURL "$BASE_URL/api/warehouses" "${AUTH[@]}" | grep -o '{"id":"[^}]*"code":"HQ-MAIN"[^}]*}' | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$PCS_UOM" ] || [ -z "$WAREHOUSE_ID" ]; then
  echo "  [FAIL] PCS UOM or HQ-MAIN warehouse not found -- is AppDbSeeder's base data present?" >&2
  exit 1
fi

ensure_item() { # $1=code $2=name $3=costingMethod $4=isSerialized -> prints item id on stdout
  local code="$1" name="$2" costing="$3" serialized="$4"
  local existing; existing=$($CURL "$BASE_URL/api/items" "${AUTH[@]}" | grep -o "{[^{}]*\"code\":\"$code\"[^{}]*}")
  local id; id=$(jget "$existing" id)
  if [ -n "$id" ]; then
    echo "  [SKIP] Item $code already exists" >&2
    echo "$id"
    return
  fi
  local created; created=$($CURL -X POST "$BASE_URL/api/items" "${AUTH[@]}" "${JSON[@]}" -d "{
    \"code\":\"$code\",\"name\":\"$name\",\"description\":null,\"categoryId\":null,
    \"baseUomId\":\"$PCS_UOM\",\"itemType\":\"FinishedGood\",\"costingMethod\":\"$costing\",
    \"isSerialized\":$serialized,\"isBatchTracked\":false,\"isSold\":true,\"isPurchased\":true,\"isStocked\":true,
    \"isTileDisplay\":false,\"allowNegativeStock\":false,
    \"salesAccountId\":null,\"purchaseAccountId\":null,\"inventoryAccountId\":null,\"cogsAccountId\":null,
    \"vatType\":\"VATABLE\",\"status\":\"active\"}")
  id=$(jget "$created" id)
  if [ -n "$id" ]; then
    echo "  [CREATE] Item $code" >&2
  else
    echo "  [FAIL] Create item $code -- $created" >&2
    exit 1
  fi
  echo "$id"
}

ITEM_A_ID=$(ensure_item "TEST-FIFO-1" "Test FIFO Item" "Fifo" "false")
ITEM_B_ID=$(ensure_item "SKU-1001" "Test Serialized Item" "Avg" "true")

ensure_branch_setting() { # $1=itemId $2=code (for logging)
  local item_id="$1" code="$2"
  local existing; existing=$($CURL "$BASE_URL/api/item-branch-settings" "${AUTH[@]}")
  if echo "$existing" | grep -q "\"itemId\":\"$item_id\""; then
    echo "  [SKIP] ItemBranchSetting for $code already exists"
    return
  fi
  local created; created=$($CURL -X POST "$BASE_URL/api/item-branch-settings" "${AUTH[@]}" "${JSON[@]}" -d "{
    \"itemId\":\"$item_id\",\"branchId\":\"br-hq\",\"defaultWarehouseId\":\"$WAREHOUSE_ID\",
    \"reorderPoint\":5,\"minStock\":1,\"maxStock\":1000,\"sellingPrice\":25.00,\"markupPct\":null,\"status\":\"active\"}")
  if echo "$created" | grep -q '"itemId"'; then
    echo "  [CREATE] ItemBranchSetting for $code"
  else
    echo "  [FAIL] Create ItemBranchSetting for $code -- $created" >&2
    exit 1
  fi
}
ensure_branch_setting "$ITEM_A_ID" "TEST-FIFO-1"
ensure_branch_setting "$ITEM_B_ID" "SKU-1001"

TERMINAL=$($CURL "$BASE_URL/api/pos-terminals" "${AUTH[@]}" | grep -o '{"id":"[^}]*"branchId":"br-hq"[^}]*}' | head -1)
if [ -n "$TERMINAL" ]; then
  echo "  [SKIP] POS terminal at br-hq already exists"
else
  created=$($CURL -X POST "$BASE_URL/api/pos-terminals" "${AUTH[@]}" "${JSON[@]}" -d '{
    "branchId":"br-hq","code":"POS-1","name":"Test POS Terminal",
    "machineIdentificationNumber":null,"machineSerialNumber":null,"posPermitNumber":null,"posPermitDateIssued":null,
    "status":"active"}')
  if echo "$created" | grep -q '"id"'; then
    echo "  [CREATE] POS terminal POS-1 at br-hq"
  else
    echo "  [FAIL] Create POS terminal -- $created" >&2
    exit 1
  fi
fi

COMPANY=$($CURL "$BASE_URL/api/company" "${AUTH[@]}")
if echo "$COMPANY" | grep -q '"salesInvoiceQuickPostEnabled":true' && \
   echo "$COMPANY" | grep -q '"customerPaymentQuickPostEnabled":true' && \
   echo "$COMPANY" | grep -q '"salesReturnQuickPostEnabled":true'; then
  echo "  [SKIP] Company quick-post toggles already on"
else
  CODE=$(jget "$COMPANY" code)
  NAME=$(jget "$COMPANY" name)
  TAX_ID=$(jget "$COMPANY" taxId)
  BASE_CCY=$(jget "$COMPANY" baseCurrencyId)
  # PUT returns 204 No Content on success (see lib/apiStore.ts's own doc comment on the FE side for
  # this same convention) -- re-GET afterward to confirm rather than inspecting the PUT response.
  $CURL -X PUT "$BASE_URL/api/company" "${AUTH[@]}" "${JSON[@]}" -d "{
    \"code\":\"$CODE\",\"name\":\"$NAME\",\"taxId\":\"$TAX_ID\",\"baseCurrencyId\":\"$BASE_CCY\",
    \"registeredAddress\":null,\"tradeName\":null,\"vatRegistrationType\":null,\"maxUnapprovedDiscountPct\":null,
    \"salesOrderQuickPostEnabled\":false,\"deliveryQuickPostEnabled\":false,\"salesInvoiceQuickPostEnabled\":true,
    \"customerPaymentQuickPostEnabled\":true,\"salesReturnQuickPostEnabled\":true,\"cicProviderCode\":null}" > /dev/null
  RECHECK=$($CURL "$BASE_URL/api/company" "${AUTH[@]}")
  if echo "$RECHECK" | grep -q '"salesInvoiceQuickPostEnabled":true'; then
    echo "  [UPDATE] Company quick-post toggles turned on"
  else
    echo "  [FAIL] Update Company quick-post toggles -- $RECHECK" >&2
    exit 1
  fi
fi

echo "=== Fixtures ready ==="
