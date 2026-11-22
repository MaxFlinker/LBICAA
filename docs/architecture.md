# LBICAA — Architecture

## 1. Architectural approach

LBICAA is organised as a .NET solution with separate projects for domain concepts, infrastructure, collection, verification, analysis and user-facing applications.

The intended architectural boundary is that core domain rules should not depend on databases, web frameworks or external services.

The domain model defines the concepts that the other components work with.

## 2. Solution structure

| Project | Responsibility | Current status |
|---|---|---|
| `LBICAA.Domain` | Claims, evidence, sources and verification records | Domain types implemented |
| `LBICAA.Infrastructure` | Persistence and external infrastructure | Project configured; implementation pending |
| `LBICAA.Crawler` | Source discovery and content collection | Scaffold |
| `LBICAA.Verification` | Verification services and workflows | Scaffold; verification record type resides in Domain |
| `LBICAA.Analysis` | Analysis and cross-source reasoning | Scaffold |
| `LBICAA.Api` | HTTP API | ASP.NET Core template with demonstration endpoint |
| `LBICAA.Atlas` | User-facing application | Minimal ASP.NET Core application |
| `LBICAA.Tests` | Automated tests | Initial domain tests implemented |

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

Entity Framework Core and SQLite packages are configured in Infrastructure.

A functioning persistence implementation has not yet been established.

The intended design should preserve claim history and provenance rather than silently replacing earlier information. Immutable revisions and append-only verification history remain design objectives.

## 6. Technology baseline

The current solution targets `.NET 11` using the pinned release candidate SDK specified in `global.json`.

The repository deliberately uses the selected preview toolchain. Package versions are aligned with that SDK baseline.

## 7. Current implementation boundary

The existence of a project does not mean its intended service is implemented.

At this stage, the domain types and initial tests are the substantive implementation. Infrastructure, collection, analysis and verification services remain development work. The API and Atlas are running applications, but are not yet complete LBICAA products.
