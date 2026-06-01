# .NET Version Upgrade Progress

## Overview

This workflow upgrades the full AI-Evlo-WPF solution to .NET 10 using a dependency-first sequence. The migration starts with foundational libraries, then progresses through higher-level libraries and finally desktop app/test projects. Validation gates are enforced between milestones to keep the upgrade stable.
**Progress**: 0/8 tasks complete <progress value="0" max="100"></progress> 0%
**Progress**: 0/8 tasks complete <progress value="0" max="100"></progress> 0%

## Tasks
- 🔄 01-prerequisites-toolchain: Validate SDK/toolchain and upgrade prerequisites ([Content](tasks/01-prerequisites-toolchain/task.md))
- 🔲 01-prerequisites-toolchain: Validate SDK/toolchain and upgrade prerequisites
- 🔲 02-convert-classic-projects-sdk-style: Convert non-SDK project files before TFM changes
- 🔲 03-upgrade-tier0-core-foundation: Upgrade core foundation library tier
- 🔲 04-upgrade-tier1-math-base: Upgrade foundational math libraries
- 🔲 05-upgrade-tier2-analytics-libs: Upgrade analytics and optimization libraries
- 🔲 06-upgrade-tier3-neuro-lib: Upgrade high-level neuro library
- 🔲 07-upgrade-desktop-app-and-tests: Upgrade WPF application and unit tests to net10.0-windows
- 🔲 08-final-solution-validation: Run full validation and document deferred post-migration recommendations
