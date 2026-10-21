# Settlement payment reference validation

`POST /api/jahez/v2/operations` and `PUT /api/jahez/v2/operations/{id}` require a nonblank `paymentReference` of at most 500 characters when the calculated `remaining` is nonzero and `transferToRider` is false. For updates, send the field under `values.paymentReference`, together with the current `rowVersion`.

`POST /api/jahez/v2/operations/preview` permits an omitted reference so the UI can calculate the amount and money direction before collecting payment evidence. Create/update return HTTP 400 with `errorCode: "jahez.invalid_request"` if a required reference is missing, empty, or whitespace. References exceeding 500 characters are also rejected.

Withdrawals without settlement, zero-balance settlements, and transfers without an immediate money movement do not require a reference.

`POST /api/jahez/v2/operations/{id}/execute` continues to use the saved reference and has no request body. To repair an existing ready operation with a missing reference, update it with the real receipt/payment reference and its current row version, then execute it. Do not generate a placeholder reference to bypass the validation.
