# ADR-0004: Service Bus over Storage Queues / Event Grid for async messaging

- **Status:** Accepted
- **Date:** 2026-05-13
- **Phase:** 0 (implemented in Phase 2)

## Context

Phase 2 introduces async document processing: the API hands work off to a background worker so the upload request returns immediately. Three Azure-native options:

**Azure Storage Queues**
- Cheapest, simplest
- 64 KB message size, no native dead-letter queue
- No sessions, no transactions, no duplicate detection
- Suitable for trivial fire-and-forget work

**Azure Event Grid**
- Push model, great for event fan-out
- At-least-once delivery
- Not designed for ordered workflow processing or operator-replayable failures
- Integration-pattern semantics, not workflow-pattern semantics

**Azure Service Bus**
- First-class dead-letter queue per logical queue
- Built-in poison-message handling, max-delivery counts, scheduled redelivery
- Sessions for ordered processing, duplicate detection, transactions
- Higher cost than Storage Queues; more concepts to learn

## Decision

Use **Azure Service Bus** for all internal asynchronous messaging.

## Why this matters for an enterprise project

The Phase 2 done-criterion is "failed documents do not disappear." That requires:

- A real DLQ where poison messages land with their original payload, attempt count, and error context.
- Operator-driven replay after the underlying issue is fixed.
- Predictable retry behavior with a max-delivery cap.

Storage Queues require building all of this by hand. Event Grid is the wrong shape — it pushes events to subscribers, which is not how a workflow with retries and DLQ replay should be modeled.

Service Bus gives all of this for free at the platform level, which is exactly what an "enterprise patterns" project should demonstrate.

## Consequences

**Positive**
- Phase 2 failure handling matches what real enterprise teams build.
- DLQ inspection and replay are first-class operations.
- Phase 9 Durable Functions can layer on top cleanly when long-running orchestration is added.

**Negative**
- Higher per-month cost than Storage Queues (negligible at this project's scale).
- More SDK concepts to learn (sessions, locks, peek-lock vs receive-and-delete).

**Mitigation**
- Use peek-lock with explicit `complete` / `abandon` to ensure no message is silently lost.
- Document the DLQ replay procedure as part of Phase 2 deliverables.

## Related

- [docs/data-flow.md](../data-flow.md) — message contract for `DocumentUploaded`
- [docs/failure-handling.md](../failure-handling.md) — DLQ strategy
