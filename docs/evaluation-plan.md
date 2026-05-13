# Evaluation Plan

> Status: **Phase 0 plan.** Implemented in Phase 10. Until then, this is the contract we are building toward.

## Why

Without measurement, "the AI works" is an opinion. We cannot answer "did that prompt change make things better?" or "is 800/100 chunking better than 400/50?" without numbers. Phase 10 turns these guesses into A/B comparisons.

## Test set

Stored under `/tests/evaluation/`. Format (per question):

```json
{
  "id": "Q-001",
  "question": "What is the cancellation policy?",
  "expectedAnswer": "Policies can be cancelled within 30 days...",
  "expectedSource": { "fileName": "policy.pdf", "pageNumber": 4 },
  "requiredKeywords": ["30 days", "refund"],
  "category": "policy"
}
```

Initial size: **20 questions** across at least 3 document types. Grow to 100 once the pipeline stabilizes.

## Metrics

| Metric | Definition | Target |
|---|---|---|
| Retrieval recall@5 | Expected source chunk appears in top-5 retrieval | ≥ 0.90 |
| Answer correctness | Required keywords present + factually right | ≥ 0.85 |
| Groundedness | Every claim traceable to a cited chunk | ≥ 0.95 |
| Citation accuracy | Cited chunk actually supports the claim | ≥ 0.90 |
| Avg latency | p50 + p95 end-to-end | p95 < 5s |
| Avg token cost | Input + output tokens per question | tracked, not gated |
| Tool success rate | Phase 7+ — agent tool calls that complete cleanly | ≥ 0.95 |

## Endpoint

```
POST /api/evaluations/run
```

Body: optional config override (chunking, top-K, prompt version). Returns aggregate metrics + per-question results.

## What "good enough" means

- A change ships only if it does not regress groundedness or citation accuracy.
- Latency regressions ≥ 20% require a written tradeoff justification.
- A failed eval run blocks the change — no "we'll fix it in prod."

## What this plan deliberately leaves out

- Human preference scoring (subjective, later).
- Adversarial / red-team evals (later, once the happy path is solid).
- Cost optimization (track first, optimize once usage is real).
