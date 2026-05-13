# Data Flow

> Status: **Phase 0 draft.** Each phase will append a "what changed" subsection.

## End-to-end ingestion (target state, post-Phase 4)

```
1. Client → POST /api/documents/upload (multipart PDF)
2. Api    → Blob Storage   (PUT raw PDF, returns blobUrl)
3. Api    → Metadata store (INSERT Document row, status = Uploaded)
4. Api    → Service Bus    (publish DocumentUploaded message)
5. Api    → Client         (200 OK, { documentId })
6. Worker ← Service Bus    (consume DocumentUploaded)
7. Worker → Metadata       (status = Processing)
8. Worker → Blob           (download PDF)
9. Worker → text extractor (PDF → plain text + page map)
10. Worker → chunker       (text → chunks[] with overlap)
11. Worker → Metadata      (INSERT chunk rows)
12. Worker → embedding API (chunks → vectors)
13. Worker → AI Search     (upsert chunk + vector)
14. Worker → Metadata      (status = Processed)
```

## Query flow (target state, post-Phase 5)

```
1. Client → POST /api/ask { question }
2. Api    → AI Search   (hybrid query: BM25 + vector, RRF)
3. Api    ← top-K chunks with scores + metadata
4. Api    → LLM         (system + retrieved context + question)
5. Api    ← grounded answer
6. Api    → Client      ({ answer, citations[], confidence })
```

## Status state machine

```
Uploaded ──▶ Processing ──▶ Processed
                │
                └──▶ Failed (with errorMessage)
```

`Failed` is terminal until manual reprocess. Worker MUST NOT delete failed records — Phase 2 done-criterion.

## Message contract: DocumentUploaded (Phase 2)

```json
{
  "documentId": "<guid>",
  "blobUrl": "https://<account>.blob.core.windows.net/raw/<id>.pdf",
  "requestedBy": "<userId or email>",
  "uploadedAt": "<ISO-8601 UTC>"
}
```

Schema versioning rule: add fields, never rename or remove. Bump `schemaVersion` once we have more than one consumer.

## Correlation

Every step propagates a `correlationId` (set by API on first request). Phase 11 wires this into Application Insights so a single failed answer can be traced from upload through retrieval to model call.
