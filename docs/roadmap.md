# LBICAA — Project Roadmap

**Project:** LBICAA
**Current target:** Hamburg prototype
**Long-term target:** Germany-wide coverage
**Status:** Active development
**Roadmap type:** Implementation and delivery plan

---

## 1. Project objective

LBICAA aims to build a source-based knowledge and analysis system for public institutions, organisational structures, responsibilities, relationships and historical changes.

The first operational target is Hamburg. The architecture should support expansion to other German jurisdictions without requiring a redesign of the core domain.

The system must distinguish:

- What is asserted.
- Which source supports an assertion.
- Where the relevant information appears in that source.
- Whether evidence supports, contradicts or contextualises the assertion.
- Who or what performed verification.
- How reliable and current the available information is.

The system must preserve provenance. An assertion must not become an established fact merely because it has been entered into the database.

## 2. Core principles

1. **Source traceability:** every material claim should be traceable to its sources.
2. **Explicit uncertainty:** distinguish confirmed, disputed, incomplete and unknown information.
3. **Separation of concerns:** domain, application, infrastructure, API, analysis and presentation remain independently maintainable.
4. **Reproducibility:** ingestion and verification operations should be repeatable.
5. **Historical integrity:** changes should not silently overwrite the historical record.
6. **Extensibility:** new source types, jurisdictions and verification methods should be addable through defined interfaces.
7. **Responsible handling:** respect source licences, privacy requirements and applicable law.
8. **Testability:** important domain rules and workflows must have automated tests.

## 3. System architecture

The solution currently contains the following projects:

| Project | Responsibility |
|---|---|
| LBICAA.Domain | Core domain model and rules |
| LBICAA.Application | Use cases and application handlers |
| LBICAA.Infrastructure | Persistence and external infrastructure |
| LBICAA.Api | HTTP API |
| LBICAA.Verification | Verification-related functionality |
| LBICAA.Analysis | Analysis functionality |
| LBICAA.Atlas | Atlas and presentation-related functionality |
| LBICAA.Crawler | Source discovery and collection |
| LBICAA.Tests | Existing automated test suite |

The exact boundaries and responsibilities of the specialised projects should be reviewed as their implementations mature.

### Architectural direction

- Domain logic should not depend on EF Core, HTTP or external services.
- Application handlers should coordinate use cases through abstractions.
- Infrastructure should implement persistence and external-service integrations.
- The API should expose application capabilities without duplicating domain rules.
- Crawler and analysis components should use application interfaces rather than bypassing the domain model.
- Atlas should consume structured, verified data through defined interfaces.

## 4. Current implementation status

### Implemented and demonstrated

- [x] Core solution structure and project separation.
- [x] Claim creation workflow.
- [x] Source creation workflow.
- [x] Claim detail retrieval.
- [x] Evidence attachment workflow.
- [x] Verification recording workflow.
- [x] SQLite persistence foundation.
- [x] Claim retrieval including evidence and verification history.
- [x] HTTP API endpoint for creating claims.
- [x] HTTP API endpoint for creating sources.
- [x] HTTP API endpoint for retrieving claims.
- [x] HTTP API endpoint for attaching evidence.
- [x] HTTP API endpoint for recording verification.
- [x] JSON enum serialization using string values.
- [x] Handling of missing claim/source references in the relevant application workflows.
- [x] Resolution of SQLite DateTimeOffset ordering limitations in evidence and verification repositories.
- [x] Successful manual end-to-end HTTP test of claim creation, source creation, evidence attachment, verification and retrieval.
- [x] Automated test suite: 45 passed, 0 failed, 0 skipped.
- [x] Automated API integration tests for the core source-to-verification workflow.
- [x] Automated API error-handling tests for selected invalid requests and missing resources.
- [x] Migration execution and repository integration tests.

### Test coverage qualification

The API integration tests use an isolated SQLite database created with EnsureCreated().
Migration execution is tested separately using MigrateAsync().

The current test suite verifies the implemented workflows. It does not establish production readiness or coverage of future functionality.

## 5. Delivery roadmap

### Phase 1 — Domain foundation

**Objective:** establish a consistent and reliable domain model.

Scope:

- Claims and claim values.
- Sources and source metadata.
- Evidence and evidence assessments.
- Verification records.
- Temporal validity and historical information.
- Relationships between domain entities.
- Domain invariants and validation.

