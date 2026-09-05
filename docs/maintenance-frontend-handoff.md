# تسليم الواجهة الأمامية — الصيانة والمخزون وورش العمل

> هذا العقد يخص الإصدار المطبق في قاعدة البيانات المستضافة. جميع الأمثلة أدناه تستخدم JSON باسماء `camelCase`، والتواريخ بصيغة ISO-8601، والقيم العشرية أرقام JSON وليست نصوصاً.

## 1. قواعد التكامل العامة

- **Base URL:** عنوان الـ API المنشور، ثم المسارات كما هي أدناه (مثال: `/api/maintenance-inventory/items`).
- أرسل دائماً `Authorization: Bearer <access-token>` و`Accept: application/json`. استخدم `Content-Type: application/json` لكل الطلبات ما عدا رفع فاتورة التوريد، فهو `multipart/form-data`.
- كل طلب ناجح يرجع الجسم مباشرةً، بلا غلاف مثل `data` أو `result`. عمليات الإنشاء والتعديل ترجع `200 OK` والجسم الناتج.
- `Guid` نص UUID، و`DateOnly` مثل `2026-09-05`، و`DateTimeOffset` مثل `2026-09-05T09:30:00+03:00`.
- الحقول `rowVersion` قيمة opaque بصيغة Base64. لا تعدلها ولا تنشئها في الواجهة؛ خزّن آخر قيمة مرجعة وأرسلها في أي تعديل/فتح برميل/تسجيل فاقد/تغيير حالة أمر يتطلبها.
- الالتزام بـ FIFO يتم في الخادم فقط. الواجهة تعرض طبقات التكلفة والبراميل للشفافية، لكنها لا تختار طبقة تكلفة عند صرف قطعة عادية.

