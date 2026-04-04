# setVars2.ps1 — Referencia para setear las variables IDE_* en Machine scope.
# ATENCION: Editar los valores antes de ejecutar.
# Ejecutar como Administrador.

[Environment]::SetEnvironmentVariable('IDE_DB_PASS',       'PoZz!bbsq-JQE7EJyNNX',    'Machine')
[Environment]::SetEnvironmentVariable('IDE_DB_ADMIN_PASS', '2nw3(HRRMzu@#UlMUjA+',    'Machine')
[Environment]::SetEnvironmentVariable('IDE_BASE_URL',      'http://localhost:8080',     'Machine')
[Environment]::SetEnvironmentVariable('IDE_ADMIN_LOGIN',   'System',                   'Machine')
[Environment]::SetEnvironmentVariable('IDE_ADMIN_PASS',    'System',                   'Machine')

Write-Host "[+] Variables IDE_* configuradas en Machine scope." -ForegroundColor Green
Write-Host "[!] Cerrar y reabrir la terminal para que tomen efecto." -ForegroundColor Yellow
