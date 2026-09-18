# Maritime booking reliability — design only

No logistics service, database, broker, simulator or mock server is implemented. This document describes coverage for a controlled environment with approved failure-injection hooks and read-only diagnostic access.

## Proposed contract and state policy

Confirm these assumptions with the product owner before turning this design into tests:

- `POST /bookings` returns a booking ID, request correlation ID and `Pending`; an idempotency key is scoped to tenant and operation.
- Capacity reservation leads to `Confirmed`; rejection leads to `Rejected`. Cancellation of a pending booking leads to `Cancelled`.
- Each state change has a monotonically increasing aggregate version. Confirm-versus-cancel uses optimistic concurrency. If cancellation wins, later capacity success cannot resurrect the booking and any late reservation must be released exactly once. If confirmation wins, cancellation is either rejected as a version conflict or enters a separate compensating cancellation workflow; the API must document which. For this suite assume a conflict response, then an explicit user-requested cancellation workflow.
- An event carries unique event ID, booking ID, correlation ID and causation ID. Projection exposes an applied version. Absence of a stable terminal-state contract is a testability issue, not permission to poll until any success appears.

## Automated cases and layer choices

| Scenario | Setup/action | Required assertions | Cadence |
|---|---|---|---|
| Pending → Confirmed | Create unique booking with available capacity | Initial Pending; command state eventually Confirmed; one effective reservation; query projection catches up to the command version; UI confirms the same booking | PR |
| Pending → Rejected | Known unavailable capacity | Terminal Rejected with reason; no active reservation/payment side effect; query/UI agree | PR |
| Repeated same key / same payload | Send request twice, including concurrent submissions | Same booking identity and equivalent result; one logical BookingRequested and one effective reservation; no extra charges | PR |
| Same key / different payload | Reuse tenant-scoped key with a materially different itinerary | Explicit conflict (proposed 409); original booking and payload unchanged; no second booking/reservation | PR |
| Duplicate CapacityReserved | Re-deliver identical event ID and duplicate business delivery with distinct transport ID | One transition and effective reservation; consumer ledger/dedup evidence; no second notification or charge | PR |
| Out-of-order success after cancellation | Cancel Pending, then deliver delayed success | Remains Cancelled; higher-version cancellation not overwritten; late capacity released once; projection never regresses version | PR |
| Confirm/cancel race | Use barriers to release both at the same aggregate version | One winning versioned transition; loser gets documented conflict/compensation; no Confirmed booking without capacity, no Cancelled booking with unreleased capacity beyond release SLA | Scheduled, deterministic permutations in PR |
| Projection delay | Pause projection while command completes | Command state is authoritative; query/UI may lag only within a budget and show honest Pending/stale indication; eventually version >= target | PR with bounded delay |
| Temporary dependency timeout | Delay capacity reply beyond one attempt budget | Observable retry policy with bounded attempts/backoff; unchanged logical booking; no duplicate effective reservation | Scheduled |
| Consumer restart before acknowledgement | Crash after state commit but before broker ack | Redelivery occurs; durable inbox/dedup prevents repeated business effect; consumer recovers within SLA | Scheduled |
| Poison message / DLQ | Deliver schema-invalid event through test hook | Bounded retries, DLQ reason/correlation, alert, no false terminal success; controlled redrive after correction applies one effect | Scheduled |

The command API is authoritative for transactional state; the query API is a projection; UI displays the query contract. A database may be used read-only for accounting/inbox assertions when the test environment exposes it, but UI-only evidence cannot prove exactly one reservation. Do not substitute test mocks for real delivery in the reliability suite.

## Bounded eventual assertion

Language-neutral pseudocode; `timer.wait` is an asynchronous scheduler await bounded by the remaining deadline, not a thread-blocking sleep or an unconditional delay before an assertion. Observation can also be awakened by a correlated projection notification.

```text
function awaitProjection(bookingId, expectedState, targetVersion, correlation, budget):
    deadline = monotonicNow() + budget
    history = []
    previousVersion = -1
    while monotonicNow() < deadline:
        remaining = deadline - monotonicNow()
        observed = query(bookingId, timeout=min(requestBudget, remaining))
        history.append(time, observed.state, observed.version, correlation)
        assert observed.bookingId == bookingId
        assert observed.version >= previousVersion       # never tolerate regression
        previousVersion = observed.version
        assert observed.state is not a forbiddenTerminalState
        if observed.version >= targetVersion and observed.state == expectedState:
            return observed
        await firstOf(correlatedProjectionSignal,
                      timer.wait(min(adaptiveIntervalWithBoundedJitter, deadline - monotonicNow())))
    fail("Projection deadline exceeded", bookingId, correlation, history)
```

If a query fails, record the response/exception. Retry only specifically allowed transient read failures within the same deadline; never swallow authentication, contract or forbidden-state errors. A single request cannot consume an unbounded amount of the polling budget. Clock measurements are monotonic; evidence includes UTC for cross-service correlation.

Initial budgets to negotiate: command acceptance 2 seconds, capacity decision 30 seconds, read projection 10 seconds after command version, UI 5 seconds after projection. Measure distributions in the environment before treating them as contractual SLAs. Failure-injection runs can have distinct declared budgets, never silently change PR budgets.

Polling is safe for a versioned projection catching up to an established terminal outcome. It is unsafe for authorisation, uniqueness, billing duplicates, forbidden intermediate states, state regression and idempotency conflicts: those require immediate assertions or observation over a defined window. A single successful snapshot cannot prove an invariant was never violated. Correlated event/audit history and counter differences must cover the whole test window.

## Idempotency boundaries

Request idempotency deduplicates client retries at the command boundary. Consumer idempotency deduplicates event processing after at-least-once delivery. Both need durable state: in-memory dedup cannot survive restart. Test the inbox/state commit transaction and, when appropriate, an outbox/event publication transaction. Use unique test tenant/key/event IDs and compare before/after records for the owned booking only.

Prove one booking, one effective capacity reservation and one charge, even when multiple requests/messages exist. It is legitimate to have two transport deliveries and one applied business effect. Do not claim end-to-end exactly-once processing: the assertions cover named persistence boundaries and identified side effects. Test key expiry and tenant isolation separately after the TTL/scope contract is agreed; do not invent it.

## Controlled injection and diagnosability

The environment owner supplies booking-scoped message interception/delay/reorder hooks, a bounded duplicate-delivery command, dependency latency/failure controls and a consumer restart mechanism. No global network degradation in shared PR infrastructure. A lease/TTL restores injected state even if a test process dies. Gate hooks by test identity and disable them in production.

Capture original request key/payload hash, aggregate versions, event IDs, causation, delivery counts, consumer attempts, inbox/outbox records, DLQ reason, projection versions and UI trace. Mask passenger/customer details. Timeline should distinguish accepted request, committed transition, published event, delivered event, applied projection and rendered UI. Attach the same correlation to diagnostics across services.

PR runs deterministic success/rejection/idempotency/duplicate/cancellation-order cases with modest data. Scheduled runs cover restart windows, dependency timeout and race permutations. Use barriers/hooks to target race order, not random sleeps or a thousand uncontrolled requests. Delete or expire only test-owned bookings/messages/reservations after capturing final state, and verify inventory is restored. Any retained data needs an owner, expiry and explicit cleanup task.
