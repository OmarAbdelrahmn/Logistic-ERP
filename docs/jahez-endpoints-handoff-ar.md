# تسليم واجهات جاهز للـFrontend

هذا العقد مبني على `JahezController` وملفات العقود والكيانات في المشروع بتاريخ **3 أكتوبر 2026**. طُبقت ترحيلات جاهز على قاعدة `db67927`: يوجد 17 جدولًا في `jahez`، وحقل `DashboardSponsorId`، و13 صلاحية و13 منحة لمدير النظام، ولا توجد ترحيلات معلقة في سياقي Application وIdentity. تطبيق الترحيلات لا يشغّل نسخة API أو Worker جديدة؛ شغّل النسخة التي تحتوي هذا الكود في بيئة التطبيق. [قواعد العمل والمحاسبة](jahez-backend-api.md) و[مجموعة Postman](jahez.postman_collection.json).

## قواعد مشتركة

- المسار الأساسي `api/jahez`. الملف المحلي للتشغيل يحدد `https://localhost:7112` أو `http://localhost:7113`؛ عنوان البيئة المنشورة يؤخذ من إعدادات نشر API وليس من اسم خادم قاعدة البيانات.
- كل طلب يحتاج `Authorization: Bearer <access_token>`. POST في وحدة جاهز يحتاج `Idempotency-Key: <قيمة فريدة للعملية>` بحد 150 حرفًا. أعد استخدام المفتاح نفسه فقط لإعادة محاولة العملية والبيانات نفسها؛ استخدامه مع جسم مختلف يعيد 409. `POST /imports` يستخدم `multipart/form-data`، وبقية أوامر POST تستخدم `application/json`.
- أمثلة UUID في هذا الملف **قيم توضيحية**؛ استخدم المعرفات المعادة من API. طلبات إنشاء حساب المنصة لا تستخدم مفتاح جاهز، لأنها مسارات قديمة مستقلة.
- مفاتيح JSON في الطلب والاستجابة بصيغة `camelCase`. التواريخ اليومية `yyyy-MM-dd`، واللحظات ISO 8601 مع منطقة زمنية؛ الأيام المالية محسوبة بتوقيت الرياض. المبالغ أرقام SAR، والقبض بدقة منزلتين عشريتين؛ ملف الحركات والدفتر قد يعيدان ست منازل. القيم `null` والحقول nullable ظاهرة في الاستجابة؛ لا تعتمد على حذفها.
- نجاح مسارات جاهز JSON يعيد HTTP 200 والجسم المبين هنا مباشرة. الملف الأصلي يعاد كـXLSX. الصفحات تعيد `items`, `page`, `pageSize` بلا `totalCount`، والافتراضي `page=1&pageSize=50`، والحد الأقصى 100.
- حالات الخطأ: 400 إدخال/صيغة غير صحيحة، 401 جلسة غير مصادق عليها، 403 صلاحية أو فصل مهام، 404 سجل غير موجود، 409 تعارض أو فترة مُرحّلة أو مفتاح عدم تكرار مستخدم ببيانات أخرى، 413 تجاوز حجم الرفع. مثال الاستجابة: `{"type":"https://httpstatuses.io/409","title":"تعارض في البيانات","status":409,"detail":"...","instance":"/api/jahez/settlements","errorCode":"jahez.conflict","correlationId":"..."}`. قد يأتي `errorCode: "api.invalid_request"` في خطأ ربط JSON قبل الخدمة. لا تعتبر 409 نجاحًا؛ اعرض `detail` وحدّث البيانات.
- الأرقام الخاصة بـenums: `kind` في طلب الموافقة = 1 إعفاء رسوم، 2 تبديل مجاني، 3 نسبة عمولة، 4 تصفير؛ حالته = 1 معلق، 2 معتمد، 3 مرفوض، 4 ملغى. `bucket` = 1 رسوم، 2 دين جاهز، 3 عمولة. نوع قيد الدفتر = 1 استحقاق، 2 دفع، 3 تسوية، 4 نقل، 5 افتتاحي. حالة تسليم الصندوق = 1 معلق، 2 أكد المحاسب، 3 معتمد، 4 مرفوض. `kind` للاستيراد = 1 حركات، 2 طلبات يومية.

## عقود الاستجابة المشتركة

الحقول في الجداول أدناه هي مفاتيح JSON الفعلية. `id` من النوع UUID، والمال رقم عشري، و`...AtUtc` لحظة زمنية، و`...Date` تاريخ يومي. كل كيان **History** يعيد أيضًا `id`, `createdAtUtc`, `createdByUserId`. كل كيان **Auditable** يعيد أيضًا هذه الثلاثة مع `updatedAtUtc`, `updatedByUserId`, `rowVersion` كسلسلة Base64، و`isDeleted`, `deletedAtUtc`, `deletedByUserId`, `deletionReason`. هذه حقول استجابة وليست حقول أوامر إنشاء جاهز.

