# Housing frontend handoff — Riyadh workbook and full room management

This is the frontend contract for the housing work completed on 2026-09-29. It covers the existing housing and room flows plus floors, equipment, name-only external occupants, iqama assignment, and pending iqama reconciliation. The database migrations and Riyadh import have been applied. The frontend was intentionally left unchanged; deploy the updated API before enabling these controls in the admin page.

## 1. Data already in the database

Riyadh city ID: `019c18d5-62e1-7000-8000-000000000004`. The linked Al Naseem 1 housing was reused; its previous room was archived and its previous current residence was closed, preserving history.

| Housing | Housing ID | Code | Floor ID | Rooms | Capacity | Occupants |
| --- | --- | --- | --- | ---: | ---: | ---: |
| الأمير بندر 1 | `7e76e86b-bbc3-471b-b55d-d5ec3a377528` | `RYD-PRINCE-BANDAR-1` | `c23f3e72-1f44-4ea0-a40c-c349d3d8bb22` | 4 | 24 | 16 |
| سكن الرياض النسيم 1 | `01a0d372-4ac7-7e6e-bd47-cbfbe074dafd` | `الرياض النسيم` | `0902950b-7f4b-4686-ab2d-d5163f7f202a` | 2 | 13 | 8 |
| النسيم 2 | `a84e09e1-d658-48f9-af9c-b443817d9e94` | `RYD-NASEEM-2` | `aa40be0d-cc3a-4537-8755-0609ff414940` | 4 | 29 | 22 |
| النسيم 3 | `8123a16b-3db8-4119-8971-a43aed976bef` | `RYD-NASEEM-3` | `d9fbacbb-1885-45c4-b8f5-da8c1f75e123` | 4 | 28 | 15 |

Each location currently has one floor named `1`. The 61 named occupants consist of 55 linked employee records, 3 explicit name-only external people, and 3 pending iqama matches. Pending people count toward occupancy and capacity but have no employee link yet.

| Pending source row | Iqama | Name | Room |
| ---: | --- | --- | --- |
| 41 | `2643584044` | عريف مريضة | النسيم 1, room 2 |
| 70 | `2588987777` | علاء بدوى مصطفى | النسيم 2, room 4 |
| 82 | `2571787197` | ابراهيم جمعه يوسف جمعه | النسيم 3, room 1 |

The workbook claims four occupants in النسيم 3, room 2, but names only three. The system uses three; do not create a fourth placeholder. Room equipment and floor equipment together match every floor equipment total in the workbook.

## 2. API conventions and permissions

- Prefix paths below with the deployed API base URL. Send the normal bearer token and `Content-Type: application/json` for request bodies. JSON fields use camel case; dates use `YYYY-MM-DD`; IDs are GUID strings.
- Reading housing data requires `operations.housing.read`. All create, update, archive, assignment, move, resolve, and delete actions require `operations.housing.manage`.
- `rowVersion` is an opaque Base64 value. Use the latest value from a response for housing, room, floor, equipment, or external-person updates that request it. Do not calculate it in the client.
- `204 No Content` means an archive, remove, or delete succeeded. Refresh the containing housing detail after a mutation to get new counts, totals, lists, and row versions.
- Errors use Problem Details with `status`, `title`, `detail`, `errorCode`, `correlationId`, and sometimes `field`. Typical statuses are `400` validation, `401` unauthenticated, `403` missing permission, `404` missing record, and `409` conflict, capacity, duplicate, or stale version.
- Treat `currentOccupancy`, `availableCapacity`, housing and floor capacity totals, and `totalEquipment` as server-calculated read-only fields.

## 3. Read models

### Housing list and detail

`GET /api/housing` returns an array of housing summaries. Filter to Riyadh in the frontend with `cityId`. On list items, `rooms` and `floors` are `null`. `GET /api/housing/{housingId}` returns one housing with both arrays populated. `GET /api/housing/{housingId}/rooms` and `GET /api/housing/{housingId}/floors` return each collection independently.

The housing fields are `id`, `code`, `nameAr`, `nameEn`, `cityId`, `cityAr`, `address`, `latitude`, `longitude`, `totalCapacity`, `currentResidents`, `availableCapacity`, `contactPhone`, `openedDate`, `closedDate`, `status`, `statusReason`, `notes`, `rowVersion`, `rooms`, and `floors`. `status` is a string such as `Active`.

`rooms` at housing level and `rooms` nested in each floor refer to the same rooms. Use `floors[].rooms` for a floor-grouped display; do not render both as separate room sets.

### Floor

