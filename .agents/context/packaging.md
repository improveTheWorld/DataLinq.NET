# Packaging & Release

## Version Management
Version is defined in: `Directory.Build.props` (repo root)

```xml
<Version>X.X.X</Version>
```

## NuGet Packages

| Package | Type | License | Current |
|---------|------|---------|---------|
| **DataLinq.NET** | Free (Fat package, all DLLs embedded) | Apache-2.0 | v1.0.0 |
| **DataLinq.Snowflake** | Premium (Freemium) | Commercial | v1.4.0 |
| **DataLinq.Spark** | Premium (Freemium) | Commercial | v1.3.0 |

> **Freemium model:** Both Snowflake and Spark have a free tier allowing up to **1,000 rows** per transaction.
> Production use requires a license key (see `shared/docs/Licensing.md` in Enterprise repo).

## Enterprise Release: Cross-Repo Documentation

> **The Enterprise repo is PRIVATE.** Official user-facing docs live in the **public DataLinq repo** at `docs/`.

When releasing Snowflake or Spark, you **must** also update these files in the DataLinq (public) repo:

| Public Doc (DataLinq repo) | Purpose |
|----------------------------|---------|
| `docs/LINQ-to-Snowflake.md` | Snowflake provider docs |
| `docs/LINQ-to-Snowflake-Capabilities.md` | Snowflake capabilities matrix |
| `docs/LINQ-to-Spark.md` | Spark provider docs |
| `docs/changelog/` | Release notes for Snowflake/Spark releases |

**Release checklist for Enterprise packages:**
1. Update code + tests in Enterprise repo (private)
2. Update public docs in DataLinq repo → commit
3. Add release notes to `docs/changelog/` in DataLinq repo
4. Pack + publish nupkg
5. Git tag in Enterprise repo

## Build Commands

```bash
# Clean build
dotnet clean DataLinq.Net.sln
dotnet build DataLinq.Net.sln -c Release

# Pack fat package (the only one published)
dotnet pack packaging/DataLinq.Net/DataLinq.Net.csproj -c Release /p:Version=1.0.0 -o nupkgs
```

## Release History

| Version | Date | Notes |
|---------|------|-------|
| **v1.0.0** | 2026-02-14 | Initial DataLinq.NET release (rebranded from DataFlow.NET), 929 unit tests, bug docs, perf baselines |

## Release Workflow

### Quick Checklist
1. Bump version in `Directory.Build.props`
2. Clean: `dotnet clean DataLinq.Net.sln`
3. Pack: `dotnet pack packaging/DataLinq.Net/DataLinq.Net.csproj -c Release -o nupkgs`
4. **Test locally** (MANDATORY before publish)
5. Publish: `dotnet nuget push nupkgs/DataLinq.Net.X.X.X.nupkg --api-key KEY --source https://api.nuget.org/v3/index.json`
6. Git tag: `git tag -a vX.X.X -m "Release vX.X.X"`

## Package Output Locations

| Folder | Purpose |
|--------|---------|
| `nupkgs/current/` | **Latest release packages** |
| `nupkgs/archive/` | Old versions (v1.0.0, v1.0.1, v1.1.0) |
| `C:\CodeSource\packages\` | **Unified local package source** for all packages (DataLinq.NET, Spark, Snowflake) |

> **Local package source:** All package integration tests use `C:\CodeSource\packages\` as their local NuGet source.
> Pack command: `dotnet pack -o C:\CodeSource\packages\`

## Spark Project Architecture (as of Feb 2026)

> **SparkQuery and Extensions compile separately.** One-way dependency: Extensions → SparkQuery.
> SparkQuery does NOT reference Extensions. Consumers reference both.

| Project | Role | Dependencies |
|---------|------|--------------|
| `DataLinq.Framework.SparkQuery` | Core Spark framework | DataLinq.NET, DataLinq.Licensing |
| `DataLinq.Extensions.SparkQueryExtensions` | Extension methods (ForEach, Cases, etc.) | SparkQuery (ProjectRef), Microsoft.Spark |

> Extensions DLL is bundled separately into the NuGet package at pack time.

## Scripts

| Script | Path | Purpose |
|--------|------|---------|
| **pack.ps1** | `pack.ps1` | Package creation script |
| **publish.ps1** | `publish.ps1` | NuGet publish script |

## Secrets

> **NuGet API key is stored in:** `C:\CodeSource\DataLinq.Enterprise\.secrets.md` (Enterprise repo, gitignored)
> Read this file to get the `--api-key` value for `dotnet nuget push`.

## Fat Package Configuration
The main package embeds all DLLs using `PrivateAssets="all"` and a custom MSBuild target (`CopyProjectReferencesToPackage`). This avoids dependency resolution issues where sub-packages would need to be published separately.

## Pre-Packaging README Update

> **Always update README before packaging!**

Before creating a package, verify and update:
- **Test count badge**: Run `dotnet test --list-tests` and count
- **Coverage badge**: Check latest coverage report
- Copy updated README to package via `Directory.Build.props`

### âš ï¸ CRITICAL: Convert Relative Links

> **Relative links BREAK in NuGet packages!**

Before packaging, convert all relative links to absolute GitHub URLs:

```markdown
# âŒ Breaks in NuGet (relative path)
[Coverage](docs/COVERAGE.md)

# ✅ Works everywhere (absolute URL)
[Coverage](https://github.com/improveTheWorld/DataLinq.NET/blob/main/docs/COVERAGE.md)
```

**Affected links:** docs/, LICENSE, images, any relative path.

## Changelog Location

> **All changelogs are in:** `docs/changelog/`

| File | Description |
|------|-------------|
| `CHANGELOG.md` | Master index of all releases |
| `{Package}_{Version}.md` | Individual release notes |

**Naming convention:** `DataLinq.NET_1.0.0.md`, `DataLinq.Spark_1.0.0.md`, etc.

### Changelog Discipline

> **Each package changelog contains ONLY changes relevant to that package.**

| Package | Include | Exclude |
|---------|---------|---------|
| DataLinq.NET | OSS features, fixes, API changes | Spark/Snowflake docs, internal reorg |
| DataLinq.Spark | Spark-specific features | DataLinq.NET changes |
| DataLinq.Snowflake | Snowflake-specific features | DataLinq.NET changes |

**Exception:** Test reorganization can be noted if it explains coverage methodology changes.


