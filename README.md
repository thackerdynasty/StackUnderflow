# StackUnderflow

StackUnderflow is a community question-and-answer web application built with ASP.NET Core MVC. Users can ask questions, share answers, comment, vote, and save threads for later.

**StackUnderflow is an independent project and is not affiliated with, endorsed by, or sponsored by Stack Overflow or Stack Exchange.**

## Public Link
https://stackunderflow-g3cad4hucqdudccv.centralus-01.azurewebsites.net/

## Features

- Questions with tags, text search, and filtering by any or all selected tags.
- Answers, comments, voting, and accepted answers.
- Saved threads, user profiles, reputation, and a leaderboard.
- Account registration and sign-in with ASP.NET Core Identity.
- Thread reporting, moderator review, and thread locking, including automatic locking after a thread has been solved for 30 days.
- Azure AI Content Safety integration for content moderation and Azure Blob Storage for profile images.
- Light, dark, and system theme preferences.

## Technology

- .NET 10, ASP.NET Core MVC, and Razor views
- Entity Framework Core with SQL Server
- ASP.NET Core Identity
- Azure Blob Storage, Azure Key Vault, and Azure AI Content Safety
- xUnit tests with SQLite and JavaScript tests using Node.js

## Getting started

### Prerequisites

- A .NET SDK capable of targeting .NET 10; SDK selection is configured in [global.json](global.json).
- SQL Server, Azure SQL, or SQL Server LocalDB for Windows development.
- Access to your own Azure Content Safety and Key Vault resources to use content submission flows that run moderation.
- Node.js if you want to run the JavaScript theme tests.

Run the following commands from the repository root.

### Configure the database

Restore dependencies:

```powershell
dotnet restore
```

Override the checked-in database connection with your own configuration. For a Windows LocalDB installation:

```powershell
dotnet user-secrets set "ConnectionStrings:ServerConnection" "Server=(localdb)\MSSQLLocalDB;Database=StackUnderflow;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project StackUnderflow
```

For another SQL Server instance, substitute its connection string. You can also use the environment variable `ConnectionStrings__ServerConnection`. Keep credentials and machine-specific settings in user secrets or environment variables.

### Configure Azure integrations

The checked-in settings reference Azure resources belonging to the project. Replace these values with resources you can access.

| Setting | Purpose |
| --- | --- |
| `KeyVault:VaultUri` | Key Vault containing the Content Safety API key. |
| `ContentSafety:Endpoint` | Your Azure AI Content Safety endpoint. |
| `ContentSafety:KeySecretName` | Name of the Key Vault secret holding the API key. |
| `AzureStorage:ServiceUri` | Blob Storage service endpoint, authenticated using `DefaultAzureCredential`. |
| `AzureStorage:ConnectionString` | Alternative Blob Storage authentication; takes precedence over `ServiceUri`. |
| `AzureStorage:ContainerName` | Profile image container; defaults to `avatars`. |

Set these using `dotnet user-secrets set "Setting:Name" "value" --project StackUnderflow`. Key Vault access uses `DefaultAzureCredential`, so supply an authorized Azure identity, such as a local Azure CLI sign-in or a deployed managed identity. That identity needs permission to read the secret. Blob Storage access through an identity needs the Storage Blob Data Contributor role.

Content Safety is initialized when moderation is first needed. Starting the app does not verify that moderation is configured correctly; content submission requires working credentials and services.

Profile image storage is optional. To disable it locally, run `dotnet user-secrets edit --project StackUnderflow` and set both `AzureStorage:ServiceUri` and `AzureStorage:ConnectionString` to empty strings. With neither configured, profile image uploads are disabled.

### Start the application

```powershell
dotnet build StackUnderflow.slnx
dotnet dev-certs https --trust
dotnet run --project StackUnderflow --launch-profile https
```

Open <https://localhost:7061>. The HTTP launch profile is also available at <http://localhost:5142>.

Development configuration enables `Database:Seed`. On startup, the seeder applies EF Core migrations and populates demo data. Use a dedicated development database. A seeded account is `alice@example.com` with password `Passw0rd!`; these are demo credentials, not production accounts.

Set `Database:Seed` to `false` to disable seeding and its automatic migration step. To apply migrations separately, install the EF Core CLI if needed and run:

```powershell
dotnet tool install --global dotnet-ef --version "10.*"
dotnet ef database update --project StackUnderflow
```

Sign-in requires a confirmed account. Seeded accounts are already confirmed; the current registration confirmation page provides an account confirmation link. Configure a real email sender and remove that development shortcut before a production deployment.

## Verification and publishing

```powershell
dotnet build StackUnderflow.slnx
dotnet test StackUnderflow.slnx
node --test StackUnderflow.Tests/theme.test.mjs
dotnet publish StackUnderflow.slnx -c Release
```

The test project covers behavior including thread pagination and reporting, tag search, home page results, and profile image validation, storage registration, and endpoints. Theme behavior has a separate Node.js test suite.

## Project structure

```text
StackUnderflow.slnx       Solution containing the application and tests
StackUnderflow/
  Areas/                 Identity pages and API endpoints
  Controllers/           MVC and API controllers
  Data/                  EF Core database context and development seeding
  Models/                Domain models, view models, and DTOs
  Services/              Voting, moderation authorization, and image storage
  Utilities/             Search, content rendering, and content safety helpers
  Extensions/            Shared extension methods
  Migrations/            EF Core schema migrations
  Views/                 Razor views grouped by feature
  wwwroot/               CSS, JavaScript, and browser libraries
StackUnderflow.Tests/    xUnit and JavaScript tests
```

See [AGENTS.md](AGENTS.md) for repository coding and contribution guidelines.
