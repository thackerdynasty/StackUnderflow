# Repository Guidelines

## Project Structure & Module Organization

`StackUnderflow.slnx` contains one .NET 10 ASP.NET Core application in `StackUnderflow/`. MVC controllers live in `Controllers/`, domain and view models in `Models/`, database setup and seeding in `Data/`, and reusable application logic in `Services/`, `Utilities/`, and `Extensions/`. Razor views are grouped by feature under `Views/`; Identity pages and API endpoints live under `Areas/`. Entity Framework Core schema changes belong in `Migrations/`. Browser assets are in `wwwroot/css`, `wwwroot/js`, and `wwwroot/lib`.

## Build, Test, and Development Commands

Run commands from the repository root with the .NET SDK specified by `global.json`:

- `dotnet restore` — restore NuGet dependencies.
- `dotnet build StackUnderflow.slnx` — compile the solution and surface analyzer errors.
- `dotnet run --project StackUnderflow` — start the application using the configured launch profile.
- `dotnet publish StackUnderflow.slnx -c Release` — create a release artifact equivalent to the CI build.
- `dotnet ef database update --project StackUnderflow` — apply EF Core migrations to the configured database.

The app requires the `ServerConnection` connection string. Keep machine-specific values in user secrets or environment variables, not committed settings files.

## Coding Style & Naming Conventions

Follow standard C# conventions: four-space indentation, file-scoped or consistently scoped namespaces, PascalCase for types and public members, camelCase for locals and parameters, and an `Async` suffix for asynchronous methods. Nullable reference types and implicit usings are enabled. Keep controllers thin; place persistence in `Data/` and reusable behavior in services or utilities. Match view names to controller actions (for example, `ThreadController.Edit` uses `Views/Thread/Edit.cshtml`). Run `dotnet format --verify-no-changes` before submitting broad formatting changes.

## Testing Guidelines

No automated test project is currently committed. For new behavior, add a sibling test project such as `StackUnderflow.Tests` using xUnit, name files `<TypeName>Tests.cs`, and use behavior-focused methods such as `Create_RejectsUnsafeContent`. Run `dotnet test` from the root. Until coverage automation exists, manually verify affected MVC pages, API responses, authentication, validation, and database migrations.

## Commit & Pull Request Guidelines

Recent history uses short, imperative, sentence-style subjects such as `Add antiforgery validation...` and `Fix edit view...`. Keep each commit focused and describe the user-visible change. Pull requests should summarize the change, explain verification performed, link related issues, and include screenshots for Razor/CSS changes. Call out migrations, configuration changes, and new secrets explicitly; never commit credentials or local database connection strings.
