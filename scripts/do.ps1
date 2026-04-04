# do.ps1 — Orchestrador principal de iDempiere.
#
# Flujo:
#   1. checkVars.ps1     -> valida variables de entorno
#   2. doCompose.ps1 -OnlyPostgres  -> levanta solo postgres
#   3. (si hay backup)   -> restaura el dump mas reciente ANTES de iniciar iDempiere
#   4. doCompose.ps1     -> levanta stack completo (iDempiere encuentra BD poblada)
#
# Para hacer backup despues de trabajar:
#   .\scripts\do.backup.ps1

# No usar $ErrorActionPreference = 'Stop' — Docker escribe warnings a stderr
# que PowerShell interpreta como errores terminales. Controlamos con $LASTEXITCODE.
$ErrorActionPreference = 'Continue'

Write-Host ""
Write-Host "[*] === iDempiere — Iniciando ===" -ForegroundColor Cyan
Write-Host ""

# Paso 1: Validar variables
Write-Host "[*] Paso 1/4: Verificando variables de entorno..." -ForegroundColor Cyan
& .\scripts\checkVars.ps1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[-] Variables faltantes. Abortando." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Paso 1.5: Limpiar stack actual
Write-Host "[*] Limpiando stack (docker compose down -v)..." -ForegroundColor Cyan
$composeFile = "Docker Idempier\docker-compose-app.yml"
docker compose -f $composeFile down -v
Write-Host ""

# Paso 2: Levantar solo PostgreSQL
Write-Host "[*] Paso 2/4: Levantando PostgreSQL..." -ForegroundColor Cyan
& .\scripts\doCompose.ps1 -OnlyPostgres
if ($LASTEXITCODE -ne 0) {
    Write-Host "[-] Error al levantar PostgreSQL. Abortando." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Paso 3: Restaurar backup si existe
$backupDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\backups"))
$backupFile = Join-Path $backupDir "idempiere_backup.dump"

if (Test-Path $backupFile) {
    Write-Host "[*] Paso 3/4: Backup encontrado — idempiere_backup.dump" -ForegroundColor Cyan
    Write-Host "[*] Restaurando antes de iniciar iDempiere..." -ForegroundColor Cyan
    & .\scripts\do.backup.ps1 -Restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[-] Error al restaurar backup. Abortando." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[!] Paso 3/4: Sin backup en $backupDir — iDempiere iniciara con BD limpia." -ForegroundColor Yellow
    Write-Host "[!] (Primera vez: iDempiere inicializara la BD automaticamente en ~5 min)" -ForegroundColor Yellow
}
Write-Host ""

# Paso 4: Levantar stack completo
Write-Host "[*] Paso 4/4: Levantando iDempiere..." -ForegroundColor Cyan
& .\scripts\doCompose.ps1
if ($LASTEXITCODE -ne 0) {
    Write-Host "[-] Error al levantar stack." -ForegroundColor Red
    exit 1
}
Write-Host ""

$baseUrl = [Environment]::GetEnvironmentVariable('IDE_BASE_URL', 'Machine')
if ($null -eq $baseUrl -or $baseUrl -eq '') { $baseUrl = 'http://localhost:8080' }

Write-Host "[+] === Stack listo ===" -ForegroundColor Green
Write-Host "[+] iDempiere:  $baseUrl/webui/" -ForegroundColor Green
Write-Host "[~] Backup:     .\scripts\do.backup.ps1" -ForegroundColor Yellow
Write-Host "[~] Tests:      cd tests\AcceptanceTests && dotnet test" -ForegroundColor Yellow
Write-Host ""
exit 0
