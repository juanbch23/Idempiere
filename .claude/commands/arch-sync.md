Verifica que la infraestructura esté sincronizada entre:
1. docker-compose.yml (y overlays)
2. La tabla de servicios en CLAUDE.md
3. scripts/checkVars.ps1 (env vars)

Para cada servicio verifica:
- ¿Está en docker-compose? ¿En CLAUDE.md services table?
- ¿Las env vars usadas en compose están en checkVars.ps1?
- ¿El container_name sigue el patrón `clouding-{service}`?
- ¿Los volumes siguen el patrón `clouding-{service}_data`?

