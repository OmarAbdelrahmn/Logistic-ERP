# Chefz rider sheet import — 13 October 2026

Status: applied and verified.

Source: `تقرير سيستم  داتا بدلاء .xlsx`, worksheet `ورقة1`, rows 2–306. Match column A to Chefz account ID and column B to the actual rider iqama. Names are ignored. Column D is day/month/year. IBAN whitespace is removed and letters are uppercased. Arabic notes in the IBAN column are treated as data exceptions, not instructions.

| Result | Count |
| --- | ---: |
| Sheet rows | 305 |
| Owner iqamas matching Chefz accounts | 304 |
| Chefz assignments created | 217 |
| Assignment rows pending | 88 |
| Previous Chefz assignments found | 0 |
| Conflicting Jahez assignment histories removed | 12 |
| Jahez assignments closed normally | 1 |
| Jahez opening ledger entries removed with conflicting histories | 36 |
| Jahez fee records removed with conflicting histories | 12 |
| Rider IBANs added | 254 |
| Rider IBANs replaced | 0 |
| Matching rider IBANs already correct | 5 |

Counts below can overlap. Duplicate riders remain pending on every affected source row. Suspended Chefz accounts remain suspended. Riders and accounts were not created for unmatched iqamas.

## Pending assignments

| Sheet row | Owner iqama | Rider iqama | Reason |
| ---: | --- | --- | --- |
| 17 | 2620008447 | 2607355886 | Rider employee missing or ambiguous |
| 19 | 2555915038 | 2580499388 | Duplicate rider iqama in sheet |
| 20 | 2588082814 | 2553239886 | Duplicate rider iqama in sheet |
| 30 | 2622291686 | 2418708117 | Duplicate rider iqama in sheet |
| 38 | 2615560725 | 2632427379 | Rider employee missing or ambiguous |
| 58 | 2578128130 | 2517027898 | Duplicate rider iqama in sheet |
| 61 | 2546064722 | 2633230970 | Duplicate rider iqama in sheet |
| 70 | 2603735420 | 2517027898 | Duplicate rider iqama in sheet |
| 71 | 2587956117 | Missing | Rider employee missing or ambiguous |
| 83 | 2620868998 | 2233903638 | Duplicate rider iqama in sheet |
| 98 | 2596606422 | 2323704763 | Duplicate rider iqama in sheet |
| 102 | 2602293777 | 2572694947 | Chefz account unavailable |
| 104 | 2612089009 | 2634841759 | Duplicate rider iqama in sheet |
| 106 | 2620688800 | 2612712600 | Duplicate rider iqama in sheet |
| 107 | 2144810625 | 2634841759 | Duplicate rider iqama in sheet |
| 110 | 2551092097 | 2329427781 | Duplicate rider iqama in sheet |
| 120 | 2601062538 | 2337457341 | Duplicate rider iqama in sheet |
| 122 | 2510059021 | 2329427781 | Duplicate rider iqama in sheet |
| 123 | 2512025293 | 2312144419 | Duplicate rider iqama in sheet |
| 125 | 2618668764 | 2594949766 | Duplicate rider iqama in sheet |
| 132 | 2585700301 | 2580499388 | Duplicate rider iqama in sheet |
| 136 | 2571262589 | 2404457430 | Duplicate rider iqama in sheet |
| 138 | 2501776401 | 2299842407 | Duplicate rider iqama in sheet |
| 150 | 2597241476 | 2606047062 | Rider unavailable |
| 152 | 2554106605 | 2299842407 | Duplicate rider iqama in sheet |
| 154 | 2612557153 | 2493112557 | Rider employee missing or ambiguous |
| 156 | 2586293173 | 2312144419 | Duplicate rider iqama in sheet |
| 157 | 2616110728 | 2564163406 | Rider employee missing or ambiguous; Duplicate rider iqama in sheet |
| 158 | 2616612368 | 2553239886 | Duplicate rider iqama in sheet |
| 163 | 2588827796 | 2580937049 | Chefz account unavailable |
| 174 | 2495975902 | 2418708117 | Duplicate rider iqama in sheet |
| 190 | 2590110595 | 2462687274 | Duplicate rider iqama in sheet |
| 199 | 2613903471 | 2578464535 | Duplicate rider iqama in sheet |
| 203 | 2614040620 | 2458663735 | Rider employee missing or ambiguous |
| 209 | 2522312970 | 2233903638 | Duplicate rider iqama in sheet |
| 210 | 2591508706 | 2017896693 | Owner Chefz account missing or ambiguous |
| 218 | 2636655173 | 2345152595 | Rider employee missing or ambiguous |
| 231 | 2341264758 | 2336593153 | Rider employee missing or ambiguous |
| 232 | 2591952813 | 2440222194 | Duplicate rider iqama in sheet |
| 234 | 2572718712 | 2564163406 | Rider employee missing or ambiguous; Duplicate rider iqama in sheet |
| 235 | 2577660661 | 2583495045 | Rider employee missing or ambiguous |
| 237 | 2577524115 | 2486856368 | Rider employee missing or ambiguous |
| 247 | 2490507635 | 2404457430 | Duplicate rider iqama in sheet |
| 250 | 2425236102 | 2499112296 | Rider employee missing or ambiguous |
| 252 | 2617590837 | 2302305301 | Duplicate rider iqama in sheet |
| 257 | 2581511629 | 2622152086 | Rider employee missing or ambiguous |
| 258 | 2630314744 | 2302305301 | Duplicate rider iqama in sheet |
| 259 | 2630315410 | 2623188535 | Chefz account unavailable |
| 260 | 2636062180 | 2339864379 | Chefz account unavailable |
| 261 | 2629018488 | 2323704763 | Chefz account unavailable; Duplicate rider iqama in sheet |
| 262 | 2602433449 | 2622386833 | Chefz account unavailable |
| 263 | 2630564751 | 2441079098 | Chefz account unavailable |
| 264 | 2576050401 | 2462687274 | Chefz account unavailable; Duplicate rider iqama in sheet |
| 265 | 2630314611 | 2562951018 | Chefz account unavailable |
| 266 | 2592789974 | 2594949766 | Chefz account unavailable; Duplicate rider iqama in sheet |
| 267 | 2590338337 | 2443672288 | Rider employee missing or ambiguous; Duplicate rider iqama in sheet |
| 268 | 2624402406 | 2328098591 | Chefz account unavailable |
| 269 | 2630315881 | 2344451212 | Chefz account unavailable |
| 270 | 2548227202 | 2443672288 | Rider employee missing or ambiguous; Duplicate rider iqama in sheet |
| 271 | 2632579013 | 2609138686 | Chefz account unavailable |
| 273 | 2598671838 | 2440222194 | Chefz account unavailable; Duplicate rider iqama in sheet |
| 274 | 2624402760 | 2578464535 | Chefz account unavailable; Duplicate rider iqama in sheet |
| 275 | 2636655306 | 2476856006 | Chefz account unavailable |
| 276 | 2595504289 | Missing | Chefz account unavailable; Rider employee missing or ambiguous |
| 277 | 2618019182 | 2633230970 | Duplicate rider iqama in sheet |
| 279 | 2636655421 | Missing | Chefz account unavailable; Rider employee missing or ambiguous |
| 280 | 2545844504 | 2607601750 | Chefz account unavailable |
| 281 | 2623827850 | Missing | Rider employee missing or ambiguous |
| 282 | 2496941606 | 2299221958 | Chefz account unavailable |
| 283 | 2556645477 | 2486850007 | Chefz account unavailable |
| 284 | 2586666717 | 2459879561 | Rider employee missing or ambiguous |
| 285 | 2591788183 | 2330313210 | Rider employee missing or ambiguous |
| 286 | 2627962364 | 2627282136 | Chefz account unavailable |
| 288 | 2630343289 | 2635452341 | Chefz account unavailable |
| 291 | 2624402620 | 2337457341 | Chefz account unavailable; Duplicate rider iqama in sheet |
| 292 | 2539395836 | Missing | Rider employee missing or ambiguous |
| 295 | 2574738866 | 2608654535 | Chefz account unavailable |
| 296 | 2584539486 | 2530717632 | Chefz account unavailable |
| 297 | 2575492794 | 2319732158 | Chefz account unavailable |
| 298 | 2563494190 | Missing | Chefz account unavailable; Rider employee missing or ambiguous |
| 299 | 2612093464 | 2515725014 | Chefz account unavailable |
| 300 | 2630343412 | 2501628420 | Chefz account unavailable |
| 301 | 2630898415 | 2534688276 | Chefz account unavailable |
| 302 | 2636062651 | 2383344435 | Chefz account unavailable |
| 303 | 2633076662 | 2612712600 | Chefz account unavailable; Duplicate rider iqama in sheet |
| 304 | 2627814672 | 2046667644 | Chefz account unavailable |
| 305 | 2609582792 | 2558565558 | Chefz account unavailable |
| 306 | 2629565314 | 2538552486 | Chefz account unavailable |

