# Testing Guide

ZARI has two independent layers of testing. Run both when you touch backend code — they catch
different things.

| Layer | What it is | Speed | What it catches |
|---|---|---|---|
| **Unit tests** | `dotnet test`, xUnit v3 + FluentAssertions + NSubstitute, against an EF Core InMemory `AppDbContext` | Seconds | Handler logic: guard clauses, validation, permission checks, business rules |
| **Full-cycle regression scripts** | Bash scripts that drive the real, running API over HTTP against the real MySQL dev DB | Minutes | Whether the whole system actually works end to end — GL journals balance, stock moves correctly, BIR books tie out, cross-module wiring is correct |

Unit tests can't catch everything: EF Core's InMemory provider doesn't support
`ExecuteUpdateAsync`, `ExecuteDeleteAsync`, or `Database.BeginTransactionAsync()`, so several
Approve/Post-style handlers are only guard-clause-tested at the unit level. The regression scripts
are what actually exercises those full success paths.

---

## 1. Unit tests

```bash
cd D:\Lenard\Personal\Project\SND\ZARI\ZARI
dotnet test tests/Application.UnitTests/ZARI.Application.UnitTests.csproj
```

Useful variants:

```bash
# Just one module (matches namespace/class name)
dotnet test tests/Application.UnitTests/ZARI.Application.UnitTests.csproj --filter "FullyQualifiedName~Features.Inventory.GoodsReceipt"

# Skip the build if you haven't changed anything since the last build
dotnet test tests/Application.UnitTests/ZARI.Application.UnitTests.csproj --no-build

# Architecture rules (layering, naming conventions)
dotnet test tests/Architecture.Tests/ZARI.Architecture.Tests.csproj
```

No live server or database is needed — everything runs against a fresh in-memory database created
per test (`TestDbContextFactory.Create()`). Test files live under
`tests/Application.UnitTests/Features/<Module>/`, mirroring `src/Application/Features/`. Shared
entity builders and fakes live in `tests/Application.UnitTests/TestSupport/`.

---

## 2. Full-cycle regression scripts

Two self-contained Bash scripts live in `SND/ZARI/scripts/` (a sibling folder to this repo, **not**
version-controlled):

- **`full-cycle-3day-test.sh`** — Purchase-to-Pay feeding Order-to-Cash over 3 backdated days
  (PR → PO → Goods Receipt → AP Invoice, two POS sales, Outgoing Payment, Sales Returns, day
  close). 40 assertions.
- **`full-cycle-5day-test.sh`** — a longer cycle that also creates its own throwaway branch/master
  data from scratch (idempotent — safe to re-run), covers Purchase Price Variance, partial
  payments, a Manual Journal Entry, period-close enforcement, and a sweep of every BIR book/report.
  108 assertions.

Both scripts discover every ID they need by code/name at runtime (no hardcoded GUIDs) and print a
`[PASS]`/`[FAIL]` line per assertion, ending in a `=== RESULT: N passed, M failed ===` summary.

### 2.1 Start the API

The scripts hit a running `ZARI.Api` instance over HTTPS, default `https://localhost:7200`.

```bash
cd D:\Lenard\Personal\Project\SND\ZARI\ZARI
dotnet run --project src/Api/ZARI.Api.csproj --launch-profile https --urls "https://localhost:7200;http://localhost:5200"
```

Run this in its own terminal (it blocks) — or, if you need it backgrounded from a tool that also
backgrounds its own shell, don't nest an explicit `&` inside an already-backgrounded call; launch
`dotnet run` itself as the backgrounded command, or you can get a misleading immediate "exited"
message even though the process is still alive. Either way, confirm it's actually up before
trusting any exit code:

```bash
curl -sk -o /dev/null -w "HTTP %{http_code}\n" https://localhost:7200/scalar/v1
```

