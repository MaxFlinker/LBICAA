# LBICAA — Architecture

## 1. Architectural approach

**LBICAA — Governmental Organigram with Governance Evidence.** LBICAA is organised as a .NET solution with separate projects for domain concepts, infrastructure, collection, verification, analysis and user-facing applications. Together these support an evidence-backed representation of public institutions, organisational units, responsibilities and relationships.

The intended architectural boundary is that core domain rules should not depend on databases, web frameworks or external services.

The domain model defines the concepts that the other components work with.

## 2. Solution structure

| Project | Responsibility | Current status |
|---|---|---|
| `LBICAA.Domain` | Claims, evidence, sources and verification records | Domain types implemented |
| `LBICAA.Infrastructure` | Persistence and external infrastructure | SQLite persistence, migrations and repositories implemented and tested |
| `LBICAA.Crawler` | Source discovery and content collection | Scaffold; operational source adapters remain outstanding |
| `LBICAA.Verification` | Verification services and workflows | Verification recording workflow implemented; rules engine and systematic re-verification remain outstanding |
| `LBICAA.Analysis` | Analysis and cross-source reasoning | Project exists; functional analytical workflows remain outstanding |
| `LBICAA.Api` | HTTP API | Core claim, source, evidence and verification routes implemented and integration-tested |
| `LBICAA.Atlas` | User-facing application | Project exists; complete end-user organigram functionality remains outstanding |
| `LBICAA.Tests` | Automated tests | Domain, persistence and core API workflow tests implemented; see roadmap qualifications |

## 3. Domain model

### Claims

The `Claim` model represents an explicit proposition.

It contains:

- `Id`
- `SubjectId`
- `Predicate`
- `Object`
- `Nature`
- `Status`
- `Confidence`
- `Validity`
- `RecordedAt`

`ClaimValue` distinguishes entity references from literal values.

`TemporalValidity` represents the period during which a claim is considered valid in the real world. `RecordedAt` records when the system stored it.

These are separate concepts and must not be conflated.

### Evidence

An `Evidence` record links to a claim and identifies a source and locator.

Its assessment is one of:

- `Supports`
- `Contradicts`
- `Contextual`

This allows multiple pieces of evidence, including conflicting evidence, to be associated with the same claim.

### Sources

A `Source` records identifying and provenance information, including title, publisher, URI, type, publication date, retrieval date and licence status.

An optional content hash can support integrity checks.

### Verification records

A `VerificationRecord` records a verification method, outcome, verifier reference, notes and verification timestamp.

Verification outcomes include confirmed, partially confirmed, inconclusive, disputed and rejected.

The record is a domain concept. A complete verification service and append-only history mechanism are future implementation work.

## 4. Architectural boundaries

The intended dependency direction is:

- Domain defines the core concepts.
- Infrastructure implements persistence and external technical integrations.
- Crawler collects source material.
- Verification evaluates claims and evidence.
- Analysis performs structured reasoning.
- API exposes application capabilities.
- Atlas provides a user-facing interface.

The domain should remain independent of infrastructure-specific implementations.

## 5. Persistence and history

Entity Framework Core and SQLite persistence are implemented in Infrastructure, with migrations, repositories and integration coverage for the workflows described in the roadmap.

The intended design should preserve claim history and provenance rather than silently replacing earlier information. Immutable revisions and append-only verification history remain design objectives.

## 6. Technology baseline

The current solution targets `.NET 11` using the pinned release candidate SDK specified in `global.json`.

The repository deliberately uses the selected preview toolchain. Package versions are aligned with that SDK baseline.

## 7. Current implementation boundary

The existence of a project does not mean its intended service is implemented.

Core domain, persistence and claim-to-verification API workflows are implemented. Source discovery, the complete organisational reconstruction and verification workflows, and the end-user governmental organigram remain development work. Refer to the roadmap for authoritative phase scope, completed work and status; this architecture summary does not replace it.
