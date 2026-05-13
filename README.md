# enterprise-ai-knowledge-ops

Enterprise AI Knowledge Operations Platform — built in phases on Azure.

## Stack

- **Backend:** .NET 8 Web API
- **Worker:** Azure Functions (isolated)
- **Orchestration:** Azure Durable Functions
- **Search:** Azure AI Search (hybrid: BM25 + vector + RRF)
- **LLM:** Azure OpenAI / Microsoft Foundry model deployment
- **Storage:** Azure Blob Storage
- **Database:** Azure SQL or Cosmos DB (decision deferred to Phase 1)
- **Messaging:** Azure Service Bus
- **Monitoring:** Application Insights
- **Frontend:** React or Blazor (decision deferred to Phase 5+)
- **Agents:** Microsoft Foundry Agent Service (Phase 7+, not first)

## Repository layout

```
/src
  /KnowledgeOps.Api            ← .NET 8 Web API (Phase 1)
  /KnowledgeOps.Worker         ← Azure Functions isolated worker (Phase 2)
  /KnowledgeOps.Orchestrator   ← Durable Functions (Phase 9)
  /KnowledgeOps.AgentService   ← Foundry Agent Service host (Phase 7+)
  /KnowledgeOps.Shared         ← Shared contracts, DTOs, message types
  /KnowledgeOps.Web            ← React or Blazor frontend (Phase 5+)
/tests                         ← Unit + integration + evaluation tests
/docs                          ← Architecture, data flow, chunking, eval, failure
/infra                         ← IaC (Bicep/Terraform — Phase 12)
```

## Phase plan

Build in this order. **Do not jump ahead.** Each phase has explicit "done" criteria.

| # | Phase | Done when |
|---|---|---|
| 0 | Repo + initial docs | Folder structure exists; 5 docs exist | ✅ |
| 1 | Upload PDF, store metadata | 5 PDFs uploaded, DB row per file, errors logged |
| 2 | Async processing via Service Bus | API returns fast; worker independent; failures don't disappear |
| 3 | Extract text + chunk | Can explain overlap; bad vs good chunks documented |
| 4 | Embeddings + Azure AI Search | Hybrid beats keyword-only and vector-only |
| 5 | RAG with citations | 20 questions answered with citations; bad Qs return "not enough information" |
| 6 | Structured extraction | 5 doc types processed; bad extractions flagged |
| 7 | Tool-calling agent | Agent can create a case; every tool call logged |
| 8 | Multi-agent workflow (bounded) | 4 agents; can explain each; loops prevented |
| 9 | Durable orchestration | Workflow survives failure; retry policy works |
| 10 | Evaluation | Chunking + prompt comparisons have numbers |
| 11 | Observability | One failed answer traceable end-to-end |
| 12 | Enterprise security | Entra ID + RBAC + Key Vault + Managed Identity |

## Current status

**Phase 0 complete.** Phase 1 starts next: a single `POST /api/documents/upload` endpoint in `KnowledgeOps.Api`.

## Docs

- [docs/architecture.md](docs/architecture.md)
- [docs/data-flow.md](docs/data-flow.md)
- [docs/chunking-strategy.md](docs/chunking-strategy.md)
- [docs/evaluation-plan.md](docs/evaluation-plan.md)
- [docs/failure-handling.md](docs/failure-handling.md)
