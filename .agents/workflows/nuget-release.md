# DataLinq.NET NuGet Release Workflow

> **CRITICAL**: Always test local package installation BEFORE publishing to NuGet.org!

## Quick Reference

```powershell
# 1. Pack
dotnet pack DataLinq.NET\DataLinq.NET.csproj -c Release -o nupkgs

# 2. Test locally (MANDATORY)
dotnet add TestProject package DataLinq.NET --version X.X.X --source c:\CodeSource\DataLinq\nupkgs

# 3. Publish (only after local test passes)
dotnet nuget push nupkgs\DataLinq.NET.X.X.X.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
```

---

## The Fat Package Pattern (Required!)

### Problem (v1.0.0)
Using `ProjectReference` without special configuration creates NuGet **dependencies** that must also be published as separate packages. Users got errors like:
```
NU1101: Package DataLinq.Data.Write not found
```

### Solution (v1.0.1)
Configure `DataLinq.NET.csproj` to **embed all DLLs** into a single package:

```xml
<ItemGroup>
  <!-- PrivateAssets="all" prevents dependency declarations -->
  <ProjectReference Include="..\DataLinq.Data.Read\DataLinq.Data.Read.csproj" PrivateAssets="all" />
</ItemGroup>

<!-- Custom target to copy referenced DLLs into package -->
<PropertyGroup>
  <TargetsForTfmSpecificBuildOutput>$(TargetsForTfmSpecificBuildOutput);CopyProjectReferencesToPackage</TargetsForTfmSpecificBuildOutput>
</PropertyGroup>

<Target Name="CopyProjectReferencesToPackage" DependsOnTargets="ResolveReferences">
  <ItemGroup>
    <BuildOutputInPackage Include="@(ReferenceCopyLocalPaths->WithMetadataValue('ReferenceSourceTarget', 'ProjectReference'))" />
  </ItemGroup>
</Target>
```

---

## Release Checklist

### Pre-Release
- [ ] **BUMP VERSION**: Update `<Version>X.X.X</Version>` in `src/Directory.Build.props`
- [ ] **UPDATE README**: Update `dotnet add package DataLinq.NET --version X.X.X` in `src/README.md` (README is bundled inside the .nupkg — must match before packing!)
- [ ] **UPDATE RELEASE NOTES**: Update `<PackageReleaseNotes>` in `src/Directory.Build.props` with v highlights
- [ ] **RUN ALL TESTS**: Run full test suite across all projects before packaging. All new failures must be investigated:
  ```powershell
  dotnet test src\UnitTests\DataLinq.Core.Tests --no-restore --verbosity minimal
  dotnet test src\UnitTests\DataLinq.Data.Tests --no-restore --verbosity minimal
  dotnet test src\UnitTests\DataLinq.Data.Write.Tests --no-restore --verbosity minimal
  dotnet test src\UnitTests\DataLinq.ParallelAsyncQuery.Tests --no-restore --verbosity minimal
  ```
- [ ] **RE-RUN FLAKY TESTS (Mandatory)**: Every failing test from the suite above must be re-run **individually** to classify it:
  ```powershell
  # Example: re-run a single failing test
  dotnet test src\UnitTests\<Project> --filter "FullyQualifiedName~<TestName>" --no-restore --verbosity minimal
  ```
  - **Passes alone** → **Flaky** (environment/timing dependent, does not block release)
  - **Fails alone** → **Deterministic bug** (must be a known, documented bug — any NEW deterministic failure blocks the release)
  - Document results before proceeding
- [ ] **SYNC BUG REGISTRY**: Regenerate `src/docs/bugs/README.md` from `// BUG:` annotations:
  ```powershell
  python src\scripts\sync_bug_registry.py
  ```
- [ ] **CLEAN**: `dotnet clean DataLinq.NET.sln` to remove old artifacts
- [ ] **PACK**: `dotnet pack DataLinq.NET\DataLinq.NET.csproj -c Release -o nupkgs`
- [ ] **CHECK SIZE**: Verify `DataLinq.NET.X.X.X.nupkg` is large (~470KB+ means DLLs are embedded)
- [ ] **TEST (Critical)**:
  1. Create temp folder: `mkdir TestRelease; cd TestRelease; dotnet new console`
  2. Clear local cache: `dotnet nuget locals all --clear`
  3. Install: `dotnet add package DataLinq.NET --version X.X.X --source c:\CodeSource\DataLinq\nupkgs`
  4. Run Verification (paste code below into `Program.cs`):

```csharp
using DataLinq;
using DataLinq.Parallel;
// 1. DataLinq.Data (Read API)
var methods = typeof(Read).GetMethods().Where(m => m.Name == "CsvSync");
Console.WriteLine($"Read API: {(methods.Any() ? "PASS" : "FAIL")}");

// 2. DataLinq.Extensions (Cases)
var list = new[] { 1, 2, 3 }.Cases(x => x > 1);
Console.WriteLine($"Cases Ext: {(list != null ? "PASS" : "FAIL")}");

// 3. DataLinq.Parallel (Type Check)
var type = typeof(ParallelAsyncQuery<int>);
Console.WriteLine($"Parallel Type: {(type != null ? "PASS" : "FAIL")}");

// 4. YAML (YamlDotNet bundled since v1.1.0)
var yamlType = Type.GetType("YamlDotNet.Serialization.Deserializer, YamlDotNet");
Console.WriteLine($"YAML (YamlDotNet): {(yamlType != null ? "PASS" : "FAIL")}");
```

### Publish
- [ ] `dotnet nuget push nupkgs\DataLinq.NET.X.X.X.nupkg --api-key KEY --source https://api.nuget.org/v3/index.json`
- [ ] Update README.md with new version in install commands
- [ ] Create git tag: `git tag -a vX.X.X -m "Release vX.X.X"`
- [ ] Push tag: `git push origin main --tags`

---

## Package Structure

| Package | Contents | License |
|---------|----------|---------|
| **DataLinq.NET** | All free components (14+ DLLs embedded, includes YamlDotNet) | Apache-2.0 |
| **DataLinq.Spark** | Spark LINQ translation (future) | Commercial |
| **DataLinq.Snowflake** | Snowflake LINQ translation (future) | Commercial |

---

## NuGet API Key

- **Glob Pattern**: `DataLinq.*`
- **Create at**: https://www.nuget.org/account/apikeys
- **Store securely** (never commit to git)

---

## Troubleshooting

### "Package X not found" after install
**Cause**: Package was published as meta-package with dependencies instead of fat package.
**Fix**: Ensure `PrivateAssets="all"` on all ProjectReferences and the `CopyProjectReferencesToPackage` target exists.

### Symbol package (.snupkg) fails
**Cause**: No PDB files in output.
**Impact**: Non-critical (debugging only). Main package still works.
**Future Fix**: Add `<DebugType>embedded</DebugType>` to `Directory.Build.props` to include symbols in the main DLLs, or configure `Microsoft.SourceLink.GitHub` properly to generate standalone .snupkg.

### Access denied during pack
**Fix**: Close Visual Studio and kill dotnet processes:
```powershell
taskkill /F /IM dotnet.exe
taskkill /F /IM VBCSCompiler.exe
```

