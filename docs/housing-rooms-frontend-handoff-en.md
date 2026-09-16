# Housing Rooms API — Frontend Handoff

## Overview

Room is now the assignment level: employee/rider → occupancy period → room → housing.

Housing capacity, current occupancy, and available capacity are derived from its non-archived rooms. The API never accepts a housing-level capacity or a capacity override for a new assignment.

All endpoints require an authenticated user:

- Read endpoints: operations.housing.read
- Mutating endpoints: operations.housing.manage

Dates use YYYY-MM-DD. IDs are GUID strings. rowVersion is a base64 concurrency token returned by the API and must be sent back unchanged on update/archive.

Expected problem responses use RFC 7807 fields plus errorCode, correlationId, and sometimes field. Common status codes are 400 validation, 404 missing resource, 409 conflict/capacity/concurrency, 401, and 403.

## Shared response shapes

### Housing summary/details

~~~json
{
  "id": "0199...",
  "code": "HOU-RUH-01",
  "nameAr": "سكن الرياض",
  "nameEn": "Riyadh Housing",
  "cityId": "0199...",
  "cityAr": "الرياض",
  "address": {
    "buildingNumber": "10",
    "street": "Example Street",
    "district": "Example District",
    "city": "Riyadh",
    "postalCode": "12345",
    "additionalNumber": null
  },
  "latitude": 24.7136,
  "longitude": 46.6753,
  "totalCapacity": 10,
  "currentResidents": 3,
  "availableCapacity": 7,
  "contactPhone": "0500000000",
  "openedDate": "2026-01-01",
  "closedDate": null,
  "status": "Active",
  "statusReason": null,
  "notes": null,
  "rowVersion": "AAAAAAAAB9E=",
  "rooms": []
}
~~~

rooms is null in the housing list response and is fully populated in housing details and create/update responses.

### Room

~~~json
{
  "id": "0199...",
  "housingId": "0199...",
  "name": "101",
  "capacity": 4,
  "currentOccupancy": 2,
  "availableCapacity": 2,
  "rowVersion": "AAAAAAAAB+E=",
  "occupants": []
}
~~~

### Current occupant

~~~json
{
  "occupancyPeriodId": "0199...",
  "roomId": "0199...",
  "housingId": "0199...",
  "employeeId": "0199...",
  "riderProfileId": null,
  "personType": "Employee",
  "iqamaNo": "1234567890",
  "employeeNameAr": "اسم الموظف",
  "employeeNameEn": "Employee Name",
  "effectiveFrom": "2026-09-15",
  "moveInReason": "New assignment",
  "sourceReference": null
}
~~~

For riders, personType is Rider, riderProfileId is populated, and employeeId is still returned because a rider profile belongs to an employee/person record.

## Changed existing housing endpoints

### GET /api/housing

Returns all non-archived housing summaries.

- Request: none.
- Response: 200 array of Housing objects; rooms is null.
- Changed: totalCapacity, currentResidents, and availableCapacity now come from room totals.

### GET /api/housing/{housingId}

Returns housing details.

- Request: path housingId.
- Response: 200 Housing object with all rooms and each room's current occupants.
- Changed: adds rooms; capacity and occupancy are room-derived.
- Errors: 404 housing.not_found.

### POST /api/housing

Creates housing metadata.

~~~json
{
  "code": "HOU-RUH-01",
  "nameAr": "سكن الرياض",
  "nameEn": "Riyadh Housing",
  "cityId": "0199...",
  "address": {
    "buildingNumber": null,
    "street": null,
    "district": null,
    "city": "Riyadh",
    "postalCode": null,
    "additionalNumber": null
  },
  "latitude": 24.7136,
  "longitude": 46.6753,
  "contactPhone": "0500000000",
  "openedDate": "2026-01-01",
  "closedDate": null,
  "status": "Active",
  "statusReason": null,
  "notes": null,
  "rowVersion": null
}
~~~

- Response: 200 Housing details. Initial capacity is 0 until rooms are created.
- Changed: remove the old totalCapacity request field. Capacity is managed only through rooms.
- Errors: required code/names, invalid coordinates/date/status, unknown cityId, or duplicate code.

### PUT /api/housing/{housingId}

Updates housing metadata using the same body as create, with the current rowVersion.

- Response: 200 updated Housing details.
- Changed: remove totalCapacity; room capacity cannot be edited here.
- Errors: 404 housing.not_found, 409 hr.concurrency_conflict, duplicate code, or create validations.

### PATCH /api/housing/{housingId}/archive

~~~json
{
  "reason": "Location closed",
  "rowVersion": "AAAAAAAAB9E="
}
~~~

- Response: 204.
- Changed: archive is blocked when any room has current occupants.
- Errors: 404 housing.not_found, 409 hr.conflict, or stale/missing concurrency data.

### GET /api/housing/{housingId}/residents?currentOnly={bool}

Returns room-based residence history for the housing.

~~~json
[
  {
    "id": "0199...",
    "housingId": "0199...",
    "roomId": "0199...",
    "roomName": "101",
    "employeeId": "0199...",
    "riderProfileId": null,
    "personType": "Employee",
    "iqamaNo": "1234567890",
    "employeeNameAr": "اسم الموظف",
    "effectiveFrom": "2026-09-15",
    "effectiveTo": null,
    "startReason": "New assignment",
    "endReason": null,
    "capacityOverrideUsed": false,
    "capacityOverrideReason": null
  }
]
~~~

- currentOnly=false is the default and includes history.
- Changed: adds room and person-type fields. Old capacity-override fields remain read-only so migrated history is not lost.
- Errors: 404 housing.not_found.

### POST /api/housing/{housingId}/residents

Compatibility route for older clients. New clients should use the dedicated room employee/rider routes below.

