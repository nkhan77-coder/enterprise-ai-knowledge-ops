# ADR-0003: Metadata store — Azure SQL vs Cosmos DB

- **Status:** Accepted — Azure SQL with EF Core
- **Date:** 2026-05-13 (opened), 2026-05-13 (closed in Phase 1)
- **Phase:** 0 (opened) / 1 (closed)

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

**Azure SQL with Entity Framework Core**, with **SQLite for local development** behind the same EF Core surface.

- Production: Azure SQL connection string, `Microsoft.EntityFrameworkCore.SqlServer` provider.
- Development: file-based SQLite, `Microsoft.EntityFrameworkCore.Sqlite` provider. Selected by environment variable / configuration so no code change is needed to switch.
- Tests: EF Core InMemory provider for unit tests; SQLite in-memory for integration tests that exercise the real query pipeline.

## Why SQL over Cosmos

Decided against Cosmos for Phase 1 based on the actual access patterns now visible:

1. **Access shape is relational.** `Document → Chunks → Extractions → Cases` form a natural one-to-many tree with frequent joined reads ("get document with all its chunks and the latest extraction"). SQL handles this with a single query; Cosmos requires either multiple round trips or denormalized writes.
2. **Reporting / ad-hoc queries.** The Phase 10 evaluation pipeline and the Phase 11 observability views need joined slices ("which chunks served which answers, scored by groundedness"). SQL is dramatically friendlier for these.
3. **Schema variability is bounded.** The Phase 6 concern about variable extraction shapes is real but resolvable in SQL by storing the variable portion as `NVARCHAR(MAX)` JSON with a `JSON_VALUE` index — a common Azure SQL pattern. We do not need full document-database flexibility.
4. **Operator familiarity.** EF Core + SQL is the most-used .NET data stack. For a learning + portfolio project, this is the right default unless there is a concrete reason to deviate. There is not.
5. **Cost at the scale we will actually hit.** Below 1M documents per tenant, Azure SQL is cheaper and simpler than Cosmos. Above 1M, the question gets re-opened with real load data.

## Why SQLite for local dev (and not LocalDB)

- **Zero install.** Just a NuGet package and a file path. LocalDB requires SQL Server tooling and a running service — friction that adds nothing.
- **Same EF Core surface.** Migrations, queries, and repository code are identical. Only the provider registration and connection string differ.
- **Known minor differences.** SQLite lacks SQL Server-specific features (computed columns with `PERSISTED`, sequences, geography types, JSON functions). Phase 1 does not use any of these. If a future phase introduces one, the dev story upgrades to LocalDB or a containerized SQL Server — not a code rewrite.

## Consequences

**Positive**
- Phase 1 ships with no DB install required for contributors.
- Production migration story is one connection string change away.
- Joined queries needed by Phase 10 and Phase 11 are natural, not contortions.

**Negative**
- Two providers to keep in sync. Mitigated by running migrations against both in CI when CI exists (Phase 11+).
- SQLite-specific quirks (case-insensitive `LIKE`, weak typing) can mask issues that only show up on SQL Server.

**Mitigation**
- Integration tests run against SQLite in-memory for speed, with a tagged "smoke" subset that runs against a containerized SQL Server (added when this matters — likely Phase 4 or 5).
- Avoid SQL Server-specific T-SQL in EF queries; prefer LINQ that translates cleanly on both providers.

## Status updates

- **2026-05-13** — Closed during Phase 1 implementation. Decision recorded above.
