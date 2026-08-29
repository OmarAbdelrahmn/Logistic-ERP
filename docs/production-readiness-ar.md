# قائمة تجهيز النظام للنشر والأسرار المطلوبة

## الحالة الحالية

الموجود الآن هو أساس قاعدة البيانات والـDomain وتهيئات EF Core وAuth/Session Services وControllers وملف المستخدم و3 أدوار محمية و55 صلاحية ومحرك Authorization ديناميكي وتشغيل API/Swagger. هذا الأساس **ليس نظامًا إنتاجيًا كاملًا بعد**؛ لا توجد Services أو Controllers لأعمال الموظفين والرايدرز أو إدارة المستخدمين والصلاحيات، ولا رفع/تنزيل ملفات، ولا واجهة Next.js، ولا استيراد Excel.

## أسرار يجب توفيرها للتطبيق الحالي

لا تضع القيم الحقيقية في `appsettings.json` ولا في Git. استخدم متغيرات بيئة في الخادم أو مدير أسرار مثل Azure Key Vault أو AWS Secrets Manager أو خدمة الأسرار في منصة الاستضافة.

| متغير البيئة | هل هو سر؟ | المطلوب |
| --- | --- | --- |
| `ConnectionStrings__LogisticsDatabase` | نعم | Connection String لمستخدم SQL محدود الصلاحيات مع تشفير الاتصال. |
| `Authentication__SigningKey` | نعم | قيمة عشوائية قوية لا تقل عن 64 بايت، مستقلة لكل بيئة، وتُدوّر بخطة مدروسة. |
| `Authentication__Issuer` | لا | اسم ثابت للجهة المصدرة للتوكن في الإنتاج. |
| `Authentication__Audience` | لا | اسم/عنوان الـAPI الذي تقبله التوكنات. |
| `Cors__AllowedOrigins__0` | لا | رابط واجهة Next.js الإنتاجي فقط، ثم أرقام إضافية عند الحاجة. |

إعدادات Auth غير السرية الاختيارية للأعمار هي `Authentication__AccessTokenMinutes` و`Authentication__RefreshTokenIdleDays` و`Authentication__RefreshTokenAbsoluteDays`. سياسة الأمان الحالية تفرض `Authentication__MaxActiveSessions=1` و`Authentication__SessionValidationCacheSeconds=0`؛ يرفض التطبيق التشغيل إذا حاولت بيئة النشر تغييرهما، لأن ذلك سيكسر ضمان الخروج الفوري من الجهاز السابق.

يجب تدوير كلمة مرور قاعدة البيانات الحالية قبل أي نشر لأنها وُضعت في ملف إعدادات محلي. بعد التدوير، احذف السر من الملف ومن تاريخ Git إن كان قد تم عمل commit له، وراجع سجلات البناء والنشر للتأكد أنه لم يظهر فيها.

