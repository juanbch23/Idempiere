para el script dobackup falta mejorar, pero quiero que al levatar con el do.ps1 si yo presiono dobackup, en mi proeycto s ehaga e backup desde e contenedor,y al hace rdo.ps1 levante con el backup que se gurado en la carpeta, esto flata mejora
Descripción
Editar
BACKUP

Revisar que BD usa el idempiere

docker exec -it postgres psql -U adempiere -l

Ejecutar esta linea en el power shell
docker exec postgres pg_dump -U adempiere idempiere -Fc > idempiere_pre_cambio.dump

RESTORE

docker exec -i postgres pg_restore -U adempiere -d idempiere --clean < idempiere_pre_cambio.dump