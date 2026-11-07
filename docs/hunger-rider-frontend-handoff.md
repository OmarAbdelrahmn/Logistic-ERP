# هنقر — تحديث عرض المندوب وحالة الحساب

في جدول حسابات هنقر، اعرض عمودًا واحدًا للمندوب بعنوان **المندوب المكلف** باستخدام `actualRider` من `GET /api/hunger/accounts`. اعرض الاسم من `actualRider.nameAr`، واترك القيمة فارغة أو اعرض «غير مكلف» عند عدم وجود تكليف نشط. عند تغيير التكليف، أعد تحميل الحساب لعرض المندوب الحالي.

حقل `assignmentStatus` يعيد `Assigned` أو `Unassigned`. حقول `currentAssignmentId` و`assignmentFrom` و`assignmentRowVersion` تخص التكليف الحالي. استخدم بيانات التكليف عند تنفيذ إجراءات التسليم والاستلام الحالية.

تعني حالة المصدر **يعمل** أن الحساب نشط تشغيليًا وقابل للتكليف بعد استيفاء شروط التسليم. تقابلها قيمة `operationalDisposition` وهي `Normal`. لا تستنتج من هذه الحالة أن الحساب مكلف أو أن المندوب عمل عليه. يمكن عرض حالة الحساب في عمود حالة واحد مستقل عن عمود المندوب.

حقل `sourceActualRider` وحقول `sourceActual*` تمثل بيانات الملف المستورد للمراجعة. لا تستخدمها لعرض المندوب الحالي. للتوافق مع الواجهات القائمة، يطابق `reportedCurrentRider` قيمة `actualRider` ويطابق `reportedAssignmentStatus` قيمة `assignmentStatus`. يعيد `currentRiderSource` القيمة `Assignment` عند وجود تكليف و`None` عند عدم وجوده.

حالة الاستخدام الشهري `monthlyUsageStatus` تصبح `Used` عند تسجيل ساعات عمل فعلية موجبة خلال الشهر، وإلا تكون `Unknown`. مصدر الدليل في `monthlyUsageSource` هو `ObservedWorkingHours` أو `None`.

تحتاج هذه السلوكيات إلى نشر إصدار API الذي يتضمن التغيير. تحديث الواجهة ونشرها عمل مستقل.