| اسم الشكل | جميع الحقول الخاصة به في الاستجابة |
|---|---|
| `Handover` | `id`, `accountId`, `externalAccountId`, `riderProfileId`, `assignmentId`, `startedAtUtc`, `endedAtUtc`, `commissionStartsOn`, `commissionPostedThrough`, `lastSettlementPaymentAtUtc`, `isLegacy`, `debtTransferred` |
| `Balance` | `handoverId`, `accountId`, `externalAccountId`, `riderProfileId`, `throughDate`, `fees`, `platformDebt`, `postedCommission`, `unpostedCommission`, `totalReceivable`, `commissionComplete`, `problems` سلسلة نصوص، `reminderAnchorAtUtc`, `daysSinceSettlementPayment`, `isOverdue`, `debtTransferred`, `latestTransactionAtUtc` |
| `Fee` + Auditable | `handoverId`, `amount`, `waivedAmount`, `approvalRequestId`؛ **لا يوجد حقل paid/remaining**. استخدم `Balance.fees` للمتبقي وقائمة الدفعات للمدفوع. |
| `ApprovalRequest` + Auditable | `handoverId`, `kind`, `status`, `requestedByUserId`, `targetAccountId`, `waiverAmount`, `fromDate`, `toDate`, `effectiveAtUtc`, `reason`, `externalResetReference` |
| `Decision` + History | `requestId`, `actorUserId`, `status`, `decidedAtUtc`, `reason` |
| `ApprovalResponse` | `request: ApprovalRequest`, `decisions: Decision[]` |
| `CommissionPolicy` + History | `handoverId`, `approvalRequestId`, `fromDate`, `toDate`, `rate`؛ القيمة المعتمدة `0.15` |
| `Earnings` + History | `handoverId`, `fromDate`, `toDate`, `totalDeliveryPrice`, `totalPenalties`, `totalCashAmount`, `totalDriverDebit`, `totalServiceDeduction`, `totalDriverCredit`, `totalBonuses`, `totalTips`, `totalFreeOrders`, `supersedesId`, `reason` |
| `Settlement` + History | `handoverId`, `throughDate`, `recordedAtUtc`, `collectedByUserId`, `feePayment`, `debtPayment`, `commissionPayment`, `countsAsSettlement`, `reason` |
| `LedgerEntry` + History | `handoverId`, `bucket`, `kind`, `amount` بإشارته، `sourceId`, `reversesEntryId`, `occurredAtUtc`, `reason`, `fromDate`, `throughDate`, `calculationJson` كسلسلة JSON نصية قد تكون `null` |
| `ImportBatch` + Auditable | `kind`, `contentHash`, `uploadedByUserId`, `committedAtUtc`, `replacesBatchId`, `correctionReason` |
| `ImportPreview` | `batchId`, `kind`, `committed`, `rows: ImportRow[]`, `issues: ImportIssue[]`, `files: {id,fileName}[]`, `accounts: ImportAccountSummary[]` |
| `ImportRow` | `rowId`, `fileName`, `rowNumber`, `driverId`, `occurredAtUtc`, `accountId`, `handoverId`, `riderProfileId`, `netAmount`, `dispatches` |
| `ImportIssue` | `rowId`, `fileName`, `rowNumber`, `code`, `description` |
| `ImportAccountSummary` | `accountId`, `driverId`, `fromDate`, `toDate`, `validRowCount`, `netAmount`, `platformDebtChange`, `dispatches`, `hasIssues` |
| `Dispatch` | `accountId`, `externalAccountId`, `riderProfileId`, `handoverId`, `date`, `count` |
| `CashboxBalance` | `fees`, `settlements`, `reservedFees`, `reservedSettlements`, `availableFees`, `availableSettlements` |
| `CashboxEntry` + Auditable | `settlementId`, `handoverId`, `section` (1 رسوم، 2 تصفيات), `amount`, `collectedByUserId`, `receivedAtUtc`, `cashboxHandoverId` |
| `CashboxHandover` + Auditable | `businessDate`, `status`, `feeAmount`, `settlementAmount`, `accountantFeeAmount`, `accountantSettlementAmount`, `requestedByUserId`, `accountantUserId`, `approvedByUserId`, `confirmedAtUtc`, `decidedAtUtc`, `reason`, `confirmationReason`, `decisionReason` |

مثال كامل لإنشاء استلام وردّه. يوم الاستلام لا يبدأ عمولة 15 ريال، والمثال يخص حسابًا موجودًا وكفيل داشبورد مكتملًا:

```http
POST /api/jahez/handovers
Authorization: Bearer <token>
Idempotency-Key: new-handover-456469-20261001
Content-Type: application/json
```
```json
{"accountId":"11111111-1111-1111-1111-111111111111","riderProfileId":"22222222-2222-2222-2222-222222222222","effectiveAtUtc":"2026-10-01T08:00:00+03:00","reason":"تسليم حساب جاهز","initialFeePayment":80}
```
```json
{"id":"33333333-3333-3333-3333-333333333333","accountId":"11111111-1111-1111-1111-111111111111","externalAccountId":"456469","riderProfileId":"22222222-2222-2222-2222-222222222222","assignmentId":"44444444-4444-4444-4444-444444444444","startedAtUtc":"2026-10-01T05:00:00+00:00","endedAtUtc":null,"commissionStartsOn":"2026-10-02","commissionPostedThrough":null,"lastSettlementPaymentAtUtc":null,"isLegacy":false,"debtTransferred":false}
```

## 1. الحسابات والإسناد والاستعراض

