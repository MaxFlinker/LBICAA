# LBICAA

**LBICAA — Governmental Organigram with Governance Evidence.** LBICAA is an open-source system for reconstructing public-sector institutions, organisational units, responsibilities and relationships, with traceable evidence and governance history for each representation.

Its central purpose is to make an understandable governmental organigram whose entries and relationships can be examined against their sources. LBICAA represents the claims behind that structure, their supporting or contradicting evidence, temporal context and verification history. The first operational target remains the Hamburg prototype, with Germany-wide coverage as the long-term target.

The project aims to support transparent, reproducible and critical research. The organigram is an evidence-backed research representation, not an authoritative statement of government or legal advice.

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

LBICAA is under active development. Core claim, source, evidence and verification workflows, SQLite persistence and API endpoints have been implemented and tested. The complete Hamburg research workflow and end-user platform have not yet been implemented; see the [roadmap](docs/roadmap.md) for phase definitions, completed work and remaining milestones.

### Implemented

- Domain records for claims, evidence, sources and verification records.
- Enumerations for claim nature, status, confidence, evidence assessment, source type, licence status, verification method and verification outcome.
- Basic domain validation, including required identifiers, source URIs and evidence locators.
- Unit tests covering selected domain validation and behaviour.
- Core claim, source, evidence and verification workflows with SQLite persistence and HTTP API endpoints (see the roadmap for scope and test qualifications).
- .NET 11 RC3 development baseline.

### In progress or scaffolded

- ASP.NET Core API project.
- Atlas web application project.
- Analysis and crawler projects.
- Source discovery, analysis and Atlas projects whose end-user capabilities remain under development.

These projects do not yet provide the complete research services or a Hamburg-to-Atlas end-to-end workflow.

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
