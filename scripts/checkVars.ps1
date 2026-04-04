# checkVars.ps1 — Valida que todas las variables de entorno IDE_* estén configuradas.
# Lee SOLO desde Machine scope. Nunca de .env.

$required = @(
    'IDE_DB_PASS',
    'IDE_DB_ADMIN_PASS',
    'IDE_BASE_URL',
    'IDE_ADMIN_LOGIN',
    'IDE_ADMIN_PASS'
)

$missing = @()

foreach ($var in $required) {
    $val = [Environment]::GetEnvironmentVariable($var, 'Machine')
    if ($null -eq $val -or $val -eq '') {
        Write-Host "[-] $var" -ForegroundColor Red
        $missing += $var
    } else {
        Write-Host "[+] $var" -ForegroundColor Green
    }
}

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "[-] Faltan $($missing.Count) variable(s). Setear con:" -ForegroundColor Red
    foreach ($v in $missing) {
        Write-Host "    [Environment]::SetEnvironmentVariable('$v', 'TU_VALOR', 'Machine')" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "[+] Todas las variables OK." -ForegroundColor Green
exit 0