## IBAN exceptions for existing riders

| Sheet row | Rider iqama | Reason |
| ---: | --- | --- |
| 48 | 2565223969 | Missing IBAN in sheet |
| 111 | 2347059640 | Invalid Saudi IBAN length/checksum |
| 220 | 2514944897 | Missing IBAN in sheet |
| 232 | 2440222194 | Text note instead of IBAN; no action taken from note |
| 273 | 2440222194 | Text note instead of IBAN; no action taken from note |
| 280 | 2607601750 | Missing IBAN in sheet |

## Jahez histories selected for removal

The linked records consist of opening ledger entries and fee setup records. No imported transactions, dispatches, payment receipts, settlements, approval requests, or earnings statements are attached to these 12 handovers. The full affected rows are saved in `before.xml`.

| Sheet row | Rider iqama | Jahez account ID | Sheet Chefz start |
| ---: | --- | --- | --- |
| 24 | 2422907911 | 405003 | 2026-10-01 |
| 35 | 2335129470 | 474795 | 2026-10-01 |
| 53 | 2215204666 | 485320 | 2026-10-01 |
| 95 | 2192779144 | 460995 | 2026-10-01 |
| 111 | 2347059640 | 474690 | 2026-10-01 |
| 118 | 2274020839 | 404965 | 2026-10-01 |
| 145 | 2611888393 | 492330 | 2026-10-01 |
| 161 | 2580974133 | 473003 | 2026-10-01 |
| 179 | 2379284215 | 457548 | 2026-10-01 |
| 227 | 2332358486 | 459598 | 2026-10-01 |
| 228 | 2397342524 | 485304 | 2026-10-01 |
| 294 | 2586306736 | 491298 | 2026-10-01 |

## Normal Jahez closure

Rider `2514944897` (sheet row 220) ends Jahez on 12 October 2026 and starts Chefz on 13 October 2026. The existing Jahez close rule adds SAR 75 commission for 8–12 October, five days at SAR 15/day. Its earlier financial history is retained.

## Verification

The transaction checks every imported account/rider/date and changed IBAN, enforces one active account for every imported rider, and confirms unrelated account, employee, and assignment records remain unchanged. The rolled-back run verified all planned changes against SQL Server. A post-commit read repeats the comparison for the committed import.