Acceptance criteria:

- Domain entities enforce their own invariants.
- Invalid identifiers and required values are rejected.
- Domain types do not depend on infrastructure.
- Domain tests cover important invariants and edge cases.

**Status:** Core claim, source, evidence and verification structures implemented. Further domain coverage remains necessary.

### Phase 2 — Persistence foundation

**Objective:** ensure domain records survive application restarts and can be retrieved consistently.

Scope:

- EF Core configuration.
- SQLite database.
- Repository implementations.
- Entity relationships.
- Database migrations.
- Indexes and query behaviour.
- Transaction boundaries.
- Persistence error handling.

Acceptance criteria:

- Claims, sources, evidence and verification records persist.
- Related records can be retrieved correctly.
- Database constraints prevent invalid relationships.
- Persistence tests cover create, retrieve and restart scenarios.
- Migrations can create and update the database reliably.

**Status: Implemented and tested.**

Implemented:
- EF Core DbContext and entity mappings.
- SQLite persistence.
- Database migrations and migration execution tests.
- Repositories for claims, sources, evidence and verification records.
- Persistence round-trip tests.
- Chronological retrieval of evidence and verification records.
- Foreign-key constraints for claim and source relationships.
- Restrict-delete behaviour for referenced records.

Migration and repository integration tests use isolated SQLite databases.

### Phase 3 — Application workflows

**Objective:** provide complete use cases independent of the HTTP interface.

Scope:

- Create claims.
- Create sources.
- Retrieve claim details.
- Attach evidence.
- Record verification.
- Update or supersede claims where permitted.
- Record historical changes.
- Handle missing references and invalid input.

Acceptance criteria:

- Handlers coordinate repositories through abstractions.
- Use cases enforce required business rules.
- Errors are predictable and testable.
- Application tests cover successful and unsuccessful operations.

**Status: Core workflows implemented and integration-tested.**

Implemented:
- Source creation.
- Claim creation.
- Claim detail retrieval.
- Evidence attachment.
- Verification recording.
- Handling of missing references in the tested workflows.

Further work:
- Claim updates and supersession.
- Historical change recording.
- Additional business-rule validation.

### Phase 4 — HTTP API

**Objective:** expose the core workflows through a consistent HTTP interface.

Implemented routes:

| Method | Route | Purpose |
|---|---|---|
| POST | /claims | Create a claim |
| GET | /claims/{id} | Retrieve a claim and its related records |
| POST | /sources | Create a source |
| POST | /claims/{id}/evidence | Attach evidence |
| POST | /claims/{id}/verifications | Record verification |

Remaining work:

- Consistent request validation.
- Consistent error response format.
- API integration tests.
- OpenAPI review.
- Pagination for collection endpoints.
- Filtering and search.
- API versioning strategy.
- Authentication and authorisation where required.
- Rate limiting and operational protections.

Acceptance criteria:

- All implemented routes have automated HTTP tests.
- Invalid requests return appropriate 400 responses.
- Missing resources return appropriate 404 responses.
- Successful creation returns appropriate 201 responses.
- OpenAPI accurately describes the API.

**Status: Implemented; core workflows integration-tested.**

Implemented routes:

- `POST /sources`
- `POST /claims`
- `GET /claims/{id}`
- `POST /claims/{id}/evidence`
- `POST /claims/{id}/verifications`

Automated integration tests cover the core source-to-verification workflow and selected error paths.

Remaining work:

- Consistent validation and error response contracts.
- OpenAPI review and documentation.
- Pagination.
- Filtering and search.
- API versioning.
- Authentication and authorisation.
- Rate limiting and operational safeguards.

### Phase 5 — Discovery and source ingestion

**Objective:** collect relevant public information from Hamburg sources.

Scope:

- Identify Hamburg public-sector data sources.
- Identify official institutional websites and publication portals.
- Identify available open-data catalogues and APIs.
- Implement source adapters.
- Retrieve metadata and source documents.
- Preserve publication dates, retrieval dates and source identifiers.
- Record content hashes where appropriate.
- Detect changed or withdrawn source material.
- Respect licences, terms of use and access restrictions.

Initial source categories:

