# نظام الحوادث والمطالبات — دليل الـAPI والربط

## نطاق التنفيذ

تبدأ الحالة برقم المرور، ثم رفع تقرير نجم وتسجيل نسب الخطأ لكل طرف. النظام يتابع المستندات والمبالغ ومراحل المطالبة والتواريخ والإشعارات واسترداد الأقساط. رفع المطالبة للتأمين أو شركة المركبة يتم خارج النظام، ثم يسجل المسؤول الرد والمستندات هنا. نسب نجم يدخلها المسؤول بعد مراجعة الـPDF؛ لا يوجد استخراج آلي/OCR أو اتصال مباشر بنجم أو التأمين.

المبالغ بالريال السعودي وبحد أقصى منزلتين عشريتين. النسب بين 0 و100 ومجموع نسبة المندوب وكل الأطراف الأخرى يجب أن يساوي 100 بالضبط. `Minor = 1` هو الحادث البسيط؛ `Moderate = 2`, `Serious = 3`, `Critical = 4` تدخل معاملة الحادث الكبير عند مسؤولية المندوب 100%.

## المسارات

| الحالة | المسار |
| --- | --- |
| خطأ المندوب 100% + حادث بسيط | تقييم الضرر + صورتان على الأقل + تكلفة الإصلاح + سند لأمر PDF بنفس المبلغ المسجل، ثم إثبات انتهاء الإصلاح |
| خطأ المندوب 100% + حادث غير بسيط | تسجيل دفع 2500 ريال وإرفاق سند فتح المطالبة PDF، ثم فتح المطالبة |
| خطأ المندوب أقل من 100% | فتح المطالبة مباشرة، بما فيها نسبة 0% أو 75% أو أي توزيع صحيح |

نوع المطالبة المطلوبة مستقل عن الرد: المطلوب `Repair = 1` أو `Compensation = 2`، والرد `Repair = 1` أو `Compensation = 2` أو `TotalLoss = 3`. يمكن طلب إصلاح ويأتي الرد تعويضًا.

التعويض: تقديم المطالبة ورقمها ← انتظار التقييم ← إيصال التعويض والمبلغ ← تسليم للتأمين ← انتظار 15 يومًا ← قبول مع إيصال سداد أو رفض مع السبب والمستند ← تسليم لشركة شراء المركبة ← انتظار 10 أيام مع المتابعة ← إثبات استلام التحويل لدى شركتنا ← المطالبة بالأقساط.

الإصلاح: تقديم المطالبة ← خطاب جهة الإصلاح ومكانها ووسيلة التواصل ← بدء الإصلاح ← تحديثات متابعة متكررة ← إثبات الانتهاء ← المطالبة بالأقساط.

الإتلاف: تقديم المطالبة ← اقتراح الإتلاف ← إعادة فحص اختيارية بموعد ومكان ← تأكيد الإتلاف ← استلام الجهة للمركبة ← التقييم المالي وإيصاله ← تسليم لشركة شراء المركبة ← إثبات استلام التحويل لدى شركتنا ← المطالبة بالأقساط. يمكن أن يؤدي الفحص إلى تحويل المسار للإصلاح.

## تسجيل الحادث

`POST /api/vehicle-accidents` يتطلب `Idempotency-Key` فريدًا لكل تسجيل. إعادة نفس الطلب بنفس المفتاح تعيد الحادث نفسه؛ تغيير محتواه مع المفتاح نفسه يسبب تعارضًا. الصلاحية تتحقق أيضًا عند إعادة الطلب.

```json
{
  "vehicleId": "<vehicle-guid>",
  "riderProfileId": "<rider-guid>",
  "occurredAtUtc": "2026-09-01T09:00:00Z",
  "locationDescription": "الرياض - موقع الحادث",
  "latitude": null,
  "longitude": null,
  "policeReportNumber": "TRAFFIC-123456",
  "insuranceClaimNumber": null,
  "severity": 3,
  "isDrivable": false,
  "hasInjuries": false,
  "injuryDetails": null,
  "thirdPartyDetails": null,
  "damageDescription": "وصف الضرر الظاهر",
  "faultAssessment": null,
  "narrative": "تفاصيل وقوع الحادث"
}
```