### شكل الخطأ الموحد

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "maintenance.concurrency_conflict",
  "status": 409,
  "detail": "تم تعديل السجل من مستخدم آخر؛ أعد تحميل البيانات.",
  "instance": "/api/maintenance-locations/…",
  "errorCode": "maintenance.concurrency_conflict",
  "correlationId": "00-…",
  "field": "optionalFieldName"
}
```

`400` بيانات غير صالحة، `401` غير مسجل، `403` لا توجد صلاحية، `404` غير موجود، و`409` تعارض حالة/رصيد/FIFO/تزامن. عند `maintenance.concurrency_conflict` أعرض رسالة ثم أعد تحميل السجل؛ لا تعيد إرسال نموذج قديم.

### الصلاحيات التي تتحكم في إظهار عناصر الواجهة

| الميزة | الصلاحية |
|---|---|
|عرض المواقع|`maintenance.locations.read`|
|إدارة المواقع|`maintenance.locations.manage`|
|قراءة أوامر العمل والتاريخ والخطط|`maintenance.work_orders.read`|
|إنشاء/تغيير أوامر الشركة والخطط|`maintenance.work_orders.manage`|
|عرض تذكيرات الزيت|`maintenance.oil.read`|
|إتمام تغيير الزيت|`maintenance.oil.complete`|
|قراءة/إدارة أعمال المركبات الخارجية|`maintenance.external_jobs.read`, `maintenance.external_jobs.manage`|
|بيع قطع للخارجي|`maintenance.part_sales.manage`|
|أجرة العميل|`maintenance.customer_labor_charges.manage`|
|مصنعية الميكانيكي|`maintenance.mechanic_labor_payments.manage`|
|تقرير الربح|`maintenance.profit_reports.read`|
|الأصناف|`inventory.items.read`, `inventory.items.manage`|
|الأرصدة|`inventory.stock.read`|
|طبقات FIFO|`inventory.cost_layers.read`|
|إضافة فاتورة/مورد|`inventory.receipts.manage`|
|نقل/صرف مخزون|`inventory.stock.move`|
|تسجيل فاقد أو عكس صرف|`inventory.stock.adjust`|
|مرتجع المورد|`inventory.returns.manage`|

## 2. القيم الرقمية (Enums)

الـ API يستقبل ويرجع هذه الـ enums كأرقام.

| الحقل | القيم |
|---|---|
|`locationType`|1 مستودع، 2 ورشة، 3 مستودع وورشة|
|`serviceSubjectType`|1 مركبة الشركة، 2 مركبة خارجية|
|`maintenanceType`|1 وقائي، 2 إصلاح، 3 فحص، 4 حادث، 5 تغيير زيت، 6 بيع قطع فقط|
|`status` لأمر العمل|1 مفتوح، 2 قيد العمل، 3 مكتمل، 4 مغلق، 5 ملغى|
|`itemType`|1 قطع غيار، 2 ملحق راكب، 3 زيت، 4 مستهلك|
|`baseUnitOfMeasure` / `purchaseUnit`|1 قطعة، 2 لتر، 3 برميل، 4 صندوق، 5 طقم|
|`usageType`|1 قطعة غيار، 2 زيت، 3 فلتر زيت، 4 مستهلك، 5 بيع خارجي|
|`paymentMethod`|1 نقدي، 2 بطاقة، 3 تحويل، 4 أخرى|
|`oil barrel status`|1 مختوم، 2 مفتوح، 3 مستنفد، 4 مرتجع|

## 3. المواقع: جدة والرياض

استخدم `GET /api/maintenance-locations` عند بدء التطبيق، ولا تثبت المعرفات داخل الواجهة. يوجد إعدادان أوليان:

- `JED-WH` / **مستودع جدة**: مركبات الشركة فقط، مخزون مفعل، ولا تعرض بيع قطع أو خدمة مدفوعة للخارجي.
- `RUH-WS` / **ورشة الرياض**: تقبل الشركة والخارجي، مخزون مفعل، وتعرض البيع والمصنعية والتحصيل وتقارير الربح.

### قراءة المواقع

`GET /api/maintenance-locations`

```json
[
  {
    "id":"guid", "code":"RUH-WS", "nameAr":"ورشة الرياض", "nameEn":"Riyadh Workshop",
    "operatingCityId":"guid", "operatingCityNameAr":"الرياض", "locationType":3,
    "allowsCompanyVehicles":true, "allowsExternalVehicles":true,
    "allowsSparePartSales":true, "allowsPaidExternalRepairs":true,
    "inventoryEnabled":true, "status":1, "address":null, "notes":null,
    "rowVersion":"base64"
  }
]
```

**قرار الواجهة:** بعد اختيار الموقع، عطّل/اخف خيارات لا يسمح بها response نفسه. لا توجد "سكنات" كمواقع صيانة في هذا النموذج.

### إنشاء/تعديل موقع

`POST /api/maintenance-locations` أو `PUT /api/maintenance-locations/{id}`. في POST أرسل `rowVersion:null`؛ في PUT أرسل القيمة الحالية.

```json
{
  "code":"RUH-WS", "nameAr":"ورشة الرياض", "nameEn":"Riyadh Workshop",
  "operatingCityId":"guid", "locationType":3,
  "allowsCompanyVehicles":true, "allowsExternalVehicles":true,
  "allowsSparePartSales":true, "allowsPaidExternalRepairs":true,
  "inventoryEnabled":true, "address":"…", "latitude":null, "longitude":null,
  "notes":null, "rowVersion":null
}
```

الرد هو كائن `MaintenanceLocationResponse` المطابق لكائن القراءة أعلاه.

## 4. كتالوج المخزون والموردين

### الأصناف

| العملية | المسار | الجسم/الرد |
|---|---|---|
|بحث/قراءة|`GET /api/maintenance-inventory/items?search=oil`|مصفوفة `InventoryItemResponse`|
|إنشاء|`POST /api/maintenance-inventory/items`|`InventoryItemRequest` ← `InventoryItemResponse`|
|تعديل|`PUT /api/maintenance-inventory/items/{id}`|نفسه، مع `rowVersion` الحالي|

```json
{
  "sku":"OIL-10W40", "barcode":null, "itemType":3,
  "nameAr":"زيت 10W-40", "nameEn":"Oil 10W-40",
  "descriptionAr":null, "descriptionEn":null,
  "baseUnitOfMeasure":2, "purchaseUnitOfMeasure":3,
  "defaultPackageQuantity":208, "minimumStockLevel":20,
  "reorderQuantity":208, "isSerialized":false, "isLotTracked":true,
  "rowVersion":null
}
```

رد الصنف يحوي: `id, sku, barcode, itemType, nameAr, nameEn, baseUnitOfMeasure, purchaseUnitOfMeasure, defaultPackageQuantity, minimumStockLevel, reorderQuantity, status, rowVersion`.

**زيت البراميل:** أنشئه دائماً `itemType:3`, `baseUnitOfMeasure:2` (لتر), `purchaseUnitOfMeasure:3` (برميل). السعر المحاسبي الفعلي لا يوضع في الصنف؛ يأتي من فاتورة كل دفعة، لذلك يمكن أن تختلف سعة وسعر كل برميل.

### الموردون

`GET|POST /api/maintenance-inventory/suppliers` و`PUT /api/maintenance-inventory/suppliers/{id}`. جسم الإنشاء/التعديل:

```json
{
  "supplierNumber":"SUP-001", "legalNameAr":"مورد الزيوت", "legalNameEn":"Oil Supplier",
  "vatNumber":null, "commercialRegistrationNumber":null, "contactName":"…",
  "phone":"…", "email":null, "address":null, "paymentTermsDays":30,
  "notes":null, "rowVersion":null
}
```

رد المورد: `id, supplierNumber, legalNameAr, legalNameEn, vatNumber, commercialRegistrationNumber, phone, status, notes, rowVersion`.

## 5. فاتورة التوريد ورفع الملف (مطلوب)

### الطلب

`POST /api/maintenance-inventory/receipts` ويجب أن يكون `multipart/form-data` بحقلين **بالاسم الدقيق**:

1. `ReceiptJson`: سلسلة JSON للفاتورة.
2. `BillFile`: ملف الفاتورة، إجباري.

الأنواع المقبولة PDF أو JPEG/PNG/WebP/GIF/BMP، والحد الأقصى للملف 10MB (حد HTTP 11MB). لا تضع يدوياً `Content-Type` عند استخدام `FormData`؛ المتصفح يضيف boundary.

```ts
const receipt = {
  supplierId, supplierInvoiceNumber: "INV-88", invoiceDate: "2026-09-05",
  receivedAtUtc: "2026-09-05T10:00:00+03:00", inventoryLocationId,
  currencyCode: "SAR",
  lines: [{
    inventoryItemId: oilItemId, purchaseUnit: 3, packageCount: 1,
    declaredQuantityPerPackage: 208, grossWeightKg: 210, netWeightKg: 208,
    packageUnitPrice: 1000, discountAmount: 0, taxAmount: 150,
    lotNumber: "LOT-9", expiryDate: null
  }]
};
const form = new FormData();
form.append("ReceiptJson", JSON.stringify(receipt));
form.append("BillFile", file);
await api.post("/api/maintenance-inventory/receipts", form);
```

لكل سطر: `packageCount × declaredQuantityPerPackage` هو الرصيد بوحدة الأساس. لسطر زيت البرميل: الوزن الإجمالي والصافي إلزاميان وموجبان، والصافي لا يتجاوز الإجمالي، و`packageCount` عدد صحيح؛ ينشئ الخادم برميلاً مستقلاً لكل package. يمكن إدخال أكثر من سطر لنفس صنف الزيت في الفاتورة إذا اختلفت الدفعة أو السعة أو السعر.

### الرد

`PurchaseReceiptResponse`:

```json
{
  "id":"guid", "receiptNumber":"PR-…", "supplierId":"guid", "supplierNameAr":"…",
  "supplierInvoiceNumber":"INV-88", "invoiceDate":"2026-09-05",
  "receivedAtUtc":"2026-09-05T07:00:00+00:00", "inventoryLocationId":"guid",
  "inventoryLocationNameAr":"ورشة الرياض", "subtotal":1000, "discountAmount":0,
  "taxAmount":150, "inventoryValuationAmount":1000, "totalAmount":1150,
  "currencyCode":"SAR", "status":1,
  "lines":[{
    "id":"guid", "inventoryItemId":"guid", "sku":"OIL-10W40", "purchaseUnit":3,
    "packageCount":1, "declaredQuantityPerPackage":208, "receivedBaseQuantity":208,
    "baseUnitOfMeasure":2, "grossWeightKg":210, "netWeightKg":208,
    "packageUnitPrice":1000, "lineSubtotal":1000, "discountAmount":0, "taxAmount":150,
    "inventoryValuationAmount":1000, "baseUnitCost":4.807692, "stockCostLayerId":"guid"
  }],
  "attachment":{"id":"guid","originalFileName":"invoice.pdf","contentType":"application/pdf","fileSizeBytes":1234,"sha256Checksum":"…","uploadedAtUtc":"…"},
  "oilBarrels":[{"id":"guid","barrelNumber":"OB-…","nominalCapacityLiters":208,"remainingLiters":208,"unitCostPerLiter":4.807692,"status":1,"rowVersion":"base64"}],
  "rowVersion":"base64"
}
```

للقراءة: `GET /api/maintenance-inventory/receipts/{id}` يرجع الرد كاملاً. لتنزيل الملف الخاص (مع token): `GET /api/maintenance-inventory/receipts/{id}/bill-file`؛ تعامل معه Blob واحفظ اسم الملف من `Content-Disposition`. لا يوجد رابط عام للفاتورة.

## 6. الرصيد، FIFO، وبراميل الزيت

### القراءة

- `GET /api/maintenance-inventory/balances?inventoryLocationId={guid}&inventoryItemId={guid}` → مصفوفة بها `quantityOnHand, quantityReserved, reportingAverageUnitCost, inventoryValue, lastMovementAtUtc, rowVersion` مع تعريف الصنف والموقع.
- `GET /api/maintenance-inventory/cost-layers?inventoryLocationId={guid}&inventoryItemId={guid}&availableOnly=true` → طبقات FIFO: `id, receivedAtUtc, originalSequence, originalQuantity, remainingQuantity, baseUnitOfMeasure, unitCost, remainingValue, lotNumber, expiryDate, sourceReceiptLineId, sourceCostLayerId, rowVersion`.
- `GET /api/maintenance-inventory/oil-barrels?inventoryLocationId={guid}&inventoryItemId={guid}&status=open` → مصفوفة براميل. `status` اختياري: `sealed`, `open`, `depleted`, `returned` (غير حساس لحالة الحروف).

رد البرميل الكامل:

```json
{
  "id":"guid", "barrelNumber":"OB-0001", "purchaseReceiptLineId":"guid", "inventoryItemId":"guid",
  "inventoryLocationId":"guid", "stockCostLayerId":"guid", "packageSequence":1,
  "nominalCapacityLiters":208, "consumedLiters":200, "remainingLiters":8,
  "unitCostPerLiter":4.807692, "remainingInventoryValue":38.461536,
  "maximumAllowedLossLiters":4.16, "recordedLossLiters":0,
  "remainingLossAllowanceLiters":5.2, "status":2,
  "openedAtUtc":"2026-09-05T07:00:00+00:00", "depletedAtUtc":null,
  "rowVersion":"base64"
}
```

### فتح البرميل

`POST /api/maintenance-inventory/oil-barrels/{barrelId}/open`

```json
{ "openedAtUtc":"2026-09-05T10:00:00+03:00", "rowVersion":"base64-from-barrel" }
```

لا تفتح الواجهة زر فتح إلا لبرميل `sealed`، ثم اعرض الرد دائماً:

```json
{
  "barrel": { "id":"guid", "remainingLiters":8, "status":2, "rowVersion":"new-base64" },
  "opened": false,
  "hasPreviousBarrelWarning": true,
  "previousOpenBarrelsRemainingLiters": 8,
  "warningCode":"oil.previous_barrel_remaining",
  "warningMessageAr":"يوجد برميل زيت مفتوح به 8 لتر متبقية. يجب استنفاده قبل فتح برميل جديد."
}
```

هذه **ليست استجابة HTTP خطأ**: إذا `opened:false`، اترك البرميل الجديد مختوماً واعرض تحذيراً بارزاً بأن المتبقي 8 لتر يجب استهلاكه أولاً. يسمح الخادم ببرميل مفتوح واحد فقط لكل صنف زيت وموقع، وبأقدم طبقة FIFO فقط.

### فاقد/إهلاك البرميل

`POST /api/maintenance-inventory/oil-barrels/{barrelId}/losses`

```json
{ "occurredAtUtc":"2026-09-05T10:30:00+03:00", "quantityLiters":2, "reason":"تسريب موثق", "rowVersion":"base64" }
```

الرد: `id, oilBarrelId, occurredAtUtc, quantityLiters, costAmount, barrelRecordedLossLiters, barrelRemainingLiters, barrelRemainingLossAllowanceLiters`.

الحد الأقصى للفقد هو 2% من سعة البرميل، وليس من كمية متبقية: 208L = 4.16L. تجاوز ذلك يرجع `400 maintenance.oil_loss_allowance_exceeded`. بعد كل فاقد حدّث صف البرميل من الرد.

### نقل ومرتجع وصرف راكب

`POST /api/maintenance-inventory/transfers`

```json
{ "sourceLocationId":"guid", "destinationLocationId":"guid", "postedAtUtc":"…", "reason":"نقل للرياض", "lines":[{"inventoryItemId":"guid","quantity":5}] }
```

الرد: `id, transferNumber, sourceLocationId, destinationLocationId, postedAtUtc, totalCost, status, rowVersion`. الزيت بالبرميل لا ينقل إلا براميل كاملة مختومة؛ الكمية يجب أن توافق سعاتها، وإلا `maintenance.oil_transfer_requires_whole_barrels`.

`POST /api/maintenance-inventory/supplier-returns`

```json
{ "supplierId":"guid", "inventoryLocationId":"guid", "purchaseReceiptId":"guid-or-null", "returnedAtUtc":"…", "reason":"عيب", "lines":[{"inventoryItemId":"guid","stockCostLayerId":"guid","quantity":1,"reason":"تالف"}] }
```

الرد: `id, returnNumber, supplierId, inventoryLocationId, returnedAtUtc, totalCost, status, rowVersion`.

`POST /api/maintenance-inventory/rider-issues`

```json
{ "riderProfileId":"guid", "inventoryLocationId":"guid", "issuedAtUtc":"…", "notes":null, "lines":[{"inventoryItemId":"guid","quantity":1,"expectedReturn":false}] }
```

الرد: `id, issueNumber, riderProfileId, relatedAssignmentId, inventoryLocationId, issuedAtUtc, totalCost, status, rowVersion`.

## 7. أوامر الصيانة

### الشركة مقابل الخارجي

`POST /api/maintenance-work-orders` للشركة فقط و`POST /api/maintenance-work-orders/external` للخارجي فقط. الجسم واحد:

```json
{
  "serviceSubjectType":1, "vehicleId":"guid", "vehicleIssueId":null,
  "maintenanceLocationId":"guid", "maintenanceType":2,
  "openedAtUtc":"2026-09-05T10:00:00+03:00", "scheduledAtUtc":null,
  "odometerAtOpen":12000, "estimatedCost":0, "diagnosis":"…", "notes":null,
  "externalVehicle":null
}
```

للمركبة الخارجية بدّل إلى `serviceSubjectType:2`, `vehicleId:null`، وأرسل أقل معلومات لازمة فقط:

```json
"externalVehicle":{"plateOrReference":"XYZ 1234","vehicleType":2,"customerName":"اسم العميل","customerPhone":"05…","notes":null}
```

لا ترسل `externalVehicle` مع مركبة شركة، ولا `vehicleId` مع خارجي؛ الخطأ `maintenance.invalid_subject`. ورشة الرياض فقط تقبل الخارجي والبيع/الإصلاح المدفوع؛ استخدم flags الموقع قبل إظهار الإجراء.

### قائمة وتفاصيل وحالة الأمر

- `GET /api/maintenance-work-orders?maintenanceLocationId={guid}&vehicleId={guid}&status=open`، الاستعلامات اختيارية والحالة نص `open|inprogress|completed|closed|cancelled`.
- `GET /api/maintenance-work-orders/{id}`.

كلاهما يرجع `MaintenanceWorkOrderResponse`: `id, workOrderNumber, serviceSubjectType, vehicleId, vehicleAssetNumber, vehicleIssueId, maintenanceLocationId, maintenanceLocationNameAr, maintenanceType, status, openedAtUtc, scheduledAtUtc, startedAtUtc, completedAtUtc, odometerAtOpen, odometerAtCompletion, riderVehicleAssignmentId, attributedRiderProfileId, estimatedCost, actualMaterialCost, actualLaborCost, actualOtherCost, actualTotalCost, externalVehicle, notes, rowVersion`.

تغيير الحالة: `POST /api/maintenance-work-orders/{id}/start|complete|close|cancel`

```json
{ "occurredAtUtc":"…", "workPerformed":"…", "qualityCheckNotes":"…", "notes":null, "rowVersion":"base64" }
```

الرد هو أمر العمل نفسه مع `rowVersion` جديد. اعتمد workflow: Open → start → InProgress → complete → Completed → close → Closed؛ `cancel` متاح فقط للحالة التي يسمح بها الخادم. عند `409 maintenance.invalid_state` أعد تحميل الحالة ولا تغيّرها محلياً.

### صرف قطعة/اكسسوار/مستهلك على أمر

`POST /api/maintenance-work-orders/{workOrderId}/materials`

```json
{ "inventoryItemId":"guid", "inventoryLocationId":"guid", "quantity":1, "usageType":1, "usedAtUtc":"…", "notes":"…" }
```

الرد `MaintenanceMaterialUsageResponse`: `id, maintenanceWorkOrderId, inventoryItemId, sku, itemNameAr, inventoryLocationId, usageType, direction, quantity, unitOfMeasure, totalCost, vehicleId, riderVehicleAssignmentId, riderProfileId, attributionStatus, usedAtUtc, reversalOfUsageId, costAllocations`؛ و`costAllocations` مصفوفة `stockCostLayerId, quantity, unitCost, cost`. اعرض إجمالي التكلفة والطبقات للقراءة فقط؛ الخادم يخصم الأقدم أولاً، لذلك بعد تغيّر السعر لا يبدأ صرف الجديد قبل نفاد القديم.

العكس: `POST /api/maintenance-work-orders/materials/{usageId}/reverse`

```json
{ "reversedAtUtc":"…", "reason":"أُدخلت الكمية بالخطأ" }
```

يرجع usage جديد direction=2. لا تعرض زر العكس لعملية معكوسة (`409 maintenance.already_reversed`).

تاريخ الاستهلاك: `GET /api/maintenance/vehicles/{vehicleId}/material-history` أو `GET /api/maintenance/riders/{riderProfileId}/material-history`؛ الرد مصفوفة `MaintenanceMaterialUsageResponse`. استخدمه في صفحة المركبة وصفحة الراكب لتظهر القطع والزيوت المصروفة ومن نُسبت إليه.

## 8. تغيير الزيت والتنبيهات

### التذكيرات والخطط

`GET /api/maintenance/oil-reminders` يرجع:

```json
[{"vehicleId":"guid","assetNumber":"CAR-1","vehicleType":2,"currentOdometer":15000,"lastCompletedAtUtc":"…","lastOilChangeOdometer":11000,"reminderFromOdometer":15000,"maximumDueOdometer":16000,"distanceSinceLastChange":4000,"status":2}]
```

صنّف الواجهة: سيارة `vehicleType:2` تذكير 4000 كم واستحقاق أقصى 5000 كم؛ دراجة `vehicleType:1` تذكير 800 كم واستحقاق أقصى 1000 كم. استخدم `status`: 1 طبيعي، 2 مستحق، 3 متأخر، 4 لم يتم سابقاً، 5 قراءة عداد مفقودة.

الخطط: `GET|POST /api/maintenance/plans` و`PUT /api/maintenance/plans/{id}`. جسمها: `code, nameAr, nameEn, vehicleModelId, vehicleType, triggerType, intervalDays, intervalKilometers, reminderAfterKilometers, maximumAfterKilometers, alertDaysBefore, alertKilometersBefore, inventoryItemId, defaultOilQuantityLiters, checklistJson, rowVersion`. الرد يحوي البيانات نفسها المهمة و`id,status,rowVersion` (لا يعتمد الرد على حقول التنبيه/checklist).

### إتمام تغيير الزيت

أنشئ أمر صيانة نوع `maintenanceType:5` أولاً، ثم:

`POST /api/maintenance-work-orders/{workOrderId}/oil-change`

```json
{
  "performedAtUtc":"2026-09-05T11:00:00+03:00", "odometerAtChange":15000,
  "inventoryLocationId":"guid", "oilInventoryItemId":"guid", "nextOilBarrelId":null,
  "oilFilterChanged":true, "oilFilterInventoryItemId":"guid",
  "configuredOilQuantityLiters":null, "laborCost":50, "otherCost":0,
  "notes":null, "workOrderRowVersion":"base64"
}
```

للسيارة: `configuredOilQuantityLiters:null` يجعل النظام يستخدم 4L مع الفلتر و3.5L بدونه. للدراجة تكون الكمية ضمن 0.8–1L وفق الخطة/الإعداد. عند تخصيص كمية أرسل قيمة صحيحة ضمن قواعد النوع؛ الخطأ `maintenance.invalid_oil_quantity` يحدد الحقل.

إذا كانت العملية ستستنفد البرميل المفتوح وتحتاج الباقي من برميل جديد، اعرض اختياراً من **أقدم برميل sealed FIFO فقط** وضع معرفه في `nextOilBarrelId`. لا تفتح البرميل الجديد يدوياً قبل ذلك. إذا بقي 8L في المفتوح فالعملية 4L تستهلك منه فقط؛ لا تحتاج التالي. إذا لم يوجد برميل مفتوح أو لم يُحدد البرميل التالي عند الحاجة يرجع `409 maintenance.open_oil_barrel_required`.

الرد:

```json
{"id":"guid","maintenanceWorkOrderId":"guid","performedAtUtc":"…","odometerAtChange":15000,"vehicleType":2,"oilQuantityLiters":4,"oilCost":19.230768,"oilFilterChanged":true,"oilFilterCost":25,"laborCost":50,"otherCost":0,"totalCost":94.230768,"vehicleId":"guid","riderProfileId":"guid-or-null"}
```

بعد النجاح حدّث تفاصيل أمر العمل، قائمة البراميل، الرصيد، سجل المركبة، وسجل الراكب من الخادم؛ لا تحسب هذه القيم محلياً.

## 9. ورشة الرياض: قطع خارجية، المصنعية، التحصيل، والربح الحقيقي

هذه الإجراءات تتطلب أمر خارجي في موقع `allowsExternalVehicles=true` و`allowsPaidExternalRepairs=true`. لا تجمع بيانات تشغيل المركبة الخارجية غير الضرورية؛ سجل المرجع/اللوحة وبيانات العميل المطلوبة فقط.

### بيع قطع للعميل الخارجي

`POST /api/maintenance-work-orders/{id}/part-sales`

```json
{ "inventoryItemId":"guid", "inventoryLocationId":"guid", "quantity":1, "sellingUnitPriceBeforeTax":100, "discountAmount":5, "taxAmount":14.25, "occurredAtUtc":"…", "notes":null }
```

الرد: `id, maintenanceWorkOrderId, inventoryItemId, quantity, partsRevenueBeforeTax, taxAmount, customerLineTotal, maintenanceMaterialUsageId`.

هنا `partsRevenueBeforeTax` هو بيع العميل، بينما تكلفة القطعة تُسجل تلقائياً FIFO في المصروف. لا تعرض للمستخدم حقلاً لإدخال تكلفة المخزون.

### المصنعية (المبلغ المحصل من العميل)

`POST /api/maintenance-work-orders/{id}/customer-labor-charges`

```json
{ "amountBeforeTax":150, "taxAmount":22.5, "occurredAtUtc":"…", "description":"أجرة إصلاح" }
```

### المبلغ المدفوع للميكانيكي

`POST /api/maintenance-work-orders/{id}/mechanic-labor-payments`

```json
{ "mechanicEmployeeId":"guid-or-null", "externalMechanicName":"اسم خارجي أو null", "amount":80, "paidAtUtc":"…", "description":"مصنعية الميكانيكي" }
```

اختر **إما** موظفاً مسجلاً أو اسم ميكانيكي خارجي بحسب سياسة الشاشة. المبلغ هنا مصروف مستقل عن أجرة العميل كي يظهر ربح المصنعية بدقة.

### دخل/مصروف آخر والتحصيل

`POST /api/maintenance-work-orders/{id}/other-financial-entries?income=true|false`

```json
{ "amountBeforeTax":20, "taxAmount":0, "occurredAtUtc":"…", "description":"خدمة إضافية" }
```

يرجع كل من إجراءات المصنعية والدخل/المصروف: `id, maintenanceWorkOrderId, entryType, sourceType, amountBeforeTax, taxAmount, totalAmount, occurredAtUtc, description, mechanicEmployeeId, externalMechanicName`.

التحصيل لا يغير قيمة الفاتورة/الربح، بل يغير المدفوع والمتبقي:

`POST /api/maintenance-work-orders/{id}/customer-payments`

```json
{ "amount":200, "paymentMethod":1, "paidAtUtc":"…", "reference":"RCPT-44" }
```

الرد: `id, maintenanceWorkOrderId, amount, paymentMethod, paidAtUtc, reference`.

### تقرير الربح

`GET /api/maintenance/external-profit?maintenanceLocationId={riyadhGuid}&startDate=2026-09-01&endDate=2026-09-30`

```json
{
  "maintenanceLocationId":"guid", "from":"2026-09-01", "to":"2026-09-30",
  "totalIncomeBeforeTax":250, "totalExpense":180, "taxCollected":36.75,
  "customerInvoiceTotal":286.75, "amountPaid":200, "netProfitBeforeTax":70,
  "workOrders":[{
    "maintenanceWorkOrderId":"guid", "workOrderNumber":"WO-…", "externalVehicleReference":"XYZ 1234",
    "partsRevenueBeforeTax":95, "customerLaborRevenueBeforeTax":150, "otherIncomeBeforeTax":5,
    "fifoInventoryCost":70, "mechanicLaborCost":80, "otherExpense":0, "taxCollected":36.75,
    "customerInvoiceTotal":286.75, "amountPaid":200, "outstandingAmount":86.75,
    "paymentStatus":2, "partsGrossProfit":25, "laborProfit":70, "netProfitBeforeTax":70
  }]
}
```

اعرض الربح قبل الضريبة بصيغة واضحة:

`صافي الربح = إيراد القطع قبل الضريبة + أجرة العميل قبل الضريبة + دخل آخر − تكلفة FIFO للقطع − مصنعية الميكانيكي − مصروف آخر`.

لا تخلط `amountPaid` (السيولة المحصلة) مع الربح، ولا `taxCollected` مع الربح قبل الضريبة. حالة الدفع: 1 غير مدفوع، 2 جزئي، 3 مدفوع، 4 مرتجع.

## 10. قائمة أخطاء الأعمال التي يجب أن تفهمها الواجهة

| code | معالجة الواجهة |
|---|---|
|`maintenance.insufficient_stock`|أظهر الرصيد الحالي وحدّثه؛ لا تسمح بتجاوز المخزون.|
|`maintenance.oil_barrel_not_next_fifo`|حدّث قائمة البراميل واختر أقدم برميل مؤهل فقط.|
|`maintenance.open_oil_barrel_required`|انتقل لاختيار/فتح البرميل حسب الرسالة، ولا تسجل تغيير زيت جزئياً.|
|`maintenance.oil_loss_allowance_exceeded`|احسب الحد المعروض `maximumAllowedLossLiters - recordedLossLiters`، ولا تتجاوز 2%.|
|`maintenance.invalid_bill_file`|اطلب PDF/صورة معتمدة أقل من 10MB.|
|`maintenance.invalid_oil_filter`|إذا `oilFilterChanged=true` فالفلتر إلزامي؛ وإذا false فاجعله null.|
|`maintenance.invalid_odometer`|اطلب قراءة أحدث من/تساوي القراءة الحالية للمركبة.|
|`maintenance.invalid_location`|أعد تحميل الموقع واختر موقعاً يسمح بالعملية.|
|`maintenance.invalid_state`|أعد تحميل أمر العمل؛ لا تُكمل انتقال حالة قديم.|
|`maintenance.concurrency_conflict`|أعد التحميل وخذ `rowVersion` الجديد قبل الحفظ.|

## 11. ترتيب الشاشات المقترح

1. **إعدادات:** المواقع، الأصناف، الموردون، خطط الصيانة.
2. **المخزون:** الفواتير مع مرفقاتها، الأرصدة، FIFO، براميل الزيت، النقل والمرتجعات وصرف الراكب.
3. **الصيانة:** تذكيرات الزيت، أوامر الشركة، تفاصيل العمل، المواد، تاريخ المركبة/الراكب.
4. **ورشة الرياض:** إنشاء مركبة خارجية مختصرة، بيع قطع، أجرة العميل، دفعة الميكانيكي، تحصيل العميل، تقرير الربح.

بعد أي عملية مؤثرة في المخزون، لا تعتمد على optimistic decrement محلياً وحده: أعد جلب `balances` و`oil-barrels` وبيانات أمر العمل. هذا مهم خصوصاً عندما يستخدم موظفان نفس القطعة أو البرميل في الوقت نفسه.
