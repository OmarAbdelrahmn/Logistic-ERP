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

تعيد عناصر قائمة الحسابات أيضًا `owner` و`actualRider` و`sourceActualRider`، ويحتوي كل شخص على `employeeId, riderProfileId, nameAr, nameEn, iqamaNo, phoneNumber`. يعرض `actualRider` التكليف النشط فقط؛ ويعرض `sourceActualRider` المستخدم المطابق في الملف حتى إذا لم يُنشأ التكليف. تتضمن العناصر معلومات المدينة والسجل والهاتف والتسوية والشريحة والمركبة المسجلة وبيانات التكليف وسبب الحالة. الحساب بلا تصنيف موثق يعاد `Unknown` بدل افتراض `Normal`.

حقول إضافية: `currentAssignmentId`, `assignmentFrom`, `assignmentRowVersion`, `compensationModel`, `paymentModel`, `accountCode`, `operatingCityId`, `operatingCityNameAr`, `sponsorId`, `sponsorNameAr`, `simOwnership`, `registeredVehiclePlate`, `registeredVehicleSerial`, `actualVehicleType`, `actualVehiclePlate`, `actualVehicleOwnership`, `sourceVehicleDescription`, `sourceAssignmentFrom`, `dispositionReason`, `monthlyUsageStatus`, `usageMonth`, `latestQualitySegment`, `latestQualityRank`, `latestQualityScore`, `latestQualityReportDate`, `hasPassword`, `lastAssignmentEvent`, `lastAssignmentEventAtUtc`. كائنات الأشخاص تضيف `nationality`, `employeeStatus`, `relationshipType`. لا تعاد كلمة المرور في القائمة.

يمكن أن تكون حالة استخدام الشهر `Used` بدليل تكليف فعلي (`monthlyUsageSource=Assignment`) أو بدليل مستورد مسجل في الشهر يشير إلى حساب يعمل وهوية مستخدم وتاريخ استلام وصل (`ImportedSource`). لا يلزم أن تكون هوية المستخدم مطابقة حتى تشير حالة التشغيل المثبتة في المصدر إلى الاستخدام؛ في هذه الحالة لا ينشأ ملف شخص تلقائيًا. الدليل المستورد لا ينشئ تكليفًا فعليًا؛ يظل `actualRider` و`assignmentStatus` معبرين عن بيانات التكليف الحقيقي، و`sourceActualRider` عن الملف. لا يتكرر استنتاج الاستخدام تلقائيًا في شهر لاحق دون دليل جديد؛ يعاد `Unknown` عندما لا يوجد دليل حديث أو تكليف للشهر.

`reportedCurrentRider` يعرض المندوب النشط المثبت، وإن لم يوجد يعرض مستخدم الملف للحساب العامل فقط. `currentRiderSource` يوضح `Assignment` أو `ImportedSource` أو `None`، ولا يغير أي تكليف أو واقعة مالية.

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


## Hunger wallet package v2.1

All endpoints below require `Hunger.FinanceManage` and the platform finance feature/access checks.

| Method | Path | Request / behavior |
|---|---|---|
| POST | `/api/hunger/accounts/{accountId}/wallet-review` | `closingBalance` is nullable. Response includes `closingBalance`, nullable `currentPeriodBalance`, `attributedBalance`, `riderBalanceChange`, and `canConfirm`. |
| POST | `/api/hunger/wallet-reviews/{reviewId}/confirm` | Returns a validation error when the prior close is unknown; record the close and create a fresh review. |
| POST | `/api/hunger/finance/settlement-documents` | Adjustment requires `sourceDocumentType`, `sourceReference`, `closingSnapshotId`; starts `Unmatched`. |
| POST | `/api/hunger/finance/settlement-documents/{documentId}/reconcile` | `{closingSnapshotId,evidenceReference}` explicitly matches the adjustment to its recorded month close. |
| POST | `/api/hunger/finance/events` | Withdrawal creates `PendingArrival`; see required fields below. |
| POST | `/api/hunger/finance/withdrawals/{withdrawalId}/confirm-arrival` | `{idempotencyKey,businessDate,evidenceReference}` records arrival evidence and posts the wallet-to-destination transfer once. |
| POST | `/api/hunger/finance/withdrawals/{withdrawalId}/cancel` | Same body, with cancellation evidence; allowed only while `PendingArrival`, releases reservations without treasury postings. |

A missing prior close is Unknown, even if the caller supplies zero. Zero is legitimate only when the account has no earlier assignments, usage, contributions, financial events, or settlement history. Existing negative-balance handover policy is unchanged.

An adjustment's `invoicePeriod` is the month being adjusted and must equal its linked MonthClose's `chargeFrom`. The economic source key is `(accountId, invoicePeriod, sourceDocumentType, sourceReference)`, independent of `idempotencyKey`. Reusing the source with a different key returns a conflict. Only `Matched` adjustments linked to the specific prior close and observed by the review date enter posted A. Capturing a source alone never matches it.

Withdrawal kinds remain `WithdrawalCompany` and `WithdrawalSupervisor`. In addition to the existing money fields, supply:

```json
{
  "sourceWalletBalance": 100.00,
  "destinationType": "Company",
  "destinationOwnerId": "SPONSOR_GUID",
  "counterSubAccountId": "COMPANY_BANK_SUBACCOUNT_GUID",
  "transferReference": "HS-transfer-123",
  "transferDate": "2026-11-01",
  "againstReservedRights": false
}
```

`sourceWalletBalance` captures the observed wallet snapshot at request time. `transferDate` must equal `businessDate`. Company ownership uses the configured Sponsor and a Bank/Central treasury account explicitly belonging to that Sponsor. Supervisor ownership uses the configured supervisor Employee and their approved Supervisor collections account (`destinationType: "Supervisor"`). `againstReservedRights: true` requires `relatedEventId` pointing to the positive WalletReturn being withdrawn; withdrawals cannot bypass reserved rights by omitting the flag/source. Pending requests reserve the source amount and linked rights; no destination treasury posting or remittance source exists until arrival. Cancellation preserves the audit record. Arrival/cancel retries with the same key and body return the existing outcome; conflicting terminal transitions fail.

Migration compatibility: historical withdrawals retain their already-posted behavior as `Arrived`. Historical adjustments are made `Unmatched` and excluded from posted A until their economic source and close are verified; recreate verified adjustments through the documented capture/reconciliation workflow. The migration is supplied for review, not applied to a database by this change.