`policeReportNumber` إلزامي وبحد أقصى 150 حرفًا. `accidentNumber` رقم داخلي مستقل يولده النظام. تاريخ الحادث لا يكون في المستقبل، والمندوب يجب أن تكون له عهدة المركبة وقت وقوع الحادث. تسجيل حادث غير قابل للقيادة يحتفظ بسلوك إنهاء العهدة الموجود بالنظام.

## قائمة الحالات وتفاصيلها

| الطلب | الاستخدام |
| --- | --- |
| `GET /api/vehicle-accidents` | قائمة الحوادث الأصلية |
| `GET /api/vehicle-accidents/workflows` | قائمة متابعة المطالبات مع المرحلة، الرقم الخارجي، الشركة، الموعد، التأخير وحالة استرداد الأقساط |
| `GET /api/vehicle-accidents/{id}` | تفاصيل الحادث ومرفقاته وتقاريره الأصلية |
| `GET /api/vehicle-accidents/{id}/workflow` | تفاصيل دورة العمل والمستندات الأصلية والمدة والمؤقتات والسجل التاريخي |

قائمة المتابعة تدعم `vehicleId`, `riderProfileId`, `stage`, `overdueOnly`, `page`, `pageSize`. مثال: `GET /api/vehicle-accidents/workflows?stage=7&overdueOnly=true&page=1&pageSize=50`. `stage` رقم من الجدول التالي. الحد الأقصى للصفحة 200.

تفاصيل دورة العمل مقسمة إلى `fault`, `claim`, `settlement`, `repair`, `refund`, `sourceDocuments`, `attachments`, `timeline`. تشمل `incidentStartedAtUtc`, `incidentEndedAtUtc`, `incidentCalendarDays`, `deadlineAtUtc`, `remainingSeconds`, `isOverdue`.

| القيمة | المرحلة |
| --- | --- |
| 1 AwaitingNajm | انتظار تقرير نجم والنسب |
| 2 Assessed | تم تحديد المسؤولية |
| 3 LocalRepair | إصلاح بسيط على المندوب |
| 4 ClaimDraft | إعداد المطالبة |
| 5 AwaitingAssessment | تم التقديم وانتظار التقييم |
| 6 CompensationOffered | وصل عرض التعويض |
| 7 AwaitingInsurance | انتظار رد التأمين |
| 8 InsuranceRejected | رفض التأمين، ويمكن إعادة التقديم |
| 9 InsuranceApproved | قبول التأمين |
| 10 AwaitingSupplierTransfer | انتظار التحويل من شركة شراء المركبة |
| 11 RepairDirected | تحددت جهة الإصلاح |
| 12 Repairing | الإصلاح جارٍ |
| 13 TotalLossProposed | اقتراح الإتلاف |
| 14 AwaitingReinspection | انتظار إعادة الفحص |
| 15 TotalLossConfirmed | تأكد الإتلاف |
| 16 AwaitingValuation | تم استلام المركبة وانتظار التقدير المالي |
| 17 TotalLossValued | تم تسجيل التقدير المالي |
| 18 Completed | اكتمل المسار الأساسي؛ قد يبقى طلب الأقساط |

`VehicleAccidentStatus` الأصلي (`Reported`, `Finalized`, `Closed`) يظل مستقلًا عن هذه المراحل. اعتماد التقرير PDF ليس معناه انتهاء المطالبة.

## تنفيذ خطوة

`POST /api/vehicle-accidents/{id}/workflow/actions`

كل خطوة ترسل `action`, `rowVersion`, `notes`, `occurredAtUtc`. استخدم **rowVersion الخاص بالـworkflow** من آخر رد. `notes` إلزامي حتى 1000 حرف. وقت الخطوة بين وقوع الحادث والوقت الحالي ولا يسبق آخر خطوة. `reference` حتى 150 حرفًا، `location` حتى 1000، و`contact` حتى 300. التعديل المتزامن يرجع تعارضًا؛ أعد تحميل الحالة قبل المحاولة.

الأرقام التالية هي قيم الـenum المرسلة في JSON، وأسماء الإجراءات موضحة لتسهيل الربط:

| action | الإجراء | البيانات الإضافية المطلوبة |
| --- | --- | --- |
| 1 AssessFault | مراجعة نجم | `attachmentId` لتقرير نجم + `riderFaultPercentage` + `otherParties` |
| 2 StartLocalRepair | بدء إصلاح بسيط | `amount` تكلفة الإصلاح، `attachmentId` سند لأمر؛ صورتا ضرر على الأقل مرفوعتان مسبقًا؛ تقييم الضرر في `notes` |
| 3 CompleteLocalRepair | انتهاء الإصلاح البسيط | `attachmentId` لإثبات انتهاء الإصلاح |
| 4 OpenClaim | فتح المطالبة | `claimType`؛ `supplierId` أو شركة الشراء المرتبطة بالمركبة. عند مسؤولية 100% غير بسيطة: `amount=2500` + سند دفع فتح المطالبة |
| 5 SubmitClaim | تسجيل تقديم المطالبة | `reference` رقم المطالبة + `attachmentId` تقرير تسليمها؛ توفر ملفات الإقامة والرخصة والاستمارة |
| 6 ReceiveCompensationOffer | تسجيل رد التعويض | `amount` + `attachmentId` إيصال التقييم |
| 7 SubmitToInsurance | تسليم للتأمين | يسجل التاريخ ويبدأ مهلة 15 يومًا؛ الإيصال محفوظ بالخطوة السابقة |
| 8 ApproveInsurance | قبول التأمين | `attachmentId` إيصال السداد |
| 9 RejectInsurance | رفض التأمين | `attachmentId` مستند القرار، والسبب في `notes` |
| 10 SubmitToSupplier | تسليم لشركة شراء المركبة | `attachmentId` إثبات تسليم من نوع ClaimSubmissionReport؛ يبدأ مؤقت 10 أيام |
| 11 ConfirmTransfer | تأكيد وصول المبلغ لشركتنا | `amount` يساوي قيمة التسوية + `attachmentId` إيصال التحويل |
| 12 ReceiveRepairDirection | استلام تعليمات الإصلاح | `location`, `contact`, `attachmentId` خطاب الإصلاح |
| 13 StartRepair | بدء الإصلاح | بيانات الخطوة المشتركة |
| 14 RepairProgress | تحديث الإصلاح | `notes`؛ `attachmentId` اختياري |
| 15 CompleteRepair | إتمام الإصلاح | `attachmentId` إثبات الانتهاء |
| 16 ProposeTotalLoss | استلام رد الإتلاف | `attachmentId` تقرير التقييم |
| 17 RequestReinspection | طلب إعادة الفحص | `location`, `appointmentAtUtc` لا يسبق وقت الخطوة |
| 18 ConfirmTotalLoss | تأكيد الإتلاف | `attachmentId` تأكيد الإتلاف النهائي |
| 19 RecordVehicleCollection | استلام الجهة للمركبة | `attachmentId` محضر الاستلام |
| 20 RecordValuation | التقدير المالي للمركبة | `amount` + `attachmentId` إيصال التقدير |
| 21 FollowUp | متابعة عامة | `notes`؛ `attachmentId`, `amount`, `reference` اختيارية وتسجل في التاريخ |
| 22 SubmitInstallmentRefund | تقديم طلب استرداد الأقساط | `reference`, `amount` يساوي إجمالي المبلغ المؤهل المسجل + `attachmentId` طلب الاسترداد |
| 23 ReceiveInstallmentRefund | تسجيل استلام الاسترداد | `amount` موجب ولا يتجاوز المطلوب + `attachmentId` إيصال؛ توضيح أي خصم في الملاحظات |
| 24 RejectInstallmentRefund | رفض استرداد الأقساط | `attachmentId` قرار PDF من نوع InsuranceDecision + السبب في `notes` |
| 25 MarkNoInstallments | لا توجد أقساط للمطالبة بها | سبب صريح في `notes`، ولا توجد أقساط مسجلة أو مطالبة سبق تقديمها |

الخطوات التي تغير المسار لا تقبل الانتقال خارج ترتيبها. إعادة التقديم للتأمين من مرحلة الرفض مسموحة وتبدأ مهلة جديدة. `FollowUp` متاح في كل مراحل الملف المفتوح، ويصلح لتسجيل متابعة التحويل أو اتصال بالشركة أو مبلغ جزئي؛ `ConfirmTransfer` يحسم وصول إجمالي التسوية.