- Hamburg public authorities.
- Senate and departmental publications.
- Public organisational directories.
- Open-data catalogues.
- Public reports and administrative documents.
- Public procurement and funding information where legally and technically accessible.

Acceptance criteria:

- At least one real Hamburg source can be ingested repeatably.
- Each collected item has traceable source metadata.
- Retrieval failures are recorded.
- Duplicate source material can be identified.
- Source updates do not silently destroy previous records.

**Status:** Crawler project exists. Operational source adapters and a repeatable Hamburg ingestion workflow remain outstanding.

### Phase 6 — Organisational reconstruction

**Objective:** reconstruct institutional structures and responsibilities from collected sources.

Scope:

- Institutions.
- Organisational units.
- Public offices and roles.
- Persons where legally appropriate and relevant.
- Responsibilities and mandates.
- Institutional relationships.
- Reporting and supervisory structures.
- Changes over time.

Acceptance criteria:

- Organisational records are linked to supporting sources.
- Relationships are explicitly represented.
- Conflicting source statements can coexist.
- Historical changes can be represented without erasing previous states.
- Inferences are distinguishable from directly documented assertions.

**Status:** Planned. Requires source ingestion and appropriate domain extensions.

### Phase 7 — Verification engine

**Objective:** make verification systematic, explainable and reproducible.

Scope:

- Manual review.
- Automated validation.
- Cross-source comparison.
- Legal review.
- Data validation.
- Verification outcomes.
- Verifier references.
- Verification notes and timestamps.
- Rules for stale, conflicting or insufficient evidence.
- Re-verification when sources change.

Acceptance criteria:

- Verification outcomes have documented meanings.
- Verification records remain linked to claims.
- Conflicting evidence does not disappear.
- Automated checks are repeatable.
- Verification decisions retain their provenance.
- Changes to underlying source material can trigger review.

**Status:** Verification domain and recording workflow implemented. Rules engine and systematic re-verification remain outstanding.

### Phase 8 — Analysis

**Objective:** derive useful, traceable analysis from the collected knowledge base.

Scope:

- Cross-source comparison.
- Organisational relationship analysis.
- Responsibility analysis.
- Funding and financial relationship analysis where supported by data.
- Historical comparison.
- Change detection.
- Contradiction detection.
- Missing-information analysis.
- Confidence and evidence-quality indicators.

Acceptance criteria:

- Analytical results can be traced to their input claims and sources.
- Inferences are distinguished from documented facts.
- Analysis is reproducible.
- Missing or contradictory information is visible.
- No unsupported conclusions are presented as established facts.

**Status:** Analysis project exists. Functional analytical workflows remain to be implemented and validated.

### Phase 9 — Atlas and visualisation

**Objective:** provide an accessible interface for exploring the knowledge base.

Scope:

- Institution and organisational profiles.
- Search and filtering.
- Claim detail views.
- Source and evidence inspection.
- Verification history.
- Relationship graphs.
- Historical timelines.
- Geographic presentation where appropriate.
- Clear indication of uncertainty and conflicting information.

Acceptance criteria:

- Users can navigate from an institutional record to its supporting claims and sources.
- Evidence and verification history are accessible.
- Relationships can be explored.
- Uncertainty is visible.
- The interface does not conceal provenance.

**Status:** Atlas project exists. End-user functionality remains outstanding.

### Phase 10 — Hamburg prototype

**Objective:** demonstrate a complete working system using real Hamburg data.

Required workflow:

1. Discover a real public information source.
2. Retrieve and preserve source metadata.
3. Extract relevant information.
4. Create claims from that information.
5. Attach source evidence to claims.
6. Verify claims.
7. Reconstruct relevant institutional relationships.
8. Present the results through Atlas.
9. Update the knowledge base when source material changes.

Acceptance criteria:

- At least one meaningful Hamburg institutional subject is represented.
- Its organisational structure is supported by traceable sources.
- Claims, evidence and verification records are persistent.
- Users can inspect the supporting information.
- The ingestion and update workflow can be repeated.
- Known limitations are documented.

**Status:** Not yet complete. This is the first major end-to-end delivery milestone.

### Phase 11 — Production readiness

**Objective:** make the Hamburg prototype maintainable and safe to operate.

Scope:

