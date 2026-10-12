# New Module Checklist

Steps for adding a new business module (`Identity`, `Content`, `Media`, `Catalog`, `Orders`, etc.),
following the pattern established by `TeamSamsara.Modules.PingPong`.

## Project setup

- [ ] Create `TeamSamsara.Modules.<Name>` project under `apps/api/src/`
- [ ] Reference `TeamSamsara.Shared` only - no references to other modules (enforced by
      `ArchitectureTests`)
- [ ] Add the project to `TeamSamsara.sln`
- [ ] Scaffold the four standard folders: `Endpoints/`, `Handlers/`, `Repositories/`, `Models/`
      (add a `.gitkeep` to any left empty, and delete it once the folder has a file)

## Implementation

- [ ] Implement `IModule.RegisterServices` and `IModule.MapEndpoints`
- [ ] Register the module in `Program.cs`'s module list
- [ ] Add permission strings for this module to `Shared/Authorization/Permissions.cs`, following
      the `domain.resource.action` naming convention
- [ ] Add any module-specific constants (routes, response text) to a `<Name>Constants.cs` file
      inside the module itself - not `Shared`, unless the value is genuinely used across modules
- [ ] Add any Firestore collection names to a `<Name>FirestoreCollections.cs` file inside the
      module (for example `Repositories/NotificationFirestoreCollections.cs`) - not
      `Shared/Persistence/FirestoreCollections.cs`
- [ ] Follow the project's string/magic-value convention (see `README.md`) - no unnamed semantic
      literals

## Testing

- [ ] Add a matching `TeamSamsara.Modules.<Name>.Tests` project, referencing
      `TeamSamsara.Shared.Testing`
- [ ] Make `ArchitectureTests` load the new module: add a `ProjectReference` to it in
      `TeamSamsara.ArchitectureTests.csproj` (the test loads assemblies by name and fails with
      `FileNotFoundException` without it) and add its assembly name to `_moduleAssemblyNames` in
      `ModuleIsolationTests`
- [ ] Write at least one endpoint test proving the module's real behavior, not just that it
      compiles

## Verification

- [ ] `dotnet build apps/api/TeamSamsara.sln` clean
- [ ] `dotnet test apps/api/TeamSamsara.sln` clean, including the new module's tests and
      `ArchitectureTests`