The API connects to MySQL on `localhost:3307` (`Database=zari_cic`, see
`src/Api/appsettings.json` → `ConnectionStrings:cwm-db`) — make sure that's reachable first:

```bash
(echo > /dev/tcp/127.0.0.1/3307) 2>&1 && echo "MySQL reachable" || echo "MySQL NOT reachable"
```

### 2.2 Run the scripts

```bash
cd /d/Lenard/Personal/Project/SND/ZARI/scripts
bash full-cycle-3day-test.sh
bash full-cycle-5day-test.sh
```

Point at a different API instance with `BASE_URL`:

```bash
BASE_URL=https://localhost:5001 bash full-cycle-3day-test.sh
```

A clean run ends with:

```
=== RESULT: 40 passed, 0 failed ===
Full 3-day cycle tied out cleanly.
```

```
=== RESULT: 108 passed, 0 failed ===
Full 5-day cycle (master data -> Day1-5) tied out cleanly.
```

### 2.3 Fixture prerequisites (the usual reason a fresh DB fails)

The 3-day script assumes some fixtures already exist in the DB — it does **not** create them, and
they are **not** part of `AppDbSeeder` (i.e. a freshly-migrated/seeded dev DB won't have them). If
`Resolved ITEM_A_ID` / `ITEM_B_ID` / `TERMINAL_ID` come back empty, create these once via the API
(as an admin) and every future run will find and reuse them:

| Fixture | What | Why |
|---|---|---|
| Item `TEST-FIFO-1` | Plain item, `costingMethod: "Fifo"`, `isSerialized: false`, sold+purchased+stocked | Day 1's plain-cost receipt/sale line |
| Item `SKU-1001` | `isSerialized: true`, sold+purchased+stocked | Day 1's serialized receipt/sale/return line |
| `ItemBranchSetting` for both items at branch `br-hq` | `defaultWarehouseId` = the `HQ-MAIN` warehouse | POS checkout refuses to sell an item with no branch setting (`PosSale.ItemNotAvailableAtBranch`) |
| A POS Terminal at branch `br-hq` | any `code`/`name`, `status: "active"` | Day 2's POS checkout needs a terminal to ring through |

```bash
TOKEN=$(curl -sk -X POST "https://localhost:7200/api/identity/login" -H "Content-Type: application/json" \
  -d '{"email":"admin@zari.coop","password":"zari123"}' | grep -o '"jwToken":"[^"]*"' | sed 's/"jwToken":"//;s/"//')

# UOM id for PCS, warehouse id for HQ-MAIN — read these from GET /api/uoms and GET /api/warehouses first.
PCS_UOM="<uom id>"
WAREHOUSE_ID="<warehouse id>"

curl -sk -X POST "https://localhost:7200/api/items" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{
  \"code\":\"TEST-FIFO-1\",\"name\":\"Test FIFO Item\",\"description\":null,\"categoryId\":null,
  \"baseUomId\":\"$PCS_UOM\",\"itemType\":\"FinishedGood\",\"costingMethod\":\"Fifo\",
  \"isSerialized\":false,\"isBatchTracked\":false,\"isSold\":true,\"isPurchased\":true,\"isStocked\":true,
  \"isTileDisplay\":false,\"allowNegativeStock\":false,
  \"salesAccountId\":null,\"purchaseAccountId\":null,\"inventoryAccountId\":null,\"cogsAccountId\":null,
  \"vatType\":\"VATABLE\",\"status\":\"active\"}"

curl -sk -X POST "https://localhost:7200/api/items" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{
  \"code\":\"SKU-1001\",\"name\":\"Test Serialized Item\",\"description\":null,\"categoryId\":null,
  \"baseUomId\":\"$PCS_UOM\",\"itemType\":\"FinishedGood\",\"costingMethod\":\"Avg\",
  \"isSerialized\":true,\"isBatchTracked\":false,\"isSold\":true,\"isPurchased\":true,\"isStocked\":true,
  \"isTileDisplay\":false,\"allowNegativeStock\":false,
  \"salesAccountId\":null,\"purchaseAccountId\":null,\"inventoryAccountId\":null,\"cogsAccountId\":null,
  \"vatType\":\"VATABLE\",\"status\":\"active\"}"

# Repeat for each item's id from the responses above:
curl -sk -X POST "https://localhost:7200/api/item-branch-settings" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "{
  \"itemId\":\"<item id>\",\"branchId\":\"br-hq\",\"defaultWarehouseId\":\"$WAREHOUSE_ID\",
  \"reorderPoint\":5,\"minStock\":1,\"maxStock\":1000,\"sellingPrice\":25.00,\"markupPct\":null,\"status\":\"active\"}"

curl -sk -X POST "https://localhost:7200/api/pos-terminals" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{
  "branchId":"br-hq","code":"POS-1","name":"Test POS Terminal",
  "machineIdentificationNumber":null,"machineSerialNumber":null,"posPermitNumber":null,"posPermitDateIssued":null,
  "status":"active"}'
```

The 5-day script doesn't have this problem — it creates its own branch (`TB`) and every master-data
record it needs from scratch on first run, and reuses them (by code) on every later run.

### 2.4 Company quick-post toggles

Several document types only reach `POSTED` immediately at Create when their matching toggle on
`GET /api/company` is `true`: `salesInvoiceQuickPostEnabled`, `customerPaymentQuickPostEnabled`,
`salesReturnQuickPostEnabled` (also `salesOrderQuickPostEnabled` / `deliveryQuickPostEnabled`,
unused by either script). Both scripts expect these three to be on. If a script's Sales
Invoice/Customer Payment/Sales Return assertions fail with the document stuck in `DRAFT` (or a
`CustomerPayment.InvoiceNotPayable` error), check `GET /api/company` and flip whichever are off:

```bash
curl -sk "https://localhost:7200/api/company" -H "Authorization: Bearer $TOKEN"

# PUT is a full replace — resend every field, changing only what you need to flip.
curl -sk -X PUT "https://localhost:7200/api/company" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{
  "code":"ZARI","name":"Zari Distribution Corp.","taxId":"000-000-000-000","baseCurrencyId":"cur-php",
  "registeredAddress":null,"tradeName":null,"vatRegistrationType":null,"maxUnapprovedDiscountPct":null,
  "salesOrderQuickPostEnabled":false,"deliveryQuickPostEnabled":false,"salesInvoiceQuickPostEnabled":true,
  "customerPaymentQuickPostEnabled":true,"salesReturnQuickPostEnabled":true}'
```

### 2.5 Reading failures

- `Resolved <X>_ID (empty -- check seed data)` → a fixture lookup came back empty; see §2.3.
- `"Failed to read parameter ... as JSON"` (500) → almost always a symptom of an empty ID from the
  line above being interpolated into a JSON body (`"itemId":""` fails `Guid` deserialization), not
  a real handler bug — fix the empty lookup first, then re-run.
- `PosSale.ItemNotAvailableAtBranch` → missing `ItemBranchSetting` for that item/branch.
- A document stuck in `DRAFT` where the script expects `POSTED`, or
  `CustomerPayment.InvoiceNotPayable` → a `Company` quick-post toggle is off; see §2.4.
- Anything else failing after fixtures/toggles are in place is a real regression — worth a git
  `bisect`/diff review before assuming it's environment.

---

## 3. Adding new tests

- **New handler** → add a test file under `tests/Application.UnitTests/Features/<Module>/`,
  named `<HandlerClassName>Tests.cs`. Reuse or extend the fixture builders in
  `tests/Application.UnitTests/TestSupport/` rather than hand-rolling entities inline.
- **New end-to-end flow** worth a permanent regression check → extend one of the two shell
  scripts (or add a new one) rather than a one-off manual `curl` session, so it's re-runnable.
