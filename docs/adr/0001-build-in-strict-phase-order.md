# ADR-0001: Build in strict phase order; defer Foundry Agent Service

- **Status:** Accepted
- **Date:** 2026-05-13
- **Phase:** 0

## Context

Microsoft Foundry Agent Service supports managed agents and code-based hosted agents (Agent Framework, LangGraph-style patterns). It is tempting to start with Foundry and skip directly to multi-agent workflows. Multiple production-shaped tutorials begin there.

The problem with starting at Foundry is that the underlying mechanics — chunking tradeoffs, retrieval ranking failures, citation contracts, idempotency in async pipelines, tool-call error handling — get hidden behind a managed surface. When something goes wrong (and it will: bad chunks, hallucinated citations, runaway agent loops), there is no mental model for *why* it went wrong, only the abstraction.

## Decision

Build phases 0 → 12 in strict order. Do not skip ahead. Specifically:

1. Implement document ingestion, chunking, embeddings, hybrid search, and RAG by hand before any agent code.
2. Implement a single tool-calling loop (Phase 7) by hand before introducing any multi-agent runtime (Phase 8).
3. Defer Microsoft Foundry Agent Service evaluation to Phase 7 at the earliest, after the in-process tool loop is working and its limits are understood.
4. Each phase ships only when its written "done when" criteria are met.

## Consequences

**Positive**
- Each capability is understood from the inside, not as a black box.
- Failure modes (poison messages, bad chunks, ungrounded answers, tool failures) are encountered in isolation, where they can be reasoned about and documented in [docs/failure-handling.md](../failure-handling.md).
- The decision of *whether* to adopt Foundry later is made with a working baseline to compare against.

**Negative**
- Slower to a "wow demo." The first user-visible RAG endpoint does not appear until Phase 5.
- More custom code that may eventually be replaced by the managed service.

**Mitigation**
- Phase 5 (working RAG) is a clear midpoint deliverable that already justifies the project on its own.
- All custom code is bounded by the phase that introduces it — no cross-phase coupling that would block a future swap to Foundry.

## Related

- [README roadmap](../../README.md#roadmap)
- [ADR-0002](0002-hybrid-search-over-vector-only.md) — also a "build the right primitive first" decision
