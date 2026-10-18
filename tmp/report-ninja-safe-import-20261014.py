import json, sys, xml.etree.ElementTree as ET
from pathlib import Path

out=Path(sys.argv[1]).resolve()
plan=json.loads((out/'plan.json').read_text(encoding='utf-8-sig'))
summary=json.loads((out/'summary.json').read_text(encoding='utf-8-sig'))
checks=ET.parse(out/'post-commit-verification.xml').getroot().findall('Checks')
assert len(checks)==20 and all(int(c.findtext('Mismatches'))==0 for c in checks)
added=[r for r in plan['rows'] if r['accountAction']=='Add']
skipped=[r for r in plan['rows'] if r['accountAction']=='Skip']
assigned=[r for r in added if r['assignmentAction']=='Assign']
pending=[r for r in added if r['assignmentAction']=='Pending']
assert (len(added),len(skipped),len(assigned),len(pending))==(summary['AccountsAdded'],summary['AccountsSkipped'],summary['AssignmentsCreated'],summary['WorkingAccountsPending'])
lines=['# نتيجة استيراد حسابات نينجا — 14 أكتوبر 2026','',
       'تم حفظ الحسابات والتعيينات التالية في قاعدة البيانات، ثم التحقق من النتائج باستخدام اتصال جديد. اجتازت فحوص التحقق العشرون دون اختلافات.', '',
       '| النتيجة | العدد |','|---|---:|',
       f"| حسابات تمت إضافتها | {len(added)} |",f"| حسابات تم تعيينها بإقامة المستخدم الفعلي | {len(assigned)} |",
       f"| حسابات تعمل تمت إضافتها وبقي تعيينها معلقًا | {len(pending)} |",f"| حسابات تم استبعادها من الإضافة | {len(skipped)} |",
       '| حسابات لا تعمل تمت إضافتها دون تعيين | 93 |','| حسابات بها مشكلة تمت إضافتها كحسابات موقوفة | 76 |','',
       'تاريخ بدء جميع التعيينات الجديدة: **14 أكتوبر 2026**. لم تُنشأ تعيينات بأثر رجعي.', '',
       'حالات الحسابات المحفوظة: 107 معيّنة، و118 متاحة دون تعيين، و76 موقوفة. تشمل الحسابات المتاحة 93 حسابًا وردت بحالة «لايعمل» و25 حسابًا يعمل يحتاج إلى معالجة بيانات المستخدم أو تعارض التعيين.', '',
       'تم حفظ المالك والكفيل والمدينة بالربط مع السجلات الموجودة. حُفظ نوع مركبة الحساب وحالته الأصلية من الملف في ملاحظات الحساب، وحُفظت أرقام الهواتف الصحيحة في بيانات نينجا. لم يتم تعديل نوع عمل الموظف صاحب الحساب.', '',
       'بقيت بيانات الموظفين وملفات المندوبين والتعيينات السابقة وكلمات المرور دون تغيير. بقي تعيين مستخدم الإقامة 2492838038 على حساب شيفز 2618863134 قائمًا؛ ولم يُعيّن على حساب نينجا 215746.', '',
       '## الحسابات التي لم تتم إضافتها','',
       'لم تُنشأ هذه الحسابات لأن بيانات ملكيتها لا تستوفي قواعد إنشاء الحساب الحالية. يوجد 26 حسابًا صاحبها مسجل كموظف مكتب، وحساب واحد صاحبه غير موجود، وحساب واحد به تعارض في المالك والمدينة والكفيل. بعض الموظفين ليس لديهم أيضًا ملف مندوب متاح.', '',
       '| صف إكسل | رقم حساب نينجا | إقامة صاحب الحساب | سبب الاستبعاد |','|---:|---|---|---|']
for r in skipped:
    lines.append(f"| {r['row']} | {r['externalId']} | {r['ownerIqama']} | {'؛ '.join(r['accountIssues'])} |")
lines+=['','## الحسابات التي أُضيفت وبقي تعيينها معلقًا','',
        'تم إنشاء الحسابات التالية وربط ملاكها، لكن لم يتم تعيينها لمستخدم. تتضمن 14 خانة مستخدم تحتوي على وصف بدل الإقامة، و10 إقامات غير موجودة في النظام، وتعارض تعيين واحد مع شيفز.', '',
        '| صف إكسل | رقم حساب نينجا | إقامة صاحب الحساب | القيمة في خانة المستخدم | سبب تعليق التعيين |','|---:|---|---|---|---|']
for r in pending:
    lines.append(f"| {r['row']} | {r['externalId']} | {r['ownerIqama']} | {r['userRaw']} | {'؛ '.join(r['assignmentIssues'])} |")
lines+=['','## التعيينات التي تم حفظها','',
        'صاحب الحساب والمستخدم الفعلي يظهران منفصلين أدناه. تم تحديد المستخدم الفعلي بمطابقة رقم إقامته مع سجل مندوب نشط لا يحمل حساب منصة نشطًا آخر.', '',
        '| رقم حساب نينجا | إقامة صاحب الحساب | إقامة المستخدم الفعلي | المدينة |','|---|---|---|---|']
for r in assigned:
    lines.append(f"| {r['externalId']} | {r['ownerIqama']} | {r['userIqama']} | {'جدة' if r['city']=='Jeddah' else 'الرياض'} |")
lines+=['','المصدر: «بيانات نينجا بعد التحديث.xlsx»، ورقة «بيانات نينجا».', '',
        f"بصمة ملف المصدر SHA-256: `{plan['sourceSha256']}`.",'',
        f"معرّف عملية الاستيراد في سجل التدقيق: `{plan['correlationId']}`."]
target=out/'result-ar.md'
target.write_text('\n'.join(lines)+'\n',encoding='utf8')
print(f'Arabic report saved; reconciled {len(skipped)} excluded accounts, {len(pending)} pending assignments, and {len(assigned)} saved assignments.')
print(target)
