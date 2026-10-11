#!/usr/bin/env bash
# ZARI Route Sales regression — one truck's day, end to end (ZARI-FE/frs/route-sales/RouteSalesContext.md).
#
# Setup (idempotent / per run): item RS-ITEM-1 (price 50) with 20 units received into HQ-MAIN at cost 30 through a
#   MANUAL Goods Receipt, the branch's Damaged warehouse, and — new every run, so the numbers are deterministic — a
#   truck (own warehouse + handheld BIR series) and a customer with a credit limit of 60.
# The day: sales order (4) approved -> trip planned with 6 extra stock -> load-out approved (10 onto the truck) ->
#   driver accepts -> sale 1 (3 of 4 delivered, 1 refused with a reason, 2 van-sold, change from cash) -> same sale
#   re-sent (idempotent) -> an out-of-sequence number refused -> next number voided -> sale 2 charged to account ->
#   sale 3 refused over the credit limit -> the charge collected -> a good and a damaged return -> trip ended one
#   unit and 5.00 cash short -> manager closes, the driver pays the shortage -> partial driver payment.
# Then: stock (source, truck, Damaged), the close + payment journals, every route-sales report and its PDF, the
#   truck's Sales Book, X- and Z-Reading on its own series, and the guard against counter POS on a truck terminal.
#
# Requires a running ZARI.Api (default https://localhost:7200) seeded in Development (admin, manager.hq, staff.hq),
# curl, grep/sed/awk — no jq/python, like the full-cycle scripts. Safe to re-run against the same database.

set -u
BASE_URL="${BASE_URL:-https://localhost:7200}"
CURL="curl -sk"
PASS=0
FAIL=0
RUN=$(date +%s)
NOW=$(date -u +%Y-%m-%dT%H:%M:%SZ)
TODAY=$(date -u +%Y-%m-%d)

ok()   { PASS=$((PASS+1)); echo "  [PASS] $1"; }
bad()  { FAIL=$((FAIL+1)); echo "  [FAIL] $1"; }
num()  { awk -v n="${1:-0}" 'BEGIN{printf "%.4f", n+0}'; }
add()  { awk -v a="${1:-0}" -v b="${2:-0}" 'BEGIN{printf "%.4f", a+b}'; }
sub()  { awk -v a="${1:-0}" -v b="${2:-0}" 'BEGIN{printf "%.4f", a-b}'; }
assert_eq() {
  local e a; e=$(num "$2"); a=$(num "$3")
  if [ "$e" = "$a" ]; then ok "$1 (= $e)"; else bad "$1 (expected $e, got $a)"; fi
}
assert_has() { if echo "$2" | grep -q "$3"; then ok "$1"; else bad "$1 -- body: $(echo "$2" | head -c 400)"; fi; }
jget() { # naive single-level JSON field extractor (first match only)
  local json="$1" field="$2" sval
  sval=$(echo "$json" | grep -o "\"$field\":\"[^\"]*\"" | head -1)
  if [ -n "$sval" ]; then echo "$sval" | sed "s/\"$field\":\"//;s/\"\$//"; return; fi
  echo "$json" | grep -o "\"$field\":[^,}]*" | head -1 | sed "s/\"$field\"://"
}
find_id_by() { # $1=json_array $2=field $3=value -> id of the first flat object where field==value
  echo "$1" | grep -o "{[^{}]*\"$2\":\"$(printf '%s' "$3" | sed 's/[&/\]/\\&/g')\"[^{}]*}" | head -1 \
    | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//'
}
# Logs in, waiting out the login rate limiter (5 per minute per address) — the CI job runs the full-cycle scripts
# right before this one from the same address.
login() { # $1=email -> prints the login response body
  local body code i
  for i in $(seq 1 12); do
    body=$($CURL -w "\n%{http_code}" -X POST "$BASE_URL/api/identity/login" -H "Content-Type: application/json" \
      -d "{\"email\":\"$1\",\"password\":\"zari123\"}")
    code=$(echo "$body" | tail -1)
    if [ "$code" != "429" ]; then echo "$body" | sed '$d'; return; fi
    sleep 10
  done
  echo ""
}
JSON=(-H "Content-Type: application/json")
post() { $CURL -X POST "$BASE_URL$2" -H "Authorization: Bearer $1" "${JSON[@]}" -d "$3"; }
get()  { $CURL "$BASE_URL$2" -H "Authorization: Bearer $1"; }
status_of() { $CURL -o /dev/null -w "%{http_code}" -X POST "$BASE_URL$2" -H "Authorization: Bearer $1" "${JSON[@]}" -d "$3"; }
is_guid() { echo "$1" | grep -qi '^"\?[0-9a-f]\{8\}-[0-9a-f]\{4\}-[0-9a-f]\{4\}-[0-9a-f]\{4\}-[0-9a-f]\{12\}"\?$'; }
pdf_head() { $CURL "$BASE_URL$2" -H "Authorization: Bearer $1" | head -c 4; }
# Sums debit and credit over a journal's lines, found by its source reference (lines hold no arrays, so the
# window from the reference to the first ']' is exactly that journal's lines).
journal_window() { echo "$1" | tr -d '\n' | grep -o "\"sourceReferenceTable\":\"$2\",\"sourceReferenceId\":\"$3\"[^]]*]"; }
sum_field() { echo "$1" | grep -o "\"$2\":[-0-9.]*" | sed "s/\"$2\"://" | awk '{s+=$1} END{printf "%.4f", s+0}'; }

