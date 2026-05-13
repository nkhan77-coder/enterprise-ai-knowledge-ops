# Chunking Strategy

> Status: **Phase 0 placeholder.** Real measurements get filled in during Phase 3.

## Why chunking matters

Retrieval quality is bounded by chunk quality. Bad chunks = bad retrieval = bad RAG, no matter how good the model is. Embedding similarity is computed *per chunk*, so a chunk that mixes two unrelated topics scores poorly on both. A chunk that splits a sentence mid-clause loses the answer.

## Why overlap exists

Without overlap, a sentence that spans a chunk boundary is split — the answer to a question can sit half in chunk N and half in chunk N+1, and neither chunk alone has enough context to be retrieved or cited correctly. Overlap (typically 10–15% of chunk size) gives boundary content a second chance to be embedded as part of a coherent unit.

## Starting configuration (Phase 3 baseline)

```
Chunk size: 800 tokens
Overlap:    100 tokens (~12.5%)
Tokenizer:  cl100k_base (GPT-4 family)
Boundary:   prefer paragraph > sentence > hard split
```

Rationale: 800 tokens is a common sweet spot — large enough to carry a full thought, small enough that retrieval scoring stays specific.

## Configurations to test in Phase 3

| Size | Overlap | Hypothesis |
|---|---|---|
| 400 | 50 | Tighter, more chunks, higher recall but more noise |
| 800 | 100 | Baseline |
| 1200 | 150 | Fewer chunks, richer context, may dilute scores |

Each configuration runs against the Phase 10 evaluation set. We compare retrieval recall, answer correctness, and groundedness — not vibes.

## Results

_To be filled in during Phase 3 with side-by-side numbers and 1–2 concrete bad-chunk examples per configuration._

| Config | Recall@5 | Correctness | Groundedness | Notes |
|---|---|---|---|---|
| 400/50   | TBD | TBD | TBD | |
| 800/100  | TBD | TBD | TBD | |
| 1200/150 | TBD | TBD | TBD | |

## Decision

_To be recorded once data exists. Format: "Chose X/Y because Z; tradeoff was W."_
