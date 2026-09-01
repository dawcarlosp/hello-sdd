# Tasks 001 — Certificados MVP

Tareas en orden de dependencia (no empezar una sin las anteriores marcadas).
Cada tarea: pequeña (20–30 min), con RF que cubre y criterio verificable.
Las tareas de Core se hacen con la dinámica del principio 2 de la
constitución (pasos explicados, Carlos escribe o revisa).

## Fase 0 — Esqueleto

- [x] **T01 — Solución y proyectos base**
  Crear la solución con `src/CertifyDocx.Core` (librería VB.NET, .NET 8,
  `Option Strict On`), `tests/CertifyDocx.Core.Tests` (VB.NET + xUnit),
  `src/CertifyDocx.Api` y `src/CertifyDocx.Web` vacíos, con el Api
  referenciando al Core.
  Cubre: prerrequisito de todos los RF.
  Hecho cuando: `dotnet build` compila y `dotnet test
  tests/CertifyDocx.Core.Tests` pasa con un test trivial.

## Fase 1 — Core (VB.NET)

- [x] **T02 — Modelos y contrato del Core**
  Tipos del plan §4: `VariableKind`, `VariableInfo`, `RowGroupInfo`,
  `TemplateInfo`, `AnalyzeResult`, `FillData`, `RowGroupValues`,
  `FillResult`, constantes `MaxTemplateBytes` y `MaxRowsPerGroup`.
  Cubre: RF-1 a RF-6 (base del contrato).
  Hecho cuando: compila con `Option Strict On` y un test construye cada tipo.

- [x] **T03 — Apertura del paquete .docx**
  Componente interno que abre los bytes como zip y lee
  `word/document.xml`; zip inválido o sin parte principal → error «no es un
  documento .docx válido».
  Cubre: RF-1.
  Hecho cuando: tests pasan con zip basura, zip sin `document.xml` y docx
  mínimo válido.

- [x] **T04 — Escáner de marcadores `$$...$$`**
  `EscanearMarcadores` sobre texto plano (plan §3.1): emite variables con
  posiciones; detecta `$` suelto, `$$` sin cerrar y nombre vacío.
  Cubre: RF-1.
  Hecho cuando: tests unitarios de cada caso (incl. `$$$` y `$$$$`) pasan.

- [x] **T05 — Mapa de texto de párrafo**
  Concatenar los `<w:t>` de un `<w:p>` en orden y mantener el mapa
  carácter → (fragmento, posición).
  Cubre: RF-2 (marcador fragmentado).
  Hecho cuando: test con párrafo repartido en 3 `<w:t>` devuelve texto y
  mapa correctos.

- [x] **T06 — Analyze: variables simples**
  Detección de variables simples: deduplicar apariciones, distinguir
  mayúsculas/minúsculas, aviso si no hay variables.
  Cubre: RF-2.
  Hecho cuando: tests de 2 apariciones → 1 variable, `$$Nombre$$` ≠
  `$$nombre$$` y plantilla sin variables → aviso pasan.

- [x] **T07 — Analyze: variables de tabla**
  Variables dentro de `<w:tr>` → `RowGroupInfo` por fila (con `TableIndex`
  y `RowIndex`); mismo nombre en dos filas → dos grupos; nombre usado como
  simple y tabla a la vez → error (D3, D4).
  Cubre: RF-2.
  Hecho cuando: tests de grupo, doble grupo y conflicto simple/tabla pasan.

- [x] **T08 — Analyze: errores de sintaxis localizados**
  Integrar el escáner (T04) en `Analyze` con mensajes en español que citan
  el número de párrafo; `AnalyzeResult` con todos los errores acumulados.
  Cubre: RF-1.
  Hecho cuando: test con plantilla con varios errores devuelve todos los
  mensajes con su párrafo.

- [x] **T09 — Sustitución dentro de un párrafo**
  `SustituirEnParrafo` + `ReescribirFragmentos` (plan §3.3), de atrás
  adelante; el texto nuevo queda en el primer run afectado (D6).
  Cubre: RF-4.
  Hecho cuando: tests con marcador en 1 y en 3 `<w:t>` sustituyen bien y el
  `<w:rPr>` del primer run se conserva.

- [x] **T10 — Fill: variables simples + validación defensiva**
  `Fill` aplica T09 a párrafos fuera de tablas y valida contrato: valor
  ausente/vacío, grupo desconocido, más de `MaxRowsPerGroup` filas (D7,
  D8).
  Cubre: RF-3, RF-4, RF-6.
  Hecho cuando: tests de sustitución completa y de cada error defensivo
  pasan.

