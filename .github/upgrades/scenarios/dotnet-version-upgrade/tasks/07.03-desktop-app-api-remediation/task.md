# 07.03-desktop-app-api-remediation: Fix desktop app API and configuration blockers after retargeting

# 07.03-desktop-app-api-remediation

## Objective
Resolve app-level source/API/configuration migration blockers introduced by retargeting and package changes.

## Scope
- Desktop app source and config in `AI-Evlo-Test`

## Steps
1. Address compile-time API incompatibilities in app code for WPF/WinForms/legacy APIs.
2. Remove/replace unsupported legacy configuration patterns as needed for net10 runtime compatibility.
3. Build app project warning-free.

**Done when**: app project builds warning-free on net10.0-windows with no unresolved tier-local blockers.
