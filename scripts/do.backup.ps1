# do.backup.ps1 — Backup y restore de la BD de iDempiere.
#
# Uso:
#   .\scripts\do.backup.ps1           -> crea backup en backups/
#   .\scripts\do.backup.ps1 -Restore  -> restaura el dump mas reciente
#
# Usuario de BD: 'sa' (superuser que crea el Docker oficial de iDempiere).
# El rol 'adempiere' no tiene permiso de login — no usarlo para pg_dump.
#
# Estrategia para evitar problemas de piping binario en Windows:
#   Backup:  pg_dump escribe con bash -c > /tmp/ dentro del contenedor
#            docker cp saca el archivo al host
#   Restore: docker cp copia el dump al contenedor
#            pg_restore lo lee ahi mismo

param(
    [switch]$Restore
)

$backupDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\backups"))
$dbUser    = 'postgres'
$dbPass    = [Environment]::GetEnvironmentVariable('IDE_DB_ADMIN_PASS', 'Machine')

if ($null -eq $dbPass -or $dbPass -eq '') {
    Write-Host "[-] IDE_DB_ADMIN_PASS no esta configurado." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
    Write-Host "[+] Carpeta $backupDir creada." -ForegroundColor Green
}

if ($Restore) {

$latest  = "$backupDir\idempiere_backup.dump"

    if (-not (Test-Path $latest)) {
        Write-Host "[-] No existe el archivo de backup: $latest" -ForegroundColor Red
        exit 1
    }

    $sizeMB  = [Math]::Round((Get-Item $latest).Length / 1MB, 2) 
    Write-Host "[*] Restaurando: idempiere_backup.dump ($sizeMB MB)" -ForegroundColor Cyan

    # Detener iDempiere para liberar conexiones a la BD
    $ideExists = docker ps -a --filter "name=^idempiere$" --format "{{.Names}}" 2>$null
    if ($ideExists -eq 'idempiere') {
        Write-Host "[*] Deteniendo iDempiere para liberar conexiones..." -ForegroundColor Cyan
        docker stop idempiere 2>$null | Out-Null
    }

    # Copiar dump al contenedor y restaurar
    Write-Host "[*] Copiando dump al contenedor postgres..." -ForegroundColor Cyan
    docker cp $latest "postgres:/tmp/idempiere_restore.dump"

    Write-Host "[*] Ejecutando pg_restore..." -ForegroundColor Cyan
    # Preparar bd (si no existe falla)
    $idePass = [Environment]::GetEnvironmentVariable('IDE_DB_PASS', 'Machine')
    docker exec -e "PGPASSWORD=$dbPass" postgres psql -U $dbUser -d postgres -c "CREATE ROLE adempiere LOGIN SUPERUSER PASSWORD '$idePass';" 2>$null
    docker exec -e "PGPASSWORD=$dbPass" postgres psql -U $dbUser -d postgres -c "CREATE DATABASE idempiere OWNER adempiere;" 2>$null
    docker exec -e "PGPASSWORD=$dbPass" postgres `
        bash -c "pg_restore -U $dbUser -d idempiere -1 --clean --if-exists /tmp/idempiere_restore.dump"

    $restoreCode = $LASTEXITCODE

    docker exec postgres rm -f /tmp/idempiere_restore.dump 2>$null | Out-Null

    if ($restoreCode -ne 0) {
        Write-Host "[!] pg_restore termino con advertencias (normal si la BD estaba vacia)." -ForegroundColor Yellow
    } else {
        Write-Host "[+] Restore exitoso." -ForegroundColor Green
    }

    # Reiniciar iDempiere solo si el contenedor ya existia
    if ($ideExists -eq 'idempiere') {
        Write-Host "[*] Reiniciando iDempiere..." -ForegroundColor Cyan
        docker start idempiere 2>$null | Out-Null
        Write-Host "[+] iDempiere reiniciado. Esperar ~3-5 min para que cargue." -ForegroundColor Green
    } else {
        Write-Host "[=] iDempiere aun no existe como contenedor (se creara con do.ps1)." -ForegroundColor White
    }

} else {

    $filename  = "$backupDir\idempiere_backup.dump"

    Write-Host "[*] Eliminando backups anteriores en $backupDir..." -ForegroundColor Yellow
    Get-ChildItem -Path $backupDir -Filter "*.dump" | Remove-Item -Force -ErrorAction SilentlyContinue

    Write-Host "[*] Haciendo backup de iDempiere (pg_dump -Fc via bash -c)..." -ForegroundColor Cyan

    # bash -c evita el problema de path resolution de -f en Docker Desktop Windows
    docker exec -e "PGPASSWORD=$dbPass" postgres `
        bash -c "pg_dump -U $dbUser idempiere -Fc > /tmp/idempiere_backup.dump"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[-] Error en pg_dump." -ForegroundColor Red
        exit 1
    }

    docker cp "postgres:/tmp/idempiere_backup.dump" $filename

    if ($LASTEXITCODE -ne 0) {
        Write-Host "[-] Error al copiar dump del contenedor." -ForegroundColor Red
        exit 1
    }

    docker exec postgres rm -f /tmp/idempiere_backup.dump 2>$null | Out-Null

    $sizeMB = [Math]::Round((Get-Item $filename).Length / 1MB, 2)
    Write-Host "[+] Backup guardado: $filename ($sizeMB MB)" -ForegroundColor Green
}

exit 0