| الطريقة والمسار | الطلب | استجابة 200 | الصلاحية |
|---|---|---|---|
| `POST /api/jahez/handovers` | جسم `HandoverRequest` بالمثال أعلاه؛ `initialFeePayment` من 0 إلى 200 بالهللة، و`feeApprovalRequestId` إن أرسل يجب أن يكون `null` | `Handover`؛ `id` هو `handoverId` لبقية العمليات | `jahez.handovers.manage` |
| `POST /api/jahez/legacy-adoptions` | جسم `LegacyAdoptionRequest` أدناه؛ `assignmentId` لإسناد جاهز قديم نشط | `Handover` مع `isLegacy:true` | `jahez.adjustments.manage` |
| `POST /api/jahez/handovers/{id}/close` | `{id}` = `handoverId`، جسم `CloseRequest` أدناه | `Handover` مع `endedAtUtc` وتثبيت العمولة حتى يوم الإغلاق | `jahez.handovers.manage` |
| `GET /api/jahez/handovers` | Query اختياري `accountId`, `riderId`, `page`, `pageSize` | `{ "items": Handover[], "page":1, "pageSize":50 }` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/balance?through=2026-10-03` | `{id}` = `handoverId`؛ `through` مطلوب، حتى اليوم ولا يسبق تاريخ عمولة/تصفية مثبتة | `Balance`؛ مثال كامل أدناه | `jahez.read` |
| `GET /api/jahez/debts` | Query `riderId?`, `overdueOnly=false`, `page=1`, `pageSize=50` | `{ "items": Balance[], "page":1, "pageSize":50 }`؛ تشمل ديون الإسنادات المغلقة | `jahez.read` |
| `GET /api/jahez/ledger` | Query `riderId?`, `handoverId?`, `page`, `pageSize` | `{ "items": LedgerEntry[], "page":1, "pageSize":50 }` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/fee` | `{id}` = `handoverId` | `Fee` مع حقول Auditable | `jahez.read` |
| `GET /api/jahez/handovers/{id}/settlements` | `{id}` = `handoverId`, Query `page`, `pageSize` | `{ "items": Settlement[], "page":1, "pageSize":50 }` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/earnings` | `{id}` = `handoverId`, Query `page`, `pageSize` | `{ "items": Earnings[], "page":1, "pageSize":50 }` | `jahez.read` |
| `GET /api/jahez/handovers/{id}/commission-policies` | `{id}` = `handoverId`, Query `page`, `pageSize` | `{ "items": CommissionPolicy[], "page":1, "pageSize":50 }` | `jahez.read` |

`LegacyAdoptionRequest` — الأرصدة الافتتاحية لا تعيد إنشاء 200 أو عمولة تاريخية:

```json
{"assignmentId":"44444444-4444-4444-4444-444444444444","financialStartOn":"2026-10-01","openingDebt":500,"openingFees":70,"openingCommission":30,"reason":"تهيئة رصيد قديم موثق"}
```
`CloseRequest`:

```json
{"effectiveAtUtc":"2026-10-03T08:00:00+03:00","reason":"إغلاق فترة استخدام الحساب"}
```

مثال `Balance` قبل قبض الدين أو العمولة، وبعد دفعة رسوم أولى 80 وحركة `Net Amount=-500`:

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","accountId":"11111111-1111-1111-1111-111111111111","externalAccountId":"456469","riderProfileId":"22222222-2222-2222-2222-222222222222","throughDate":"2026-10-03","fees":120,"platformDebt":500,"postedCommission":0,"unpostedCommission":30,"totalReceivable":650,"commissionComplete":true,"problems":[],"reminderAnchorAtUtc":"2026-10-01T05:00:00+00:00","daysSinceSettlementPayment":2,"isOverdue":false,"debtTransferred":false,"latestTransactionAtUtc":"2026-10-02T10:00:00+00:00"}
```

`Balance` هو المستحق الحالي حتى نقطة القطع، وليس لقطة تاريخية بعد دفعات أحدث. `platformDebt` السالب يعني رصيدًا دائنًا. `postedCommission` مستحق مثبت سابقًا، و`unpostedCommission` معاينة للعمولة غير المثبتة بعد. إذا كان `commissionComplete=false`، اعرض `problems` وامنع التصفية حتى تُستكمل بيانات النسبة. `latestTransactionAtUtc=null` يعني عدم وجود حركات مستوردة. `fees` تشمل الباقي فقط؛ نقل الدين بالتصفير لا يمحو الرصيد. `overdueOnly=true` مرشح للمتأخرين وفق أكثر من 10 أيام.

## 2. طلبات الموافقة

| الطريقة والمسار | الطلب | استجابة 200 | الصلاحية |
|---|---|---|---|
| `POST /api/jahez/requests` | `ApprovalCreateRequest`؛ `handoverId`, `kind`, `reason`، وباقي الحقول حسب النوع أدناه | `ApprovalResponse` وحالة `request.status=1` و`decisions=[]` | `jahez.requests.create` |
| `GET /api/jahez/requests?status=1&page=1&pageSize=50` | `status?` من 1 إلى 4، `page`, `pageSize` | `{ "items": ApprovalRequest[], "page":1, "pageSize":50 }`؛ عناصر القائمة لا تحتوي قرارات | `jahez.read` |
| `GET /api/jahez/requests/{id}` | `{id}` = `approvalRequestId` | `ApprovalResponse` مع تاريخ القرار | `jahez.read` |
| `POST /api/jahez/requests/{id}/decision` | `{id}` = `approvalRequestId`، `DecisionRequest` | `ApprovalResponse` محدث؛ بعد الموافقة status=2 والرفض status=3 | `jahez.read` عند الـController؛ الخدمة تتطلب `jahez.requests.approve` أو `jahez.resets.approve` للتصفير |
| `POST /api/jahez/requests/{id}/cancel` | `{id}` = `approvalRequestId`، جسم `{ "reason":"لم يعد مطلوبًا" }` | `ApprovalResponse` مع status=4 وقرار إلغاء | `jahez.requests.create`، والمقدم نفسه فقط |