مثال تسجيل نسب نجم:

```json
{
  "action": 1,
  "rowVersion": "<latest-workflow-rowVersion>",
  "notes": "تمت مراجعة تقرير نجم",
  "occurredAtUtc": "2026-09-02T09:00:00Z",
  "attachmentId": "<najm-attachment-guid>",
  "riderFaultPercentage": 75,
  "otherParties": [
    {
      "name": "الطرف الثاني",
      "faultPercentage": 25,
      "vehiclePlate": "ABC 1234",
      "insuranceCompany": "اسم شركة التأمين"
    }
  ]
}
```

يمكن تسجيل أكثر من طرف. عند 100% على المندوب يمكن إرسال قائمة فارغة أو أطراف بنسب صفر. تعديل النسب متاح قبل بدء الإصلاح أو فتح المطالبة فقط، وكل تعديل يحتفظ بسجله التاريخي.

مثال فتح مطالبة لحادث كبير بنسبة 100%:

```json
{
  "action": 4,
  "rowVersion": "<latest-workflow-rowVersion>",
  "notes": "تم دفع سند فتح المطالبة",
  "occurredAtUtc": "2026-09-02T10:00:00Z",
  "claimType": 1,
  "supplierId": "<vehicle-supplier-guid>",
  "amount": 2500,
  "attachmentId": "<opening-fee-pdf-guid>"
}
```

## المرفقات والسطحات

`POST /api/vehicle-accidents/{id}/workflow/attachments` بصيغة `multipart/form-data`:

- `file`: الملف.
- `evidenceType`: الرقم من الجدول أدناه.
- `description`, `fromLocation`, `toLocation`, `transportedAtUtc`, `amount`: بيانات إيصال السطحة. جميعها مطلوبة للسطحات، واختيارية لغيرها.

الملف الواحد حتى 10 MiB. الملفات الرسمية والسندات PDF؛ الصور يجب أن تكون من أنواع الصور المقبولة في التخزين. يتم فحص امتداد الملف ونوعه وبدايته الفعلية، وحساب SHA-256. لا يوجد حد ثابت لعدد مرفقات الحادث أو السطحات.

| evidenceType | النوع |
| --- | --- |
| 1 Image | صورة عامة |
| 2 UploadedReport | تقرير مرفوع |
| 3 Other | ملف آخر ضمن الأنواع المسموحة |
| 4 NajmReport | تقرير نجم PDF |
| 5 DamagePhoto | صورة الضرر |
| 6 DamagePromissoryNote | سند لأمر بتكلفة الضرر |
| 7 ClaimOpeningFeeReceipt | سند دفع فتح المطالبة |
| 8 ClaimSubmissionReport | تقرير تقديم/تسليم المطالبة، ويشمل إثبات تسليم الشركة |
| 9 AssessmentReceipt | إيصال التقييم/عرض التعويض/اقتراح الإتلاف |
| 10 InsuranceDecision | قرار القبول/الرفض أو رفض استرداد الأقساط |
| 11 PaymentReceipt | إيصال السداد الصادر بعد موافقة التأمين |
| 12 TransferReceipt | إثبات وصول التحويل لشركتنا |
| 13 RepairDirection | خطاب جهة الإصلاح |
| 14 RepairCompletion | إثبات انتهاء الإصلاح |
| 15 ReinspectionReport | تقرير إعادة الفحص |
| 16 TotalLossConfirmation | تأكيد الإتلاف |
| 17 VehicleCollectionReceipt | محضر استلام المركبة |
| 18 ValuationReceipt | إيصال التقدير المالي للإتلاف |
| 19 TowingReceipt | إيصال السطحة |
| 20 InstallmentReceipt | إثبات دفع القسط |
| 21 InstallmentRefundRequest | طلب استرداد الأقساط |
| 22 InstallmentRefundReceipt | إيصال استلام استرداد الأقساط |

مثال بيانات سطحة: `evidenceType=19`, `description=نقل المركبة لإعادة الفحص`, `fromLocation=المستودع`, `toLocation=مركز الفحص`, `transportedAtUtc=2026-09-03T08:00:00Z`, `amount=150`, مع `file=receipt.pdf`.