- Authentication and authorisation.
- Secrets management.
- Input and output validation.
- Structured logging.
- Audit trail.
- Backup and recovery.
- Database migration strategy.
- Monitoring and health checks.
- Error handling.
- Data retention and deletion procedures.
- Licence and privacy compliance.
- Deployment documentation.
- Operational testing.

Acceptance criteria:

- Deployment can be repeated from documented instructions.
- Data can be backed up and restored.
- Sensitive operations are protected.
- Errors are logged without leaking secrets.
- Dependencies and licences are documented.
- Operational limitations are understood.

**Status:** Outstanding.

### Phase 12 — Germany-wide expansion

**Objective:** extend the architecture beyond Hamburg.

Scope:

- Additional federal states.
- Federal institutions.
- Reusable jurisdiction-specific source adapters.
- Source catalogues and discovery configuration.
- Jurisdiction-specific organisational structures.
- Common data model with local extensions.
- Scalable ingestion and update processes.

Acceptance criteria:

- New jurisdictions can be added without changing core domain rules unnecessarily.
- Source adapters are independently testable.
- Data provenance remains consistent across jurisdictions.
- Different administrative structures can be represented accurately.

**Status:** Future milestone. Begin after the Hamburg prototype demonstrates the complete workflow.

---

## 6. Immediate development priorities

The next work should proceed in this order:

1. Review API contracts, validation and OpenAPI documentation.
2. Identify and document a real Hamburg source, including its licence and update characteristics.
3. Implement repeatable source ingestion.
4. Preserve source metadata, retrieval dates and content history.
5. Connect ingested information to claims and evidence.
6. Extend verification beyond recording individual verification results.
7. Model institutional structures and relationships.
8. Build the first Atlas views.

Persistence, migrations, repository behaviour and core API workflows have already received automated integration-test coverage.

Avoid implementing large numbers of features before the first real-data workflow is demonstrated.

## Next milestone: Hamburg source ingestion

**Objective:** demonstrate that LBICAA can retrieve and preserve real information from an official Hamburg source.

Acceptance criteria:

- At least one official Hamburg source is identified and documented.
- Its access method and licence conditions are recorded.
- Source metadata and retrieval timestamps are preserved.
- Relevant content can be retrieved repeatably.
- Duplicate and changed source material can be identified.
- Retrieval failures are recorded.
- Existing source and evidence provenance is preserved.

**Status: Planned.**

The persistence and core API foundations have been implemented and tested. The next milestone moves development towards real-world source material.

## 7. Definition of project completion

### Hamburg prototype complete

LBICAA reaches its first major completion milestone when a user can:

- Select or discover a real Hamburg public information source.
- Retrieve and preserve its relevant information.
- Inspect claims derived from that information.
- Follow each claim back to its source evidence.
- Inspect verification results and conflicting evidence.
- Explore the represented institutional relationships.
- See relevant historical changes.
- Repeat the process when the source is updated.

### Production-ready Hamburg deployment

The Hamburg prototype becomes production-ready when the preceding functionality is supported by adequate security, automated testing, operational documentation, backup and recovery, and legal and licence review.

### Germany-wide project completion

The broader project is complete when the architecture supports the intended German jurisdictions, with repeatable discovery, reconstruction, verification, analysis and visualisation workflows and documented coverage limitations.

---

## 8. Project governance

This roadmap is the authoritative high-level delivery plan.

Implementation decisions should be recorded alongside the relevant code or in project documentation.

Recommended supporting documents:

| File | Purpose |
|---|---|
| docs/roadmap.md | Project milestones and delivery status |
| docs/architecture.md | System architecture and dependency rules |
| docs/domain-model.md | Domain entities, relationships and invariants |
| docs/data-sources.md | Source catalogue, access methods and licences |
| docs/verification.md | Verification rules and evidence standards |
| docs/testing.md | Test strategy and test coverage |
| docs/decisions/ | Architecture decision records |
| CHANGELOG.md | Notable implementation changes |

### Status update convention

Use these status labels consistently:

- **Not started** — no implementation work completed.
- **Planned** — scope identified, implementation not started.
- **In progress** — implementation underway.
- **Implemented** — code exists.
- **Tested** — relevant automated tests pass.
- **Demonstrated** — working behaviour verified in an actual workflow.
- **Complete** — acceptance criteria met and documented.

A feature should not be marked complete merely because it compiles.

---

*Last updated: 22 November 2026*