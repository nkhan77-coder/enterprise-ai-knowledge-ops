# Current Limitations & Known Gaps

> Status as of: **2026-05-13, end of Phase 0.**
> This file is updated at the close of every phase. It exists so contributors and reviewers can see what is *not* done without having to reverse-engineer it from the code.

The point of this document: an honest "serious work in progress" view, not a polished marketing surface. If a section here is gone, the work is genuinely complete — not hidden.

## What does not exist yet

### Code
- No compiled .NET projects yet. The `/src` folders contain only `.gitkeep` markers.
- No `KnowledgeOps.sln`. Created in Phase 1 alongside `KnowledgeOps.Api`.
- No `KnowledgeOps.Shared` contracts. Defined in Phase 1.
- No tests. The `/tests` folder is a placeholder until Phase 1 introduces the first project to test.
- No infrastructure-as-code. `/infra` is a placeholder until Phase 12 (or earlier if a deploy is needed sooner).

### Running services
- No deployed environment. There is no live URL, no demo, no public endpoint.
- No Azure subscription provisioning steps documented yet — added in Phase 1.
- No CI/CD. GitHub Actions wiring is deferred until there is something to build (Phase 1+).

### Observability artifacts
- No Application Insights screenshots. Wired in Phase 11.
- No Azure AI Search index portal screenshots. Index is created in Phase 4.
- No evaluation dashboards. Evaluation pipeline is built in Phase 10.

These are deliberately not faked. They will be captured from real running services as each phase produces them.

## Open architecture decisions

| Decision | Tracking | Resolved by |
|---|---|---|
| SQL vs Cosmos for metadata | [ADR-0003](adr/0003-metadata-store-sql-vs-cosmos.md) | Phase 1 |
| Chunk persistence: SQL/Cosmos + Search, or Search-only? | [docs/architecture.md#open-architecture-questions](architecture.md#open-architecture-questions) | Phase 3 |
| Frontend: React vs Blazor | [docs/architecture.md](architecture.md) | Phase 5+ |
| Agent runtime: hand-built vs Foundry Agent Service | [ADR-0001](adr/0001-build-in-strict-phase-order.md) | Phase 7 |
| License | (none yet) | Before going public for portfolio use |

## Things to revisit

These are deferred, not forgotten. Listed here so they don't quietly disappear.

- **Authentication on `/api/documents/upload`.** Phase 1 ships unauthenticated for local development. Real auth lands in Phase 12, but a `[Authorize]` placeholder + dev bypass should be in place by Phase 5 so the eventual swap is mechanical.
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
