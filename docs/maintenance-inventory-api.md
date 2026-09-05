# واجهات الصيانة والمخزون وورشة الرياض

هذه الواجهات تنفذ قواعد [خطة الصيانة والمخزون V2](vehicle-maintenance-v2-plan.md). جميع المسارات تحت `/api` وتتطلب JWT والصلاحية المبينة على الـ endpoint.

## المواقع الافتراضية

- `JEDDAH_WAREHOUSE` — مستودع جدة: مركبات الشركة فقط، مع مخزون.
- `RIYADH_WORKSHOP` — ورشة الرياض: مركبات الشركة والخارجية، بيع قطع، إصلاح مدفوع، ومخزون.

يمكن قراءة المواقع من `GET /api/maintenance-locations`. لا تستقبل أوامر الصيانة أي `HousingId`.

## إدخال فاتورة شراء مع الملف

`POST /api/maintenance-inventory/receipts` يستخدم `multipart/form-data` ويشترط جزأين:

- `ReceiptJson`: بيانات الفاتورة والسطور بصيغة JSON.
- `BillFile`: ملف الفاتورة، PDF أو صورة، من 1 بايت إلى 10 MB.

مثال `ReceiptJson` لبرميل زيت واحد:

```json
{
  "supplierId": "00000000-0000-0000-0000-000000000001",
  "supplierInvoiceNumber": "INV-1001",
  "invoiceDate": "2026-09-05",
  "receivedAtUtc": "2026-09-05T09:00:00+03:00",
  "inventoryLocationId": "019d77f0-0000-7000-8000-000000000004",
  "currencyCode": "SAR",
  "lines": [
    {
      "inventoryItemId": "00000000-0000-0000-0000-000000000002",
      "purchaseUnit": 3,
      "packageCount": 1,
      "declaredQuantityPerPackage": 208,
      "grossWeightKg": 225,
      "netWeightKg": 208,
      "packageUnitPrice": 1040,
      "discountAmount": 0,
      "taxAmount": 156,
      "lotNumber": "OIL-2026-09",
      "expiryDate": null
    }
  ]
}
```

القيمة `purchaseUnit = 3` تعني `Barrel`. يحسب النظام 208 لترًا بسعر مخزون 5 ريالات/لتر، وينشئ طبقة FIFO وبرميلًا مستقلًا، ويعيد البراميل المنشأة في `oilBarrels` ضمن رد الاستلام. ملف الفاتورة لا ينجح الاستلام بدونه، ويمكن تنزيله للمصرح له فقط من:

`GET /api/maintenance-inventory/receipts/{id}/bill-file`

## إدارة براميل الزيت

- `GET /api/maintenance-inventory/oil-barrels` يعيد السعة والمتبقي وحالة البرميل وحد الفقد.
- `POST /api/maintenance-inventory/oil-barrels/{id}/open` يفتح البرميل باستخدام `RowVersion`.
- لا يوجد إلا برميل مفتوح واحد للصنف داخل الموقع. إذا كان هناك برميل مفتوح لم ينفد، يعيد الرد `opened = false` و`hasPreviousBarrelWarning = true` و`previousOpenBarrelsRemainingLiters`، ولا يغير حالة البرميل المختار. بعد صرف 200 لتر من 208، تكون القيمة 8 لترات.
- عندما يصل الحالي إلى صفر، يختار المستخدم أي برميل مقفل صالح من أقدم طبقة FIFO ويفتحه. الاختيار من طبقة أحدث يُرفض حتى تنفد الأقدم.
- كل عنصر في القائمة يعرض `nominalCapacityLiters`, `consumedLiters`, `remainingLiters`, `unitCostPerLiter`, و`remainingInventoryValue`، لذلك تظل الأحجام وأسعار اللتر المختلفة واضحة.
- `POST /api/maintenance-inventory/oil-barrels/{id}/losses` يسجل الفقد الفعلي وحركة المخزون. الحد التراكمي هو 2% من السعة؛ أي 4.160 لتر لبرميل 208 لتر.
- لا يخصم النظام 2% مقدمًا؛ المتبقي المعروض هو المتبقي الفعلي.

مثال فتح برميل:

```json
{
  "openedAtUtc": "2026-09-05T10:00:00+03:00",
  "rowVersion": "<base64-row-version>"
}
```

مثال تسجيل فقد:

```json
{
  "occurredAtUtc": "2026-09-30T17:00:00+03:00",
  "quantityLiters": 2.2,
  "reason": "فاقد تشغيل مثبت عند جرد البرميل",
  "rowVersion": "<base64-row-version>"
}
```

## أوامر الصيانة والزيت

- أوامر مركبات الشركة: `POST /api/maintenance-work-orders`.
- أوامر المركبات الخارجية المختصرة: `POST /api/maintenance-work-orders/external`.
- صرف قطعة/مادة: `POST /api/maintenance-work-orders/{id}/materials`.
- عكس صرف: `POST /api/maintenance-work-orders/materials/{usageId}/reverse`.
- إكمال تغيير الزيت: `POST /api/maintenance-work-orders/{id}/oil-change`.
- التذكيرات: `GET /api/maintenance/oil-reminders`.

إكمال تغيير الزيت يرحل الزيت والفلتر في transaction واحدة، يحدث عداد المركبة، وينسب التكلفة إلى المركبة والرايدر صاحب العهدة وقت التنفيذ. السيارة تستهلك 3.5 لتر دون فلتر أو 4 لترات مع فلتر واحد؛ كمية الدراجة تأتي من إعداد الصنف/الخطة. إذا كانت الكمية ستنفد البرميل المفتوح وتتطلب جزءًا من التالي، يرسل العميل `nextOilBarrelId` لاختيار البرميل الذي سيفتحه النظام بعد وصول الحالي إلى صفر.

## بيع القطع والمصنعية والمكسب

هذه المسارات تعمل لأمر مركبة خارجية في موقع يسمح بالخدمة الخارجية، والـ seed الافتراضي الذي يسمح بها هو ورشة الرياض:

- بيع القطعة: `POST /api/maintenance-work-orders/{id}/part-sales`.
- مصنعية محصلة من العميل: `POST /api/maintenance-work-orders/{id}/customer-labor-charges`.
- أجر مدفوع للميكانيكي: `POST /api/maintenance-work-orders/{id}/mechanic-labor-payments`.
- دخل/مصروف آخر: `POST /api/maintenance-work-orders/{id}/other-financial-entries?income=true|false`.
- تحصيل العميل: `POST /api/maintenance-work-orders/{id}/customer-payments`.
- تقرير المكسب: `GET /api/maintenance/external-profit?maintenanceLocationId={id}&startDate=2026-09-01&endDate=2026-09-30`.

المعادلات قبل الضريبة:

```text
ربح القطع = سعر بيع القطع - تكلفة FIFO
ربح المصنعية = مصنعية العميل - أجر الميكانيكي
صافي المكسب = ربح القطع + ربح المصنعية + الدخل الآخر - المصروف الآخر
```

الضريبة والتحصيل والمتبقي على العميل تظهر منفصلة ولا تضخم المكسب.
