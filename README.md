# Document Formatter

Document Formatter is a React SPA backed by an ASP.NET Core API. The .NET solution uses a Clean Architecture dependency direction:

- `DocumentFormatter.Domain` contains business concepts and rules.
- `DocumentFormatter.Application` contains use-case contracts.
- `DocumentFormatter.Infrastructure` contains ASP.NET Identity, EF Core, and SQL Server persistence.
- `DocumentFormatter.Server` hosts the HTTP API and composes the layers.
- `documentformatter.client/src/features/auth` contains the login experience and auth API client.

## Run locally

Requirements: .NET 10 SDK and Node.js/npm.

```powershell
dotnet run --project DocumentFormatter.Server --launch-profile https
```

The API launch profile starts the Vite development server through ASP.NET SPA Proxy. Identity schema migrations are applied at startup. The app uses the HTTPS launch profile so secure authentication cookies work in development. Set `ConnectionStrings:DefaultConnection` in .NET user-secrets before starting the API; do not put SQL credentials in appsettings files.

## Authentication API

- `GET /api/auth/csrf` issues an antiforgery token for state-changing requests.
- `POST /api/auth/register` creates an account. Passwords require 12 characters, upper/lowercase, a number, and a symbol.
- `POST /api/auth/login` starts an Identity cookie session.
- `GET /api/auth/session` returns the current signed-in user.
- `POST /api/auth/logout` ends the current session.
- `GET /api/admin/users/recent` returns the newest accounts and their roles; requires the `Admin` role.
- `POST /api/chats` stores a PDF, DOCX, or PPTX and starts a conversation.
- `GET /api/chats` lists the signed-in user's previous conversations.
- `GET /api/chats/{chatId}` restores a conversation and its document versions.
- `POST /api/chats/{chatId}/messages` applies an editing instruction using the active document version.
- `GET /api/chats/{chatId}/documents/{documentId}` downloads an original or edited document version.

The browser client uses HttpOnly, Secure, SameSite cookies and sends the antiforgery token in `X-CSRF-TOKEN`. SQL Server connection details are loaded through the standard .NET configuration system and should be supplied using user-secrets locally or a secret manager in deployment.

OpenAI document editing uses the Responses API with Code Interpreter. Configure the API key outside source control before sending edit instructions:

```powershell
dotnet user-secrets set "OpenAI:ApiKey" "YOUR_OPENAI_API_KEY" --project DocumentFormatter.Server
```

Uploads, conversation messages, and original/edited document versions are persisted in SQL Server. OpenAI response storage is disabled and temporary uploaded files/containers are deleted after each edit.

New registrations receive the `User` role. Roles are initialized at startup. No default administrator password is created. To provision the first administrator locally, set both secrets before starting the API:

```powershell
dotnet user-secrets set "AdminBootstrap:Email" "admin@your-company.com" --project DocumentFormatter.Server
dotnet user-secrets set "AdminBootstrap:Password" "Use-A-Unique-Strong-Password" --project DocumentFormatter.Server
```

The configured account is created (or promoted) into `Admin` at startup. Keep the bootstrap password in a secret manager and remove or rotate it after provisioning.