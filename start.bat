@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Denarius AI - Docker Auto Update and Run

cd /d "%~dp0"

set "BRANCH=main"
set "CHECK_INTERVAL=30"
set "WEB_SERVICE=denarius-ai-web"
set "MCP_SERVICE=denarius-ai-mcp"

echo.
echo Denarius AI Docker development runner
echo Watching origin/%BRANCH% every %CHECK_INTERVAL% seconds.
echo Local Docker builds are used when affected files change.
echo Press Ctrl+C to stop.
echo.

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
if errorlevel 1 goto operation_error
call :wait_for_web

:watch
call :check_update
if errorlevel 2 goto retry
if errorlevel 1 goto update_repository

call :ensure_running
timeout /t %CHECK_INTERVAL% /nobreak >nul
goto watch

:update_repository
echo.
echo ============================================================
echo Repository update detected. Analysing changed files...
echo ============================================================

set "OLD_SHA=!LOCAL_SHA!"
set "NEW_SHA=!REMOTE_SHA!"
set "REBUILD_WEB=0"
set "REBUILD_MCP=0"

for /f "delims=" %%F in ('git diff --name-only !OLD_SHA! !NEW_SHA!') do call :classify_change "%%F"

echo Updating repository...
git pull --ff-only origin %BRANCH%
if errorlevel 1 goto operation_error

if "!REBUILD_WEB!"=="0" if "!REBUILD_MCP!"=="0" (
    echo No container-impacting changes detected. No rebuild required.
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

echo Update complete. Monitoring for new commits...
timeout /t %CHECK_INTERVAL% /nobreak >nul
goto watch

:classify_change
set "CHANGED=%~1"

rem Files that only affect repository documentation or automation do not require a container rebuild.
if /i "!CHANGED!"=="AGENTS.md" exit /b 0
if /i "!CHANGED!"=="CONTRIBUTING.md" exit /b 0
if /i "!CHANGED!"=="README.md" exit /b 0
if /i "!CHANGED:~0,5!"=="docs\" exit /b 0
if /i "!CHANGED:~0,8!"==".github\" exit /b 0
if /i "!CHANGED:~0,8!"==".agents\" exit /b 0

rem MCP-specific source changes rebuild the optional MCP image.
if /i "!CHANGED:~0,18!"=="src\DenariusAI.Mcp\" (
    set "REBUILD_MCP=1"
    exit /b 0
)

rem Docker and shared build inputs can affect both application images.
if /i "!CHANGED!"=="Dockerfile" (
    set "REBUILD_WEB=1"
    set "REBUILD_MCP=1"
    exit /b 0
)
if /i "!CHANGED!"=="docker-compose.yml" (
    set "REBUILD_WEB=1"
    set "REBUILD_MCP=1"
    exit /b 0
)
if /i "!CHANGED!"=="Directory.Build.props" (
    set "REBUILD_WEB=1"
    set "REBUILD_MCP=1"
    exit /b 0
)
if /i "!CHANGED!"=="Directory.Packages.props" (
    set "REBUILD_WEB=1"
    set "REBUILD_MCP=1"
    exit /b 0
)

rem Application, domain, infrastructure and web changes affect the web image.
if /i "!CHANGED:~0,4!"=="src\" (
    set "REBUILD_WEB=1"
    exit /b 0
)

rem Tests and other known non-runtime files do not affect the image.
if /i "!CHANGED:~0,6!"=="tests\" exit /b 0

rem Unknown changes use the safe default and rebuild the web application.
set "REBUILD_WEB=1"
exit /b 0

:check_update
git fetch --quiet origin %BRANCH%
if errorlevel 1 exit /b 2
for /f %%i in ('git rev-parse HEAD') do set "LOCAL_SHA=%%i"
for /f %%i in ('git rev-parse origin/%BRANCH%') do set "REMOTE_SHA=%%i"
if /i not "!LOCAL_SHA!"=="!REMOTE_SHA!" exit /b 1
exit /b 0

:ensure_running
docker compose ps --status running --services | findstr /x /c:"%WEB_SERVICE%" >nul
if errorlevel 1 (
    echo Web container is not running. Starting it...
    docker compose up -d %WEB_SERVICE%
    if errorlevel 1 exit /b 1
    call :wait_for_web
)
exit /b 0

:wait_for_web
echo Waiting for Denarius AI health check...
for /l %%N in (1,1,30) do (
    for /f "delims=" %%H in ('docker inspect --format "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}" denarius-ai-denarius-ai-web-1 2^>nul') do set "WEB_HEALTH=%%H"
    if /i "!WEB_HEALTH!"=="healthy" (
        echo Denarius AI is healthy: http://localhost:8080
        exit /b 0
    )
    if /i "!WEB_HEALTH!"=="unhealthy" goto health_error
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
echo The watcher remains active and will retry after the next repository change.
timeout /t %CHECK_INTERVAL% /nobreak >nul
goto watch
