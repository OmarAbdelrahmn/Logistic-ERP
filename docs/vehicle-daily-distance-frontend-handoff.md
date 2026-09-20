# نظام المسافة اليومية للمركبات — تسليم الواجهة الأمامية

## الهدف

يوفر النظام سجلًا واحدًا لكل مركبة ولكل يوم يجمع بين مصدرين:

- مسافة GPS اليومية المستوردة من تقرير Excel.
- قراءة العداد اليدوية الإجمالية التي يدخلها المستخدم للمركبة.

يحسب النظام المسافة اليدوية لليوم تلقائيًا من الفرق بين قراءة العداد المدخلة والعداد التشغيلي الفعلي في بداية اليوم، شاملاً مسافات GPS السابقة. إذا توفرت مسافة GPS فهي المصدر المعتمد، وإذا لم تتوفر يستخدم النظام المسافة اليدوية المحسوبة.

## قواعد الحساب

1. `gpsDistanceKm` هي مسافة اليوم فقط، وتقبل منزلتين عشريتين.
2. `manualOdometerReading` هي قراءة عداد المركبة الإجمالية وليست مسافة اليوم.
3. `manualDistanceKm = manualOdometerReading - manualBaselineOdometerReading`.
4. `appliedDistanceKm = gpsDistanceKm` عند توفر GPS.
5. إذا لم يتوفر GPS: `appliedDistanceKm = manualDistanceKm`.
6. `vehicleTrackedDistanceKm` هو العداد الفعلي التشغيلي الدقيق للمركبة بعد إضافة المسافة المعتمدة مرة واحدة فقط، ويحتفظ بالكسور العشرية القادمة من GPS.
7. `currentOdometer` في سجل المركبة يتزامن تلقائيًا مع الجزء الصحيح من `vehicleTrackedDistanceKm` حتى تستخدم الخدمات القديمة والواجهة الرئيسية نفس الكيلومترات.
8. `effectiveOdometerAfterKm` هو عداد المركبة بعد تطبيق سجل اليوم المحدد، ويستخدم لعرض التسلسل اليومي ومراجعته.
9. عند إدخال قراءة عداد مؤكدة من المركبة أو التسليم/الاستلام أو الصيانة، تصبح القراءة نقطة أساس جديدة لكل من العداد الرئيسي والإجمالي التشغيلي.
10. إذا أُدخلت قراءة يدوية أولًا ثم رُفع GPS لنفس اليوم، يعاد اعتماد ذلك اليوم على GPS ولا يجمع المصدرين معًا، مع الاحتفاظ بالقيمة اليدوية وبيانات مدخلها للمراجعة.
11. عند رفع GPS ليوم سابق أو تصحيح مسافته، يعاد حساب ذلك اليوم وكل الأيام اللاحقة للمركبة؛ لا تضاف المسافة مرتين.
12. المسافة اليدوية تحسب من العداد التشغيلي الفعلي قبل اليوم، بما في ذلك أيام GPS السابقة، وليس من آخر قراءة يدوية فقط.

مثال: قراءة الأساس 10,000 وقراءة اليوم اليدوية 10,164، إذًا المسافة اليدوية 164 كم. إذا كان GPS لليوم 150 كم، تصبح المسافة المعتمدة 150 كم ويزيد الإجمالي التشغيلي بمقدار 150 كم فقط.

## صفحة الواجهة المقترحة

المسار المقترح: **الأسطول ← المسافات اليومية**.

رأس الصفحة:

- عنوان: `المسافات اليومية للمركبات`.
- محدد تاريخ إلزامي، والقيمة الافتراضية يوم أمس بتوقيت الرياض.
- زر رئيسي: `رفع تقرير GPS`.
- حقل بحث برقم الأصل أو اللوحة.
- مرشحات: `الكل`، `GPS`، `يدوي`، `بدون مسافة`.

بطاقات الملخص:

- مركبات GPS من `gpsCount`.
- البديل اليدوي من `manualFallbackCount`.
- بدون مسافة من `missingCount`.
- مجموع اليوم من `appliedTotalKm`.

أعمدة الجدول:

- رقم الأصل.
- اللوحة العربية / الإنجليزية.
- مسافة GPS.
- قراءة العداد اليدوية الإجمالية.
- قراءة الأساس.
- المسافة اليدوية المحسوبة.
- المسافة المعتمدة.
- المصدر المعتمد.
- إجمالي المركبة التشغيلي.
- العداد بعد هذا اليوم.
- وقت ومستخدم الإدخال، ورقم عملية استيراد GPS عند وجوده.
- إجراء `إدخال/تعديل القراءة اليدوية`.

