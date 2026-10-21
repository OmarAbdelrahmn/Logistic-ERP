"""Read-only reconstruction from a committed roster, sponsor audit chain and source snapshot."""
import collections
import datetime as dt
from decimal import Decimal
import hashlib
import json
from pathlib import Path
import sys
import uuid
import xml.etree.ElementTree as ET


def tables(path):
    result = collections.defaultdict(list)
    for row in ET.parse(path).getroot():
        if not row.tag.startswith("{"):
            result[row.tag].append({c.tag: c.text for c in row})
    return result


def instant(value):
    return dt.datetime.fromisoformat(value.replace("Z", "+00:00"))


def key(value):
    return value.lower() if value else None


def new_id():
    return str(uuid.uuid4())


def main(folder, roster_folder):
    source = tables(folder / "source-evidence.xml")
    meta = tables(folder / "metadata-evidence.xml")
    roster_path = roster_folder / "plan.json"
    roster = json.loads(roster_path.read_text(encoding="utf-8-sig"))
    workbook_hash = hashlib.sha256((roster_folder / "source.xlsx").read_bytes()).hexdigest()
    assert workbook_hash == roster["sourceSha256"], "The original workbook hash differs."
    imports = meta["Table"]
    assert len(imports) == 1 and imports[0]["CorrelationId"] == roster["correlationId"], "Committed roster audit is required."
    assert json.loads(imports[0]["BeforeJson"])["SourceSha256"] == workbook_hash, "Roster audit does not prove the supplied workbook."
    imported_at = instant(imports[0]["OccurredAtUtc"])
    baseline = {key(a["accountId"]): a for a in roster["accounts"]}
    accounts = {key(a["Id"]): a for a in source["Table"]}
    sessions = {key(s["Id"]): s for s in source["Table1"]}
    sponsors = {key(s["Id"]): s for s in source["Table10"]}
    sponsor_backup = roster_folder / "app.Sponsors-before.xml"
    assert sponsor_backup.exists(), "The original sponsor identity backup is required."
    original_sponsors = {key(s["Id"]): s for rows in tables(sponsor_backup).values() for s in rows}
    for sponsor_id, sponsor in sponsors.items():
        original = original_sponsors.get(sponsor_id)
        assert original and all(original.get(n) == sponsor.get(n) for n in ("RegistryNameAr", "EmployerIdentityNumber")), \
            "Historical sponsor identity/name differs from the original backup; finance review is required."
    users = {key(u["Id"]): u for u in meta["Table1"]}
    routes = meta["Table2"]
    existing_owners = {(int(o["Source"]), key(o["SourceId"])): o for o in meta["Table3"]}
    audits = collections.defaultdict(list)
    for audit in source["Table6"]:
        audits[key(audit["EntityId"])].append(audit)
    failures = []
    timelines = {}
    for account_id, account in accounts.items():
        if account_id not in baseline:
            failures.append(f"Missing original roster evidence: {account['ExternalAccountId']}")
            continue
        current = key(baseline[account_id]["sponsorId"])
        timeline = [(imported_at, current, "committed October 17 roster")]
        for audit in sorted(audits[account_id], key=lambda a: (instant(a["OccurredAtUtc"]), int(a["Sequence"]))):
            if instant(audit["OccurredAtUtc"]) < imported_at:
                continue
            before = json.loads(audit.get("BeforeJson") or "{}")
            after = json.loads(audit.get("AfterJson") or "{}")
            if "SponsorId" not in before and "SponsorId" not in after:
                continue
            if "SponsorId" not in before or "SponsorId" not in after or key(before["SponsorId"]) != current:
                failures.append(f"Incomplete sponsor audit chain: {account['ExternalAccountId']}")
                break
            current = key(after["SponsorId"])
            timeline.append((instant(audit["OccurredAtUtc"]), current, f"sponsor audit sequence {audit['Sequence']}"))
        if current != key(account.get("SponsorId")):
            failures.append(f"Current sponsor differs from proven chain: {account['ExternalAccountId']}")
        timelines[account_id] = timeline

    owners = {}

    def own(source_kind, row, at=None, parent=None):
        session = sessions[key(row["HandoverId"])] if "HandoverId" in row else sessions[parent["sessionId"]]
        account_id = key(session["PlatformRiderAccountId"])
        if parent:
            sponsor_id, proof = parent["sponsorId"], f"Inherited from source {parent['source']} ({parent['sourceId']})"
        else:
            when = instant(at)
            candidates = [p for p in timelines.get(account_id, []) if p[0] <= when]
            if not candidates:
                failures.append(f"Source predates verified roster: {accounts[account_id]['ExternalAccountId']}")
                return None
            _, sponsor_id, proof = candidates[-1]
            proof = f"Ownership at {at}; {proof}; original roster SHA256={workbook_hash}"
        record_key = (source_kind, key(row["Id"]))
        existing = existing_owners.get(record_key)
        if existing and (key(existing["SponsorId"]) != sponsor_id or key(existing["PlatformAccountId"]) != account_id):
            failures.append(f"Existing frozen ownership differs: {accounts[account_id]['ExternalAccountId']}")
        sponsor = sponsors[sponsor_id]
        result = dict(id=existing["Id"] if existing else new_id(), source=source_kind, sourceId=key(row["Id"]),
                      platformAccountId=account_id, sessionId=key(session["Id"]), sponsorId=sponsor_id,
                      sponsorIdentifier=sponsor["EmployerIdentityNumber"], sponsorNameAr=sponsor["RegistryNameAr"], evidence=proof)
        owners[record_key] = result
        return result

    fees_by_session = {}
    for fee in source["Table2"]:
        fees_by_session[key(fee["HandoverId"])] = own(1, fee, fee["CreatedAtUtc"])
    for operation in source["Table3"]:
        if operation["Status"] == "3":
            if not operation.get("ExecutedAtUtc"):
                failures.append("Executed source has no execution timestamp.")
                continue
            own(2, operation, operation["ExecutedAtUtc"])
    for debt in source["Table5"]:
        parent = owners.get((2, key(debt["OperationId"])))
        if parent:
            own(3, debt, parent=parent)
        else:
            failures.append("Transferred debt has no proven originating operation.")
    for movement in source["Table4"]:
        if movement.get("TransferredDebtId"):
            parent = owners.get((3, key(movement["TransferredDebtId"])))
        elif movement.get("OperationId"):
            parent = owners.get((2, key(movement["OperationId"])))
        elif movement["Purpose"] == "3":
            parent = fees_by_session.get(key(movement["HandoverId"]))
        else:
            parent = None
        if parent:
            own(4, movement, parent=parent)
        else:
            failures.append("Movement has no proven original obligation.")
    if failures:
        (folder / "unresolved-evidence.json").write_text(json.dumps(failures, ensure_ascii=False, indent=2), encoding="utf-8")
        raise ValueError("Historical attribution is unresolved; no financial plan generated: " + "; ".join(failures))

    def route(sponsor_id, location, purpose):
        matches = [a for a in routes if key(a.get("SponsorId")) == sponsor_id and int(a["Location"]) == location
                   and int(a["Purpose"]) in (purpose, 3) and a["IsDefault"] == "true" and a["IsActive"] == "true" and a["IsPosting"] == "true"]
        assert len(matches) == 1, "Missing or ambiguous treasury routing."
        return matches[0]

    def display_name(user_id):
        user = users.get(key(user_id), {})
        return next((user[n] for n in ("DisplayNameAr", "DisplayNameEn", "UserName") if user.get(n)), "اسم المستخدم غير متاح")

    postings = []
    movement_posts = {}
    for m in source["Table4"]:
        owner = owners[(4, key(m["Id"]))]
        location = {1: 1, 2: 4, 3: 3}[int(m["Fund"])]
        account = route(owner["sponsorId"], location, 2 if m["Purpose"] == "3" else 1)
        amount = Decimal(m["Amount"]) * (1 if m["Direction"] == "1" else -1)
        assert amount != 0
        post = dict(id=new_id(), subAccountId=key(account["Id"]), sponsorId=owner["sponsorId"], moneyMovementId=key(m["Id"]),
                    cashboxHandoverId=None, batchId=new_id(), revision=0, reversesPostingId=None, kind=1,
                    amount=str(amount), occurredAtUtc=m["OccurredAtUtc"], actorUserId=key(m["ActorUserId"]),
                    actorNameAr=display_name(m["ActorUserId"]), reference=m.get("Reference") or "ترحيل حركة جاهز محفوظة", reason=m["Reason"])
        postings.append(post)
        movement_posts[key(m["Id"])] = post
    lines, memberships = [], []
    for handover in source["Table8"]:
        assert handover["Status"] == "2", "Complete or cancel pending legacy handovers before carryforward."
        grouped = collections.defaultdict(list)
        linked = [movement_posts[key(link["MoneyMovementId"])] for link in source["Table9"] if key(link["CashboxHandoverId"]) == key(handover["Id"])]
        assert len(linked) == int(handover["MovementCount"]), "Legacy handover membership differs from frozen total."
        for post in linked:
            grouped[post["subAccountId"]].append(post)
            memberships.append(dict(id=new_id(), handoverId=key(handover["Id"]), postingId=post["id"]))
        fund_totals = collections.defaultdict(Decimal)
        for account_id, group in grouped.items():
            account = next(a for a in routes if key(a["Id"]) == account_id)
            sponsor = sponsors[key(account["SponsorId"])]
            amount = sum((Decimal(p["amount"]) for p in group), Decimal(0))
            fund_totals[int(account["Location"])] += amount
            lines.append(dict(id=new_id(), handoverId=key(handover["Id"]), subAccountId=account_id,
                              accountCode=account["Code"], accountNameAr=account["NameAr"], sponsorId=key(account["SponsorId"]),
                              sponsorNameAr=sponsor["RegistryNameAr"], sponsorIdentifier=sponsor["EmployerIdentityNumber"],
                              amount=str(amount), movementCount=len(group)))
            if account["Location"] == "1" and amount:
                assert amount > 0, "A legacy custody deficit needs documented funding; never invent it."
                central = route(key(account["SponsorId"]), 2, int(account["Purpose"]))
                batch = new_id()
                for destination, signed in ((account, -amount), (central, amount)):
                    postings.append(dict(id=new_id(), subAccountId=key(destination["Id"]), sponsorId=key(account["SponsorId"]),
                                         moneyMovementId=None, cashboxHandoverId=key(handover["Id"]), batchId=batch,
                                         revision=0, reversesPostingId=None, kind=2, amount=str(signed),
                                         occurredAtUtc=handover["ConfirmedAtUtc"], actorUserId=key(handover["AccountantUserId"]),
                                         actorNameAr=display_name(handover["AccountantUserId"]), reference=f"legacy:{handover['Id']}",
                                         reason="إعادة بناء تسليم مؤكد سابقاً؛ لا يمثل تحصيلاً جديداً"))
        for location, column in ((1, "CashboxAmount"), (3, "BankAmount"), (4, "PetroAppAmount")):
            assert fund_totals[location] == Decimal(handover[column]), "The frozen handover differs from its exact movements."

    balances = collections.defaultdict(Decimal)
    for post in postings:
        balances[post["subAccountId"]] += Decimal(post["amount"])
    balance_rows = []
    for account in routes:
        if account["IsPosting"] == "true":
            balance_rows.append(dict(accountName=account["NameAr"], sponsorName=sponsors[key(account["SponsorId"])]["RegistryNameAr"],
                                     location=int(account["Location"]), purpose=int(account["Purpose"]), balance=str(balances[key(account["Id"])])))
    plan = dict(version=1, rosterSha256=workbook_hash, rosterPlanSha256=hashlib.sha256(roster_path.read_bytes()).hexdigest(),
                rosterAuditId=imports[0]["Id"], rosterImportedAt=imports[0]["OccurredAtUtc"],
                sourceSnapshotSha256=hashlib.sha256((folder / "source-evidence.xml").read_bytes()).hexdigest(),
                ownership=list(owners.values()), postings=postings, handoverLines=lines, handoverPostings=memberships,
                balances=balance_rows, sourceCounts={"fees":len(source["Table2"]),"executedOperations":sum(o["Status"]=="3" for o in source["Table3"]),
                                                   "debts":len(source["Table5"]),"movements":len(source["Table4"]),"confirmedHandovers":len(source["Table8"])})
    (folder / "carryforward-plan.json").write_text(json.dumps(plan, ensure_ascii=False, indent=2), encoding="utf-8")
    summary = ["# مراجعة نقل عهدة جاهز V2 إلى V3", "", "الملكية من كشف الاستيراد الأصلي المعتمد وسلسلة تغييرات الكفيل، وليست من كفيل الموظف أو الكفيل الحالي وحده.",
               "", f"حركات المال: {plan['sourceCounts']['movements']}، المحاضر المؤكدة: {len(source['Table8'])}، قيود الدفتر بعد إعادة البناء: {len(postings)}.",
               "", "| الشركة | الحساب | الرصيد المعاد بناؤه (ر.س) |", "|---|---|---|"]
    summary += [f"| {b['sponsorName']} | {b['accountName']} | {Decimal(b['balance']):,.2f} |" for b in balance_rows]
    summary += ["", "الأرصدة البنكية السالبة تعكس الصرف التاريخي بلا تمويل مثبت في البيانات. لا تمثل رصيد بنك تم التحقق منه، ولا تُغطى بافتتاح تخميني. تظل الخزينة غير مفعلة حتى مطابقة المالية."]
    (folder / "review-ar.md").write_text("\n".join(summary)+"\n", encoding="utf-8")
    print(json.dumps({"sources":plan["sourceCounts"],"ownership":len(owners),"postings":len(postings),"handoverLines":len(lines),
                      "confirmedMemberships":len(memberships),"negativeAccounts":[b for b in balance_rows if Decimal(b["balance"])<0]},ensure_ascii=False))


if __name__ == "__main__":
    main(Path(sys.argv[1]), Path(sys.argv[2]))
