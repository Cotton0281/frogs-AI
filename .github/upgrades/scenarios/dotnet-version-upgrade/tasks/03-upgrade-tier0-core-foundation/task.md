# 03-upgrade-tier0-core-foundation: Upgrade core foundation library tier

Upgrade the lowest dependency layer starting with Accord.Core to add net10.0 support while retaining compatibility for dependent projects still migrating. Address package removals that are framework-included and apply required source-level API updates identified by assessment.

This tier forms the base for all higher libraries, so it must be clean and validated before continuing.

**Done when**: Tier-0 projects target the required frameworks, compile cleanly, and dependent higher tiers still build on existing TFMs.
