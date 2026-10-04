# Project Structure
The general structure of the project is as follows:

```md
C#/
├── README.md
├── .vscode/
│   └── settings.json
├── API_Management/
│   ├── API_Management.csproj
│   ├── Authenticator.cs
│   ├── HttpReq.cs
│   └── SecretsPrimer.cs
└── BusinessCentral_API/
    ├── BusinessCentral_API.csproj
    ├── JSONParser.cs
    ├── OAuthPrimer.cs
    ├── Program.cs **X**
    ├── RequestManager.cs
    └── Properties/
        └── launchSettings.json
```

The **main program**/**entry point** in a project folder is marked with **X**.

## Utilities

Contains small shared library for general-purpose helper code used across the solution. It keeps reusable logic separate from other projects and is intended for generic multipurpose tasks that can be leveraged throughout the solution.

| File | Description |
|------|-------------|
| `SecretsPrimer.cs` | Resolves the project's `UserSecretsId` from the `.csproj` file and determines the path to the local `secrets.json`. Also provides `PromptGuid()` to interactively read a GUID from the console, and `BuildUserSecrets()` to invoke `dotnet user-secrets set` programmatically. |

This project is not the main application entry point; instead, it provides support code that can be referenced by other projects when common logic needs to be reused without duplication.

## API_Management

| File | Description |
|------|-------------|
| `Authenticator.cs` | Reads OAuth config (tenant ID, client ID, secret, scope, authority) from a JSON file and acquires a Bearer token via the **client credentials flow**. Posts to the token authority endpoint and stores the resulting `TokenType` and `AccessToken` as internal properties. |
| `HttpReq.cs` | Wraps `HttpClient` and pre-sets the `Authorization` header using a token from `Authenticator`. Exposes `SendRequest(url, method, body?)` which dispatches GET or POST requests and returns the raw `HttpResponseMessage`. |


## BusinessCentral_API

| File | Description |
|------|-------------|
| `OAuthPrimer.cs` | Extends `SecretsPrimer`. On first run it interactively prompts for tenant ID, client ID, client secret and writes them to User Secrets via `dotnet user-secrets set`. On subsequent runs it loads the existing `secrets.json` and validates that all required OAuth keys are present, prompting for any that are missing. |
| `RequestManager.cs` | Builds and resolves the full Business Central API URI step by step: prompts for environment name, lets the user pick a company, select an API route (manual, standard v2.0, or from a live list), and select an endpoint. Exposes `EstablishConnection()` to run this setup and `GetValues(query?)` to fetch records. |
| `JSONParser.cs` | Static helper for parsing API responses. `ParseJsonContent()` deserializes a JSON string to a `JsonObject`; `GetValueJsonArray()` extracts the `value` array from an OData envelope; `PrintJsonArrayContent()` dumps each element to the console. |
| **`Program.cs`** | Main Program/Entry point. Bootstraps `OAuthPrimer` → `Authenticator` → `HttpReq` → `RequestManager` in sequence, then either runs a preconfigured demo request (top 10 items ordered by unit price) or lets the user interactively select an endpoint and query.|

### Prerequisites
- [API Filtering Guidelines](https://github.com/Microsoft/api-guidelines/blob/master/Guidelines.md#97-filtering)  
- [BC - Tips for working with APIs](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-connect-apps-tips)
- [BC - Using filter expressions in OData URIs](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/webservices/use-filter-expressions-in-odata-uris)
- [BC -Entering criteria in filters](https://learn.microsoft.com/en-us/dynamics365/business-central/dev-itpro/developer/devenv-entering-criteria-in-filters)

<div align="center">
  <hr style="border: 0; height: 1px; background: linear-gradient(to right, transparent, #6a737d, transparent); margin: 2rem 0;" />
</div>
<div align="center">
  <strong>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</strong>
</div>

# Getting Started
## Utilities
Contains reusable helper code for resolving .NET User Secrets, prompting for GUIDs, and writing secrets programmatically; it is not a standalone application.

## API_Management
A shared library — not a standalone application. It provides reusable classes for OAuth 2.0 authentication and HTTP request dispatching, and is referenced as a dependency by other C# projects in this repo. No setup required; just add it as a project reference.

## BusinessCentral_API

`BusinessCentral_API` depends on `API_Management`, which is already referenced as a project reference in the `.csproj` — no extra steps needed there.

### Oauth
OAuth credentials are stored locally using [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and are never committed to source control. The `UserSecretsId` is configured in the `.csproj` after running `dotnet user-secrets init` for the first time manually in `BusinessCentral_API`.

**On first run**, the application detects that no secrets exist and walks you through the setup interactively:

1. **Tenant ID** — your Azure AD tenant GUID
2. **Client ID** — the app registration client GUID
3. **Client Secret** — the client secret value from the app registration

Authority and scope are derived automatically:
- Authority: `https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token`
- Scope: `https://api.businesscentral.dynamics.com/.default`

All values are saved via `dotnet user-secrets set` to your local user profile. On subsequent runs the stored secrets are validated, and you will only be prompted for any that are missing. On Windows, secrets are stored at:
```
%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
```

The app registration in Azure AD must have the `Dynamics 365 Business Central` API permission with the `API.ReadWrite.All` application permission (client credentials flow — no user sign-in).

### Prerequisites and Access Scope

Before running requests, verify these prerequisites beyond OAuth setup:

1. **Business Central environment access**
    - You need access to the target environment name (for example `Sandbox`, `Production`, or a custom environment name in your tenant).
    - The environment name must match exactly what Business Central expects, because it is used directly in the API URL path.

2. **Business Central company data availability**
    - The app fetches `/v2.0/companies` first and requires at least one company to be returned.
    - If no companies are returned, verify that your tenant/environment is correct and that the app has permission to read company metadata.

3. **Microsoft Entra app permissions**
    - Application permission required: `Dynamics 365 Business Central` → `API.ReadWrite.All`.
    - Admin consent must be granted for the tenant.
    - The client credentials flow is app-only, so no delegated user sign-in permissions are used.

4. **Supported API route scope in this app**
    - Standard Microsoft APIs via `/v2.0`.
    - Custom APIs via `/api/{publisher}/{group}/{version}`.
    - Route discovery via `/v2.0/apicategoryroutes` (when selecting the listing option in custom mode).

5. **Runtime/platform prerequisites**
    - .NET SDK installed (matching the project target framework in `BusinessCentral_API.csproj`).
    - Internet access to:
      - `https://login.microsoftonline.com`
      - `https://api.businesscentral.dynamics.com`

Recommended quick validation before deeper testing:
- Start in **Demo** mode to verify token + environment + company + endpoint connectivity with a known `/v2.0/items` query.
- Then switch to **Custom** mode to validate route discovery and endpoint selection for your target API surface.

### Run Modes: Demo vs Custom

At startup, the app asks whether to run in **Demo** mode or **Custom** mode.

1. **Demo mode**
     - Uses a preconfigured API path after company selection:
         - API route: `/v2.0`
         - Endpoint: `/items`
     - Executes a built-in query:
         - `?$top=10&$orderby=unitPrice desc`
     - Best for first-time validation that OAuth, environment, company resolution, and request execution all work end-to-end.

2. **Custom mode**
     - Lets you configure the request interactively:
         - Select API route input mode (manual, standard v2.0, or discovered routes).
         - Select endpoint from returned metadata.
         - Provide optional OData query input before execution.
     - Best for exploring available APIs and testing real integration scenarios.

How to choose:
- Use **Demo** when verifying setup or troubleshooting authentication/connectivity.
- Use **Custom** when targeting specific resources, custom APIs, or specific OData filters.

Expected output behavior:
- The app prints the fully resolved URI before the final request.
- In Demo mode, the selected items payload is printed.
- In Custom mode, the app prints records from your selected endpoint and query.