```json
{
  "id": "c23f3e72-1f44-4ea0-a40c-c349d3d8bb22",
  "housingId": "7e76e86b-bbc3-471b-b55d-d5ec3a377528",
  "name": "1",
  "rowVersion": "<opaque Base64>",
  "equipment": [{ "id": "<guid>", "name": "غسالة", "quantity": 2, "rowVersion": "<opaque Base64>" }],
  "totalEquipment": [{ "id": "00000000-0000-0000-0000-000000000000", "name": "مرتبة", "quantity": 25, "rowVersion": "" }],
  "rooms": [],
  "totalCapacity": 24,
  "currentOccupancy": 16,
  "availableCapacity": 8
}
```

`equipment` is stored directly on the floor. `totalEquipment` groups and sums floor equipment plus all active room equipment by item name. Its rows are read-only aggregates, with an empty GUID and empty row version; never send those IDs to update or delete endpoints. These room and floor quantities are separate from the housing warehouse stock API.

### Room and three occupant kinds

```json
{
  "id": "5fdfcbbf-27fe-4960-93b3-9ced4b836ebc",
  "housingId": "7e76e86b-bbc3-471b-b55d-d5ec3a377528",
  "floorId": "c23f3e72-1f44-4ea0-a40c-c349d3d8bb22",
  "name": "1",
  "capacity": 8,
  "currentOccupancy": 5,
  "availableCapacity": 3,
  "notes": "موجود بها 4 سراير ب دورين",
  "rowVersion": "<opaque Base64>",
  "equipment": [{ "id": "<guid>", "name": "مرتبة", "quantity": 8, "rowVersion": "<opaque Base64>" }],
  "occupants": [{
    "occupancyPeriodId": "<guid>", "roomId": "<guid>", "housingId": "<guid>",
    "employeeId": "<guid>", "riderProfileId": "<guid or null>",
    "personType": "Rider", "iqamaNo": "2541115800",
    "employeeNameAr": "حمزه اسعد ابو بكر حامد", "employeeNameEn": null,
    "effectiveFrom": "2026-09-29", "moveInReason": null,
    "sourceReference": "Riyadh housing workbook"
  }],
  "externalOccupants": [{ "id": "<guid>", "roomId": "<guid>", "name": "محمود خطيب", "rowVersion": "<opaque Base64>" }],
  "pendingOccupants": []
}
```

`occupants` are linked employees or riders. A rider still has an `employeeId`; `riderProfileId` and `personType: "Rider"` identify a rider profile. `externalOccupants` contain only a name and room link, with no employee or iqama; they are not external rider profiles. `pendingOccupants` contain `id`, `roomId`, `iqamaNo`, `name`, `sourceRow`, and `rowVersion`; they are unmatched workbook people, not external people. `currentOccupancy` includes all three arrays. Use that server value for vacancy displays.

`GET /api/housing/{housingId}/residents?currentOnly=true|false` returns linked employee/rider residence periods and their history, with `roomId`, `roomName`, `effectiveFrom`, `effectiveTo`, and reasons. It does **not** include external or pending people. Room detail is the source for the complete current occupant list.

## 4. Endpoint and request matrix

All paths use the HTTP method shown. A create body may omit `rowVersion` or send it as `null`. Update requests must send the latest row version where noted.

