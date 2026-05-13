# Architecture

> Status: **Phase 0 draft.** Contents are aspirational. Each phase replaces the matching section with what was actually built.

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
| Metadata store | Azure SQL or Cosmos DB ([ADR-0003](adr/0003-metadata-store-sql-vs-cosmos.md) open) |
| Messaging | Azure Service Bus ([ADR-0004](adr/0004-messaging-service-bus.md)) |
| Monitoring | Application Insights |
| Frontend | React or Blazor (deferred to Phase 5+) |

## Component map (target — not yet built)

```mermaid
flowchart LR
    User([User / Web UI]) -->|upload PDF| API[KnowledgeOps.Api<br/>.NET 8]
    API -->|store raw| Blob[(Blob Storage)]
    API -->|metadata| DB[(SQL / Cosmos)]
    API -->|enqueue| SB[[Service Bus]]
    SB -->|consume| Worker[KnowledgeOps.Worker<br/>Functions isolated]
    Worker -->|extract → chunk → embed| Search[(Azure AI Search<br/>BM25 + vector)]
    Worker -->|status| DB
    User -->|ask question| API
    API -->|hybrid query| Search
    Search -->|top-K chunks| API
    API -->|grounded prompt| LLM{{Azure OpenAI<br/>/ Foundry}}
    LLM -->|answer + citations| API
    API -->|response| User
    Orch[KnowledgeOps.Orchestrator<br/>Durable Functions] -.coordinates.-> Worker
    Agents[KnowledgeOps.AgentService<br/>Phase 7+] -.tool calls.-> API
    AppInsights[(Application Insights)]
    API -.traces.-> AppInsights
    Worker -.traces.-> AppInsights
    Agents -.traces.-> AppInsights
```

## Ingestion pipeline (Phase 1 → Phase 4)

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant A as KnowledgeOps.Api
    participant B as Blob Storage
    participant D as Metadata DB
    participant Q as Service Bus
    participant W as KnowledgeOps.Worker
    participant E as Embedding API
    participant S as Azure AI Search

    U->>A: POST /api/documents/upload (PDF)
    A->>B: PUT raw file
    A->>D: INSERT Document (status=Uploaded)
    A->>Q: publish DocumentUploaded
    A-->>U: 200 OK { documentId }
    Q->>W: deliver DocumentUploaded
    W->>D: UPDATE status=Processing
    W->>B: GET raw PDF
    W->>W: extract text + chunk (800/100)
    W->>D: INSERT chunks
    W->>E: embed chunks
    W->>S: upsert chunk + vector
    W->>D: UPDATE status=Processed
    Note over W,D: On failure: status=Failed,<br/>error preserved, do not delete
```

## RAG query flow (Phase 5)

```mermaid
sequenceDiagram
    autonumber
    participant U as User
    participant A as KnowledgeOps.Api
    participant S as Azure AI Search
    participant L as Azure OpenAI

    U->>A: POST /api/ask { question }
    A->>S: hybrid query (BM25 + vector, RRF)
    S-->>A: top-K chunks + scores + metadata
    alt retrieval too weak
        A-->>U: { answer: "not enough information", confidence: low }
    else retrieval ok
        A->>L: prompt(system + retrieved context + question)
        L-->>A: grounded answer
        A-->>U: { answer, citations[], confidence }
    end
```

## Agent orchestration (Phase 7 → Phase 8)

```mermaid
flowchart TD
    Req[User request] --> Orchestrator{Orchestrator<br/>step budget=5}
    Orchestrator -->|1. retrieve| Retriever[Retriever Agent<br/>tool: SearchDocuments]
    Retriever --> Orchestrator
    Orchestrator -->|2. extract| Extractor[Extraction Agent<br/>tool: ExtractFields]
    Extractor --> Orchestrator
    Orchestrator -->|3. validate| Compliance[Compliance Agent<br/>tool: GetDocument]
    Compliance --> Orchestrator
    Orchestrator -->|4. act| Action[Action Agent<br/>tool: CreateCase, UpdateStatus]
    Action --> Orchestrator
    Orchestrator -->|5. respond| Response[Final response<br/>+ audit log]
    Orchestrator -.budget exceeded.-> Halt[Halt with<br/>terminated:step_limit]
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

- **Metadata store:** SQL vs Cosmos. See [ADR-0003](adr/0003-metadata-store-sql-vs-cosmos.md) — decided in Phase 1.
- **Chunk persistence:** Chunks in SQL/Cosmos *and* Search, or Search-only with metadata pointers? Decided in Phase 3.
- **Frontend:** React vs Blazor. Deferred until there is an API worth driving.
- **Agent runtime:** Foundry Agent Service vs in-process tool loop. Per [ADR-0001](adr/0001-build-in-strict-phase-order.md), build mechanics by hand first; revisit Foundry in Phase 7.
