# Ambiguities and Decisions

## 1. Status Transitions
**Unclear:** The rules say the normal flow is `New` → `InProgress` → `Done`, that `Cancelled` can only go back to `New`, that `Done` is final, and that "any other change is forbidden". It never says from which statuses a request can be cancelled, whether `InProgress` → `New` is allowed, or what happens when the same status is sent again.

**Decision:** Cancellation is allowed from `New` and `InProgress`. `InProgress` → `New` is not allowed (it is not part of the normal flow). Same → same is treated as a forbidden transition.

| From         | Permitted targets         |
| ------------ | ------------------------- |
| `New`        | `InProgress`, `Cancelled` |
| `InProgress` | `Done`, `Cancelled`       |
| `Cancelled`  | `New`                     |
| `Done`       | —                         |

All other combinations return `409 Conflict`.

## 2. Re-opening a Cancelled Request (`Cancelled` → `New`)
**Unclear:** Whether this counts as a normal status change, and what happens to timestamps.

**Decision:** It is a normal status change. `CreatedAt` stays the same, `UpdatedAt` is refreshed, and a message is published like for any other change.

## 3. Logging Condition in the Consumer
**Unclear:** The consumer must save the event and log it if it has a high priority or has been completed.

**Decision:** An event is logged if `Priority == High` or `NewStatus == Done`, but every event is stored regardless of logging.

## 4. Pagination
**Unclear:** Defaults and the maximum page size are not specified.

**Decision:** `page` defaults to `1` and `pageSize` to `20`, with a maximum of `100`. Out-of-range values return `400`. Results are sorted by `CreatedAt` descending, with `Id` as a tiebreaker.

## 5. "Messages Must Not Be Lost"
**Unclear:** What exactly should ensure that the data is not lost?

**Decision:** A Transactional Outbox pattern writes the request and the message in one transaction, and a background publisher uses publisher confirms with retries. The consumer is idempotent by `messageId`.

## 6. Failures in the Consumer
**Unclear:** The retry policy and what happens to messages that keep failing.

**Decision:** Manual ack. Transient errors are retried 3 times with backoff, then the message goes to a dead-letter queue. Malformed messages go to the DLQ immediately.

## 7. Consumer Placement
**Unclear:** "May run in the same process" is optional.

**Decision:** The consumer is a `BackgroundService` in the same API process.