- [x] **T11 — Fill: clonación de filas de tabla**
  Por grupo: 0 filas → eliminar la `<w:tr>` plantilla; N filas → N copias
  profundas sustituidas, en orden, en la posición original.
  Cubre: RF-4.
  Hecho cuando: tests con 0, 1 y 5 filas generan la estructura esperada.

- [x] **T12 — Fill: recomposición del zip**
  Reescribir `document.xml` y copiar el resto de entradas sin cambios;
  salida siempre un .docx válido.
  Cubre: RF-4.
  Hecho cuando: test verifica que la salida abre como zip, tiene
  `document.xml` bien formado y conserva las demás entradas; plantilla sin
  variables → salida idéntica.

## Fase 2 — Api (C#)

- [x] **T13 — Api, EF Core y entidad plantilla**
  Proyecto Api con EF Core/SQL Server, entidad plantilla (nombre, bytes,
  esquema JSON, fecha), migración inicial.
  Cubre: RF-5.
  Hecho cuando: `dotnet ef database update` aplica la migración sin
  errores.

- [x] **T14 — POST /api/templates**
  Multipart (`file` + `name`): tamaño ≤ 10 MB → nombre único →
  `TemplateAnalyzer.Analyze` → persistir y responder 201 con esquema y
  avisos; errores 400/409/413 en español (plan §5).
  Cubre: RF-1, RF-2, RF-6.
  Hecho cuando: con curl/httpie se obtienen 201, 400 (archivo .txt y
  `$$roto`), 409 (nombre duplicado) y 413 (archivo > 10 MB).

- [x] **T15 — GET /api/templates y GET /api/templates/{id}**
  Lista resumida y detalle con esquema; 404 si no existe.
  Cubre: RF-2, RF-3, RF-5.
  Hecho cuando: respuestas verificadas con curl para id existente y no
  existente.

- [x] **T16 — POST /api/templates/{id}/document**
  Validar `FillRequest` (campos obligatorios, longitud ≤ 1000 caracteres
  A2, ≤ 100 filas), llamar a `DocumentFiller.Fill`, devolver el binario
  con `Content-Disposition` según nombre de plantilla (A3); 400/404.
  Cubre: RF-3, RF-4, RF-6.
  Hecho cuando: curl genera un .docx descargable correcto y los casos de
  error devuelven 400/404 con mensaje en español.

## Fase 3 — Web (React)

- [x] **T17 — Proyecto Web y cliente HTTP**
  Vite + React + TypeScript, cliente HTTP hacia la Api (único punto de
  acceso al backend, principio 5) y navegación básica.
  Cubre: prerrequisito de RF-1/3/5 en UI.
  Hecho cuando: `npm run dev` muestra datos reales de la Api.

- [x] **T18 — Vista de subida de plantilla**
  Selector de archivo + campo nombre; muestra errores de la Api en
  español; confirma subida con resumen de variables detectadas.
  Cubre: RF-1.
  Hecho cuando: subir una plantilla válida confirma; un archivo inválido y
  un nombre duplicado muestran su error.

- [x] **T19 — Lista de plantillas y selección**
  Lista de plantillas guardadas con botón para rellenar.
  Cubre: RF-5.
  Hecho cuando: una plantilla recién subida aparece en la lista y puede
  seleccionarse.

- [x] **T20 — Formulario dinámico**
  Un campo de texto por variable simple; sección de filas por grupo
  (añadir/eliminar, tope 100 con mensaje A2/A-plan §5); todos obligatorios
  con indicación de los que faltan.
  Cubre: RF-3, RF-6.
  Hecho cuando: el formulario refleja el esquema; enviar con campos vacíos
  queda bloqueado indicando cuáles faltan.

- [x] **T21 — Envío y descarga**
  Enviar `FillRequest`, recibir el binario y guardarlo con el nombre de la
  descarga; flujo correcto también con 0 filas y con plantilla sin
  variables.
  Cubre: RF-4.
  Hecho cuando: el archivo descargado se abre en Word con los valores
  correctos (y fila eliminada en el caso 0 filas).

## Fase 4 — Cierre

- [x] **T22 — Prueba manual del flujo completo**
  Checklist del plan §7 (Api/Web): camino feliz + errores (archivo
  inválido, sintaxis rota, campos vacíos, límites).
  Cubre: RF-1 a RF-6.
  Hecho cuando: la checklist completa queda ejecutada y anotada en esta
  tarea.

- [x] **T23 — Actualizar la spec con los supuestos confirmados**
  Sustituir las dudas abiertas de la spec por las decisiones A1–A3 una vez
  confirmadas por Carlos.
  Cubre: criterio de finalización «no quedan [NECESITA ACLARACIÓN]».
  Hecho cuando: `spec.md` no contiene ningún `[NECESITA ACLARACIÓN]`.
