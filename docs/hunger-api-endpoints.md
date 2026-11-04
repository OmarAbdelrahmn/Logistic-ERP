# هنقرستيشن — عقود API

تنفيذ Backend فقط. المسار الأساسي `/api/hunger`. يلزم تفعيل `Features:Hunger`؛ المسارات المالية تحتاج أيضًا `Features:HungerFinance`. المصادقة والصلاحيات ونطاق منصة HUNGER تتحقق خادميًا. الأخطاء المتوقعة تستخدم ProblemDetails، والتزامن/المعاينة القديمة يعيدان 409.

## الحسابات والتأسيس

| Method | المسار | الوظيفة |
|---|---|---|
| GET | `/accounts?disposition=&page=1&pageSize=50` | الحسابات، صاحب الحساب، المستخدم الوارد في المصدر والمستخدم المكلف فعليًا كحقول مستقلة |
| POST | `/accounts/imports/preview` | multipart: `file`؛ مطابقة Sheet1 بالإقامة ورقم معرف المنصة |
| POST | `/accounts/imports/{previewId}/confirm` | اعتماد أصحاب الحسابات وبيانات المصدر؛ لا يفتح تكليفات مالية تلقائيًا |
| PUT | `/accounts/{accountId}/disposition` | `disposition, reason, deferredUntil, rowVersion` |

التصنيفات: `Normal`, `Problem`, `AdministrativeStop`, `Blocked`, `DeferredNextMonth`. التفعيل يدوي. إيقاف الحساب لا يمحو تكليفًا أو ينشئ حركة مالية. راجع [قواعد الاستيراد](hunger-import-rules.md).

## الجودة والتشغيل

| Method | المسار | المدخلات الأساسية |
|---|---|---|
| GET | `/quality/template` | تنزيل قالب الجودة التراكمي |
| POST | `/quality/imports/preview` | multipart: `file, reportDate` |
| POST | `/quality/imports/{previewId}/confirm` | اعتماد الصفوف الصحيحة وإعادة توازن السجل التالي |
| GET | `/quality/imports/{importId}` | نتيجة الجودة المعتمدة للمستخدم المخول |
| POST | `/quality/{sourceId}/reallocate` | `rowVersion, reason`؛ إعادة تخصيص بعد استكمال أدلة الاستخدام |
| GET | `/reports/quality` | `month, accountId?, riderId?, page, pageSize` |
| GET | `/reports/quality/export` | `month, accountId?`؛ كامل النطاق المسموح |
| GET | `/operations/template` | تنزيل قالب التشغيل اليومي |
| POST | `/operations/imports/preview` | multipart: `file, reportDate` |
| POST | `/operations/imports/{previewId}/confirm` | اعتماد دفعة التشغيل السليمة |
| GET | `/reports/operations` | `from, through, accountId?, riderId?, page, pageSize`؛ فترة حتى سنة |
| GET | `/reports/operations/export` | `from, through, accountId?` |
| PUT | `/operations/{sourceId}/manual` | `orders?, hours?, reason, rowVersion`؛ المقارنة دون تغيير المصدر |
| GET | `/reports/summary` | `month`؛ المخزون والتكليف والتصنيف والأفراد بأبعاد منفصلة |
| GET | `/alert-rules` | سجل نسخ قواعد المتابعة |
| POST | `/alert-rules` | `platformId, code, threshold, severity, effectiveFrom, enabled, evidenceReference` |

`month` تاريخ ISO مثل `2026-11-01`. حجم الصفحة 1–100. سجلات الأداء تمثل مصدر المعرف وتعرض `allocations` مستقلة؛ فلتر المندوب يحدد حصص الإجماليات، ولا يحول لقطة تصنيف المعرف إلى تقييم شخصي. تصنيف الجودة A=1 وB=2 وهكذا. التقرير الشهري يجمع الفروق، ويعرض أحدث لقطة حسب تاريخ التقرير لكل حساب ظاهر في الصفحة.

قواعد التنبيه المتاحة: `HoursCritical` (افتراضي أقل من 8)، `HoursFollowUp` (أقل من 10)، `NoShowPositive` (أكبر من 0). الدرجات `Critical/Warning/Info`. النسخ مؤرخة؛ NoShow تنبيه متابعة محتمل وليس غرامة. الصفر معروف، وغياب المعلومة ليس صفرًا.

## المحافظ والتوريد