~~~json
{
  "roomId": "0199...",
  "employeeId": "0199...",
  "effectiveFrom": "2026-09-15",
  "moveInReason": "New assignment",
  "sourceReference": null
}
~~~

- Response: 200 full residence history for the housing.
- Changed: roomId is mandatory; capacityOverrideUsed and capacityOverrideReason were removed; this route no longer auto-moves an already-assigned person.
- Errors: room missing, room belongs to another housing, housing inactive, employee missing, person already assigned, or room full.

### POST /api/housing/residence-periods/{occupancyPeriodId}/close

~~~json
{
  "effectiveTo": "2026-09-30",
  "reason": "Left housing"
}
~~~

- Response: 204.
- Changed: closing the current period also atomically releases one place in its room.
- Errors: period missing, already closed, invalid date, or inconsistent persisted occupancy.

### Housing supervisor endpoints

- GET /api/housing/{housingId}/supervisors?currentOnly={bool}
- POST /api/housing/{housingId}/supervisors
- POST /api/housing/supervisor-periods/{periodId}/close

Assign body:

~~~json
{
  "employeeId": "0199...",
  "effectiveFrom": "2026-09-15",
  "assignmentReason": "Regional supervision"
}
~~~

Close body:

~~~json
{
  "effectiveTo": "2026-12-31",
  "reason": "Assignment ended"
}
~~~

- One housing may have one current supervisor.
- One employee may supervise multiple housing locations concurrently.
- Supervisor periods are not occupants and do not consume room capacity.
- GET returns the shared period response, with roomId, roomName, and riderProfileId null and personType Employee.
- Errors: housing/employee/period missing, replacement date conflict, or invalid close.

## Completely new room endpoints

### GET /api/housing/{housingId}/rooms

- Request: path housingId.
- Response: 200 array of Room objects, including current occupants and available capacity.
- Errors: 404 housing.not_found.

### POST /api/housing/{housingId}/rooms

~~~json
{
  "name": "101",
  "capacity": 4,
  "rowVersion": null
}
~~~

- Response: 201, Location: /api/rooms/{roomId}, body is the new Room.
- Validation: trimmed name is required and at most 100 characters; capacity must be greater than zero; active room names must be unique within the housing.
- Errors: unknown housing, duplicate room name, or invalid name/capacity.

### GET /api/rooms/{roomId}

- Response: 200 Room with current occupants.
- Errors: 404 housing.room_not_found.

### PUT /api/rooms/{roomId}

~~~json
{
  "name": "101-A",
  "capacity": 6,
  "rowVersion": "AAAAAAAAB+E="
}
~~~

- Response: 200 updated Room.
- Validation: same as create. Capacity cannot be reduced below currentOccupancy.
- Errors: missing room, duplicate name, 409 housing.capacity_exceeded, or 409 hr.concurrency_conflict.

### DELETE /api/rooms/{roomId}

This is a soft delete and requires a JSON body:

~~~json
{
  "reason": "Room permanently closed",
  "rowVersion": "AAAAAAAAB+E="
}
~~~

- Response: 204.
- Validation: occupied rooms cannot be archived.
- Errors: missing room, 409 housing.room_occupied, invalid reason, or stale row version.

### POST /api/rooms/{roomId}/occupants/employees

~~~json
{
  "employeeId": "0199...",
  "effectiveFrom": "2026-09-15",
  "moveInReason": "New assignment",
  "sourceReference": null
}
~~~

- Response: 200 Current occupant.
- Validation: the target person must be an employee (isEmployee=true), have no current room, housing must be active, and the room must have available capacity.
- Errors: 404 housing.employee_not_found, 404 housing.room_not_found, 409 housing.person_already_assigned, 409 housing.not_active, or 409 housing.capacity_exceeded.

### POST /api/rooms/{roomId}/occupants/riders

~~~json
{
  "riderProfileId": "0199...",
  "effectiveFrom": "2026-09-15",
  "moveInReason": "New rider assignment",
  "sourceReference": null
}
~~~

- Response: 200 Current occupant with both riderProfileId and its underlying employeeId.
- Validation/error behavior is the same as employee assignment, using housing.rider_not_found for an unknown rider profile.

### POST /api/rooms/occupants/{occupancyPeriodId}/move

~~~json
{
  "destinationRoomId": "0199...",
  "effectiveFrom": "2026-10-01",
  "reason": "Moved to another room"
}
~~~

- Response: 200 the new Current occupant/period in the destination room.
- Behavior: atomically reserves destination capacity, closes the source period on the previous day, releases source capacity, and creates the destination period. Cross-housing moves are supported.
- Validation: source period must be current; destination must differ; effectiveFrom must be later than the source period start; destination housing must be active and destination room must have capacity.
- Errors: missing period/room, date/same-room conflict, inactive housing, or room full.

### POST /api/rooms/occupants/{occupancyPeriodId}/remove

~~~json
{
  "effectiveTo": "2026-10-31",
  "reason": "Left accommodation"
}
~~~

- Response: 204.
- Behavior: atomically closes the current period and releases one place.
- Errors: missing/already-closed period, end date before start, or missing reason.

## Frontend implementation notes

- Do not send totalCapacity when creating/updating housing.
- Create at least one room after creating housing.
- Use availableCapacity > 0 to enable assignment controls, but always handle 409 housing.capacity_exceeded because another user may take the last place concurrently.
- Use the returned occupancyPeriodId for move/remove actions.
- Use the rider endpoint with riderProfileId, not employeeId; use the employee endpoint only for administrative employees.
- Refresh housing/room details after every assignment, move, removal, room update, or archive to receive new occupancy totals and row versions.
- The two production records that existed before this upgrade each have a room named Migrated room; users can rename those rooms with PUT /api/rooms/{roomId}.