أنواع أجسام `POST /requests`:

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":1,"reason":"استثناء رسوم موثق","waiverAmount":50}
```
```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":2,"reason":"الحساب لا تصله طلبات","targetAccountId":"55555555-5555-5555-5555-555555555555","effectiveAtUtc":"2026-10-03T08:00:00+03:00"}
```
```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":3,"reason":"أداء منخفض","fromDate":"2026-10-02","toDate":"2026-10-03"}
```
```json
{"handoverId":"33333333-3333-3333-3333-333333333333","kind":4,"reason":"المندوب لم يحضر","effectiveAtUtc":"2026-10-03T08:00:00+03:00","externalResetReference":"مرجع إجراء الداشبورد الخارجي"}
```

الإعفاء لا يزيد عن رسوم الاستلام المتبقية عند القرار؛ لا يعيد نقدًا مقبوضًا. التبديل المجاني يعفي رسم الاستلام الجديد 200 بالكامل، ويبقي الديون والرسوم القديمة على صاحبها. نسبة العمولة `15%` تحل محل 15 ريالًا يوميًا داخل فترة الموافقة؛ مدتها بحد أقصى 732 يومًا بحسب تحقق الكود، ولا تتداخل مع عمولة مثبتة. التصفير يغلق الإسناد ويحرر الحساب ويحفظ دين المندوب السابق؛ مرجع داشبورد جاهز نص توثيقي وليس تنفيذًا آليًا. القرار ينفذه مستخدم آخر مختلف عن مقدم الطلب. الموافقة أو الرفض ينشئ إشعارًا داخليًا للمقدم.

`DecisionRequest` للموافقة؛ للرفض استخدم `approve:false`:

```json
{"approve":true,"reason":"تم التحقق واعتماد الطلب"}
```

مثال شكل `ApprovalResponse` بعد قرار، مع كامل حقول `ApprovalRequest` و`Decision` الخاصة؛ حقول Auditable وHistory الإضافية موضحة في جدول العقود:

```json
{"request":{"id":"66666666-6666-6666-6666-666666666666","handoverId":"33333333-3333-3333-3333-333333333333","kind":1,"status":2,"requestedByUserId":"77777777-7777-7777-7777-777777777777","targetAccountId":null,"waiverAmount":50,"fromDate":null,"toDate":null,"effectiveAtUtc":null,"reason":"استثناء رسوم موثق","externalResetReference":null,"createdAtUtc":"2026-10-03T05:00:00+00:00","createdByUserId":"77777777-7777-7777-7777-777777777777","updatedAtUtc":"2026-10-03T05:10:00+00:00","updatedByUserId":"88888888-8888-8888-8888-888888888888","rowVersion":"AAAAAAAAAAA=","isDeleted":false,"deletedAtUtc":null,"deletedByUserId":null,"deletionReason":null},"decisions":[{"id":"99999999-9999-9999-9999-999999999999","requestId":"66666666-6666-6666-6666-666666666666","actorUserId":"88888888-8888-8888-8888-888888888888","status":2,"decidedAtUtc":"2026-10-03T05:10:00+00:00","reason":"تم التحقق واعتماد الطلب","createdAtUtc":"2026-10-03T05:10:00+00:00","createdByUserId":"88888888-8888-8888-8888-888888888888"}]}
```

## 3. أرباح النسبة، القبض، والتصحيح

| الطريقة والمسار | الطلب | استجابة 200 | الصلاحية |
|---|---|---|---|
| `POST /api/jahez/earnings` | `EarningsRequest` بكل البنود التسعة صراحةً؛ `supersedesId?` لتصحيح بيان لم يُثبت | `Earnings` + History | `jahez.earnings.manage` |
| `POST /api/jahez/settlements` | `PaymentRequest`؛ توزيع المقبوض على الرسوم والدين والعمولة | `Settlement` + History | `jahez.collections.manage` |
| `POST /api/jahez/adjustments` | `LedgerAdjustmentRequest`؛ مبلغ موقع موجب لزيادة الدين أو سالب لتخفيضه، `reversesEntryId?` | `LedgerEntry` + History؛ لا ينشئ قبضًا | `jahez.adjustments.manage` |

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","fromDate":"2026-10-02","toDate":"2026-10-03","totalDeliveryPrice":1000,"totalPenalties":20,"totalCashAmount":100,"totalDriverDebit":10,"totalServiceDeduction":5,"totalDriverCredit":50,"totalBonuses":30,"totalTips":10,"totalFreeOrders":5,"reason":"كشف أرباح معتمد يدويًا","supersedesId":null}
```

