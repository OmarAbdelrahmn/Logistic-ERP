# كتالوج الأدوار والصلاحيات

## قواعد القراءة

- `SA` تعني أن الصلاحية ضمن الحد الأساسي لـ`SYSTEM_ADMIN`.
- `M` تعني أنها ضمن الحد الأساسي لـ`MANAGER`.
- `—` تعني أنها لا تمنح تلقائيًا وتحتاج Grant مباشرًا.
- Sensitive تعني أن عرضها أو منحها يحتاج مراجعة وصول البيانات.
- High Trust تعني صلاحية إدارية/مالية/اعتماد عالية الخطورة.
- Client/Housing Scope يعني أن وجود المفتاح وحده لا يكفي؛ يجب تحديد كل النطاق أو أهداف محددة.

## الأمن والكتالوج

| Permission Key | الوصف | المستوى | Baseline |
| --- | --- | --- | --- |
| `users.read` | عرض الحسابات وحالتها | Sensitive | SA |
| `users.create` | إنشاء مستخدم | High Trust | SA |
| `users.update` | تعديل مستخدم وحالته | High Trust | SA |
| `users.archive` | أرشفة مستخدم وإبطال جلساته دون حذف | High Trust | SA |
| `roles.read` | عرض الأدوار والقوالب | Normal | SA |
| `roles.manage` | إدارة الأدوار غير المحمية والتكليفات | High Trust | SA |
| `permissions.read` | عرض المنح والمنع والنطاقات | Normal | SA |
| `permissions.manage` | إدارة المنح والمنع والنطاقات | High Trust | SA |
| `audit.read` | عرض سجل التدقيق | High Trust | SA |
| `support_access.manage` | إدارة وصول الدعم والطوارئ | High Trust | SA |
| `operating_cities.read` | عرض المدن التشغيلية | Normal | SA, M |
| `operating_cities.manage` | إضافة وتعديل وتعطيل المدن | Normal | SA |

## الموظفون والرايدرز

| Permission Key | الوصف | المستوى | Baseline |
| --- | --- | --- | --- |
| `employees.read` | عرض بيانات الموظف غير الحساسة | Normal | M |
| `employees.create` | إنشاء سجل موظف | Normal | — |
| `employees.update` | تعديل بيانات الموظف التشغيلية | Normal | — |
| `employees.archive` | أرشفة الموظف دون حذف تاريخه | High Trust | — |
| `employees.sensitive.read` | عرض الهوية والإقامة والبيانات المقيدة | High Trust | — |
| `riders.read` | عرض ملفات الرايدرز | Normal | M |
| `riders.manage` | إدارة ملفات وحالات الرايدرز | Normal | — |
| `sponsors.read` | عرض الكفلاء وبيانات السجل | Sensitive | — |
| `sponsors.manage` | إدارة الكفلاء وفترات الكفالة | Sensitive | — |

## الالتزام النظامي

| Permission Key | الوصف | المستوى | Baseline |
| --- | --- | --- | --- |
| `residency.read` | عرض الإقامات | Sensitive | — |
| `residency.manage` | إصدار وتجديد وتحديث الإقامات | Sensitive | — |
| `licenses.read` | عرض الرخص وإصداراتها | Sensitive | — |
| `licenses.manage` | إدارة إصدار وتجديد الرخص | Sensitive | — |
| `rider_cards.read` | عرض بطاقات السائق | Sensitive | — |
| `rider_cards.manage` | إدارة بطاقات السائق | Sensitive | — |
| `health_cards.read` | عرض البطاقات الصحية | Sensitive | — |
| `health_cards.manage` | إدارة البطاقات الصحية | Sensitive | — |
| `insurance.read` | عرض وثائق ومستويات التأمين | Sensitive | — |
| `insurance.manage` | إدارة وثائق وتجديدات التأمين | Sensitive | — |
| `promissory_notes.read` | عرض بيانات سندات الأمر المالية | High Trust | — |
| `promissory_notes.manage` | إدارة حالات ونسخ سندات الأمر | High Trust | — |

## الوثائق

| Permission Key | الوصف | المستوى | Baseline |
| --- | --- | --- | --- |
| `documents.read` | عرض Metadata والنسخ دون المحتوى | Sensitive | — |
| `documents.upload` | رفع نسخة وثيقة | Sensitive | — |
| `documents.download` | تنزيل الوثائق العادية | Sensitive | — |
| `documents.download_sensitive` | تنزيل وثائق الهوية والمالية | High Trust | — |

## التشغيل والنطاقات

| Permission Key | الوصف | النطاق | Baseline |
| --- | --- | --- | --- |
| `platform_accounts.read` | عرض حسابات منصات العملاء | Client Scope | M |
| `platform_accounts.manage` | إدارة تسجيل وملكية الحسابات | Client Scope | — |
| `platform_assignments.read` | عرض تاريخ الاستخدام الفعلي | Client Scope | M |
| `platform_assignments.manage` | إدارة تكليفات الاستخدام | Client Scope | — |
| `housing.read` | عرض السكن وفترات الإقامة | Housing Scope | M |
| `housing.manage` | إدارة السكن والمشرفين والفترات | Housing Scope | — |

وجود الصلاحيات الثلاث Scoped داخل دور المدير لا يمنحه بيانات كل المنصات أو كل المساكن. يجب أن يحمل `UserRoleAssignment` قيمة All مناسبة أو سجلات `AccessScope` محددة.

## التقارير وسير العمل

| Permission Key | الوصف | المستوى | Baseline |
| --- | --- | --- | --- |
| `reports.read` | عرض التقارير المصرح بها | Normal | SA, M |
| `exports.create` | إنشاء تصدير من البيانات المصرح بها | Sensitive | — |
| `notifications.read` | عرض الإشعارات التشغيلية | Normal | M |
| `notifications.manage` | إدارة الإشعارات | Normal | — |
| `leave_requests.read` | عرض طلبات الإجازة | Sensitive | — |
| `leave_requests.manage` | إنشاء وتعديل طلبات الإجازة | Sensitive | — |
| `leave_requests.approve` | اعتماد أو رفض طلب إجازة | High Trust | — |
| `absence_cases.read` | عرض حالات الغياب والهروب | Sensitive | — |
| `absence_cases.manage` | إدارة الحالة وسجل أحداثها | Sensitive | — |
| `employee_status_changes.read` | عرض طلبات تغيير الحالة | Sensitive | — |
| `employee_status_changes.manage` | إنشاء وتعديل طلبات تغيير الحالة | Sensitive | — |
| `employee_status_changes.approve` | اعتماد تغيير حالة الموظف | High Trust | — |

العدد الإجمالي: **55 Permission Definition**. الدور `USER` لا يحمل صلاحيات أعمال أساسية؛ الوصول الذاتي للملف والجلسات تحميه مصادقة المستخدم وملكية المورد، وليس Permission عامة قد تكشف بيانات الآخرين.
