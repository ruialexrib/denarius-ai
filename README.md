<div align="center">

# DenariusAI

### Personal and family finance, clearly managed.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![SQL Server 2022](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Mistral AI](https://img.shields.io/badge/AI-Mistral-FF7000)](https://mistral.ai/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Developed by [Rui Ribeiro](https://github.com/ruialexrib)

</div>

---

## About

DenariusAI is a personal and family finance management platform built around double-entry accounting. It brings daily transactions, budgets, bank reconciliation, savings and financial analysis into one secure and consistent workspace.

AI features assist with transaction entry, classification, financial questions and Markdown report generation. Suggestions are always reviewed by the user before financial data is saved.

## Highlights

- Double-entry transactions with accounts, groups and categories
- Monthly budgets with explicit transaction allocation
- Bank reconciliation and assisted Excel statement import
- Dashboards, period comparisons and financial analytics
- Portuguese Savings Certificates portfolio and projections
- Mistral-powered assistant and intelligent reports
- Authentication, user roles and configurable application settings
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

## License

Distributed under the [MIT License](LICENSE). Copyright © 2026 [Rui Ribeiro](https://github.com/ruialexrib).