| Operation | Method and path | JSON body | Success |
| --- | --- | --- | --- |
| List housing | `GET /api/housing` | none | `200` housing summaries |
| Get housing | `GET /api/housing/{housingId}` | none | `200` housing detail |
| Create housing | `POST /api/housing` | housing metadata, below | `200` housing detail |
| Update housing | `PUT /api/housing/{housingId}` | housing metadata with `rowVersion` | `200` housing detail |
| Archive housing | `PATCH /api/housing/{housingId}/archive` | `{ "reason": "...", "rowVersion": "..." }` | `204` |
| List floors | `GET /api/housing/{housingId}/floors` | none | `200` floor array |
| Create floor | `POST /api/housing/{housingId}/floors` | `{ "name": "2", "rowVersion": null }` | `200` floor |
| Rename floor | `PUT /api/housing/{housingId}/floors/{floorId}` | `{ "name": "2", "rowVersion": "..." }` | `200` floor |
| Archive floor | `DELETE /api/housing/floors/{floorId}` | `{ "reason": "...", "rowVersion": "..." }` | `204` |
| List rooms | `GET /api/housing/{housingId}/rooms` | none | `200` room array |
| Get room | `GET /api/rooms/{roomId}` | none | `200` room |
| Create room | `POST /api/housing/{housingId}/rooms` | room body, below | `201` room, `Location: /api/rooms/{roomId}` |
| Update room | `PUT /api/rooms/{roomId}` | room body with `rowVersion` | `200` room |
| Archive room | `DELETE /api/rooms/{roomId}` | `{ "reason": "...", "rowVersion": "..." }` | `204` |
| Add floor equipment | `POST /api/housing/floors/{floorId}/equipment` | equipment body, below | `200` equipment |
| Update floor equipment | `PUT /api/housing/floors/{floorId}/equipment/{equipmentId}` | equipment body with `rowVersion` | `200` equipment |
| Add room equipment | `POST /api/rooms/{roomId}/equipment` | equipment body | `200` equipment |
| Update room equipment | `PUT /api/rooms/{roomId}/equipment/{equipmentId}` | equipment body with `rowVersion` | `200` equipment |
| Delete either equipment item | `DELETE /api/housing/equipment/{equipmentId}` | none | `204` |
| Add name-only external | `POST /api/rooms/{roomId}/occupants/external` | external body, below | `200` external |
| Rename or move external | `PUT /api/rooms/occupants/external/{occupantId}` | external body with destination `roomId` and `rowVersion` | `200` external |
| Remove external | `DELETE /api/rooms/occupants/external/{occupantId}` | none | `204` |
| Assign by iqama | `POST /api/rooms/{roomId}/occupants/iqama` | iqama body, below | `200` linked occupant |
| Resolve pending | `POST /api/rooms/occupants/pending/{pendingId}/resolve` | `{ "effectiveFrom": "2026-09-29" }` | `200` linked occupant |
| Remove pending | `DELETE /api/rooms/occupants/pending/{pendingId}` | none | `204` |
| Assign employee | `POST /api/rooms/{roomId}/occupants/employees` | employee body, below | `200` linked occupant |
| Assign rider | `POST /api/rooms/{roomId}/occupants/riders` | rider body, below | `200` linked occupant |
| Move linked occupant | `POST /api/rooms/occupants/{occupancyPeriodId}/move` | move body, below | `200` new linked occupant |
| Remove linked occupant | `POST /api/rooms/occupants/{occupancyPeriodId}/remove` | remove body, below | `204` |
| List linked residence history | `GET /api/housing/{housingId}/residents?currentOnly=false` | none | `200` period array |
| Assign through compatibility route | `POST /api/housing/{housingId}/residents` | `{ "roomId": "<guid>", "employeeId": "<guid>", "effectiveFrom": "2026-09-29", "moveInReason": null, "sourceReference": null }` | `200` period array |
| Close linked residence through compatibility route | `POST /api/housing/residence-periods/{periodId}/close` | `{ "effectiveTo": "2026-09-30", "reason": "..." }` | `204` |
| List supervisors | `GET /api/housing/{housingId}/supervisors?currentOnly=false` | none | `200` period array |
| Assign supervisor | `POST /api/housing/{housingId}/supervisors` | `{ "employeeId": "<guid>", "effectiveFrom": "2026-09-29", "assignmentReason": "..." }` | `200` period array |
| Close supervisor | `POST /api/housing/supervisor-periods/{periodId}/close` | `{ "effectiveTo": "2026-09-30", "reason": "..." }` | `204` |

### Request bodies

Housing create and update use the same shape. Creating a housing also creates a floor named `1` and a default housing warehouse. Capacity and occupancy are not writable housing fields.

```json
{
  "code": "RYD-EXAMPLE",
  "nameAr": "سكن الرياض",
  "nameEn": "Riyadh housing",
  "cityId": "019c18d5-62e1-7000-8000-000000000004",
  "address": null,
  "latitude": null,
  "longitude": null,
  "contactPhone": null,
  "openedDate": null,
  "closedDate": null,
  "status": "Active",
  "statusReason": null,
  "notes": null,
  "rowVersion": null
}
```

Floor names are required, trimmed, and at most 100 characters. Room body: `{ "name": "3", "capacity": 4, "floorId": "<guid>", "notes": "...", "rowVersion": null }`. `floorId` is optional on create; the server chooses the first floor when omitted. On update, omitting it keeps the current floor. Room numbers are unique **within a floor**, so different floors may each have room `1`. Room names are required, trimmed, and at most 100 characters. `capacity` must be greater than zero and cannot fall below `currentOccupancy`. `notes` is optional, up to 2,000 characters.

Equipment body: `{ "name": "مكيف", "quantity": 1, "rowVersion": null }`. Name is required, trimmed, at most 100 characters, and unique within its owning room or floor. Quantity is an integer at least zero. Floor equipment and room equipment are separate lists; choose the endpoint for the intended scope. Do not edit floor `totalEquipment` directly.

