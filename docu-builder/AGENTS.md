# AGENTS.md — CertifyDocx

## Proyecto
Generador de certificados/diplomas a partir de plantillas Word (.docx) con
variables `$$variable$$` (incluidas variables de tabla). El usuario sube su
plantilla, la web detecta los campos y genera un formulario; al enviarlo, se
descarga el documento ya relleno. Arquitectura: motor de sustitución en
VB.NET (`Core`), API en C# (`Api`, ASP.NET Core + EF Core) que expone el
Core y persiste plantillas/historial en SQL Server, y cliente en React
(`Web`).

## Comandos
- Ejecutar Api: `dotnet run --project src/CertifyDocx.Api`
- Ejecutar Web: `npm run dev` (dentro de `src/CertifyDocx.Web`)
- Tests Core: `dotnet test tests/CertifyDocx.Core.Tests`
- Migraciones DB: `dotnet ef database update --project src/CertifyDocx.Api`
- Lint/formato: `dotnet format` (backend) · `npm run lint` (frontend)

## Estilo y convenciones
- Core en VB.NET sobre .NET 8, `Option Strict On` y `Option Explicit On`.
- Api en C#, EF Core para SQL Server.
- Web en React + Vite + TypeScript.
- Identificadores y comentarios en inglés; textos de interfaz en español.
- El Core no depende de Api ni de Web, ni toca disco/BD; solo Api referencia
  a Core, y Web solo habla con Api por HTTP.

## Reglas
- Lee `docs/constitution.md` y la spec activa en `specs/` antes de tocar código.
- En el Core (VB.NET): divide cada tarea en pasos pequeños y explica el
  cambio antes de escribirlo — es la parte nueva a aprender, y Carlos
  escribe o revisa cada parte. En Api/Web puede avanzar con más autonomía.
- No cambies el esquema de la base de datos ni la sintaxis de `$$variable$$`
  sin actualizar antes la spec.
- El Core sigue sin dependencias externas (solo BCL).
- No modifiques archivos dentro de `specs/` salvo petición explícita.

## Al terminar cualquier tarea
- Ejecuta `dotnet test tests/CertifyDocx.Core.Tests` y confirma que todo pasa.
- Si la tarea toca Api o Web, prueba manualmente el flujo completo (subir
  plantilla → rellenar formulario → descargar) y describe el resultado.