# ADR-0003: Metadata store — Azure SQL vs Cosmos DB

- **Status:** Open (decision deferred to Phase 1)
- **Date:** 2026-05-13
- **Phase:** 0

## Context

The platform needs a metadata store for `Document`, `Chunk`, `Extraction`, and `Case` entities. Two reasonable Azure-native options:

**Azure SQL**
- Strong schemas, foreign keys, transactional joins
- Familiar tooling (EF Core, SSMS)
- Easier reporting / ad-hoc queries
- Vertical scaling has a ceiling; horizontal sharding is non-trivial
- Lower cost at small scale

**Cosmos DB**
- Horizontal scale by partition key from day one
- Schema-flexible (good for documents with variable extracted fields, Phase 6)
- Globally distributed if needed
- More expensive per RU; requires partition strategy up front
- Joins are limited; reporting requires a secondary store or Synapse Link

## Decision

**Deferred until Phase 1**, when the actual access patterns become concrete:

- Are reads dominated by `getById` (Cosmos-friendly) or by joined queries across documents and chunks (SQL-friendly)?
- Is the per-document extraction shape (Phase 6) uniform enough for a relational schema, or does it vary wildly across document types?
- Will a single tenant ever exceed ~1M documents? (Triggers a real scaling conversation.)

## Decision criteria

When this ADR is closed in Phase 1, the chosen option must be justified against:

1. Concrete access patterns from the Phase 1 endpoints.
2. Estimated cost at 100K, 1M, and 10M documents.
3. Schema flexibility needed for Phase 6 extraction outputs.
4. Operator familiarity (this is a learning project — SQL is the safer default unless there is a real reason).

## Consequences (provisional)

Either option is reversible at the cost of a one-time migration. To minimize that cost:

- Phase 1 entity contracts live in `KnowledgeOps.Shared` and are persistence-agnostic.
- The repository interface in `KnowledgeOps.Api` does not leak EF Core or Cosmos SDK types.
- Connection details live in configuration only.

## Status updates

_To be appended when this ADR is closed._