| Method | المسار | الوظيفة |
|---|---|---|
| PUT | `/finance/configuration` | اعتماد ربط حسابات المشرف: `platformId, supervisorEmployeeId, sponsorId, custodySubAccountId, collectionsSubAccountId, walletSubAccountId, riderClearingSubAccountId, evidenceReference, isActive, rowVersion?` |
| POST | `/finance/events` | واقعة مالية موثقة بمفتاح منع تكرار |
| GET | `/finance/events` | `platformId, supervisorEmployeeId, page, pageSize`؛ الوقائع ورصيد العهدة والمعلق |
| POST | `/finance/remittances` | `platformId, supervisorEmployeeId, sources:[{sourceEventId,amount}], idempotencyKey, businessDate, evidenceReference` |
| POST | `/finance/remittances/{remittanceId}/confirm` | `destinationSubAccountId, sources?, idempotencyKey, businessDate, evidenceReference`؛ حذف sources يؤكد كل المتبقي |
| POST | `/accounts/{accountId}/wallet-review` | تحليل الرصيد وربطه بطلب التسليم أو الاستلام |
| POST | `/wallet-reviews/{reviewId}/confirm` | تنفيذ التكليف وواقعة المحفظة في معاملة واحدة |
| POST | `/finance/settlement-documents` | تسجيل أدلة `Invoice/MonthClose/Adjustment` ومطابقتها؛ لا يعيد قيد سلف |
| GET | `/finance/settlement-documents` | `platformId, month, accountId?, page, pageSize` |

أنواع `/finance/events`: `Funding`, `TopUpAdvance`, `TopUpCollected`, `Collection`, `WithdrawalCompany`, `WithdrawalSupervisor`, `RiderPayment`. الطلب يحتوي `platformId, supervisorEmployeeId, kind, amount, accountId?, riderProfileId?, counterSubAccountId?, relatedEventId?, businessDate, idempotencyKey, evidenceReference`. مبالغ تحويل الأموال موجبة؛ إشارة **رصيد المحفظة** مستقلة عنها. التحصيل/الصرف يرتبط بواقعة ذمة صحيحة؛ لا يتجاوز المتبقي. التوريد لا يعيد تمويل العهدة تلقائيًا.

طلب مراجعة المحفظة:

```json
{
  "action": "Deliver",
  "riderProfileId": "RIDER_GUID",
  "supervisorEmployeeId": "SUPERVISOR_EMPLOYEE_GUID",
  "businessDate": "2026-11-01",
  "mobileBalance": "-100.00",
  "closingBalance": 0,
  "provenAdjustments": 0,
  "walletEvidence": "وثيقة/مرجع المصدر",
  "closingEvidence": null,
  "adjustmentEvidence": null,
  "assignment": {
    "actualRiderProfileId": "RIDER_GUID",
    "effectiveFrom": "2026-11-01",
    "reason": "تسليم حساب هنقر",
    "wasBackdated": true,
    "backdatedReason": "تأسيس موثق من الملف",
    "compensationModel": "Commission"
  }
}
```

القيم الموجبة والسالبة والصفر مقبولة في التسليم والاستلام دون استثناء خاص للإشارة. يمكن إرسال الرصيد عددًا أو نصًا موقعًا مثل `"+100.00"` و`"-100.00"`. عند `Return` أرسل `return:{effectiveTo,status:"Ended",reason,rowVersion}` بدل `assignment`، واجعل المندوب مطابقًا لحامل التكليف. استكمل تقسيم اليوم من `assignment-context` و`previousDayUsages` وفق الآلية المشتركة؛ لا يلزم إدخال حقول الجودة عند التسليم.

`CurrentPeriodBalance = MobileBalance - (ClosingBalance + ProvenAdjustments)` ثم تعزل الحقوق المحجوزة. الإغلاق والتسويات غير الصفرية يجب أن تطابق مستندات معتمدة. التسليم الموجب يسجل سلفة، والتسليم السالب رصيدًا لصالح المستلم؛ الاستلام الموجب رصيد لصالح السابق، والسالب التزام عليه. لا يغيّر API الرصيد لدى هنقر خارجيًا.

## المكونات المشتركة لكل المنصات

| Method | المسار | المدخلات |
|---|---|---|
| GET | `/api/platform-accounts/{accountId}/business-context` | `from, through`؛ سياق المركبة والشريحة وتاريخ الملكية |
| PUT | `/api/platform-accounts/{accountId}/business` | حقول الاتصال والسجل والتسوية والمركبة المسجلة، و`effectiveFrom, reason, rowVersion` |
| POST | `/api/platform-accounts/{accountId}/credentials/reveal` | `reason`؛ صلاحية مستقلة، no-store، وتدقيق دون السر |
| PUT | `/api/platform-accounts/sim-cards/{simId}/ownership` | `ownership, evidenceReference, effectiveFrom, rowVersion` |

تعديل أي مجموعة حقول يتطلب صلاحيتها المستقلة؛ طلب يضم تغييرًا غير مخول يرفض كاملًا. المركبة المسجلة لا تعاد كتابتها من عهد الحركة، وملكية الشريحة لا تستنتج من اسم المسؤول. الربط غير المثبت يبقى Unknown.
