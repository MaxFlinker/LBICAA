# LBICAA

**LBICAA** is an open-source, claims-centred system for researching, documenting and analysing public administration, institutions, organisations and their activities.

Its central purpose is to make claims traceable to evidence and sources. Rather than treating an institution or publication as a single authoritative record, LBICAA is designed to represent individual claims, their supporting or contradicting evidence, their temporal context and their verification history.

The project aims to support transparent, reproducible and critical research.

## Core principles

- **Claims are the central unit of knowledge.** Information is represented as identifiable claims rather than unqualified assertions.
- **Evidence is traceable.** Evidence references a specific source and a precise location within it.
- **Contradictions are preserved.** Supporting, contradicting and contextual evidence can coexist.
- **Epistemic status is explicit.** Documented claims, inferences and hypotheses are distinguished.
- **Confidence is separate from evidence.** A confidence assessment does not replace the evidence on which a claim depends.
- **Time matters.** The period in which a claim is valid is distinguished from when it was recorded.
- **Provenance matters.** Sources carry publication, retrieval and licensing metadata.
- **Verification is identifiable.** Verification records identify the method, outcome and verifier.

## Current development status

LBICAA is under active development. Its initial domain model and solution structure are in place, but the complete research platform has not yet been implemented.

### Implemented

- Domain records for claims, evidence, sources and verification records.
- Enumerations for claim nature, status, confidence, evidence assessment, source type, licence status, verification method and verification outcome.
- Basic domain validation, including required identifiers, source URIs and evidence locators.
- Unit tests covering selected domain validation and behaviour.
- .NET 11 RC3 development baseline.

### Configured or scaffolded

- ASP.NET Core API project.
- Atlas web application project.
- Analysis and crawler projects.
- Entity Framework Core and SQLite dependencies in the infrastructure project.

These projects are not yet complete application services. In particular, a configured database dependency does not mean that persistent storage and repository workflows are implemented.

See [Architecture](docs/architecture.md) for project responsibilities and current boundaries.

## Technology

- C#
- .NET 11 Preview (RC3)
- ASP.NET Core
- Entity Framework Core
- SQLite
- xUnit

The repository currently pins the .NET SDK to `11.0.100-rc.3.26522.148` in `global.json`.

**Important:** This is a preview development baseline, not a declaration of production readiness or a stable long-term support baseline.

## Getting started

### Prerequisites

Install the .NET SDK version specified in `global.json`:

`11.0.100-rc.3.26522.148`