التخزين الفعلي:

```text
wwwroot/private/vehicle-accidents/{accidentId}/evidence/{attachmentId}/{generated-file-name}
wwwroot/private/vehicle-accidents/{accidentId}/reports/{version-directory}/{generated-file-name}.pdf
```

الرد يعيد `downloadUrl` وبيانات الرحلة. الـfrontend يعرض `attachments.filter(x => x.evidenceType === 19)` كمصفوفة سطحات قابلة للإضافة المتكررة. التخزين في جدول متعدد السجلات، والـAPI يعيده كـarray؛ لا حاجة لحفظ JSON قابل للكتابة من العميل أو رابط ملف يختاره المستخدم.

تحميل المرفق: `GET /api/vehicle-accidents/{id}/evidence/{attachmentId}/download`. الرابط محمي بالصلاحيات ويجب طلبه بتوثيق المستخدم. كل مستند مشار إليه في خطوة يجب أن يكون من الحادث نفسه ومن النوع المطلوب. endpoint `/evidence` القديم يظل يعمل للمرفقات العادية؛ السطحات تحتاج endpoint الجديد لبيانات الرحلة.

## مستندات السائق والمركبة

`sourceDocuments` يعرض ثلاثة عناصر: `iqama`, `license`, `registration`، مع `versionId`, `originalFileName`, `contentType`, `downloadUrl`. عدم توفر الملف يظهر كـ`versionId: null` و`downloadUrl: null`، ويمنع تقديم المطالبة.

`GET /api/vehicle-accidents/{id}/workflow/documents/{kind}/download` يتيح لمسؤول المطالبات عرض الملفات من النظام. الإقامة والرخصة من مستندات موظف السائق المرتبط بالحادث، والاستمارة من مرفقات المركبة من نوع Istimara. عند تقديم المطالبة تثبت أرقام نسخ الملفات، بحيث تظل مستندات التقديم معروفة حتى بعد تجديد مستندات النظام. التقديم يتحقق أيضًا من وجود الملفات في التخزين.

## مؤقتات المتابعة ومدة الأقساط

- مدة التأمين: `insuranceSubmittedAtUtc + 15` يومًا تقويميًا، وتتجدد عند إعادة التقديم بعد الرفض.
- مدة الشركة: `supplierSubmittedAtUtc + 10` أيام تقويمية، لمساري التعويض والإتلاف.
- `remainingSeconds` قيمة موقعة؛ تصبح سالبة عند التأخير. `deadlineAtUtc` فارغ عندما لا تكون الحالة في مرحلة انتظار لها مؤقت. الواجهة تحدث العد التنازلي محليًا بين طلبات التحديث.
- المؤقت للمتابعة ولا يحول الحالة تلقائيًا لقبول أو دفع.
- `incidentStartedAtUtc` هو وقت الحادث. النهاية في الإصلاح هي وقت اكتماله، وفي التعويض وقت استلام التحويل، وفي الإتلاف وقت استلام الجهة للمركبة؛ تأخر التقييم أو التحويل بعد الاستلام لا يغير نهاية فترة الإتلاف.
- `incidentCalendarDays` يحسب أيام التقويم بتوقيت الرياض شاملًا يوم البداية والنهاية. نفس اليوم = يوم واحد، مع الاحتفاظ بالتواريخ الدقيقة أيضًا.

`POST /api/vehicle-accidents/{id}/workflow/installments` يسجل الأقساط المدفوعة بعد معرفة نهاية الفترة:

```json
{
  "rowVersion": "<latest-workflow-rowVersion>",
  "periodFrom": "2026-09-01",
  "periodTo": "2026-09-30",
  "paidOn": "2026-09-03",
  "amount": 1000,
  "refundEligibleAmount": 400,
  "receiptAttachmentId": "<installment-pdf-guid>",
  "notes": "المبلغ المطالب به عن أيام تعطل المركبة داخل فترة القسط"
}
```

تاريخ الدفع داخل فترة الحادث، وفترة القسط تتداخل معها، والمبلغ المؤهل موجب ولا يتجاوز المدفوع. يمنع تكرار الإيصال أو تداخل فترات الأقساط المسجلة للحادث نفسه. `refundEligibleAmount` يحدده المسؤول حسب العقد؛ النظام لا يفترض قاعدة خصم أو تقسيم شهري غير متفق عليها.

