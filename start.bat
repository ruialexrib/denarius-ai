@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Denarius AI - Docker Auto Update and Run

cd /d "%~dp0"

if not defined BRANCH set "BRANCH=main"
if not defined CHECK_INTERVAL set "CHECK_INTERVAL=30"
set "WEB_SERVICE=denarius-ai-web"
set "MCP_SERVICE=denarius-ai-mcp"
set "DEPLOYED_SHA="
set "PENDING_SHA="
set "REBUILD_WEB=0"
set "REBUILD_MCP=0"

echo.
echo Denarius AI Docker development runner
echo Watching origin/%BRANCH% every %CHECK_INTERVAL% seconds.
echo Local Docker builds are used when affected files change.
echo Press Ctrl+C to stop.
echo.

where git >nul 2>&1
if errorlevel 1 (
    echo ERROR: Git was not found in PATH.
    exit /b 1
)

where docker >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker was not found in PATH.
    exit /b 1
)

docker compose version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker Compose is not available.
    exit /b 1
)

if not exist ".env" (
    echo ERROR: .env was not found. Configure Denarius AI before starting.
    exit /b 1
)

echo Preparing Denarius AI...
docker compose config >nul
if errorlevel 1 exit /b 1
docker compose up -d --build %WEB_SERVICE%
if errorlevel 1 goto startup_error
call :wait_for_web
if errorlevel 1 goto startup_error
for /f %%i in ('git rev-parse HEAD') do set "DEPLOYED_SHA=%%i"

:watch
call :check_update
if errorlevel 2 goto retry
if errorlevel 1 goto update_repository

call :ensure_running
if errorlevel 1 goto operation_error
timeout /t %CHECK_INTERVAL% /nobreak >nul
goto watch

:update_repository
echo.
echo ============================================================
echo Repository update detected. Analysing changed files...
echo ============================================================

set "OLD_SHA=!DEPLOYED_SHA!"
set "NEW_SHA=!REMOTE_SHA!"
set "PENDING_SHA=!NEW_SHA!"
set "REBUILD_WEB=0"
set "REBUILD_MCP=0"

for /f "delims=" %%F in ('git diff --name-only !OLD_SHA! !NEW_SHA!') do call :classify_change "%%F"

for /f %%i in ('git rev-parse HEAD') do set "LOCAL_SHA=%%i"
if /i not "!LOCAL_SHA!"=="!NEW_SHA!" (
    echo Updating repository...
    git pull --ff-only origin %BRANCH%
    if errorlevel 1 goto operation_error
)

if "!REBUILD_WEB!"=="0" if "!REBUILD_MCP!"=="0" (
    echo No container-impacting changes detected. No rebuild required.
    set "DEPLOYED_SHA=!PENDING_SHA!"
    set "PENDING_SHA="
    timeout /t %CHECK_INTERVAL% /nobreak >nul
    goto watch
)

if "!REBUILD_WEB!"=="1" (
    echo Building Denarius AI web image locally...
    docker compose build %WEB_SERVICE%
    if errorlevel 1 goto operation_error
    echo Recreating Denarius AI web container...
    docker compose up -d --no-deps %WEB_SERVICE%
    if errorlevel 1 goto operation_error
    call :wait_for_web
    if errorlevel 1 goto operation_error
)

if "!REBUILD_MCP!"=="1" (
    echo Building Denarius AI MCP image locally...
    docker compose --profile mcp build %MCP_SERVICE%
    if errorlevel 1 goto operation_error
    rem Do not start the optional MCP profile unless it is already running.
    docker compose --profile mcp ps --status running --services | findstr /x /c:"%MCP_SERVICE%" >nul
    if not errorlevel 1 (
        echo Recreating running MCP container...
        docker compose --profile mcp up -d --no-deps %MCP_SERVICE%
        if errorlevel 1 goto operation_error
    )
)

set "DEPLOYED_SHA=!PENDING_SHA!"
set "PENDING_SHA="
echo Update complete. Monitoring for new commits...
timeout /t %CHECK_INTERVAL% /nobreak >nul
goto watch

:classify_change
set "CHANGED=%~1"
set "CHANGED=!CHANGED:\=/!"

