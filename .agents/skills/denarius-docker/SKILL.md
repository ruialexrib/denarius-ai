---
name: denarius-docker
description: Build, rebuild, run, and verify DenariusAI Docker Compose services, including the web, SQL Server, and optional MCP containers.
---

# DenariusAI Docker operations

Use this skill when the user asks to update, rebuild, diagnose, or verify the local containers.

## Service model

- `denarius-ai-db` is the SQL Server service and owns the persistent database volume.
- `denarius-ai-web` is the application served on the configured host port, normally `8080`.
- `denarius-ai-mcp` belongs to the optional `mcp` profile and does not start during an ordinary `docker compose up`.
- Rebuilding the application must not remove database or data-protection volumes unless the user explicitly requests data deletion.

## Workflow

1. Inspect `git status`, `docker compose config`, and current service state.
2. Build only the affected targets when that is sufficient; otherwise rebuild the web service with its dependencies.
3. Start or recreate the requested services without using volume-removal flags.
4. Wait for the database and web health checks and inspect recent logs for startup or migration failures.
5. Verify `http://localhost:8080/health` and, for UI work, load the affected page.

Use `docker compose --profile mcp` only when the MCP service itself is requested or needs verification. Do not imply that the web application requires the MCP container for its built-in Mistral features.

Report which images/services were rebuilt and their final health state.

