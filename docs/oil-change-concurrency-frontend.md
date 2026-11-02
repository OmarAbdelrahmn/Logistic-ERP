# Completed oil-change corrections

The edit/cancellation workflow is implemented in the backend source. See [API contract](oil-change-edit-cancel-api.md), [frontend integration](oil-change-edit-cancel-frontend.md), and [verification and deployment status](oil-change-edit-cancel-acceptance.md).

Operation responses contain `rowVersion`, `isCancelled`, and `revisionNumber`. The existing vehicle-only correction still accepts its optional operation version; new edit/cancel commits require versions, a successful preview hash, and an idempotency key.

Database migrations were applied and verified on hosted db67927 on 31 October 2026; see oil-change-workflow-hosted-deployment-20261031.md. The matching API still needs deployment before enabling this UI.