استجابة الأرباح تُرجع كل حقول الجسم نفسها مع `id`, `createdAtUtc`, `createdByUserId`. `totalFreeOrders` **قيمة مالية**. أساس المثال = 960 والعمولة 144. عند أساس صفر أو سالب، العمولة صفر. يجب تقديم كشف كامل بلا فجوات للأيام التي ستُصفّى وفق النسبة، أو يظهر `commissionComplete=false`. لا تُعدل فترة مثبتة.

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","throughDate":"2026-10-03","feePayment":50,"debtPayment":100,"commissionPayment":30,"countsAsSettlement":true,"reason":"دفعة تصفية جزئية"}
```

استجابة القبض مثال بجميع حقول `Settlement` وHistory:

```json
{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","handoverId":"33333333-3333-3333-3333-333333333333","throughDate":"2026-10-03","recordedAtUtc":"2026-10-03T09:00:00+00:00","collectedByUserId":"77777777-7777-7777-7777-777777777777","feePayment":50,"debtPayment":100,"commissionPayment":30,"countsAsSettlement":true,"reason":"دفعة تصفية جزئية","createdAtUtc":"2026-10-03T09:00:00+00:00","createdByUserId":"77777777-7777-7777-7777-777777777777"}
```

الـ50 تدخل قسم الرسوم، والـ130 قسم التصفيات. دفع جزء يقلل البند المحدد فقط. `countsAsSettlement:true` يحدث آخر دفعة ويبدأ عد 10 أيام جديدًا حتى لو بقي دين. دفع رسوم فقط خارج التصفية: `feePayment>0`, و`debtPayment=0`, و`commissionPayment=0`, و`countsAsSettlement:false`؛ لا يعيد عد التذكير. لا يمكن قبض أكثر من رصيد أي بند، أو إرسال مبالغ صفر كلها. استدعِ balance قبل عرض خيارات الدفع ثم حدثه بعد نجاح الطلب. لا يمكن اختيار `throughDate` أقدم من آخر فترة عمولة أو تصفية مثبتة.

```json
{"handoverId":"33333333-3333-3333-3333-333333333333","bucket":2,"amount":-25,"reason":"تصحيح مديونية موثق","reversesEntryId":null}
```

استجابة التسوية هي قيد `LedgerEntry` مع `kind=3`, `amount=-25`, `sourceId` مستقل، و`calculationJson=null`. عند عكس قيد سابق، أرسل `reversesEntryId`، والبند نفسه والمبلغ المعاكس تمامًا؛ لا يمكن عكس دفعة نقدية بهذه الواجهة. دفتر الحساب يتتبع مصدر كل استحقاق وقيمته وتواريخه. `calculationJson` نص JSON داخلي لحساب العمولة؛ حلله عند عرض التفاصيل فقط.

## 4. الاستيراد والطلبات

| الطريقة والمسار | الطلب | استجابة 200 | الصلاحية |
|---|---|---|---|
| `POST /api/jahez/imports` | `multipart/form-data`: `kind` = 1 للحركات أو 2 للطلبات، حقل `files` مكرر لكل ملف XLSX، `replacesBatchId?`, `correctionReason?` | `ImportPreview` محفوظة، `committed:false` | `jahez.imports.manage` |
| `GET /api/jahez/imports` | `page`, `pageSize` | `{ "items": ImportBatch[], "page":1, "pageSize":50 }` | `jahez.imports.manage` |
| `GET /api/jahez/imports/{id}` | `{id}` = `batchId` | `ImportPreview` حديثة مع مشكلات الربط والتداخل | `jahez.imports.manage` |
| `POST /api/jahez/imports/{id}/commit` | `{id}` = `batchId`، جسم `{}` للحركات أو `ImportCommitRequest` للطلبات الملتبسة | `ImportPreview` مع `committed:true` | `jahez.imports.manage` |
| `GET /api/jahez/import-files/{id}` | `{id}` = `files[].id` من المعاينة | بايتات ملف XLSX، `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`، واسم الملف الأصلي للتنزيل | `jahez.imports.manage` |
| `GET /api/jahez/dispatches?from=2026-10-02&to=2026-10-02` | `from`, `to` مطلوبان، `riderId?`, `page`, `pageSize` | `{ "items": Dispatch[], "page":1, "pageSize":50 }` | `jahez.read` |

مثال رفع الملفين في **عمليتين منفصلتين** لأن لكل عملية نوعًا واحدًا. يمكن رفع عدة ملفات من النوع نفسه في عملية واحدة، حتى 10 ملفات، بحد 10MB للملف و30MB إجماليًا. اسم حقل الملفات حرفيًا `files`. استخدم الملفات المطلوبة عبر واجهة اختيار الملفات؛ لا ترسل مسارات ملفات الجهاز للمستخدم الآخر.

```bash
curl -X POST "$BASE_URL/api/jahez/imports" -H "Authorization: Bearer $TOKEN" -H "Idempotency-Key: import-transactions-20261003" -F "kind=1" -F "files=@transactions.xlsx"
curl -X POST "$BASE_URL/api/jahez/imports" -H "Authorization: Bearer $TOKEN" -H "Idempotency-Key: import-dispatches-20261003" -F "kind=2" -F "files=@dispatches.xlsx"
```

هذه أمثلة صياغة للطلب؛ تاريخ الملف نفسه لا يحدد فترة الحركات. `SDP_Report` يحتاج `Driver ID`, `Date`, `Delivery Price`, `Cash Amount`, `Net Amount`, `Driver Adjustment`. `Delivery Insights Report` يحتاج `Driver ID`, `From`, `To`, `Number Of Dispatches`. `Driver ID` يطابق `ExternalAccountId` في منصة جاهز كنص. قيمة `Net Amount` السالبة تنشئ دينًا موجبًا، والموجبة رصيدًا دائنًا؛ الاستيراد لا ينشئ حركة قبض أو صندوق.

استجابة معاينة مختصرة ولكن بجميع مستويات `ImportPreview`:

```json
{"batchId":"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb","kind":1,"committed":false,"rows":[{"rowId":"cccccccc-cccc-cccc-cccc-cccccccccccc","fileName":"transactions.xlsx","rowNumber":2,"driverId":"456469","occurredAtUtc":"2026-10-02T10:00:00+00:00","accountId":"11111111-1111-1111-1111-111111111111","handoverId":"33333333-3333-3333-3333-333333333333","riderProfileId":"22222222-2222-2222-2222-222222222222","netAmount":-500,"dispatches":null}],"issues":[],"files":[{"id":"dddddddd-dddd-dddd-dddd-dddddddddddd","fileName":"transactions.xlsx"}],"accounts":[{"accountId":"11111111-1111-1111-1111-111111111111","driverId":"456469","fromDate":"2026-10-02","toDate":"2026-10-02","validRowCount":1,"netAmount":-500,"platformDebtChange":500,"dispatches":null,"hasIssues":false}]}
```

`issues` قد تحتوي `parse_error`, `unknown_account`, `future_date`, `assignment_missing`, `multi_day_report`, `duplicate_daily_row`, `assignment_ambiguous`, `invalid_allocation`, `unknown_allocation_row`, `overlapping_period`. اعرض اسم الملف ورقم الصف والوصف. لا تتيح زر الترحيل إن بقيت مشكلة؛ في تغير المندوب خلال اليوم يمكن حل `assignment_ambiguous` بتوزيع يدوي عند commit. مجموع التوزيعات للصف يجب أن يساوي العدد، وكل `handoverId` لمستخدم الحساب في ذلك اليوم، مع سبب لكل جزء:

```json
{"allocations":[{"rowId":"cccccccc-cccc-cccc-cccc-cccccccccccc","handoverId":"33333333-3333-3333-3333-333333333333","count":10,"reason":"قبل التبديل"},{"rowId":"cccccccc-cccc-cccc-cccc-cccccccccccc","handoverId":"eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee","count":4,"reason":"بعد التبديل"}]}
```

الصفحة `GET /imports/{id}` تعيد معاينة وربطًا محدثين عند الطلب. التقارير متعددة الأيام لا توزع الطلبات تلقائيًا. التداخل مع دفعة مرحلة يحتاج رفعًا بديلًا كاملًا بواسطة `replacesBatchId` و`correctionReason` موثقين؛ المعاينة ثم الترحيل تحفظ النسخة القديمة وقيود عكس حركاتها. `accounts.platformDebtChange` أثر الدفعة المعروضة، وليس رصيد الحساب الحالي بعد دفعاته. إذا أعيد رفع الملف نفسه، تظهر الدفعة القديمة بدل مضاعفة الحركات. `GET /dispatches` يوزع العدد على المستخدم الفعلي الذي استلم الحساب، لا مالكه المسجل.

## 5. الصندوق والتسليم للمحاسب

| الطريقة والمسار | الطلب | استجابة 200 | الصلاحية |
|---|---|---|---|
| `GET /api/jahez/cashbox` | بلا Query | `CashboxBalance` | `jahez.cashbox.read` |
| `GET /api/jahez/cashbox/entries` | `cashboxHandoverId?`, `page`, `pageSize` | `{ "items": CashboxEntry[], "page":1, "pageSize":50 }` | `jahez.cashbox.read` |
| `GET /api/jahez/cashbox/handovers` | `page`, `pageSize` | `{ "items": CashboxHandover[], "page":1, "pageSize":50 }` | `jahez.cashbox.read` |
| `POST /api/jahez/cashbox/handovers` | `CashboxCreateRequest` | `CashboxHandover` بحالة 1 | `jahez.cashbox.submit` |
| `POST /api/jahez/cashbox/handovers/{id}/confirm` | `{id}` = معرف تسليم الصندوق، `AccountantConfirmRequest` | `CashboxHandover` بحالة 2؛ يجب تطابق كل قسم | `jahez.cashbox.confirm` |
| `POST /api/jahez/cashbox/handovers/{id}/decision` | `{id}` = معرف تسليم الصندوق، `DecisionRequest` نفسه المستخدم للموافقات | `CashboxHandover` بحالة 3 للموافقة أو 4 للرفض | `jahez.cashbox.approve` |

مثال رصيد: `{"fees":130,"settlements":130,"reservedFees":0,"reservedSettlements":0,"availableFees":130,"availableSettlements":130}`. إذا أُنشئ طلب تسليم لكل مبلغ، يصبح المحجوز مساويًا للقسم، والمتاح صفرًا. التحصيل الذي يصل بعد إنشاء الطلب يبقى متاحًا للتسليم التالي.

`CashboxCreateRequest`:

```json
{"businessDate":"2026-10-03","reason":"تسليم اليومية للمحاسب"}
```
`AccountantConfirmRequest`:

```json
{"feeAmount":130,"settlementAmount":130,"reason":"تم استلام ومطابقة القسمين"}
```
`DecisionRequest`؛ للرفض استخدم `approve:false` مع سبب الرفض:

```json
{"approve":true,"reason":"اعتماد التسليم بعد تأكيد المحاسب"}
```

شكل `CashboxHandover` بعد تأكيد المحاسب وقبل القرار:

```json
{"id":"ffffffff-ffff-ffff-ffff-ffffffffffff","businessDate":"2026-10-03","status":2,"feeAmount":130,"settlementAmount":130,"accountantFeeAmount":130,"accountantSettlementAmount":130,"requestedByUserId":"77777777-7777-7777-7777-777777777777","accountantUserId":"88888888-8888-8888-8888-888888888888","approvedByUserId":null,"confirmedAtUtc":"2026-10-03T09:15:00+00:00","decidedAtUtc":null,"reason":"تسليم اليومية للمحاسب","confirmationReason":"تم استلام ومطابقة القسمين","decisionReason":null,"createdAtUtc":"2026-10-03T09:00:00+00:00","createdByUserId":"77777777-7777-7777-7777-777777777777","updatedAtUtc":"2026-10-03T09:15:00+00:00","updatedByUserId":"88888888-8888-8888-8888-888888888888","rowVersion":"AAAAAAAAAAA=","isDeleted":false,"deletedAtUtc":null,"deletedByUserId":null,"deletionReason":null}
```

المُرسل والمحاسب وصاحب الاعتماد النهائي **ثلاثة مستخدمين مختلفون**. لا تقارن الإجمالي فقط؛ يجب أن يطابق `feeAmount` و`settlementAmount` كل قسم. رفض الطلب يحرر الحركات المحجوزة؛ يحفظ `decisionReason`. التسليم للصندوق لا يغيّر آخر تاريخ تصفية المندوب.

## 6. حساب المنصة والكفلاء والإشعارات الموجودة

هذه المسارات موجودة في API العام واستُخدمت/عُدلت لدعم جاهز. `sponsorId` كفيل الحساب و`dashboardSponsorId` كفيل الداشبورد **معرفان مستقلان**. قيمة Dashboard Sponsor مطلوبة في حساب منصة جديد، ومن `GET /api/sponsors` ضمن الكفلاء النشطين. الحسابات القديمة قد تعيد `dashboardSponsorId:null` حتى تُستكمل. يسمح تحديثها بإضافته، ولا يسمح بمسح قيمة موجودة. صاحب الحساب المسجل قد يختلف عن `riderProfileId` الذي يستلم الحساب.

| الطريقة والمسار | الطلب | استجابة 200 | الصلاحية |
|---|---|---|---|
| `GET /api/platform-accounts` | `accountId?`, `platformId?`, `operatingCityId?`, `sponsorId?`, `dashboardSponsorId?`, `ownerRiderProfileId?`, `actualRiderProfileId?`, `status?`, `paymentModel?`, `currentOnly=false`, `includeArchived=false` | مصفوفة `SimplePlatformAccountResponse[]`، **وليست صفحة جاهز** | `platform_accounts.read` |
| `GET /api/platform-accounts/{id}` | معرف الحساب | `SimplePlatformAccountResponse` | `platform_accounts.read` |
| `POST /api/platform-accounts` | `SimplePlatformAccountUpsertRequest` أدناه، `dashboardSponsorId` مطلوب | `SimplePlatformAccountResponse` | `platform_accounts.manage` |
| `PUT /api/platform-accounts/{id}` | جسم الإنشاء مع `rowVersion` من GET؛ أرسل `dashboardSponsorId` الموجود | `SimplePlatformAccountResponse` | `platform_accounts.manage` |
| `GET /api/platform-operations/accounts` | `platformId?`, `sponsorId?`, `dashboardSponsorId?` | `PlatformAccountResponse[]` | `platform_accounts.read` |
| `POST /api/platform-operations/accounts` | `PlatformAccountUpsertRequest` أدناه، `dashboardSponsorId` مطلوب | `PlatformAccountResponse` | `platform_accounts.manage` |
| `PUT /api/platform-operations/accounts/{id}` | جسم الإنشاء مع `rowVersion` من GET | `PlatformAccountResponse` | `platform_accounts.manage` |

مسارا الإسناد `POST /api/platform-accounts/{id}/assign` و`POST /api/platform-operations/assignments` وإغلاق الإسناد العام يرجعان تعارضًا لحساب جاهز، لضمان إنشاء الرسوم والتاريخ المالي عبر `/api/jahez/handovers` و`/close`. عند إنشاء حساب جاهز، استخدم `paymentModel:"PayPerOrder"` و`status:"Available"`. عند تحديث حساب له تاريخ مالي لا تغير `clientPlatformId`/`platformId` أو `externalAccountId`.

`SimplePlatformAccountUpsertRequest` لمسار `POST /api/platform-accounts`:

```json
{"platformId":"12121212-1212-1212-1212-121212121212","operatingCityId":"13131313-1313-1313-1313-131313131313","sponsorId":"14141414-1414-1414-1414-141414141414","dashboardSponsorId":"15151515-1515-1515-1515-151515151515","ownerRiderProfileId":"16161616-1616-1616-1616-161616161616","code":"J-456469","externalAccountId":"456469","userName":null,"paymentModel":"PayPerOrder","status":"Available","statusReason":null,"acquisitionDate":null,"startDate":null,"endDate":null,"notes":null,"archiveReason":null,"rowVersion":null}
```
`PlatformAccountUpsertRequest` لمسار `POST /api/platform-operations/accounts`:

```json
{"clientPlatformId":"12121212-1212-1212-1212-121212121212","registeredEmployeeId":"17171717-1717-1717-1717-171717171717","operatingCityId":"13131313-1313-1313-1313-131313131313","sponsorId":"14141414-1414-1414-1414-141414141414","dashboardSponsorId":"15151515-1515-1515-1515-151515151515","code":"J-456469","externalAccountId":"456469","userName":null,"paymentModel":"PayPerOrder","status":"Available","statusReason":null,"acquisitionDate":null,"startDate":null,"endDate":null,"ownershipNotes":null,"operationalNotes":null,"rowVersion":null}
```

كل حقول استجابة الحساب الأساسي `SimplePlatformAccountResponse`: `id`, `platformId`, `platformCode`, `platformNameAr`, `platformNameEn`, `operatingCityId`, `operatingCityNameAr`, `operatingCityNameEn`, `sponsorId`, `sponsorNameAr`, `sponsorNameEn`, `ownerRiderProfileId`, `ownerEmployeeId`, `ownerRiderNameAr`, `ownerRiderNameEn`, `code`, `externalAccountId`, `userName`, `paymentModel`, `status`, `statusReason`, `acquisitionDate`, `startDate`, `endDate`, `notes`, `currentAssignment`, `rowVersion`, `dashboardSponsorId`. إن وُجد `currentAssignment`، حقوله: `id`, `accountId`, `paymentModel`, `actualRiderProfileId`, `actualEmployeeId`, `actualRiderNameAr`, `actualRiderNameEn`, `effectiveFrom`, `effectiveTo`, `status`, `startReason`, `endReason`, `wasBackdated`, `backdatedReason`, `assignedByUserId`, `endedByUserId`, `rowVersion`.

كل حقول استجابة التوافق `PlatformAccountResponse`: `id`, `clientPlatformId`, `platformNameAr`, `registeredEmployeeId`, `registeredEmployeeNameAr`, `operatingCityId`, `operatingCityAr`, `sponsorId`, `sponsorNameAr`, `sponsorNameEn`, `code`, `externalAccountId`, `userName`, `paymentModel`, `status`, `statusReason`, `acquisitionDate`, `startDate`, `endDate`, `ownershipNotes`, `operationalNotes`, `rowVersion`, `dashboardSponsorId`.

| مسار مساند | الاستخدام والاستجابة | الصلاحية |
|---|---|---|
| `GET /api/platforms` | مصفوفة المنصات؛ ابحث عن `code:"JAHEZ"` وخذ `id` عند إنشاء الحساب | `platform_accounts.read` |
| `GET /api/sponsors` | قائمة الكفلاء؛ اختَر `dashboardSponsorId` و`sponsorId` كلًا على حدة | `sponsors.read` |
| `GET /api/hr-catalogs/operating-cities` | قائمة المدن واستخدام `id` | `operating_cities.read` |
| `GET /api/riders` | المندوبون الفعليون؛ `id` يُستخدم `riderProfileId` | `riders.read` |
| `GET /api/notifications?permissions=jahez.read` | إشعارات تذكير التصفية، عند منح `notifications.read`؛ يعيد `{items,nextCursor}` | `notifications.read`، وفلتر `jahez.read` بحسب نطاق المنصة |

## ترتيب صفحات الواجهة وتحديث البيانات

1. حسابات المنصات: اقرأ القوائم والكفلاء، احفظ الحساب بكفيل داشبورد مستقل، ثم اعرض صاحب الحساب والمستخدم الحالي كلًا في خانته.
2. صفحة الاستلام: نفذ `POST /handovers` واحفظ `id` المعاد. اعرض `GET /handovers/{id}/balance` و`/fee` والدفعات. لا تستنتج الرسوم المتبقية من `amount - waivedAmount` فقط.
3. صفحة التصفية: اعرض الرصيد والتواريخ أولًا، وخذ مبلغًا مستقلًا لكل بند. بعد نجاح `POST /settlements` أعد طلب الرصيد والصندوق.
4. صفحة الاستثناءات: أنشئ الطلب، ثم اعرض القرار للمقدم. افصل زر الاعتماد عن مقدم الطلب، وخصص صلاحية `jahez.resets.approve` للتصفير.
5. صفحة الاستيراد: ارفع الملفات، اعرض `issues` و`accounts` و`rows`، ثم رحّل عند زوال المشاكل. في تقرير الطلبات اليومي الملتبس اعرض حقول التوزيع اليدوي مع الأسباب.
6. صفحة الصندوق: اقرأ الرصيد المتاح والمحجوز، أنشئ التسليم، سجل تأكيد المحاسب بمبلغ كل قسم، ثم قرار الاعتماد النهائي من مستخدم ثالث. بعد كل خطوة أعد جلب الرصيد وقائمة التسليمات.
7. صفحة التأخر: `GET /debts?overdueOnly=true` مع إشعارات النظام. يبدأ العد من الاستلام أو آخر دفعة تصفية فعلية؛ يبقى الدين على المندوب السابق بعد التصفير.

لم تُنشأ بيانات حقيقية تجريبية أو حسابات جاهز على قاعدة التشغيل أثناء تحديث البنية. الأمثلة هنا توضح العقد البرمجي؛ قيم المعرفات وأرصدة المثال لا تعكس بيانات قاعدة `db67927`.
