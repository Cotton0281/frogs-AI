# 04-upgrade-tier1-math-base: Upgrade foundational math libraries

Upgrade Accord.Math.Core and Accord.Math with dependency-aware sequencing, including framework/package adjustments and required API compatibility fixes. This tier has shared dependencies for higher-level statistics/genetic/neuro components and therefore carries medium blast-radius risk if unstable.

Assessment flags include both source and behavioral changes in this layer, so runtime-sensitive areas must be validated with focused tests where available.

**Done when**: Tier-1 projects build warning-free with target framework changes applied and all direct dependents remain buildable.
