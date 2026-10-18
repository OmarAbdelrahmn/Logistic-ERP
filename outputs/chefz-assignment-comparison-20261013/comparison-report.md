# Chefz account assignment comparison

Compared the current ERP database on 13 October 2026, 20:20 Riyadh time with `تقرير سيستم  داتا بدلاء .xlsx`, worksheet `ورقة1`, rows 2–306.

Match key: column A (owner iqama / Chefz external account ID). Assigned rider: column B (rider iqama). Handover date: column D, interpreted as day/month/year. Current assignments have no end date and are not deleted. Names and IBANs are outside this assignment comparison.

| Result | Rows |
| --- | ---: |
| Match | 216 |
| Not assigned in system | 87 |
| Different rider | 1 |
| Account missing in system | 1 |
| Active Chefz assignments outside the sheet | 0 |

The system contains 465 Chefz accounts. 304 of the 305 sheet account IDs exist in the system. 161 system accounts are outside the supplied sheet; this alone does not indicate a missing assignment.

## Reasons for differences

Reasons can overlap on the same row. These are checked against the current data. Previous import reasons are included in the CSV separately for reference.

| Reason | Affected rows |
| --- | ---: |
| Duplicate rider iqama in sheet | 42 |
| Chefz account unavailable | 36 |
| Rider employee missing or ambiguous | 16 |
| Rider iqama missing in sheet | 6 |
| Rider assigned to another platform: JAHEZ | 2 |
| Rider unavailable | 1 |
| Different assigned rider | 1 |
| Different handover date | 1 |
| Chefz account missing | 1 |

## All assignment differences

