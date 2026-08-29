<div align="center">

# DenariusAI

### Personal and family finance, clearly managed.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![SQL Server 2022](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Mistral AI](https://img.shields.io/badge/AI-Mistral-FF7000)](https://mistral.ai/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Live Demo](https://img.shields.io/badge/Live_Demo-Open-159A70?logo=azure&logoColor=white)](https://denarius-ai.westeurope.cloudapp.azure.com)

Developed by [Rui Ribeiro](https://github.com/ruialexrib)

<h3><a href="https://denarius-ai.westeurope.cloudapp.azure.com/" target="_blank" rel="noopener noreferrer">Open the live demonstration</a></h3>

Demo access: `guest@denarius-ai.local` / `Denarius2026!`

_The demo runs on an Azure virtual machine and may be temporarily unavailable when the VM is switched off._

### Application tour

![DenariusAI application tour](docs/assets/denarius-ai-tour.gif)

</div>

---

## About

DenariusAI is a personal and family finance management platform built around double-entry accounting. It combines daily financial management with budgeting, bank reconciliation, savings, analytics and administrative organisation in one secure and consistent workspace.

AI features assist with transaction entry, classification, financial questions, document analysis and Markdown report generation. Suggestions are always reviewed by the user before relevant data is saved.

## Highlights

- Double-entry financial management with accounts, groups, categories and transactions
- Monthly budgeting and transaction allocation
- AI-assisted bank reconciliation and transaction classification
- Dashboards, period comparisons and financial analytics
- Portuguese Savings Certificates portfolio and projections
- Document, correspondence and warranty management
- Reminders and alerts for relevant dates and expirations
- AI-assisted document analysis and metadata extraction
- Mistral-powered financial assistant and intelligent reports
- Authentication, user roles, audit capabilities and configurable application settings
- Optional read-only MCP financial tools

## Technology

| Technology | Role |
| --- | --- |
| **.NET 9 / ASP.NET Core MVC** | Web application and business workflows |
| **Entity Framework Core** | Persistence and database migrations |
| **SQL Server 2022** | Financial and identity data |
| **Mistral AI** | Natural-language assistance and reports |
| **Docker Compose** | Reproducible local deployment |
| **xUnit** | Unit, integration and MCP tests |

## Quick start

Requirements: Docker Engine or Docker Desktop with Docker Compose.

```powershell
git clone https://github.com/ruialexrib/denarius-ai.git
cd denarius-ai
Copy-Item .env.example .env
```

Set secure local passwords in `.env` and optionally add `MISTRAL_API_KEY`. Then start the application:

```powershell
docker compose up --build -d
docker compose ps
```

Open [http://localhost:8080](http://localhost:8080).

```powershell
docker compose logs -f denarius-ai-web
docker compose down
```

Never commit `.env`, credentials or real financial data.

### Optional Google authentication

User provisioning remains exclusive to DenariusAI administrators. Google authentication never creates a local account: access is granted only when the Google email exactly matches an existing application user. The same user can continue to sign in with the local email and password.

Create OAuth 2.0 web credentials in Google Cloud and register this authorized redirect URI for a local installation:

```text
http://localhost:8080/signin-google
```

For a public installation, register the equivalent HTTPS address for its domain. Configure the credentials in `.env`:

```text
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
```

When either value is absent, the Google button is not displayed and local authentication continues to work normally.

## License

Distributed under the [MIT License](LICENSE). Copyright © 2026 [Rui Ribeiro](https://github.com/ruialexrib).
