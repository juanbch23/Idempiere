Lee `CLAUDE.md` sección "Pruebas de Aceptación" y `Pruebas-Idempiere/` para entender el estado actual.

Identifica qué módulo de iDempiere todavía NO tiene Page Object ni ItCase en `Pruebas-Idempiere/NavegadorIdempiere/` y `Pruebas-Idempiere/PruebasAceptacionIdempiere/`.

Antes de crear cualquier test:
1. Lee `tests/AcceptanceTests/IdempiereTests/` — son los tests de referencia que ya funcionan. Sigue exactamente ese patrón.
2. Lee `.claude/rules/tests-rules.md` — reglas de ZK Framework, selectores correctos, cómo usar `EscribirCampoZk`.

Para crear el próximo test:
1. Identifica la ventana/módulo de iDempiere a testear
2. Crea el Page Object en `NavegadorIdempiere/Pagina{Modulo}Erp.cs` con selectores CSS estables
3. Registra el Page Object en `PruebasAceptacionIdempiere/BaseTest.cs`
4. Crea el ItCase en `PruebasAceptacionIdempiere/{Modulo}ErpItCase.cs`
5. Crea los unit tests en `PruebasUnitariasIdempiere/{Modulo}ErpTests.cs` con Moq
6. Ejecuta: `cd Pruebas-Idempiere && dotnet test` — deben pasar todos

NUNCA usar `id` dinámicos de ZK. SIEMPRE usar `EscribirCampoZk()` para escribir en campos ZK.