بعد تسجيل الأقساط يرفع المسؤول PDF طلب الاسترداد ثم ينفذ action 22 بمجموع المبالغ المؤهلة بالضبط. حالات الاسترداد: `NotSubmitted=1`, `Submitted=2`, `Received=3`, `Rejected=4`, `NotApplicable=5`. إعادة طلب مرفوض متاحة، ولا يمكن تقديم طلب آخر أثناء انتظار الرد أو بعد الاستلام. بعد تقديم الطلب تتجمد الأقساط حتى يتحدد الرد. يمكن تسجيل الاستلام بمبلغ أقل مع توضيح سبب الخصم.

## الشركات

تم استخدام كاتالوج شركات شراء المركبات الحالي بدل إنشاء كاتالوج مكرر:

- `GET /api/vehicle-suppliers`
- `GET /api/vehicle-suppliers/{id}`
- `POST /api/vehicle-suppliers`
- `PUT /api/vehicle-suppliers/{id}`
- `PATCH /api/vehicle-suppliers/{id}/archive`

عند فتح المطالبة يمكن اختيار شركة نشطة أو الاعتماد على `PurchasedFromSupplierId` بالمركبة. اختيار الشركة يثبت على ملف المطالبة، فلا يتغير تلقائيًا إذا تغيرت شركة المركبة لاحقًا.

## الإشعارات والتشغيل

يمكن للواجهة إرسال الصلاحيات المطلوبة في `POST /api/notifications/query` داخل `permissions`. السيرفر يطابقها مع صلاحيات المستخدم الفعلية ثم يعيد `items`, `nextCursor`, `unreadCount`, `effectivePermissions`. نفس الفلتر متاح على GET والعداد. التفاصيل والأمثلة في [دليل إشعارات الصلاحيات](notification-permissions-api.md).

تسجيل الحادث وتنفيذ خطواته يولدان إشعارات دائمة ضمن نفس عملية الحفظ. تصل للمستخدمين النشطين أصحاب صلاحيات قراءة الحوادث أو المركبات أو تشغيل حسابات المنصات أو أوامر الصيانة. تحتوي على مرجع الحادث والمركبة ورابط للمركبة. لا تتضمن صور الإقامة أو تفاصيل السندات المالية.

الـWorker يفحص المواعيد كل خمس دقائق ويرسل إشعارًا واحدًا لكل مستخدم ولكل مهلة انتهت. إعادة التقديم بموعد جديد تسمح بإشعار جديد. القراءة لا تحذف الإشعار.

| الطلب | الاستخدام |
| --- | --- |
| `GET /api/notifications?unreadOnly=false&pageSize=50` | الإشعارات الحالية والسابقة غير المؤرشفة، بما فيها المقروءة |
| `GET /api/notifications?unreadOnly=true` | غير المقروءة |
| `GET /api/notifications/unread-count` | عداد غير المقروء |
| `POST /api/notifications/{id}/state` | تغيير حالة إشعار المستخدم نفسه |

```json
{ "action": "read", "rowVersion": "<notification-rowVersion>" }
```

الحالات الأخرى الموجودة: `unread`, `acknowledge`, `archive`. يرسل العميل `nextCursor` كما هو لتحميل الصفحة التالية؛ المؤشر يشمل وقت الإشعار ومعرفه لضمان عدم سقوط الإشعارات ذات الوقت المتطابق. لا تفسره في الواجهة كرقم فقط. يحتاج المستخدم إلى `notifications.read` للوصول إلى مركز الإشعارات.

بدء الإصلاح المحلي أو المطالبة يحجب تشغيل المركبة من خلال المشكلة المرتبطة و`AccidentHold` عندما يسمح وضعها التشغيلي بذلك. إتمام الإصلاح يحل مشكلة الحادث ويعيد المركبة للتوفر أو للعهدة الحالية، مع الحفاظ على أي مشكلة مانعة أخرى أو حالة مثل السرقة أو الخروج من الخدمة. عند استلام مركبة متلفة تصبح `Decommissioned` وتنتهي ارتباطاتها التشغيلية النشطة.

