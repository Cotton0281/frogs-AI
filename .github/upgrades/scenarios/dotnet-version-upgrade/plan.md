# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade the AI-Evlo-WPF solution to .NET 10 while preserving Windows desktop functionality.
**Scope**: 8 projects with mixed net481/net472 and netstandard1.4/2.0 targets, one classic non-SDK desktop project, incompatible packages, and significant API compatibility changes.

### Selected Strategy
**Bottom-Up (Dependency-First)** — Upgrade from leaf nodes to root applications, tier by tier.
**Rationale**: Multiple .NET Framework projects and a multi-tier dependency graph require dependency-ordered migration with per-tier validation.

### Dependency Graph (Tier View)
Tier 5: [AI-Evlo-WPF.UnitTests]
		 ↓
Tier 4: [AI-Evlo-WPF]
		 ↓
Tier 3: [Accord.Neuro]
		 ↓
Tier 2: [Accord.Genetic] [Accord.Statistics]
		 ↓             ↓
Tier 1: [Accord.Math] [Accord.Math.Core]
		 ↓          ↓
Tier 0: [Accord.Core]

## Tasks

### 01-prerequisites-toolchain: Validate SDK/toolchain and upgrade prerequisites

Confirm that the local environment is ready for a net10.0 migration and that solution-level prerequisites are stable before any project edits. This includes validating .NET 10 SDK availability, ensuring global SDK pinning does not block restore/build, and confirming baseline restore/build tooling is usable on the working branch.

This task reduces early failure risk and ensures that later compile/runtime failures are actual migration issues rather than environment misconfiguration.

**Done when**: .NET 10 SDK and global.json compatibility are verified, prerequisite blockers are documented/resolved, and the branch is ready for upgrade edits.

---

### 02-convert-classic-projects-sdk-style: Convert non-SDK project files before TFM changes

Convert the classic project in scope to SDK-style format while keeping current framework behavior intact. This isolates structural project system migration from framework/API migration and avoids conflating failure modes.

The task includes validating that conversion is behavior-preserving at the original target framework and that package reference format is normalized for downstream upgrade work.

**Done when**: All required non-SDK projects are converted, build at original TFMs succeeds, and conversion artifacts are stable for TFM upgrades.

---

### 03-upgrade-tier0-core-foundation: Upgrade core foundation library tier

Upgrade the lowest dependency layer starting with Accord.Core to add net10.0 support while retaining compatibility for dependent projects still migrating. Address package removals that are framework-included and apply required source-level API updates identified by assessment.

This tier forms the base for all higher libraries, so it must be clean and validated before continuing.

**Done when**: Tier-0 projects target the required frameworks, compile cleanly, and dependent higher tiers still build on existing TFMs.

---

### 04-upgrade-tier1-math-base: Upgrade foundational math libraries

Upgrade Accord.Math.Core and Accord.Math with dependency-aware sequencing, including framework/package adjustments and required API compatibility fixes. This tier has shared dependencies for higher-level statistics/genetic/neuro components and therefore carries medium blast-radius risk if unstable.

Assessment flags include both source and behavioral changes in this layer, so runtime-sensitive areas must be validated with focused tests where available.

**Done when**: Tier-1 projects build warning-free with target framework changes applied and all direct dependents remain buildable.

---

### 05-upgrade-tier2-analytics-libs: Upgrade analytics and optimization libraries

Upgrade Accord.Genetic and Accord.Statistics after lower tiers stabilize. Apply target framework updates, remove obsolete package references, and resolve remaining API issues surfaced for this tier.

This tier is a dependency bridge into higher-level neuro and application layers; completion quality here directly affects later upgrade risk.

**Done when**: Tier-2 projects compile and tests (if any) pass, with no unresolved compatibility blockers for upstream projects.

---

### 06-upgrade-tier3-neuro-lib: Upgrade high-level neuro library

Upgrade Accord.Neuro with all required framework and package updates after its dependency chain is fully migrated. Validate compatibility against updated lower tiers and ensure no unresolved migration incidents remain in this project.

This task completes shared library migration and prepares the application tier for final upgrade.

**Done when**: Accord.Neuro builds successfully against upgraded dependencies and no tier-local upgrade blockers remain.

---

### 07-upgrade-desktop-app-and-tests: Upgrade WPF application and unit tests to net10.0-windows

Upgrade AI-Evlo-WPF and AI-Evlo-WPF.UnitTests to modern Windows-compatible TFMs, resolve incompatible packages, and apply inline API migration fixes for desktop framework usage. Include configuration migration to modern configuration patterns as applicable and maintain Windows desktop runtime compatibility.

This is the highest-risk task due to volume of API incompatibilities and desktop framework surface area. The work must leave both app and tests buildable together on updated frameworks.

**Done when**: App and test projects target modern frameworks, incompatible packages are resolved, builds are warning-free, and related tests pass.

---

### 08-final-solution-validation: Run full validation and document deferred post-migration recommendations

Run full-solution restore/build/test validation and confirm that all upgrade objectives were met for every project. Capture any explicitly deferred follow-up work, including centralized package management recommendation after full migration stabilization.

This task provides the final technical acceptance gate for the upgrade branch.

**Done when**: Entire solution builds without errors/warnings, relevant tests pass, and deferred recommendations are documented.
