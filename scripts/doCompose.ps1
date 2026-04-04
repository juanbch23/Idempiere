# doCompose.ps1 — Levanta el stack de Docker Compose de iDempiere.
# Lee credenciales desde Machine env vars y las inyecta al proceso
# para que docker compose las tome sin necesitar el archivo .env.
#
# Parámetros:
#   -OnlyPostgres  → levanta solo el servicio postgres (para restaurar backup antes de iDempiere)

param(
    [switch]$OnlyPostgres
)

$composeFile = "Docker Idempier\docker-compose-app.yml"

# ── Inyectar vars al proceso (override del .env) ──────────────────────────────
$env:DB_USER           = 'adempiere'
$env:DB_PASS           = [Environment]::GetEnvironmentVariable('IDE_DB_PASS', 'Machine')
$env:DB_ADMIN_PASS     = [Environment]::GetEnvironmentVariable('IDE_DB_ADMIN_PASS', 'Machine')
$env:POSTGRES_PASSWORD = $env:DB_ADMIN_PASS

# ── Verificar que Docker está corriendo ───────────────────────────────────────
# Usar cmd /c para evitar que warnings de stderr Docker disparen errores de PowerShell
Write-Host "[*] Verificando Docker..." -ForegroundColor Cyan
cmd /c "docker info > NUL 2>&1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "[!] Docker no está corriendo. Intentando iniciarlo..." -ForegroundColor Yellow
    Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe" -ErrorAction SilentlyContinue

    $timeout = 120; $elapsed = 0
    while ($elapsed -lt $timeout) {
        Start-Sleep -Seconds 5
        $elapsed += 5
        cmd /c "docker info > NUL 2>&1"
        if ($LASTEXITCODE -eq 0) { break }
        Write-Host "[*] Esperando Docker... ($elapsed/$timeout s)" -ForegroundColor Cyan
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[-] Docker no respondió en $timeout segundos." -ForegroundColor Red
        exit 1
    }
    Write-Host "[+] Docker listo." -ForegroundColor Green
}

# ── Levantar servicios ────────────────────────────────────────────────────────
if ($OnlyPostgres) {
    Write-Host "[*] Levantando solo PostgreSQL..." -ForegroundColor Cyan
    docker compose -f $composeFile up -d postgres
} else {
    Write-Host "[*] Levantando stack completo..." -ForegroundColor Cyan
    docker compose -f $composeFile up -d
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "[-] Error al levantar servicios." -ForegroundColor Red
    exit 1
}

# ── Esperar PostgreSQL healthy ─────────────────────────────────────────────────
Write-Host "[*] Esperando PostgreSQL..." -ForegroundColor Cyan
$timeout = 60; $elapsed = 0
while ($elapsed -lt $timeout) {
    $health = docker inspect --format "{{.State.Health.Status}}" postgres 2>$null
    if ($health -eq 'healthy') {
        Write-Host "[+] PostgreSQL listo." -ForegroundColor Green
        break
    }
    Start-Sleep -Seconds 3
    $elapsed += 3
    Write-Host "[*] postgres: $health ($elapsed/$timeout s)" -ForegroundColor Cyan
}
if ($elapsed -ge $timeout) {
    Write-Host "[-] PostgreSQL no alcanzó estado healthy en $timeout s." -ForegroundColor Red
    exit 1
}

# ── Esperar iDempiere (solo si levantamos todo) ───────────────────────────────
if (-not $OnlyPostgres) {
    $baseUrl = [Environment]::GetEnvironmentVariable('IDE_BASE_URL', 'Machine')
    if ($null -eq $baseUrl -or $baseUrl -eq '') { $baseUrl = 'http://localhost:8080' }

    Write-Host "[*] Esperando iDempiere en $baseUrl (puede tardar 3-5 min la primera vez)..." -ForegroundColor Cyan
    $timeout = 300; $elapsed = 0; $ready = $false

    while ($elapsed -lt $timeout) {
        try {
            $r = Invoke-WebRequest -Uri $baseUrl -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
            if ($r.StatusCode -eq 200) { $ready = $true; break }
        } catch {}
        Start-Sleep -Seconds 10
        $elapsed += 10
        Write-Host "[*] iDempiere iniciando... ($elapsed/$timeout s)" -ForegroundColor Cyan
    }

    if (-not $ready) {
        Write-Host "[-] iDempiere no respondió en $timeout s." -ForegroundColor Red
        exit 1
    }

    Write-Host "[+] Stack listo → $baseUrl/webui/" -ForegroundColor Green
}

exit 0