echo "=== ZARI Route Sales day against $BASE_URL (run $RUN) ==="

ADMIN=$(login "admin@zari.coop");      A=$(jget "$ADMIN" jwToken);   ADM_ID=$(jget "$ADMIN" id)
MANAGER=$(login "manager.hq@zari.coop"); M=$(jget "$MANAGER" jwToken); MGR_ID=$(jget "$MANAGER" id)
DRIVER=$(login "staff.hq@zari.coop");  D=$(jget "$DRIVER" jwToken);  DRV_ID=$(jget "$DRIVER" id)
if [ -z "$A" ] || [ -z "$M" ] || [ -z "$D" ]; then bad "log in as admin, manager.hq and staff.hq -- aborting"; echo "=== RESULT: $PASS passed, $FAIL failed ==="; exit 1; fi
ok "Logged in as admin, manager.hq (approver) and staff.hq (driver)"

echo ""
echo "--- Setup ---"
BRANCH_ID="br-hq"
WH_LIST=$(get "$A" "/api/warehouses")
MAIN_WH=$(find_id_by "$WH_LIST" code "HQ-MAIN")
DMG_WH=$(echo "$WH_LIST" | grep -o "{[^{}]*\"branchId\":\"$BRANCH_ID\"[^{}]*\"warehouseType\":\"Damaged\"[^{}]*\"status\":\"active\"[^{}]*}" | head -1 | grep -o '"id":"[^"]*"' | head -1 | sed 's/"id":"//;s/"//')
if [ -z "$DMG_WH" ]; then
  DMG_BODY=$(post "$A" "/api/warehouses" "{\"branchId\":\"$BRANCH_ID\",\"code\":\"HQ-DMG\",\"name\":\"HQ Damaged\",\"warehouseType\":\"Damaged\",\"status\":\"active\"}")
  DMG_WH=$(jget "$DMG_BODY" id)
  [ -n "$DMG_WH" ] && ok "Created the branch Damaged warehouse HQ-DMG" || bad "Create Damaged warehouse -- $DMG_BODY"
else
  ok "Branch Damaged warehouse exists"
fi
[ -n "$MAIN_WH" ] && ok "Resolved HQ-MAIN" || bad "Resolve HQ-MAIN"