استخدم شارات واضحة للمصدر: GPS، يدوي، بدون مسافة. لا تعتمد على اللون وحده.

## واجهات API

### جلب سجل يوم

`GET /api/vehicle-daily-distances?workDate=2026-08-31&search=&source=&page=1&pageSize=100`

قيم `source`: `gps` أو `manual` أو `missing`، أو اتركها فارغة لعرض الكل.

الاستجابة:

```json
{
  "items": [
    {
      "id": "daily-record-id-or-null",
      "vehicleId": "vehicle-id",
      "workDate": "2026-08-31",
      "assetNumber": "VEH-0001",
      "plateNumberAr": "أ ط س 1098",
      "plateNumberEn": "1098 ATS",
      "currentOdometer": 10150,
      "vehicleTrackedDistanceKm": 10150.00,
      "gpsDistanceKm": 150.00,
      "manualOdometerReading": 10164,
      "manualBaselineOdometerReading": 10000,
      "manualDistanceKm": 164.00,
      "appliedDistanceKm": 150.00,
      "appliedSource": "Gps",
      "effectiveOdometerAfterKm": 10150.00,
      "gpsImportedAtUtc": "2026-09-01T05:00:00Z",
      "lastGpsImportId": "gps-import-id",
      "gpsImportedByUserId": "user-id",
      "manualEnteredAtUtc": "2026-09-01T06:00:00Z",
      "manualEnteredByUserId": "user-id",
      "manualNotes": null,
      "rowVersion": "base64-row-version"
    }
  ],
  "workDate": "2026-08-31",
  "page": 1,
  "pageSize": 100,
  "totalCount": 285,
  "gpsCount": 211,
  "manualFallbackCount": 20,
  "missingCount": 54,
  "appliedTotalKm": 50380.77
}
```

تسلسل enum للمصدر: `None = 0`، `Manual = 1`، `Gps = 2`. إعداد JSON الحالي قد يعيد الرقم بدل الاسم؛ دعم الحالتين في الواجهة.

### معالجة الواجهة بعد الدمج

1. بعد نجاح `gps-import` استخدم `workDate` من الاستجابة ثم أعد طلب صفحة ذلك اليوم وبيانات المركبات المتأثرة؛ لا تعدّل العداد محليًا بعملية جمع في المتصفح.
2. في قائمة المركبات اعرض `trackedDistanceKm` باعتباره **الكيلومترات الحالية الدقيقة**. يمكن عرض `currentOdometer` كقيمة صحيحة متوافقة مع الشاشات القديمة، لكن لا تستخدمه للحسابات ذات الكسور.
3. في جدول اليوم اعرض `effectiveOdometerAfterKm` تحت عنوان **العداد بعد اليوم**.
4. اعرض المصدر من `appliedSource`: `2` أو `Gps` = GPS، و`1` أو `Manual` = يدوي، و`0` أو `None` = بدون مسافة.
5. عند وجود المصدرين في الصف، اعرض القيمتين مع شارة أن GPS هو المعتمد؛ لا تخفِ القراءة اليدوية.
6. اعرض `lastGpsImportId` كرابط أو معرف لعملية الرفع، واعرض `gpsImportedByUserId`/`gpsImportedAtUtc` أو `manualEnteredByUserId`/`manualEnteredAtUtc` في تفاصيل الصف.
7. بعد إدخال قراءة يدوية ناجحة استبدل الصف كاملًا باستجابة `PUT` ثم أعد تحميل ملخص الصفحة وقائمة المركبات، لأن إعادة الحساب قد تغيّر الأيام اللاحقة.
8. إذا عاد الخطأ `fleet.daily_distance.invalid_manual_odometer` فهذا يعني أن القراءة اليدوية أقل من العداد الفعلي الذي نتج عن الأيام السابقة. اعرض العداد الأساسي من الصف واطلب قراءة صحيحة أو تصحيحًا إداريًا.
9. لا تجمع `gpsDistanceKm` مع `manualDistanceKm`. استخدم دائمًا `appliedDistanceKm` للمجاميع و`effectiveOdometerAfterKm` للعداد المتسلسل.
10. بعد الرفع اعرض `matchedRows` و`unmatchedRows` و`invalidRows`. المركبات غير المطابقة لا تتغير كيلومتراتها.

### إدخال القراءة اليدوية

