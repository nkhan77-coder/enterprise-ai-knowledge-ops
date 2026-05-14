# Current Limitations & Known Gaps

> Status as of: **2026-05-13, end of Phase 1.**
> This file is updated at the close of every phase. It exists so contributors and reviewers can see what is *not* done without having to reverse-engineer it from the code.

The point of this document: an honest "serious work in progress" view, not a polished marketing surface. If a section here is gone, the work is genuinely complete — not hidden.

## What does not exist yet

### Code
- ~~No compiled .NET projects yet.~~ ✅ Phase 1: `KnowledgeOps.Api`, `KnowledgeOps.Shared`, and `KnowledgeOps.Api.Tests` exist with 5 passing tests.
- No `KnowledgeOps.Worker` yet. Phase 2.
- No `KnowledgeOps.Orchestrator` yet. Phase 9.
- No `KnowledgeOps.AgentService` yet. Phase 7+.
- No `KnowledgeOps.Web` frontend yet. Phase 5+.
- No infrastructure-as-code. `/infra` is a placeholder until Phase 12 (or earlier if a deploy is needed sooner).

### Running services
- No deployed environment. There is no live URL, no demo, no public endpoint. The Phase 1 API runs locally only.
- No Azure subscription provisioning steps documented yet.
- No CI/CD. GitHub Actions wiring is deferred until there is reason for it (probably Phase 2 once async jobs make manual test runs slower).

### Observability artifacts
- No Application Insights screenshots. Wired in Phase 11.
- No Azure AI Search index portal screenshots. Index is created in Phase 4.
- No evaluation dashboards. Evaluation pipeline is built in Phase 10.

These are deliberately not faked. They will be captured from real running services as each phase produces them.

## Phase 1 specifically did not do

- **No EF migrations.** Schema is created via `EnsureCreated()` at startup. Fine for SQLite/dev, breaks for Azure SQL deploys. First proper migration is added when Phase 2 introduces a second entity (or sooner if a schema change is needed).
- **No file-content sniffing.** The endpoint trusts the multipart `Content-Type` header. A request claiming `application/pdf` for an arbitrary blob will be accepted. Magic-byte validation is a Phase 2 or Phase 3 concern when extraction would fail noisily on bad inputs anyway.
- **No deduplication.** Uploading the same file twice produces two `Document` rows and two blobs. Hashing + dedup decision belongs in Phase 2 alongside async processing.
- **No retention policy on the SQLite dev DB or local Azurite.** They grow until manually cleared.
- **No HTTPS in dev.** Listens on plain HTTP at `:5099` for ergonomics. Production runs behind TLS termination at the platform layer (Phase 12).
- **No structured logging sink.** `AddSimpleConsole` with scopes is good enough for Phase 1; Serilog or OpenTelemetry exporter to Application Insights lands in Phase 11.

## Open architecture decisions

| Decision | Tracking | Resolved by |
|---|---|---|
| ~~SQL vs Cosmos for metadata~~ | [ADR-0003](adr/0003-metadata-store-sql-vs-cosmos.md) | ✅ Closed in Phase 1 — Azure SQL prod / SQLite dev |
| Chunk persistence: SQL/Cosmos + Search, or Search-only? | [docs/architecture.md#open-architecture-questions](architecture.md#open-architecture-questions) | Phase 3 |
| Frontend: React vs Blazor | [docs/architecture.md](architecture.md) | Phase 5+ |
| Agent runtime: hand-built vs Foundry Agent Service | [ADR-0001](adr/0001-build-in-strict-phase-order.md) | Phase 7 |
| License | (none yet) | Before going public for portfolio use |

## Things to revisit

These are deferred, not forgotten. Listed here so they don't quietly disappear.

- **Authentication on `/api/documents/upload`.** Phase 1 ships unauthenticated for local development. The endpoint accepts an `X-Uploaded-By` header verbatim — trivially forgeable. Real auth lands in Phase 12, but a `[Authorize]` placeholder + dev bypass should be in place by Phase 5 so the eventual swap is mechanical.
- **Multi-tenancy.** Out of scope for this build. Documenting the assumption now so that a future tenant-isolated version doesn't get retrofitted into a single-tenant data model.
- **PII handling.** No PII redaction or detection yet. Required before any real customer document touches the system; expected scope addition in Phase 6 or Phase 12.
- **Cost guardrails.** No per-user or per-tenant token budget. Phase 11 adds visibility; an enforcement layer is a Phase 12+ candidate.
- **Embedding model versioning.** Re-embedding the corpus when the model version changes is not yet handled. Becomes relevant in Phase 4.
- **Prompt versioning.** Phase 5 introduces prompts; Phase 10 evaluation needs to compare versions. A simple `prompts/<name>/v1.md` convention is the working plan.

## What is intentionally out of scope

- A second LLM provider behind an abstraction layer. Azure OpenAI / Foundry only.
- A self-hosted vector database. Azure AI Search only.
- A full-blown agent framework on day one. By design — see [ADR-0001](adr/0001-build-in-strict-phase-order.md).
- A "production-ready" claim. This is a learning + portfolio project. Each pattern is implemented to be *understood and replicable*, not to serve a real customer load.

## How to read this file

- A bullet here means "we know about it and it is not done."
- A bullet that disappears in a future commit means it is now done — check the corresponding phase's PR or commit message for what changed.
- A new bullet appearing means a real gap was discovered, which is a healthy sign.