UOM_PCS=$(find_id_by "$(get "$A" "/api/uoms")" code "PCS")
ITEM_ID=$(find_id_by "$(get "$A" "/api/items")" code "RS-ITEM-1")
if [ -z "$ITEM_ID" ]; then
  IT_BODY=$(post "$A" "/api/items" "{
    \"code\":\"RS-ITEM-1\",\"name\":\"Route Sales Regression Item\",\"description\":null,\"categoryId\":null,
    \"baseUomId\":\"$UOM_PCS\",\"itemType\":\"FinishedGood\",\"costingMethod\":\"Avg\",
    \"isSerialized\":false,\"isBatchTracked\":false,\"isSold\":true,\"isPurchased\":true,\"isStocked\":true,
    \"salesAccountId\":null,\"purchaseAccountId\":null,\"inventoryAccountId\":null,\"cogsAccountId\":null,
    \"vatType\":\"VATABLE\",\"status\":\"active\"}")
  ITEM_ID=$(jget "$IT_BODY" id)
  [ -n "$ITEM_ID" ] && ok "Created item RS-ITEM-1" || bad "Create item -- $IT_BODY"
else
  ok "Item RS-ITEM-1 exists, reusing"
fi
if ! get "$A" "/api/item-branch-settings" | grep -q "{[^{}]*\"itemId\":\"$ITEM_ID\"[^{}]*\"branchId\":\"$BRANCH_ID\""; then
  IBS=$(post "$A" "/api/item-branch-settings" "{\"itemId\":\"$ITEM_ID\",\"branchId\":\"$BRANCH_ID\",\"defaultWarehouseId\":\"$MAIN_WH\",
    \"reorderPoint\":0,\"minStock\":0,\"maxStock\":1000,\"sellingPrice\":50,\"markupPct\":null,\"status\":\"active\"}")
  [ -n "$(jget "$IBS" id)" ] && ok "Item branch setting (price 50)" || bad "Item branch setting -- $IBS"
else
  ok "Item branch setting exists (price 50)"
fi
CASH=$(find_id_by "$(get "$A" "/api/payment-methods")" code "CASH")

bal() { # $1=warehouseId -> qty of RS-ITEM-1 there (0 when no row)
  local row; row=$(get "$A" "/api/stock-ledger/balances" | grep -o "{\"id\":\"[^}]*\"itemId\":\"$ITEM_ID\"[^}]*\"warehouseId\":\"$1\"[^}]*}" | head -1)
  local q; q=$(jget "$row" qtyOnHand); echo "${q:-0}"
}
MAIN_BASE=$(bal "$MAIN_WH"); DMG_BASE=$(bal "$DMG_WH")

GR=$(post "$A" "/api/goods-receipts" "{\"branchId\":\"$BRANCH_ID\",\"warehouseId\":\"$MAIN_WH\",\"receiptType\":\"MANUAL\",\"receivedBy\":null,
  \"grDate\":\"$NOW\",\"remarks\":\"Route sales regression stock\",\"goodsIssueRefNo\":null,\"goodsIssueId\":null,\"reasonCode\":\"FOUND\",
  \"costCenterId\":null,\"createdBy\":\"$ADM_ID\",
  \"lines\":[{\"itemId\":\"$ITEM_ID\",\"batchNo\":null,\"serialNo\":null,\"qtyReceived\":20,\"uomId\":\"$UOM_PCS\",\"unitCost\":30,\"locationId\":null}]}")
GR_ID=$(jget "$GR" id)
post "$A" "/api/goods-receipts/$GR_ID/submit" "{\"requestedBy\":\"$ADM_ID\"}" > /dev/null
GR_OK=$(post "$M" "/api/goods-receipts/$GR_ID/approve" "{\"approverUserId\":\"$MGR_ID\",\"comments\":null}")
assert_has "20 units received into HQ-MAIN at cost 30" "$GR_OK" '"status":"POSTED"'

TRUCK_CODE="RS$(echo "$RUN" | tail -c 7)"
TRUCK=$(post "$A" "/api/vehicles" "{\"branchId\":\"$BRANCH_ID\",\"code\":\"$TRUCK_CODE\",\"plateNo\":\"$TRUCK_CODE\",\"description\":\"Route sales regression\",
  \"status\":\"active\",\"orPrefix\":\"$TRUCK_CODE-\",\"orNextNumber\":1}")
TRUCK_ID=$(jget "$TRUCK" id); TRUCK_WH=$(jget "$TRUCK" warehouseId); TERMINAL=$(jget "$TRUCK" posTerminalId)
assert_has "Truck $TRUCK_CODE created with its own warehouse TRK-$TRUCK_CODE" "$TRUCK" "\"warehouseCode\":\"TRK-$TRUCK_CODE\""