`PUT /api/vehicle-daily-distances/{vehicleId}/{workDate}`

```json
{
  "odometerReading": 10164,
  "baselineOdometerReading": 10000,
  "notes": "قراءة نهاية اليوم",
  "rowVersion": null
}
```

- `baselineOdometerReading` اختياري عندما يستطيع النظام إيجاد قراءة سابقة.
- أرسله عند أول قراءة أو عندما يعيد API الخطأ `fleet.daily_distance.manual_baseline_required`.
- أرسل `rowVersion` عند تعديل سجل موجود.
- لا تسمح الواجهة بقراءة حالية أقل من قراءة الأساس.

### رفع تقرير GPS

`POST /api/vehicle-daily-distances/gps-import`

الطلب `multipart/form-data`:

- `file`: ملف `.xls` أو `.xlsx` أو `.htm` أو `.html` أو `.zip` بحد أقصى 10 MB.
- `expectedWorkDate`: اختياري بصيغة `yyyy-MM-dd` لحماية المستخدم من رفع يوم خاطئ.

يدعم المستورد:

- Excel الحقيقي بصيغتي XLS وXLSX.
- تقارير HTML التي تحمل امتداد `.xls` مثل الملف المرفق من نظام GPS.
- صفحات Excel HTML المفردة، أو ملف ZIP يحتوي على ملف XLS ومجلد `.files` المرافق له.
- الأرقام العربية والإنجليزية والفارسية، وفواصل الآلاف والعلامة العشرية العربية أو الإنجليزية.
- ترتيب اللوحة سواء كانت الأرقام أولًا أو الحروف أولًا، مع مطابقة حروف اللوحات العربية والإنجليزية.
- القيم `لم يتم العثور على طلبك.` و`كيلومترا` بدون رقم كصفوف لا تحتوي GPS.

تعيد الاستجابة عدادات `matchedRows` و`unmatchedRows` و`invalidRows` وقائمة `errors`. بعد نجاح الرفع اعرض ملخصًا واضحًا، ثم أعد تحميل جدول اليوم.

### سجل عمليات الرفع

`GET /api/vehicle-daily-distances/gps-imports?workDate=2026-08-31`

يعيد آخر 100 عملية رفع مع اسم الملف، بصمة SHA-256، التاريخ، العدادات، المستخدم، ووقت الرفع.

## حالات الخطأ المهمة

- `fleet.daily_distance.invalid_gps_file`: بنية الملف غير صالحة.
- `fleet.daily_distance.gps_frameset_missing_sheet`: ملف XLS هو صفحة ربط والبيانات موجودة في مجلد `.files` المرافق؛ اطلب من المستخدم رفع `sheet001.htm` أو ZIP يحتوي الملف والمجلد، أو حفظ التقرير كـ XLSX.
- `fleet.daily_distance.gps_date_mismatch`: تاريخ التقرير لا يطابق التاريخ المختار.
- `fleet.daily_distance.duplicate_gps_import`: الملف نفسه مرفوع مسبقًا لليوم.
- `fleet.daily_distance.invalid_manual_odometer`: القراءة أقل من الأساس أو تكسر تسلسل القراءات اللاحقة.
- `fleet.daily_distance.manual_baseline_required`: لا توجد قراءة أساس تلقائية.
- `fleet.concurrency_conflict`: السجل عُدّل من مستخدم آخر؛ أعد تحميل الصف.

أخطاء الصفوف في نتيجة الاستيراد:

- `vehicle_not_found`: لا توجد مركبة مطابقة للوحة.
- `ambiguous_plate`: اللوحة تطابق أكثر من مركبة.
- `duplicate_plate`: اللوحة مكررة داخل الملف.
- `invalid_distance`: قيمة المسافة غير قابلة للقراءة.

## الصلاحيات

- `fleet.daily_distances.read`: عرض اليوم وسجل الرفع.
- `fleet.daily_distances.manage`: إدخال وتعديل القراءة اليدوية.
- `fleet.daily_distances.import`: رفع تقرير GPS.

## الترحيل والتشغيل

الترحيل: `20260902114323_AddVehicleDailyDistances`.

لا يتم تطبيق الترحيل تلقائيًا. بعد مراجعة إعداد الاتصال شغّل:

```powershell
dotnet ef database update --project src/LogisticsERP.Infrastructure/LogisticsERP.Infrastructure.csproj --startup-project src/LogisticsERP.Api/LogisticsERP.Api.csproj --context ApplicationDbContext
```