rem Files that only affect repository documentation or automation do not require a container rebuild.
if /i "!CHANGED!"=="AGENTS.md" exit /b 0
if /i "!CHANGED!"=="CONTRIBUTING.md" exit /b 0
if /i "!CHANGED!"=="README.md" exit /b 0
if /i "!CHANGED:~0,5!"=="docs/" exit /b 0
if /i "!CHANGED:~0,8!"==".github/" exit /b 0
if /i "!CHANGED:~0,8!"==".agents/" exit /b 0

rem MCP-specific source changes rebuild the optional MCP image.
if /i "!CHANGED:~0,18!"=="src/DenariusAI.Mcp/" (
    set "REBUILD_MCP=1"
    exit /b 0
)

rem Docker and shared build inputs can affect both application images.
if /i "!CHANGED!"=="Dockerfile" goto shared_build_input
if /i "!CHANGED!"=="docker-compose.yml" goto shared_build_input
if /i "!CHANGED!"==".dockerignore" goto shared_build_input
if /i "!CHANGED!"=="global.json" goto shared_build_input
if /i "!CHANGED!"=="DenariusAI.slnx" goto shared_build_input
if /i "!CHANGED!"=="Directory.Build.props" goto shared_build_input
if /i "!CHANGED!"=="Directory.Packages.props" goto shared_build_input

rem Application, domain, infrastructure and web changes affect the web image.
if /i "!CHANGED:~0,4!"=="src/" (
    set "REBUILD_WEB=1"
    exit /b 0
)

rem Tests and other known non-runtime files do not affect the image.
if /i "!CHANGED:~0,6!"=="tests/" exit /b 0

rem Unknown changes use the safe default and rebuild the web application.
set "REBUILD_WEB=1"
exit /b 0

:shared_build_input
set "REBUILD_WEB=1"
set "REBUILD_MCP=1"
exit /b 0

:check_update
git fetch --quiet origin %BRANCH%
if errorlevel 1 exit /b 2
for /f %%i in ('git rev-parse origin/%BRANCH%') do set "REMOTE_SHA=%%i"
if not defined DEPLOYED_SHA for /f %%i in ('git rev-parse HEAD') do set "DEPLOYED_SHA=%%i"
if /i not "!DEPLOYED_SHA!"=="!REMOTE_SHA!" exit /b 1
exit /b 0

:ensure_running
docker compose ps --status running --services | findstr /x /c:"%WEB_SERVICE%" >nul
if errorlevel 1 (
    echo Web container is not running. Starting it...
    docker compose up -d %WEB_SERVICE%
    if errorlevel 1 exit /b 1
    call :wait_for_web
    if errorlevel 1 exit /b 1
)
exit /b 0

:wait_for_web
echo Waiting for Denarius AI health check...
set "WEB_HEALTH="
set "WEB_CONTAINER="
for /l %%N in (1,1,30) do (
    for /f "delims=" %%C in ('docker compose ps -q %WEB_SERVICE% 2^>nul') do set "WEB_CONTAINER=%%C"
    if defined WEB_CONTAINER (
        for /f "delims=" %%H in ('docker inspect --format "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}" !WEB_CONTAINER! 2^>nul') do set "WEB_HEALTH=%%H"
        if /i "!WEB_HEALTH!"=="healthy" (
            echo Denarius AI is healthy.
            exit /b 0
        )
        if /i "!WEB_HEALTH!"=="unhealthy" goto health_error
    )
    timeout /t 2 /nobreak >nul
)

:health_error
echo ERROR: Denarius AI did not become healthy.
docker compose logs --tail 50 %WEB_SERVICE%
exit /b 1

:retry
echo WARNING: Could not check GitHub. Retrying in %CHECK_INTERVAL% seconds...
timeout /t %CHECK_INTERVAL% /nobreak >nul
goto watch

:operation_error
echo.
echo ERROR: Update, Docker build, container recreation or startup failed.
docker compose logs --tail 50 %WEB_SERVICE%
echo The update remains pending and will be retried in %CHECK_INTERVAL% seconds.
timeout /t %CHECK_INTERVAL% /nobreak >nul
goto watch

:startup_error
echo.
echo ERROR: Initial Docker build, startup or health verification failed.
docker compose logs --tail 50 %WEB_SERVICE%
exit /b 1
