# LBICAA — Data Governance

## 1. Purpose

Data governance defines how LBICAA handles information, preserves its provenance, communicates uncertainty and manages the responsible use of collected material for its governmental organigram. Governance evidence lets readers inspect the basis and history of represented institutions, units, responsibilities and relationships.

The objective is to make the research record reliable, traceable and appropriately controlled.

## 2. Provenance

Information should retain a connection to its origin.

Source records are intended to identify:

- Title.
- Publisher.
- URI.
- Source type.
- Publication date, where available.
- Retrieval timestamp.
- Licence status.
- Licence reference, where available.
- Content hash, where available.

A source without complete provenance should not be silently treated as fully documented.

Unknown information should be represented as unknown rather than filled with assumptions.

## 3. Evidence traceability

Every evidence record should identify the claim it concerns and the source from which it originates.

Locators should make the relevant material retrievable, for example through a document page, section, URL fragment or dataset record.

Excerpts should preserve sufficient context to avoid misleading interpretation.

Evidence that contradicts a claim is as important to preserve as evidence that supports it.

## 4. Licensing and permitted use

Public accessibility does not automatically mean unrestricted reuse.

LBICAA distinguishes licence states including:

- Known.
- Unknown.
- Restricted.
- Public domain.

Where available, the specific licence or legal basis should be recorded.

Before reproducing or redistributing source material, the applicable permissions and restrictions must be assessed.

The presence of a source URI in LBICAA does not itself grant permission to republish that source's contents.

## 5. Integrity and change history

Content hashes may be used to identify changes in retrieved source material and support integrity checks.

The intended system should preserve the history of important changes to claims and verification records.

In particular:

- Earlier claims should not disappear through silent replacement.
- Retractions should remain distinguishable from deletion.
- Verification history should be retained.
- Changes should be attributable and timestamped.

Immutable revision snapshots and append-only audit mechanisms are intended governance capabilities; they are not yet fully implemented.

## 6. Personal and sensitive information

LBICAA should collect only information that is relevant and justified for its research purpose.

Particular care is required with personal data, confidential material and information that could cause harm if published or taken out of context.

Public availability alone is not sufficient justification for unrestricted collection, retention or publication.

Where applicable, data protection requirements and other legal obligations must be considered.

## 7. Access and publication

Access to research data should be governed according to its nature and permitted use.

The intended distinction is between:

- Publicly reusable information.
- Publicly viewable information subject to reuse restrictions.
- Material requiring controlled access.
- Material that should not be collected or retained.

Publication of the organigram should preserve source attribution and communicate uncertainty, limitations and unresolved disputes. Its presentation must not imply that a represented structure is complete, current or authoritative beyond the available evidence.

## 8. Corrections and disputes

A correction should not silently erase the previous research record.

Where a claim is disputed or retracted, its status should be updated while preserving the information needed to understand its history.

New evidence should be associated with the relevant claim and assessed on its merits.

## 9. Governance responsibilities

Governance rules must apply to human and automated processes alike.

Automated extraction, classification and analysis should remain distinguishable from human review.

No automated process should silently convert a hypothesis or inference into a documented fact.

## 10. Current status

The domain model already includes source licensing fields, evidence assessments, claim status, claim nature, temporal validity and verification records.

A complete access-control model, retention policy, audit implementation, immutable revision mechanism and publication governance workflow remain future work.