| Sheet row | Account owner iqama | Sheet rider iqama | Sheet rider name | Sheet start date | Current rider iqama | Current start date | Difference | Reason |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| 17 | 2620008447 | 2607355886 | سلمان ابتل ورك | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 19 | 2555915038 | 2580499388 | عمرو صلاح مفتاح عبد النبي | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 20 | 2588082814 | 2553239886 | محمد ازهر بشير محمد بشير | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 30 | 2622291686 | 2418708117 | مصطفي حسين السيد صادق | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 38 | 2615560725 | 2632427379 | احمد حامد احمد محمد | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 58 | 2578128130 | 2517027898 | أصيل سعيد قاسم عماد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 61 | 2546064722 | 2633230970 | احمد عبدالفتاح عبداللطيف محمد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 70 | 2603735420 | 2517027898 | أصيل سعيد قاسم عماد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 71 | 2587956117 | Missing | #N/A | 2026-10-01 | None | None | Not assigned in system | Rider iqama missing in sheet |
| 83 | 2620868998 | 2233903638 | أنعام ال حق عبد ال حي | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 98 | 2596606422 | 2323704763 | سعيد ليجاس محمد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 102 | 2602293777 | 2572694947 | وليد نبيه السيد كشك | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 104 | 2612089009 | 2634841759 | سليمان شعبان عبد البديع زيدان | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 106 | 2620688800 | 2612712600 | عمار فراص علي قاسم جميل | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 107 | 2144810625 | 2634841759 | سليمان شعبان عبد البديع زيدان | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 110 | 2551092097 | 2329427781 | محمد علي فانتا | 2026-10-11 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 120 | 2601062538 | 2337457341 | ابراهيم كاسي وسي | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 122 | 2510059021 | 2329427781 | محمد علي فانتا | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 123 | 2512025293 | 2312144419 | عبد الباري غزالي خضير | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 125 | 2618668764 | 2594949766 | فهد محمد احمد عبدالحميد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 132 | 2585700301 | 2580499388 | عمرو صلاح مفتاح عبد النبي | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 136 | 2571262589 | 2404457430 | محمد الشريف احمد عبد الرحيم محمدين | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 138 | 2501776401 | 2299842407 | عبدالعليم عبدالسلام سيف خالد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 150 | 2597241476 | 2606047062 | ايمن عبدالمعطي ابراهيم محمد | 2026-10-01 | None | None | Not assigned in system | Rider unavailable |
| 152 | 2554106605 | 2299842407 | عبدالعليم عبدالسلام سيف خالد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 154 | 2612557153 | 2493112557 | مجاهد منصر هادي احمد ذييب | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 156 | 2586293173 | 2312144419 | عبد الباري غزالي خضير | 2026-10-10 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 157 | 2616110728 | 2564163406 | اسلام عيسى احمد عيسى | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet; Rider employee missing or ambiguous |
| 158 | 2616612368 | 2553239886 | محمد ازهر بشير محمد بشير | 2026-10-11 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 163 | 2588827796 | 2580937049 | عبد الرحمن أحمد نسيم منير | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 174 | 2495975902 | 2418708117 | مصطفي حسين السيد صادق | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 190 | 2590110595 | 2462687274 | وائل مصطفى الحاج جمعه | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet; Rider assigned to another platform: JAHEZ |
| 199 | 2613903471 | 2578464535 | سيكاندا علي محمد خان | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 203 | 2614040620 | 2458663735 | اسماعيل عبد الله علي مونس | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 207 | 2570298998 | 2334140718 | خالد ختم العارفين حسن عبدالرحيم | 2026-10-01 | 2497250247 | 2026-10-13 | Different rider | Different assigned rider; Different handover date |
| 209 | 2522312970 | 2233903638 | أنعام ال حق عبد ال حي | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 210 | 2591508706 | 2017896693 | اسماعيل محمد - عثمان | 2026-10-01 | None | None | Account missing in system | Chefz account missing |
| 218 | 2636655173 | 2345152595 | ساني محمد كاساو | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 231 | 2341264758 | 2336593153 | اشرف محمود محمد النفياوي | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 232 | 2591952813 | 2440222194 | فاسمير علالدين فاسيل | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 234 | 2572718712 | 2564163406 | اسلام عيسى احمد عيسى | 2026-10-08 | None | None | Not assigned in system | Duplicate rider iqama in sheet; Rider employee missing or ambiguous |
| 235 | 2577660661 | 2583495045 | حماده فتحى محمد عبد العزيز | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 237 | 2577524115 | 2486856368 | احمد عبدالمنعم عبدالخالق عطيه | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 247 | 2490507635 | 2404457430 | محمد الشريف احمد عبد الرحيم محمدين | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 250 | 2425236102 | 2499112296 | محمد عبده محمد صالح | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 252 | 2617590837 | 2302305301 | حافظ بشير آدم آدم | 2026-10-08 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 257 | 2581511629 | 2622152086 | مدحت مصطفى على عسكر | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 258 | 2630314744 | 2302305301 | حافظ بشير آدم آدم | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 259 | 2630315410 | 2623188535 | محمد عارف محمد ابراهيم | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 260 | 2636062180 | 2339864379 | عبد القادر طه سعيد | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 261 | 2629018488 | 2323704763 | سعيد ليجاس محمد | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Duplicate rider iqama in sheet |
| 262 | 2602433449 | 2622386833 | هاني سعيد محمد رضوان | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 263 | 2630564751 | 2441079098 | محمد محمد السعيد عبد الوهاب | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 264 | 2576050401 | 2462687274 | وائل مصطفى الحاج جمعه | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Duplicate rider iqama in sheet; Rider assigned to another platform: JAHEZ |
| 265 | 2630314611 | 2562951018 | عمران كاد امبيري كاد امبيري بوليكال | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 266 | 2592789974 | 2594949766 | فهد محمد احمد عبدالحميد | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Duplicate rider iqama in sheet |
| 267 | 2590338337 | 2443672288 | احمد حامد على عبد الرحيم | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet; Rider employee missing or ambiguous |
| 268 | 2624402406 | 2328098591 | عبد المولا كاسو | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 269 | 2630315881 | 2344451212 | عبدالله احمد احمد الفروي | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 270 | 2548227202 | 2443672288 | احمد حامد على عبد الرحيم | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet; Rider employee missing or ambiguous |
| 271 | 2632579013 | 2609138686 | جمال محمد الأوربية | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 273 | 2598671838 | 2440222194 | فاسمير علالدين فاسيل | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Duplicate rider iqama in sheet |
| 274 | 2624402760 | 2578464535 | سيكاندا علي محمد خان | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Duplicate rider iqama in sheet |
| 275 | 2636655306 | 2476856006 | عمران صالح احمد صالح المشرقي | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 276 | 2595504289 | Missing | #N/A | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Rider iqama missing in sheet |
| 277 | 2618019182 | 2633230970 | احمد عبدالفتاح عبداللطيف محمد | 2026-10-01 | None | None | Not assigned in system | Duplicate rider iqama in sheet |
| 279 | 2636655421 | Missing | #N/A | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Rider iqama missing in sheet |
| 280 | 2545844504 | 2607601750 | احمد علي احمد سفيان | 2026-10-11 | None | None | Not assigned in system | Chefz account unavailable |
| 281 | 2623827850 | Missing | #N/A | 2026-10-01 | None | None | Not assigned in system | Rider iqama missing in sheet |
| 282 | 2496941606 | 2299221958 | منير بشير سيد | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 283 | 2556645477 | 2486850007 | اسعد جعفر عبد ال جعفر | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 284 | 2586666717 | 2459879561 | حسين مرعى حسين حامد | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 285 | 2591788183 | 2330313210 | احمد محمد ابر | 2026-10-01 | None | None | Not assigned in system | Rider employee missing or ambiguous |
| 286 | 2627962364 | 2627282136 | ياسين على عبداللطيف علام | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 288 | 2630343289 | 2635452341 | محمود احمد حجازى جوده | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 291 | 2624402620 | 2337457341 | ابراهيم كاسي وسي | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Duplicate rider iqama in sheet |
| 292 | 2539395836 | Missing | #N/A | 2026-10-01 | None | None | Not assigned in system | Rider iqama missing in sheet |
| 295 | 2574738866 | 2608654535 | سليم حسن | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 296 | 2584539486 | 2530717632 | ابراهيم عربى ابراهيم محجوب | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 297 | 2575492794 | 2319732158 | شمس جميل | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 298 | 2563494190 | Missing | #N/A | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Rider iqama missing in sheet |
| 299 | 2612093464 | 2515725014 | باسم صالح عبده علي | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 300 | 2630343412 | 2501628420 | عبدالرحمن حسين ناصر الكاملي | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 301 | 2630898415 | 2534688276 | عبد العزيز ابراهيم عبد العزيز رمضان | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 302 | 2636062651 | 2383344435 | عبد الرحمن جعفر يوسف حاج على | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 303 | 2633076662 | 2612712600 | عمار فراص علي قاسم جميل | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable; Duplicate rider iqama in sheet |
| 304 | 2627814672 | 2046667644 | رائد جواد سليم الحرثاني | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 305 | 2609582792 | 2558565558 | وثيق محمد شمسان حيدر | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |
| 306 | 2629565314 | 2538552486 | احمد حسن محمد الصعيدى | 2026-10-01 | None | None | Not assigned in system | Chefz account unavailable |