CUST=$(post "$A" "/api/customers" "{\"name\":\"RS Day Customer $RUN\",\"type\":\"business\",\"email\":\"rs$RUN@example.com\",\"phone\":\"0917\",
  \"branchId\":\"$BRANCH_ID\",\"status\":\"active\",\"owner\":\"Regression\",\"address\":\"1 Route St\",\"notes\":null,\"arAccountId\":null,
  \"paymentTermsDays\":null,\"standingDiscountPct\":null,\"memberNo\":null,\"creditLimit\":60}")
CUST_ID=$(jget "$CUST" id)
[ -n "$CUST_ID" ] && ok "Customer with a credit limit of 60" || bad "Create customer -- $CUST"

echo ""
echo "--- Plan and load ---"
SO=$(post "$A" "/api/sales-orders" "{\"branchId\":\"$BRANCH_ID\",\"customerId\":\"$CUST_ID\",\"orderDate\":\"$NOW\",\"remarks\":\"Route sales regression\",
  \"discountPct\":null,\"createdBy\":\"$ADM_ID\",\"lines\":[{\"itemId\":\"$ITEM_ID\",\"qty\":4,\"uomId\":\"$UOM_PCS\",\"unitPrice\":50,\"discountPct\":0}]}")
SO_ID=$(jget "$SO" id)
case "$(jget "$SO" status)" in DRAFT) SO=$(post "$A" "/api/sales-orders/$SO_ID/submit" "{\"requestedBy\":\"$ADM_ID\"}");; esac
case "$(jget "$SO" status)" in PENDING_APPROVAL) SO=$(post "$M" "/api/sales-orders/$SO_ID/approve" "{\"approverUserId\":\"$MGR_ID\"}");; esac
assert_has "Sales order for 4 approved" "$SO" '"status":"POSTED"'

TRIP=$(post "$A" "/api/trips" "{\"branchId\":\"$BRANCH_ID\",\"vehicleId\":\"$TRUCK_ID\",\"routeId\":null,\"sourceWarehouseId\":\"$MAIN_WH\",
  \"driverUserId\":\"$DRV_ID\",\"driverEmployeeId\":null,\"helperNames\":\"Regression helper\",\"tripDate\":\"$NOW\",\"remarks\":\"Route sales regression\",
  \"salesOrderIds\":[\"$SO_ID\"],\"extraLines\":[{\"itemId\":\"$ITEM_ID\",\"qty\":6}],\"createdBy\":\"$ADM_ID\"}")
TRIP_ID=$(jget "$TRIP" id); TRIP_NO=$(jget "$TRIP" tripNo)
assert_eq "Trip $TRIP_NO plans 4 ordered" 4 "$(jget "$TRIP" qtyOrdered)"
assert_eq "...plus 6 extra" 6 "$(jget "$TRIP" qtyExtra)"
post "$A" "/api/trips/$TRIP_ID/submit" "{\"requestedBy\":\"$ADM_ID\"}" > /dev/null
LOADED=$(post "$M" "/api/trips/$TRIP_ID/approve-load" "{\"approverUserId\":\"$MGR_ID\",\"comments\":null}")
assert_has "Manager approved the load-out" "$LOADED" '"status":"LOADED"'
assert_eq "10 units moved onto the truck" 10 "$(bal "$TRUCK_WH")"

MINE=$(get "$D" "/api/trips/mine")
assert_has "Handheld download has the trip" "$MINE" "\"tripNo\":\"$TRIP_NO\""
assert_has "...the BIR invoice header" "$MINE" '"invoiceHeader":{"registeredName"'
assert_has "...and the refusal reasons" "$MINE" '"code":"NO_PAYMENT"'
N1="$TRUCK_CODE-000001"; N2="$TRUCK_CODE-000002"; N3="$TRUCK_CODE-000003"; N4="$TRUCK_CODE-000004"
assert_has "Next handheld number is $N1" "$MINE" "\"nextInvoiceNumber\":\"$N1\""
SO_LINE=$(echo "$MINE" | grep -o '"salesOrderLineId":"[^"]*"' | head -1 | sed 's/"salesOrderLineId":"//;s/"//')
ACCEPT=$(post "$D" "/api/trips/$TRIP_ID/accept-load" "{\"counts\":[{\"itemId\":\"$ITEM_ID\",\"qtyCounted\":10}]}")
assert_has "Driver accepted the load (on the road)" "$ACCEPT" '"status":"IN_TRANSIT"'

