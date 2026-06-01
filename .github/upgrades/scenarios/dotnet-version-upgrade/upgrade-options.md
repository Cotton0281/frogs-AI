# Upgrade Options — AI-Evlo-WPF

Assessment: 8 projects with mixed .NET Framework + .NET Standard targets, old-style desktop app project, incompatible packages, and significant API incompatibilities.

## Strategy

### Upgrade Strategy
Multiple projects include .NET Framework targets, so tiered framework-to-modern migration mechanics apply.

| Value | Description |
|-------|-------------|
| **Bottom-Up** (selected) | Upgrade leaf-node libraries first, then work upward through the dependency graph tier by tier with validation at each tier. |

## Project Structure

### Project Approach
The solution contains a .NET Framework desktop app plus shared libraries; no ASP.NET/System.Web web migration path is indicated.

| Value | Description |
|-------|-------------|
| **Multi-targeting** (selected) | Add new TFM alongside existing for libraries where mixed-framework consumers may coexist during migration. |
| In-place | Replace TFM directly; requires dependent consumers to migrate together. |

### Package Management
The upgrade crosses .NET Framework to modern .NET with non-uniform project formats, where temporary dependency divergence is expected.

| Value | Description |
|-------|-------------|
| **Per-Project (defer CPM to post-migration)** (selected) | Keep package versions per project during migration and defer centralized package management until post-migration stabilization. |
| Central Package Management (CPM) | Introduce Directory.Packages.props and centralize package versions now. |

## Compatibility

### Unsupported Packages
Assessment found incompatible packages for target net10.0 in app and test projects.

| Value | Description |
|-------|-------------|
| **Resolve Inline** (selected) | Research and resolve each incompatible package within the same task with no deferred package-resolution backlog. |
| Defer Resolution | Keep builds moving using temporary stubs/conditioning and create follow-up resolution subtasks. |
| Compatibility Mode | Keep .NET Framework reference compatibility mode where applicable, with elevated runtime risk. |

### Unsupported API Handling
Assessment reported many binary/source API incompatibilities, including desktop framework API usage.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve API changes in the same task, including complex changes, leaving no stub debt. |
| Defer Complex Changes | Apply simple replacements now and defer complex API changes via temporary stubs and follow-up subtasks. |

### Windows Native APIs
Assessment shows heavy Windows desktop API usage (WPF/WinForms/System.Drawing), indicating Windows-only runtime expectations during migration.

| Value | Description |
|-------|-------------|
| **Windows Compatibility Pack** (selected) | Add Microsoft.Windows.Compatibility to enable Windows APIs on modern .NET while deferring cross-platform refactoring. |
| No Compatibility Pack | Surface Windows-specific build errors immediately and replace APIs with cross-platform alternatives now. |

## Modernization

### Configuration Migration
Legacy desktop/test projects are expected to carry app/web-style configuration that needs migration decisions during framework transition.

| Value | Description |
|-------|-------------|
| **Auto-migrate to .NET Core Configuration** (selected) | Convert standard configuration to appsettings/IConfiguration with minimal manual mapping overhead. |
| Manual Migration with Mapping Document | Produce a detailed mapping first and migrate configuration manually for tighter control. |

### Nullable Reference Types
This is a large multi-project codebase with high migration complexity, making nullable enablement too disruptive during the framework upgrade.

| Value | Description |
|-------|-------------|
| **Leave Disabled** (selected) | Keep nullable disabled during upgrade and consider enabling as a separate follow-on effort. |
| Enable Nullable Reference Types | Enable nullable during upgrade and fix new compiler warnings as part of migration. |