## Riders repeated in the sheet

| Rider iqama | Source rows | Account owner iqamas |
| --- | --- | --- |
| 2580499388 | 19, 132 | 2555915038, 2585700301 |
| 2553239886 | 20, 158 | 2588082814, 2616612368 |
| 2418708117 | 30, 174 | 2622291686, 2495975902 |
| 2517027898 | 58, 70 | 2578128130, 2603735420 |
| 2633230970 | 61, 277 | 2546064722, 2618019182 |
| 2233903638 | 83, 209 | 2620868998, 2522312970 |
| 2323704763 | 98, 261 | 2596606422, 2629018488 |
| 2634841759 | 104, 107 | 2612089009, 2144810625 |
| 2612712600 | 106, 303 | 2620688800, 2633076662 |
| 2329427781 | 110, 122 | 2551092097, 2510059021 |
| 2337457341 | 120, 291 | 2601062538, 2624402620 |
| 2312144419 | 123, 156 | 2512025293, 2586293173 |
| 2594949766 | 125, 266 | 2618668764, 2592789974 |
| 2404457430 | 136, 247 | 2571262589, 2490507635 |
| 2299842407 | 138, 152 | 2501776401, 2554106605 |
| 2564163406 | 157, 234 | 2616110728, 2572718712 |
| 2462687274 | 190, 264 | 2590110595, 2576050401 |
| 2578464535 | 199, 274 | 2613903471, 2624402760 |
| 2440222194 | 232, 273 | 2591952813, 2598671838 |
| 2302305301 | 252, 258 | 2617590837, 2630314744 |
| 2443672288 | 267, 270 | 2590338337, 2548227202 |