echo ""
echo "--- On the road ---"
SALE1="{\"birOrSeriesNumber\":\"$N1\",\"customerId\":\"$CUST_ID\",\"salesOrderId\":\"$SO_ID\",\"soldAt\":\"$NOW\",\"discountPct\":null,
  \"lines\":[{\"itemId\":\"$ITEM_ID\",\"qty\":3,\"unitPrice\":50,\"discountPct\":0,\"salesOrderLineId\":\"$SO_LINE\"},
             {\"itemId\":\"$ITEM_ID\",\"qty\":2,\"unitPrice\":50,\"discountPct\":0}],
  \"tenders\":[{\"paymentMethodId\":\"$CASH\",\"amount\":300,\"referenceNo\":null,\"bankOrPartnerName\":null}],
  \"receivedByName\":\"Store owner\",
  \"refusals\":[{\"salesOrderLineId\":\"$SO_LINE\",\"qty\":1,\"reasonCode\":\"NO_PAYMENT\",\"notes\":null}]}"
NO_REASON=$(post "$D" "/api/trips/$TRIP_ID/route-sales" "$(echo "$SALE1" | sed 's/"refusals":\[[^]]*\]/"refusals":[]/')")
assert_has "Partial delivery without a reason is refused" "$NO_REASON" 'ShortfallReasonRequired'
RS1=$(post "$D" "/api/trips/$TRIP_ID/route-sales" "$SALE1")
RS1_ID=$(jget "$RS1" id)
assert_has "Sale $N1: 3 of 4 delivered (1 refused, reason), 2 van-sold" "$RS1" '"status":"COMPLETED"'
assert_eq "...total 250" 250 "$(jget "$RS1" invoiceTotal)"
assert_eq "...change 50 from cash 300" 50 "$(jget "$RS1" changeDue)"
RS1B=$(post "$D" "/api/trips/$TRIP_ID/route-sales" "$SALE1")
[ "$(jget "$RS1B" id)" = "$RS1_ID" ] && ok "Re-sending $N1 returns the same sale (idempotent)" || bad "Re-send not idempotent -- $RS1B"
OOS=$(post "$D" "/api/trips/$TRIP_ID/route-sales" "$(echo "$SALE1" | sed "s/$N1/$TRUCK_CODE-000009/")")
assert_has "An out-of-sequence number is refused" "$OOS" 'out of sequence'
VOID=$(post "$D" "/api/trips/$TRIP_ID/route-sales/void" "{\"birOrSeriesNumber\":\"$N2\",\"reason\":\"Spoiled printout\"}")
assert_has "$N2 voided (number used up)" "$VOID" '"status":"VOID"'
SALE2="{\"birOrSeriesNumber\":\"$N3\",\"customerId\":\"$CUST_ID\",\"salesOrderId\":null,\"soldAt\":\"$NOW\",\"discountPct\":null,
  \"lines\":[{\"itemId\":\"$ITEM_ID\",\"qty\":1,\"unitPrice\":50,\"discountPct\":0}],\"tenders\":[]}"
RS2=$(post "$D" "/api/trips/$TRIP_ID/route-sales" "$SALE2")
INV2=$(jget "$RS2" salesInvoiceId)
assert_eq "Sale $N3 charged to account (balance due)" 50 "$(jget "$RS2" balanceDue)"
OVER=$(post "$D" "/api/trips/$TRIP_ID/route-sales" "$(echo "$SALE2" | sed "s/$N3/$N4/")")
assert_has "Another charge sale over the 60 credit limit is refused" "$OVER" 'CreditLimitExceeded'
assert_has "...and uses no number ($N4 is still next)" "$(get "$D" "/api/trips/mine")" "\"nextInvoiceNumber\":\"$N4\""
# The handheld names an invoice issued on this trip by its number (it has no ZARI id offline); older ones by id.
collect() { # $1=clientRef suffix $2=allocation json $3=amount
  post "$D" "/api/trips/$TRIP_ID/collections" "{\"clientRef\":\"$(printf '%08x-0000-4000-8%03d-%012x' "$RUN" "$1" "$RUN")\",\"customerId\":\"$CUST_ID\",
    \"collectedAt\":\"$NOW\",\"allocations\":[$2],
    \"tenders\":[{\"paymentMethodId\":\"$CASH\",\"amount\":$3,\"referenceNo\":null,\"bankOrPartnerName\":null}]}"
}
assert_has "Collecting against a number not issued on this trip is refused" "$(collect 0 "{\"salesInvoiceId\":null,\"invoiceNumber\":\"$N4\",\"amount\":20}" 20)" 'InvoiceNotOnTrip'
COLL=$(collect 1 "{\"salesInvoiceId\":null,\"invoiceNumber\":\"$N3\",\"amount\":20}" 20)
is_guid "$COLL" && ok "Collected 20 of the charge by its invoice number $N3" || bad "Collection by number -- $COLL"
COLL2=$(collect 2 "{\"salesInvoiceId\":\"$INV2\",\"amount\":30}" 30)
is_guid "$COLL2" && ok "Collected the other 30 by the invoice's ZARI id" || bad "Collection by id -- $COLL2"
assert_eq "...the charge invoice is paid off" 0 "$(jget "$(get "$A" "/api/sales-invoices/$INV2")" balance)"
RET=$(post "$D" "/api/trips/$TRIP_ID/returns" "{\"clientRef\":\"$(printf '%08x-0000-4000-9000-%012x' "$RUN" "$RUN")\",\"customerId\":\"$CUST_ID\",
  \"returnedAt\":\"$NOW\",\"reason\":\"Regression\",\"lines\":[{\"itemId\":\"$ITEM_ID\",\"qty\":1,\"unitPrice\":50,\"isDamaged\":false},
  {\"itemId\":\"$ITEM_ID\",\"qty\":1,\"unitPrice\":50,\"isDamaged\":true}]}")
is_guid "$RET" && ok "One good and one damaged return onto the truck" || bad "Return -- $RET"

ONROAD=$(get "$D" "/api/trips/$TRIP_ID")
assert_eq "Trip: 3 delivered" 3 "$(jget "$ONROAD" qtyDelivered)"
assert_eq "Trip: 3 van-sold" 3 "$(jget "$ONROAD" qtyVanSold)"
assert_eq "Trip: 1 refused" 1 "$(jget "$ONROAD" qtyRefused)"
assert_eq "Trip: 5 good units should be on the truck (10-3-3+1)" 5 "$(jget "$ONROAD" qtyExpectedOnTruck)"
assert_eq "Trip: cash expected 300 (250 sale + 50 collection)" 300 "$(jget "$ONROAD" cashExpected)"
POS=$(post "$A" "/api/pos/checkout" "{\"branchId\":\"$BRANCH_ID\",\"posTerminalId\":\"$TERMINAL\",\"customerId\":null,\"invoiceDate\":\"$NOW\",
  \"discountPct\":null,\"costCenterId\":null,\"lines\":[{\"itemId\":\"$ITEM_ID\",\"qty\":1,\"uomId\":\"$UOM_PCS\",\"unitPrice\":50,\"discountPct\":0,
  \"discountSourceType\":null,\"discountSourceId\":null,\"vatType\":\"VATABLE\",\"statutoryDiscountTypeId\":null,\"statutoryIdNumber\":null}],
  \"tenders\":[{\"paymentMethodId\":\"$CASH\",\"amount\":50}],\"createdBy\":null}")
assert_has "Counter POS on the truck's terminal is refused" "$POS" 'TruckTerminal'

echo ""
echo "--- End and close ---"
ENDED=$(post "$D" "/api/trips/$TRIP_ID/end" "{\"cashRemitted\":295,\"counts\":[{\"itemId\":\"$ITEM_ID\",\"qtyCounted\":4}]}")
assert_has "Driver ended the trip 1 unit and 5.00 short" "$ENDED" '"status":"PENDING_CLOSE_APPROVAL"'
CLOSED=$(post "$M" "/api/trips/$TRIP_ID/approve-close" "{\"approverUserId\":\"$MGR_ID\",\"comments\":\"Route sales regression\",\"shortageSettlement\":\"DRIVER_PAYS\"}")
assert_has "Manager closed the trip, the driver pays the shortage" "$CLOSED" '"shortageSettlement":"DRIVER_PAYS"'
assert_has "...status CLOSED" "$CLOSED" '"status":"CLOSED"'
assert_eq "Shortage 35 (1 unit at cost 30 + 5.00 cash)" 35 "$(jget "$CLOSED" shortageAmount)"
assert_eq "...all of it owed by the driver" 35 "$(jget "$CLOSED" shortageBalance)"
assert_eq "Variance -1 recorded" -1 "$(jget "$CLOSED" qtyVariance)"
assert_eq "Truck warehouse ends empty" 0 "$(bal "$TRUCK_WH")"
assert_eq "HQ-MAIN net +14 (20 received, 10 loaded, 4 back)" "$(add "$MAIN_BASE" 14)" "$(bal "$MAIN_WH")"
assert_eq "Damaged warehouse +1" "$(add "$DMG_BASE" 1)" "$(bal "$DMG_WH")"

JOURNALS=$(get "$A" "/api/gl-journals")
CLOSE_J=$(journal_window "$JOURNALS" "TripClose" "$TRIP_ID")
[ "$(echo "$CLOSE_J" | grep -c 'sourceReferenceTable')" = "1" ] && ok "Exactly one close journal" || bad "Close journals found: $(echo "$CLOSE_J" | grep -c 'sourceReferenceTable')"
assert_eq "Close journal debits 35 (Due from Employees)" 35 "$(sum_field "$CLOSE_J" debitAmount)"
assert_eq "Close journal credits 35 (balanced)" 35 "$(sum_field "$CLOSE_J" creditAmount)"

PAY=$(post "$M" "/api/trips/$TRIP_ID/shortage-payments" "{\"amount\":20,\"paymentMethodId\":\"$CASH\",\"paidAt\":\"$NOW\",\"referenceNo\":\"RS-$RUN\",\"remarks\":null,\"receivedBy\":\"$MGR_ID\"}")
PAY_ID=$(jget "$PAY" id)
assert_eq "Driver paid 20 of the shortage" 20 "$(jget "$PAY" amount)"
assert_eq "...15 still owed" 15 "$(jget "$(get "$A" "/api/trips/$TRIP_ID")" shortageBalance)"
assert_has "Overpaying the rest is refused" "$(post "$M" "/api/trips/$TRIP_ID/shortage-payments" "{\"amount\":16,\"paymentMethodId\":\"$CASH\",\"paidAt\":\"$NOW\",\"referenceNo\":null,\"remarks\":null,\"receivedBy\":null}")" 'OverpaidShortage'
PAY_J=$(journal_window "$(get "$A" "/api/gl-journals")" "TripShortagePayment" "$PAY_ID")
assert_eq "Payment journal balanced (debit)" 20 "$(sum_field "$PAY_J" debitAmount)"
assert_eq "Payment journal balanced (credit)" 20 "$(sum_field "$PAY_J" creditAmount)"

NOTES=$(get "$M" "/api/notifications" | tr -d '
' | grep -o "{[^{}]*\"entityType\":\"TRIP\",\"entityId\":\"$TRIP_ID\"[^{}]*}")
for t in SUBMITTED APPROVED ENDED CLOSED; do
  echo "$NOTES" | grep -q "\"type\":\"$t\"" && ok "Trip notification $t sent" || bad "Trip notification $t missing"
done

echo ""
echo "--- Reports, Sales Book, X/Z-Reading ---"
SUMMARY=$(get "$A" "/api/route-sales/reports/trips/$TRIP_ID/summary")
assert_eq "Trip Summary: refused 1" 1 "$(jget "$SUMMARY" qtyRefused)"
assert_eq "Trip Summary: variance value -30 at cost" -30 "$(jget "$SUMMARY" varianceValue)"
assert_eq "Trip Summary: sales invoiced 300" 300 "$(jget "$(echo "$SUMMARY" | grep -o '"totals":{.*')" salesAmount)"
DELIV=$(get "$A" "/api/route-sales/reports/trips/$TRIP_ID/deliveries")
assert_eq "Trip Deliveries: 2 completed invoices" 2 "$(echo "$DELIV" | grep -o '"status":"COMPLETED"' | wc -l)"
assert_eq "Trip Deliveries: 1 void" 1 "$(echo "$DELIV" | grep -o '"status":"VOID"' | wc -l)"
assert_has "Trip Deliveries: the refusal and its reason" "$DELIV" '"reasonCode":"NO_PAYMENT"'
CASHREP=$(get "$A" "/api/route-sales/reports/trips/$TRIP_ID/cash-remittance")
assert_eq "Cash Remittance: short 5.00" -5 "$(jget "$CASHREP" cashShortOver)"
assert_eq "Cash Remittance: collections 50" 50 "$(jget "$CASHREP" collectionsTotal)"
UNDEL=$(get "$A" "/api/route-sales/reports/undelivered?tripId=$TRIP_ID")
assert_eq "Undelivered: 1 still open on the order" 1 "$(jget "$UNDEL" qtyOpen)"
assert_has "Undelivered: last reason shown" "$UNDEL" "can't pay"
PERF=$(get "$A" "/api/route-sales/reports/performance?from=$TODAY&to=$TODAY&branchId=$BRANCH_ID&groupBy=vehicle")
PERF_ROW=$(echo "$PERF" | grep -o "{[^{}]*\"label\":\"$TRUCK_CODE ([^{}]*}")
assert_eq "Route Performance: fill rate 75% (3 of 4)" 75 "$(jget "$PERF_ROW" fillRatePct)"
SHORT=$(get "$A" "/api/route-sales/reports/driver-shortages?branchId=$BRANCH_ID&outstandingOnly=true")
assert_eq "Driver Shortages: 15 still owed on $TRIP_NO" 15 "$(jget "$(echo "$SHORT" | grep -o "{[^{}]*\"tripId\":\"$TRIP_ID\"[^{}]*}")" balance)"
for path in "trips/$TRIP_ID/summary/pdf" "trips/$TRIP_ID/deliveries/pdf" "trips/$TRIP_ID/cash-remittance/pdf" \
            "undelivered/pdf?tripId=$TRIP_ID" "performance/pdf?from=$TODAY&to=$TODAY" "driver-shortages/pdf?branchId=$BRANCH_ID"; do
  label=$(echo "${path%%\?*}" | sed "s#trips/$TRIP_ID/#trip #;s#/pdf##")
  [ "$(pdf_head "$A" "/api/route-sales/reports/$path")" = "%PDF" ] && ok "PDF: $label" || bad "PDF: $label ($path)"
done
BOOK=$(get "$A" "/api/sales/reports/sales-book?branchId=$BRANCH_ID&posTerminalId=$TERMINAL")
assert_eq "Sales Book on the truck's series: 2 invoices" 2 "$(echo "$BOOK" | grep -o '"invoiceId"' | wc -l)"
XR=$(get "$A" "/api/pos-closing/x-reading?branchId=$BRANCH_ID&posTerminalId=$TERMINAL")
assert_eq "Truck X-Reading: 2 invoices" 2 "$(jget "$XR" invoiceCount)"
ZR=$(post "$A" "/api/pos-closing/z-reading" "{\"branchId\":\"$BRANCH_ID\",\"runBy\":\"$ADM_ID\",\"posTerminalId\":\"$TERMINAL\"}")
assert_eq "Truck Z-Reading #1 on its own counter" 1 "$(jget "$ZR" zCounterValue)"
assert_has "...closing $N1 to $N3" "$ZR" "\"lastOrNumber\":\"$N3\""

echo ""
echo "=== RESULT: $PASS passed, $FAIL failed ==="
[ "$FAIL" -eq 0 ]
