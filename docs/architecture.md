# Architecture

> Status: **Phase 0 draft.** Contents are aspirational. Each phase will replace the matching section with what was actually built.

## Goal

Build an enterprise document intelligence platform that ingests PDFs, extracts structured information, answers grounded questions with citations, and exposes agent-driven workflows — all on Azure-native services with full traceability.

## Target stack

| Concern | Service |
|---|---|
| Backend API | .NET 8 Web API (`KnowledgeOps.Api`) |
| Async worker | Azure Functions isolated worker (`KnowledgeOps.Worker`) |
| Long-running orchestration | Azure Durable Functions (`KnowledgeOps.Orchestrator`) |
| Agents | Microsoft Foundry Agent Service (`KnowledgeOps.AgentService`) |
| Search | Azure AI Search (hybrid: BM25 + vector + RRF) |
| LLM | Azure OpenAI / Foundry model deployment |
| Object storage | Azure Blob Storage |
| Metadata store | Azure SQL or Cosmos DB (decision deferred to Phase 1) |
| Messaging | Azure Service Bus |
| Monitoring | Application Insights |
| Frontend | React or Blazor (decision deferred to Phase 5+) |

## Component map (target — not yet built)

```
┌──────────┐   upload    ┌──────────────────┐   enqueue   ┌────────────────┐
│  Web UI  │ ──────────▶ │ KnowledgeOps.Api │ ──────────▶ │ Service Bus    │
└──────────┘             └──────────────────┘             └────────────────┘
                                  │                                │
                                  │ metadata                       │ message
                                  ▼                                ▼
                          ┌────────────────┐             ┌────────────────────┐
                          │ SQL / Cosmos   │             │ KnowledgeOps.Worker │
                          └────────────────┘             └────────────────────┘
                                                                   │
                                                                   ▼
                                                    extract → chunk → embed → index
                                                                   │
                                                                   ▼
                                                          ┌────────────────┐
                                                          │ Azure AI Search│
                                                          └────────────────┘
```

## Phase ownership

| Phase | Component(s) introduced |
|---|---|
| 1 | Api + Blob + metadata store |
| 2 | Service Bus + Worker, async status flow |
| 3 | Chunking pipeline (still in Worker) |
| 4 | Azure AI Search index, embeddings, search endpoint |
| 5 | RAG `/api/ask` endpoint, citation contract |
| 6 | Structured extraction endpoint |
| 7 | Single tool-calling agent |
| 8 | Multi-agent orchestrator (bounded) |
| 9 | Durable Functions for long-running workflows |
| 10 | Evaluation harness |
| 11 | App Insights / distributed tracing |
| 12 | Entra ID, Key Vault, RBAC, Managed Identity |

## Open architecture questions

- SQL vs Cosmos for metadata? Decide in Phase 1 based on shape of read queries.
- Where do chunks live: SQL/Cosmos *and* Search, or Search-only with metadata pointers? Decide in Phase 3.
- React vs Blazor frontend? Defer until there is an API worth driving.
- Foundry Agent Service vs in-process tool loop? Defer until Phase 7 — first build the mechanics ourselves.
