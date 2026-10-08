# Keeta account and vehicle follow-up — 8 October 2026

The source is the unchanged `C:\Users\omarf\Downloads\Keeta.xlsx` workbook, SHA-256 `0E3852755F99FA71D2B36D967C99E5F8027D2B1094D7BEC20440AF7BD18BB2CC`. Before this follow-up, the live `db67927` application database held 259 Keeta accounts and 221 active vehicle-account assignments. Full pre-change snapshots are in `outputs/keeta-followup-20261008-backup/`.

Column M (`وضع التسوية`) is the account payment type. `Slab mode` maps to the ERP `Salary` payment model and `Per order mode` maps to `PayPerOrder`. The follow-up changed 257 accounts to Salary; the two Per order accounts retained their existing model. All 259 live payment values now match the source workbook.

The follow-up created 35 more vehicle-to-account assignments using the unique ERP vehicle serial, including 21 initially held for vehicle operational status, two rejected-review accounts whose Suspended status will be flagged by the assignment problems endpoint, and 12 source/ERP plate conflicts. Those 12 contain both plate values in `AssignmentReason` and in the [initial review list](../outputs/keeta-unassigned-20261007.md). The existing problem tracker does not compare source and ERP plates, so those differences require manual review. The two pending-review accounts have no vehicle serial or plate and could not be linked.

| Current result | Count |
| --- | ---: |
| Keeta accounts | 259 |
| Active vehicle-account assignments | 256 |
| Unassigned accounts without vehicle identity | 3 |
| Salary accounts | 257 |
| PayPerOrder accounts | 2 |

The three accounts still lacking vehicle identity are:

| Source row | Iqama | Keeta ID | Name | Review |
| ---: | --- | --- | --- | --- |
| 80 | 2607221690 | 1791211650202527 | MAHMOUD SALEM | Accepted |
| 208 | 2582105843 | 1791305216202026 | JOY HOSSAIN | Pending |
| 209 | 2547821708 | 1791300204222234 | MD BABUL MIAH | Pending |

At verification time, current ERP data met the existing problem rules for 39 assignments with unavailable vehicle operational status, 58 with sponsor mismatch, two with suspended account status, and two vehicles over the Keeta city/platform capacity limit. These counts can change when vehicle, account, lease, or assignment data changes. The 12 plate differences are recorded but are not an automated problem code.

The transaction dry run rolled back successfully. Post-commit comparison found all 259 payment models and all 256 account-to-vehicle links matched the staged source, with zero mapping errors. The original 221 assignment IDs remained present.
