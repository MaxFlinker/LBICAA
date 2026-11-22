# LBICAA — Research Methodology

## 1. Research as a traceable process

LBICAA is designed around a structured relationship:

**Source → Evidence → Claim → Assessment → Conclusion**

Each stage has a distinct purpose. A source is not itself a conclusion, and the existence of a statement in a source does not automatically establish its truth.

Research should remain inspectable from the final conclusion back to the original material.

## 2. Sources

A source is an identifiable origin of information.

Examples include:

- Legislation.
- Official documents.
- Public datasets.
- Official websites.
- Publications.
- Archives.
- Other identifiable material.

Source records should retain relevant provenance, including publisher, URI, publication date where known, retrieval date and licensing information.

The authority of a source depends on the question being investigated. An official source may be authoritative about its own published decisions, but that does not automatically make every assertion within it independently verified.

## 3. Evidence

Evidence identifies material relevant to a particular claim.

Each evidence record refers to:

- The claim being examined.
- The source containing the material.
- A locator identifying where the material can be found.
- An optional excerpt.
- An assessment of its relationship to the claim.

The assessment can be:

- **Supports:** the material provides support for the claim.
- **Contradicts:** the material conflicts with the claim.
- **Contextual:** the material provides relevant background without directly establishing or refuting the claim.

Evidence should be assessed in context. Extracted passages must not be treated as independent of their surrounding material.

## 4. Claims

Claims are explicit propositions about subjects and their relationships.

A claim should be sufficiently precise to be examined independently.

LBICAA distinguishes:

- **Documented claims:** explicitly recorded in an identifiable source.
- **Inferred claims:** derived from other information through reasoning.
- **Hypotheses:** propositions that remain to be tested.

Claims can also have different statuses:

- Active.
- Disputed.
- Retracted.

A disputed claim is retained as part of the research record. Retraction should not be confused with deletion of historical information.

## 5. Confidence and verification

Confidence describes the current degree of confidence assigned to a claim.

It is distinct from the nature of the claim and from the number of sources associated with it.

Verification should record what was checked, how it was checked, who or what performed the check, and the outcome.

Available verification outcomes include:

- Confirmed.
- Partially confirmed.
- Inconclusive.
- Disputed.
- Rejected.

A verification outcome is an assessment made using a particular method and available information. It should not be presented as an absolute guarantee of truth.

## 6. Contradictions and uncertainty

Contradictory information is a normal part of research.

LBICAA should preserve conflicting claims and their supporting evidence rather than selecting one account and erasing the other.

A contradiction should prompt examination of:

- The sources involved.
- Their publication and retrieval dates.
- Their respective scope and authority.
- The period to which each statement applies.
- Whether the apparent conflict is genuine or only contextual.

Uncertainty must remain visible until sufficient evidence supports a more definite assessment.

## 7. Temporal reasoning

Two different timelines matter:

- **Validity time:** when a claim applies in the real world.
- **Recording time:** when LBICAA recorded the claim.

A statement may describe an event in the past while only being entered into the system much later.

Preserving both timelines is essential for reconstructing what was known and when it was known.

## 8. Conclusions

Conclusions should be derived from identifiable claims and evidence.

They should distinguish established information from inference, identify unresolved contradictions and communicate material limitations.

Automated analysis may assist research, but it must not silently promote generated interpretations into documented facts.

## 9. Implementation status

The current domain model provides structures for claims, evidence, sources and verification records.

The complete research workflow, automated source evaluation, cross-source analysis and operational verification services remain to be implemented.