مثال أسماء متغيرات البيئة فقط، من دون قيم حقيقية:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__LogisticsDatabase=<from-secret-store>
Authentication__SigningKey=<from-secret-store>
Authentication__Issuer=LogisticsERP.Api.Production
Authentication__Audience=LogisticsERP.Web.Production
Cors__AllowedOrigins__0=https://erp.example.com
```

## أسرار مطلوبة بعد تنفيذ الميزات التالية

حقول الإقامة والرخص والبطاقات والتأمين مصممة لتخزين Ciphertext وLookup Hash، لكن خدمة التشفير لم تُنفذ في هذه المرحلة. قبل إدخال أي بيانات حقيقية يجب تنفيذها ثم إضافة أسرار مثل:

| السر المستقبلي المقترح | الغرض |
| --- | --- |
| `SensitiveData__EncryptionKey` | تشفير أرقام الإقامة والرخص والبطاقات والوثائق التأمينية. |
| `SensitiveData__LookupHmacKey` | إنشاء بصمات بحث دقيقة لا تكشف الرقم الأصلي. |
| `SensitiveData__KeyVersion` | معرفة إصدار المفتاح لتدويره وترحيل البيانات بأمان؛ الإعداد نفسه ليس سرًا. |
| `DataProtection__CertificatePassword` | حماية مفاتيح ASP.NET Data Protection عند استخدام شهادة؛ لا يلزم إذا تولى Key Vault الحماية مباشرة. |
| `Storage__AccessKey` أو هوية مُدارة | إذا انتقلت الملفات من القرص المحلي إلى Object Storage خاص. |
| `Telemetry__ConnectionString` | ربط المراقبة المركزية عند استخدام مزود يحتاج مفتاحًا. |

يجب ألا يكون اسم المفتاح أو المفتاح نفسه ثابتًا داخل الكود. يفضّل استخدام Key Vault وهوية مُدارة، مع فصل مفاتيح Development وStaging وProduction وخطة تدوير واستعادة.

## العمل المطلوب قبل أول نشر إنتاجي

1. تدوير سر قاعدة البيانات الحالي وإزالته من الملفات وتاريخ المستودع عند الحاجة.
2. تسجيل الدخول والجلسات وJWT وملف المستخدم ومحرك الصلاحيات وسياسة تغيير كلمة المرور وأداة أول مسؤول منفذة. المتبقي هو Services وControllers لإدارة المستخدمين وتكليف الأدوار والمنح والمنع والنطاقات.
3. تنفيذ Services وControllers والتحقق من المدخلات وقواعد الإغلاق/التجديد والحذف المنطقي لكل النماذج.
4. تنفيذ تشفير الحقول الحساسة وHMAC للبحث مع إدارة إصدار المفتاح وتدويره، ثم منع تسجيل هذه القيم في Logs أو Audit payloads.
5. تنفيذ رفع وتنزيل الوثائق عبر Endpoints محمية بالصلاحيات، مع أسماء عشوائية، فحص MIME والتوقيع الحقيقي والحجم وSHA-256 وفحص برمجيات خبيثة. لا تُفعّل Static Files للمجلد الخاص.
6. بناء واجهة Next.js وربطها بالـAPI، وضبط CORS على نطاق الواجهة المحدد فقط. تحميل `Get All` والبحث الحي يجب قياسه على أكثر من 500 سجل؛ إن زاد الحجم لاحقًا، انتقل إلى pagination/search من الخادم قبل أن يصبح الأداء مشكلة.
7. إنشاء قاعدة Production منفصلة، أخذ Backup، وتجربة سكربتات الترحيل على نسخة Staging أولًا. شغّل `application.sql` و`identity.sql` من Pipeline بصلاحية ترحيل منفصلة، وليس بحساب التطبيق اليومي.
8. إعداد HTTPS وشهادة موثوقة وReverse Proxy مضبوط، وتقييد `AllowedHosts`، والتحقق من إعدادات Forwarded Headers للمضيف الفعلي.
9. تحديد مكان دائم وآمن للوثائق؛ القرص المحلي غير مناسب إذا كانت الاستضافة مؤقتة أو متعددة النسخ. فعّل تشفير التخزين، النسخ الاحتياطي، وسياسة الاستعادة.
10. إضافة اختبارات للوحدات والتكامل والصلاحيات والترحيلات والتحميل، ثم فحص أمني واعتماد نسخة قبل الإنتاج. تأجيل الاختبارات مقبول لهذه المرحلة فقط، وليس قبل نشر بيانات حقيقية.
11. إعداد Logs مركزية بدون بيانات شخصية، مراقبة Health/Errors/Latency، تنبيهات، Audit retention، ونسخ احتياطي مجرّب الاستعادة.
12. مراجعة حماية البيانات السعودية وسياسات الاحتفاظ والوصول والتصدير، وتوثيق من يستطيع رؤية الأرقام الكاملة أو الملفات.

## تشغيل الترحيلات في النشر

السكربتان idempotent موجودان في:

- `database/scripts/application.sql`
- `database/scripts/identity.sql`

لا تمنح حساب تشغيل الـAPI صلاحية تعديل المخطط. استخدم حساب نشر مؤقتًا لتطبيق الترحيلات، ثم شغّل التطبيق بحساب SQL محدود إلى القراءة والكتابة اللازمة فقط.

تفضّل Design-time factories متغير البيئة `ConnectionStrings__LogisticsDatabase` وتستخدم LocalDB فقط عند غيابه. لذلك يجب ضبط المتغير في نفس عملية الـShell قبل `dotnet ef database update` وإزالته بعدها، والتأكد من اسم قاعدة Staging/Production قبل التنفيذ.

## إنشاء أول مسؤول Production

لا يوجد مستخدم أو كلمة مرور افتراضية للإنتاج، ولا يوجد Endpoint عام للتهيئة. بعد تطبيق ترحيلات Identity شغّل أداة الـConsole مرة واحدة من جهاز إداري آمن:

```powershell
$env:ConnectionStrings__LogisticsDatabase = "<from-secret-store>"
dotnet run --project tools/LogisticsERP.Bootstrap/LogisticsERP.Bootstrap.csproj
Remove-Item Env:ConnectionStrings__LogisticsDatabase
```

تطلب الأداة Username وEmail والاسمين العربي والإنجليزي، ثم كلمة مرور مؤقتة وتأكيدها بإدخال مخفي. وتقوم بما يلي:

- تتحقق من وجود الدور المحمي `SYSTEM_ADMIN`.
- ترفض التنفيذ إذا كان مسؤول إنتاج فعّال موجودًا.
- تطبق قواعد كلمة المرور نفسها المستخدمة في الـAPI.
- تنشئ المستخدم والتكليف في عملية حفظ واحدة، وتجعله `PendingTemporaryPassword` مع `RequiresPasswordChange=true`.
- لا تطبع كلمة المرور ولا تحفظها في ملف أو Migration.

## حساب Omar المحلي

`Omar / P@ssword1234` خاص ببيئة `Development` فقط وموسوم `IsDevelopmentOnly`. كود Login وفحص الجلسات يرفض هذا الحساب في `Production` حتى عند مشاركة قاعدة البيانات. لا تغيّر `ASPNETCORE_ENVIRONMENT` إلى `Development` على خادم منشور، ولا تستخدم كلمة المرور المحلية لحساب إنتاج.
