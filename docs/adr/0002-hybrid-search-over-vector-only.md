# ADR-0002: Hybrid search over vector-only retrieval

- **Status:** Accepted
- **Date:** 2026-05-13
- **Phase:** 0 (implemented in Phase 4)

## Context

Vector-only retrieval is the default in most RAG tutorials: embed everything, do cosine similarity, return top-K. It works for paraphrased questions and semantic matches, but it has known weaknesses:

- **Exact-term queries underperform.** "What is the cancellation policy for plan SKU-9912?" — the SKU is a literal string the user expects to match exactly. Pure vector similarity often ranks a semantically similar but wrong-SKU chunk above the right one.
- **Acronyms, identifiers, version numbers, and proper nouns** are not well-represented in embedding space.
- **Out-of-distribution domain vocabulary** (insurance jargon, internal product codes) gets poor embeddings unless the model was trained on it.

Azure AI Search supports hybrid retrieval natively: it runs BM25 keyword search and vector similarity in the same request and merges the result lists with Reciprocal Rank Fusion (RRF). Microsoft's published benchmarks and our own intuition both favor hybrid over either component alone for general enterprise corpora.

## Decision

Phase 4 indexes use:

- A `searchable` text field for BM25 keyword scoring.
- A `vector` field for embedding similarity.
- All retrieval queries issue **hybrid** by default, with RRF result merging.
- Vector-only and keyword-only modes are kept available as evaluation baselines, not as production paths.

## Consequences

**Positive**
- Better retrieval recall on identifier-heavy enterprise queries.
- Single index, single request — no client-side merging logic to maintain.
- Phase 4 done-criterion ("hybrid beats keyword-only and vector-only") becomes a measurable claim, not a marketing claim.

**Negative**
- Slightly more index storage (text + vector representation of every chunk).
- Hybrid scoring is harder to reason about than pure cosine similarity when debugging a bad result.

**Mitigation**
- Phase 10 evaluation runs report scores per retrieval mode so degradations are caught.
- Phase 11 observability captures the actual hybrid score breakdown per query for forensics.

## Related

- [docs/architecture.md](../architecture.md)
- [docs/evaluation-plan.md](../evaluation-plan.md)
