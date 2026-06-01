# 02-convert-classic-projects-sdk-style: Convert non-SDK project files before TFM changes

Convert the classic project in scope to SDK-style format while keeping current framework behavior intact. This isolates structural project system migration from framework/API migration and avoids conflating failure modes.

The task includes validating that conversion is behavior-preserving at the original target framework and that package reference format is normalized for downstream upgrade work.

**Done when**: All required non-SDK projects are converted, build at original TFMs succeeds, and conversion artifacts are stable for TFM upgrades.
