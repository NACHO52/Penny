# Penny

Penny is a personal finance web app for multiple users. The goal is to let each person track income and expenses, check their balance, and review monthly reports by category.

## Project status

The first phase focuses on user accounts. The API supports registration, cookie-based sign-in and sign-out, and a lookup for the current signed-in user. The Vue frontend supports sign-in and displays the email address associated with the session.

The next steps are to add registration and sign-out to the frontend, restore the session on page reload, and test the flow with two users. Financial accounts, transactions, and reports are planned for later phases.

## Prerequisites

- .NET 8 SDK and Visual Studio 2022 with the **ASP.NET and web development** workload for the API setup below.
- A SQL Server instance that you can connect to and create or update a development database on.
- Node.js with npm for the Vue 3, TypeScript, and Vite 6 frontend. Node.js 24 LTS is recommended.

## Local development

### Start the API

Open `Penny.sln` in Visual Studio. In PowerShell, run the following commands from the repository root to store the connection string in .NET User Secrets and trust the local HTTPS development certificate. Replace the server and database names with the values for your SQL Server instance. The example uses Windows authentication.

```powershell
dotnet user-secrets set "ConnectionStrings:PennyDatabase" "Server=localhost;Database=Penny;Trusted_Connection=True;Encrypt=True;" --project Penny.Api
dotnet dev-certs https --trust
```

If your local SQL Server uses a self-signed certificate, you can add `TrustServerCertificate=True;` to the development connection string.

Set `Penny.Api` as the startup project in Visual Studio. Open **Tools → NuGet Package Manager → Package Manager Console** and apply the migrations. `Penny.Infrastructure` contains the EF Core context and migrations. `Penny.Api` provides the startup configuration.

```powershell
Update-Database -Context PennyDbContext -Project Penny.Infrastructure -StartupProject Penny.Api
```

Start the API with the **https** launch profile and open Swagger UI at `https://localhost:7242/swagger`.

### Start the frontend

Keep the API running and open a second terminal at the repository root.

```sh
cd penny-web
npm ci
npm run dev
```

If PowerShell blocks the npm script shim on Windows, use `npm.cmd` in place of `npm`.

Open the URL printed by Vite, usually `http://localhost:5173`. The development proxy sends requests under `/auth` to `https://localhost:7242`, so the browser makes those requests to the same origin as the frontend. If you change the API's HTTPS port, update the proxy target in [vite.config.ts](penny-web/vite.config.ts).

To try the flow, create a user with `POST /auth/register` in Swagger UI, then enter the same credentials on the frontend. A successful sign-in displays the email address for that session.

## Authentication API

| Method and path | Result |
| --- | --- |
| `POST /auth/register` | Creates an account from an email address and password. Returns `201 Created`. |
| `POST /auth/login` | Sets the session cookie for valid credentials. Returns `204 No Content`. |
| `GET /auth/me` | Returns the signed-in user's ID and email with `200 OK`, or `401 Unauthorized` when there is no active session. |
| `POST /auth/logout` | Requires an active session, clears the session cookie, and returns `204 No Content`. |

## Code and interface languages

Use English for identifiers, source filenames, API routes, and code comments. The login screen currently uses Spanish, with user-facing strings in a [locale file](penny-web/src/locales/es.ts) to make it easier to add more languages.

## Success notifications

Use the shared [SuccessToast](penny-web/src/components/SuccessToast.vue) for confirmations after the API reports a successful change. Child components emit `success` with a localized message from `es.ts`. The app displays the toast in the bottom-right corner for four seconds and announces it without moving focus. Show it after closing any modal or completing any navigation triggered by the operation.

Keep validation errors, failures, and progress messages in the form or section where they apply. Reflect saved changes in the relevant view so users can verify the result after the toast disappears.
