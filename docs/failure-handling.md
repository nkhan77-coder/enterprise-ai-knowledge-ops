# Failure Handling

> Status: **Phase 0 contract.** Each phase that introduces new failure modes appends a section here.

## Principles

1. **Failures are visible, not silent.** A failed document stays in the system with a `Failed` status and an error message — never deleted, never hidden.
2. **Retry the transient, surface the permanent.** Network blips retry automatically. Malformed PDFs do not.
3. **Idempotency by default.** Re-processing the same message must not create duplicate chunks, duplicate index entries, or duplicate database rows.
4. **No swallowed exceptions.** Every catch block either retries, transitions state, or logs with `correlationId`. No empty `catch {}`.

## Failure modes by phase

### Phase 1 — API ingestion
| Failure | Response |
|---|---|
| Blob upload fails | 500 to client, no DB row created |
| DB write fails after blob upload | Best-effort blob delete, 500 to client |
| File exceeds size limit | 413 Payload Too Large, no blob, no row |
| Non-PDF MIME type | 415 Unsupported Media Type |

### Phase 2 — Worker / Service Bus
| Failure | Response |
|---|---|
| Worker exception (transient) | Service Bus redelivery, max 5 attempts |
| Worker exception (permanent) | Move to dead-letter queue, status = `Failed`, error logged |
| Blob missing on download | Status = `Failed`, do not retry |
| Duplicate message delivery | Idempotency check on `documentId` — skip if already `Processed` |

### Phase 3+ — Chunking, embedding, indexing
| Failure | Response |
|---|---|
| Text extraction fails (scanned image, encrypted) | Status = `Failed`, error = "extraction_failed: <reason>" |
| Embedding API throttle (429) | Exponential backoff, max 3 attempts, then DLQ |
| AI Search index write fails | Retry once, then status = `Failed` |
| Partial chunk index (some succeeded, some failed) | Roll back to "no chunks indexed for this doc" — all-or-nothing per document |

### Phase 5 — RAG
| Failure | Response |
|---|---|
| No chunks retrieved | Return `"not enough information"`, confidence = low |
| LLM timeout | One retry, then 504 with correlationId |
| LLM returns ungrounded answer | Detected by groundedness check (Phase 10), flagged in logs |

### Phase 7+ — Agents
| Failure | Response |
|---|---|
| Tool call throws | Return structured tool error to agent, agent decides next step |
| Agent exceeds step budget | Halt, return partial result with `terminated: "step_limit"` |
| Tool returns malformed schema | Log + treat as tool failure, never pass garbage to next step |

## Dead-letter strategy

- One DLQ per logical queue.
- DLQ messages retain original payload, error, attempt count, and `correlationId`.
- A separate operator endpoint replays DLQ messages after the underlying issue is fixed (Phase 2 done-criterion: "failed documents do not disappear").

## What we will NOT do

- Auto-delete failed documents on a timer. They stay until a human acts.
- Catch-all `Exception` handlers without logging context.
- Return 200 OK when something failed silently in the background.