في التعويض، وصول المال لا يثبت سلامة المركبة؛ لذلك لا يرفع الحجز التشغيلي تلقائيًا. يتم حل المشكلة وإعادة المركبة للخدمة عبر إجراءات الأسطول بعد التحقق من حالتها. تاريخ نهاية المطالبة المالية يظل وقت استلام التحويل.

## الصلاحيات والإغلاق

| العملية | الصلاحية |
| --- | --- |
| قراءة الحوادث ودورة العمل | `fleet.accidents.read` |
| تسجيل حادث ورفع المرفقات | `fleet.accidents.report` |
| خطوات المطالبة والأقساط وتحميل مستندات السائق الأصلية | `fleet.accidents.finalize` |
| تحميل أدلة الحادث والتقرير PDF | `fleet.accidents.download` |

كل التغييرات تحفظ الفاعل والتاريخ والملاحظات وsnapshot للبيانات في `timeline`. مرفقات الحالات المغلقة لا تقبل الإضافة.

اعتماد التقرير الأصلي: `POST /api/vehicle-accidents/{id}/finalize`. الإغلاق: `POST /api/vehicle-accidents/{id}/close`. الطلبان يستخدمان `{ "reason": "...", "rowVersion": "<accident-summary-rowVersion>" }`، وليس نسخة الـworkflow.

الإغلاق يتطلب تقريرًا معتمدًا ومرحلة `Completed`، ومع المطالبات يجب حسم استرداد الأقساط بالاستلام أو الرفض أو إثبات عدم وجود أقساط. لا يمكن استخدام endpoint الإغلاق القديم لتجاوز هذه الشروط. تعديل شدة الحادث أو رقم المرور أو رقم المطالبة بعد تحديد المسؤولية ممنوع من endpoint تصحيح التقرير حتى لا يتغير أساس المسار المالي.

## قاعدة البيانات والتحقق

Migration: `20260906100157_AddAccidentClaimWorkflow`. تضيف `VehicleAccidentCases` بعلاقة واحد لواحد مع الحادث، و`VehicleAccidentInstallments`، وبيانات المرفقات الخاصة بالنقل. تتضمن مفاتيح أجنبية، rowversion، قيود مبالغ ونسب، وفهارس مؤقتات. الحوادث المفتوحة الموجودة تبدأ بانتظار نجم، والمغلقة الموجودة تبقى مكتملة مع تاريخ إغلاقها السابق. لا تحذف مرفقات أو تقارير قديمة.

أمر تطبيقها على قاعدة التطبيق المطلوبة بعد ضبط اتصال البيئة:

```powershell
dotnet ef database update --project src/LogisticsERP.Infrastructure --context ApplicationDbContext
```

يجب نشر الـAPI والـWorker مع التحديث وتشغيل الـWorker لإشعارات انتهاء المدد. ملفات `wwwroot/private` تحتاج تخزينًا دائمًا ونسخًا احتياطيًا مع قاعدة البيانات.

الاختبارات تغطي نسب الخطأ، كل مسارات المطالبة، المرفقات المطلوبة، سند 2500، تغيير نوع الرد، رفض التأمين وإعادة التقديم، حساب المواعيد، استرداد الأقساط، حجز المركبة وإتلافها، إشعارات الأقسام، القراءة والملكية وعدم تكرار الإشعار، وتقسيم الصفحات. اختبارات دورة العمل الافتراضية تستخدم EF InMemory مع محاكاة rowversion؛ اختبار SQL الحقيقي اختياري ويستخدم قاعدة LocalDB باسم عشوائي ثم يحذف قاعدة الاختبار فقط:

```powershell
dotnet test LogisticsERP.slnx
$env:LOGISTICS_ACCIDENT_SQL_TESTS = '1'
dotnet test tests/LogisticsERP.Fleet.UnitTests --filter 'FullyQualifiedName~AccidentWorkflowSqlTests'
```

في بيئة التنفيذ الحالية، LocalDB يتعطل عند بدء تشغيل SQL Server، لذلك لم يتم التحقق من تطبيق الـmigration على خادم SQL فعلي ولم تطبق على قاعدة المستخدم. توليد نموذج SQL وترجمة الاستعلامات ومطابقة الـmigration للنموذج يتم التحقق منها دون اتصال.