External body for create: `{ "name": "محمد سعيد", "roomId": null, "rowVersion": null }`. The route's room is used. The record stores a name only for the person; the name is required and at most 200 characters. For update or move: `{ "name": "محمد سعيد", "roomId": "<destination-room-guid>", "rowVersion": "<latest>" }`. `roomId` is required on PUT, even for a rename in the same room. The target housing must be active and a move requires a free place in the destination.

Assign by iqama: `{ "iqamaNo": "2541115800", "effectiveFrom": "2026-09-29", "moveInReason": null, "sourceReference": null }`. The iqama must match exactly one employee. If a pending row with the same iqama already occupies **that room**, this operation converts it to a linked occupant without increasing room occupancy; the pending source row is preserved as the new period's reference. Otherwise it creates a new residence period and consumes a place. The pending conversion uses the workbook reference; it does not copy `moveInReason` or `sourceReference` from this request.

Employee assignment: `{ "employeeId": "<guid>", "effectiveFrom": "2026-09-29", "moveInReason": null, "sourceReference": null }`. Rider assignment uses `riderProfileId` in place of `employeeId` and the same other fields. For a pending match, use iqama assignment or the resolve route to avoid consuming capacity twice.

Move linked occupant: `{ "destinationRoomId": "<guid>", "effectiveFrom": "2026-10-01", "reason": "Moved" }`. The source period closes the day before the destination period starts. The destination must be different and have capacity. Cross-housing moves are supported. Remove linked occupant: `{ "effectiveTo": "2026-10-31", "reason": "Left accommodation" }`. Use `occupancyPeriodId`, not employee ID, for these actions.

## 5. Suggested admin page behavior

1. Load `GET /api/housing/{housingId}` for the detail page. Show housing totals, then floor sections with each floor's room cards and its own equipment plus `totalEquipment` summary.
2. On each room card, show `capacity`, `currentOccupancy`, `availableCapacity`, notes, and three occupant groups. Display pending matches with an obvious “Needs employee match” state and their iqama. Do not label them external.
3. Let managers add and edit floors, rooms, and equipment from the relevant section. Use the returned `rowVersion` for edits and archive actions. Hide archive actions for occupied rooms and floors with active rooms or floor equipment, while still handling server conflicts.
4. Offer “Assign by iqama” for an existing person and “Add external person” for a name-only person. External form asks for name only; room comes from context. A room with `availableCapacity: 0` cannot receive a new person, though resolving a pending person already in that room does not consume an additional place.
5. Offer pending reconciliation after the matching employee record exists. Resolve changes the pending row into a linked occupant; `currentOccupancy` stays the same. Removing an erroneous pending or external occupant reduces occupancy by one.
6. After any mutation, reload housing detail. For a `409` stale row version or capacity conflict, show the API error and current data. Do not update counts optimistically.
7. Use the history endpoint for linked employee/rider periods. External and pending people have no residence-period history in this API.

## 6. Error handling and acceptance checks

| Error code | Typical meaning | Frontend action |
| --- | --- | --- |
| `housing.capacity_exceeded` | No remaining place, or reduced capacity is too low | Refresh room and show current occupancy |
| `housing.person_already_assigned` | Employee already has a current room | Open their current assignment or use move |
| `housing.employee_not_found` | Iqama or employee ID does not resolve | Keep pending row visible; correct/create employee record |
| `housing.room_name_duplicate` | Room name already exists on this floor | Ask for another number/name |
| `housing.room_occupied` | Room cannot be archived | Remove or move occupants first |
| `housing.not_active` | Target housing is not active | Choose an active housing |
| `hr.concurrency_conflict` | Submitted row version is stale | Reload and retry after user review |
| `hr.duplicate` | Duplicate floor or equipment name | Edit the existing item or choose another name |
| `hr.conflict` | Invalid move, pending resolve, or floor archive state | Reload the affected record and explain the rule |
| `hr.invalid_request` | Invalid name, quantity, scope, or pending ID | Show field validation or reload |

Verify the UI against these cases: Al Naseem 1 shows two active numbered rooms and no archived old room; the four housing totals match the table above; room occupancy counts all three occupant types; a room with three linked people and one external shows four occupied places; floor total equipment updates after editing either a room or floor item; a pending match resolves without changing occupancy; duplicate room numbers are allowed on different floors but blocked on the same floor; room and housing archive fail while occupied; the `residents` history includes the closed old Al Naseem residence.

The existing [housing rooms handoff](housing-rooms-frontend-handoff-en.md) describes the older employee, rider, supervisor, and period fields in more detail. This document reflects the current extended API and takes precedence where the older examples omit floors or new occupant kinds.
