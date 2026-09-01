# Plan 001 — Certificados MVP

Plan técnico para la spec `specs/001-certificados-mvp/spec.md`. Cumple la
constitución: Core VB.NET solo con BCL (principio 1), Api única capa que toca
SQL Server y el Core (principio 5), Web solo habla con Api por HTTP
(principio 5), tests como criterio de "hecho" (principio 4).

## 1. Estructura de módulos

### Core — librería VB.NET (.NET 8), sin dependencias externas [RF-1 validación, RF-2, RF-4]
Recibe bytes de .docx y datos de relleno; devuelve bytes o errores. No toca
disco, red ni base de datos.
- **TemplateAnalyzer**: analiza la plantilla, valida sintaxis y estructura,
  devuelve las variables y grupos de filas detectados. [RF-1, RF-2]
- **DocumentFiller**: sustituye variables simples y clona/elimina filas de
  tabla, devuelve el .docx generado. [RF-4]
- **Modelos**: `TemplateInfo`, `VariableInfo`, `RowGroupInfo`, `FillData`,
  resultados con errores en español.
- Componentes internos (privados): apertura del paquete zip, mapa de texto de
  párrafo a fragmentos `<w:t>`, escáner de marcadores `$$...$$`, resolución
  de filas de tabla.

### Api — ASP.NET Core + EF Core (C#) [RF-1, RF-5, RF-6; orquesta RF-2/3/4]
Única capa que accede a SQL Server y al Core.
- Endpoints REST para subir, listar, obtener esquema y generar documento.
- Validaciones de UX: tamaño ≤ 10 MB, nombre único y obligatorio, campos
  obligatorios, ≤ 100 filas por grupo. [RF-1, RF-3, RF-6]
- Persistencia de plantillas (nombre + bytes + esquema detectado). [RF-5]
- Entrega del documento generado con cabecera de descarga. [RF-4]
- No contiene lógica de sustitución: delega en el Core.

### Web — React + Vite + TypeScript [RF-1 UI, RF-3, RF-6 UI]
- Vistas: subir plantilla (archivo + nombre), lista de plantillas guardadas,
  formulario dinámico (campos simples + filas añadibles/eliminables), botón
  de descarga.
- Validación en cliente espejo de la del servidor (campos obligatorios,
  tope de 100 filas con mensaje explicativo). [RF-3, RF-6]
- Todos los textos en español. Solo llama a la Api por HTTP. [RF-5]

## 2. Modelo de datos JSON

### Esquema de plantilla (lo produce el Core al subir; la Api lo persiste y lo sirve a la Web) [RF-2, RF-3]
```json
{
  "variables": [
    { "name": "nombre", "kind": "simple" },
    { "name": "fecha", "kind": "simple" },
    { "name": "asignatura", "kind": "row", "rowGroupId": 0 },
    { "name": "nota", "kind": "row", "rowGroupId": 0 }
  ],
  "rowGroups": [
    {
      "rowGroupId": 0,
      "tableIndex": 0,
      "rowIndex": 2,
      "variables": ["asignatura", "nota"]
    }
  ],
  "warnings": []
}
```
- `kind: "simple"` → un campo de texto en el formulario.
- `kind: "row"` → columna de la sección de filas de su `rowGroupId`.
- Un `rowGroup` es cada fila de tabla de la plantilla que contiene
  variables; `tableIndex`/`rowIndex` sirven para mensajes de error
  localizables («tabla 1, fila 3»).

### Datos de relleno (formulario → Api) [RF-3, RF-4]
```json
{
  "simpleValues": {
    "nombre": "Ana Pérez",
    "fecha": "31/08/2026"
  },
  "rowValues": [
    {
      "rowGroupId": 0,
      "rows": [
        { "asignatura": "Matemáticas", "nota": "9,5" },
        { "asignatura": "Lengua", "nota": "8" }
      ]
    }
  ]
}
```
- Plantilla sin variables: ambos mapas vacíos; se genera un documento
  idéntico. [Caso límite spec]
- Tabla con 0 filas: `"rows": []` → se elimina la fila de plantilla. [RF-4]

### Error estándar de la Api [RF-1, RF-3, RF-6]
```json
{ "error": "Falta el valor de la variable «nombre»." }
```

## 3. Algoritmos (pseudocódigo)

Concepto clave: Word divide el texto en fragmentos (`<w:t>`) de forma
arbitraria (corrector, formato), por lo que `$$nombre$$` puede quedar
repartido en varios fragmentos. Todo se resuelve trabajando sobre el **texto
concatenado de cada párrafo** con un mapa carácter → (fragmento, posición).
[Caso límite: marcador fragmentado — RF-2]

### 3.1 Escaneo de marcadores y validación sintáctica [RF-1]
```
FUNCION EscanearMarcadores(textoDelParrafo):
    i ← 0; estado ← Fuera; apertura ← -1
    MIENTRAS i < longitud(texto):
        SI texto[i] = '$':
            SI i+1 < longitud Y texto[i+1] = '$':
                SI estado = Fuera:
                    estado ← Abierto; apertura ← i; i ← i + 2
                SINO:                       // cierre
                    nombre ← texto[apertura+2 .. i-1]
                    SI nombre vacío → ERROR "Variable con nombre vacío"
                    EMITIR Variable(nombre, apertura, i+2)
                    estado ← Fuera; i ← i + 2
            SINO:
                ERROR "Marcador $ suelto (sin pareja $$)"
        SINO: i ← i + 1
    SI estado = Abierto → ERROR "Marcador $$ sin cerrar"
```
Un párrafo no puede contener más de un marcador abierto a la vez; los
marcadores nunca atraviesan límites de párrafo (un `$$` abierto que no se
cierra en su párrafo es error, mensaje con el número de párrafo).

### 3.2 Detección de variables y grupos de filas [RF-2]
```
FUNCION Analizar(docxBytes):
    resultado ← AnalizarResultado()
    SI docxBytes no es un zip con parte principal "word/document.xml" legible:
        // cubre .doc, corruptos y protegidos con contraseña
        DEVOLVER resultado.Error("El archivo no es un documento .docx válido")

    doc ← cargar XML de word/document.xml
    variables ← mapa nombre → informacion        // sensible a mayúsculas
    grupos ← lista; errores ← lista

    PARA CADA párrafo p EN document order (incluidos los dentro de tablas):
        fragmentos ← todos los <w:t> descendientes de p, en orden
        texto ← concatenar(fragmentos)
        marcadores ← EscanearMarcadores(texto)
        SI hay errores: añadirlos a errores CON número de párrafo; CONTINUAR

        fila ← ancestro <w:tr> más cercano de p (o NULO)
        PARA CADA marcador m:
            SI fila = NULO:
                registrar m.nombre como variable simple
            SINO:
                grupo ← grupoDe(fila)          // se crea al ver la fila por primera vez
                grupo.añadirVariable(m.nombre) // sin duplicar nombres
                registrar m.nombre como variable de fila del grupo

    // Coherencia global:
    SI algún nombre se usa como simple Y como de tabla:
        errores += "«X» se usa como variable simple y de tabla; usa nombres distintos"
    SI errores no vacía → DEVOLVER resultado con errores

    DEVOLVER resultado con TemplateInfo(variableas, grupos, avisos)
    // sin variables → éxito con aviso "El documento saldrá idéntico a la plantilla"
```

### 3.3 Sustitución y clonación de filas de tabla [RF-4]
```
FUNCION Rellenar(docxBytes, datos):
    info ← Analizar(docxBytes)
    SI inválido → DEVOLVER error con los mismos mensajes
    VALIDAR contrato (defensivo, mensajes en español):
        - cada variable simple tiene valor no vacío en datos
        - cada grupo existe en datos y tiene ≤ 100 filas
        - cada fila de cada grupo tiene valor no vacío para todas sus variables
    SI incumplido → DEVOLVER error indicando la variable/grupo afectado

    doc ← cargar word/document.xml

    // 1) Variables simples: párrafos fuera de filas de tabla
    PARA CADA párrafo p SIN ancestro <w:tr>:
        SustituirEnParrafo(p, datos.simpleValues)

    // 2) Variables de tabla, por grupo (cada grupo conoce su <w:tr> plantilla)
    PARA CADA grupo g:
        filas ← datos.rowValues[g]          // puede ser vacía
        SI filas está vacía:
            ELIMINAR la <w:tr> plantilla de su tabla      // RF-4: 0 filas
        SINO:
            clones ← []
            PARA CADA filaDatos EN filas:
                clon ← copia profunda de la <w:tr> plantilla
                PARA CADA párrafo p DENTRO de clon:
                    SustituirEnParrafo(p, filaDatos)      // solo vars de este grupo
                clones.añadir(clon)
            INSERTAR clones en la posición de la plantilla (mismo orden)
            ELIMINAR la <w:tr> plantilla

    DEVOLVER recomprimir zip: document.xml modificado + resto de entradas sin cambios

FUNCION SustituirEnParrafo(p, valores):
    fragmentos ← <w:t> de p en orden; texto ← concatenación
    marcadores ← EscanearMarcadores(texto)
    PARA CADA marcador DE ÚLTIMO A PRIMERO:      // de atrás adelante: los
        valor ← valores[marcador.nombre]         // offsets previos no se mueven
        ReescribirFragmentos(marcador, valor)

FUNCION ReescribirFragmentos(marcador, valor):
    afectados ← fragmentos <w:t> tocados por el marcador, con sus offsets
    primero.contenido ← parteIzquierda + valor + (SI solo hay uno: parteDerecha)
    intermedios ← vaciar los caracteres del marcador
    último.contenido ← parteDerecha (SI es distinto del primero)
    // El texto sustituido queda con el formato del PRIMER run afectado
    // (decisión D6)
```

## 4. Contrato del Core (VB.NET)

Todo público en la librería `CertifyDocx.Core`; `Option Strict On`,
`Option Explicit On`. Ningún método lanza excepciones de negocio: los errores
se devuelven en el resultado, con mensajes en español. [Decisión D7]

```
Constantes compartidas (fuente única para Api y Web):
    MaxTemplateBytes  = 10 * 1024 * 1024     ' RF-6
    MaxRowsPerGroup   = 100                  ' RF-6

ENUM VariableKind: Simple | Row

CLASS VariableInfo                    ' RF-2
    Name As String
    Kind As VariableKind
    RowGroupId As Integer             ' solo con sentido si Kind = Row

CLASS RowGroupInfo                    ' RF-2, RF-3
    Id As Integer                     ' 0..n-1, estable dentro de la plantilla
    TableIndex As Integer             ' posición de la tabla en el documento
    RowIndex As Integer               ' posición de la fila dentro de su tabla
    Variables As IReadOnlyList(Of String)   ' nombres, sin duplicar, en orden

CLASS TemplateInfo                    ' RF-2
    Variables As IReadOnlyList(Of VariableInfo)
    RowGroups As IReadOnlyList(Of RowGroupInfo)
    Warnings As IReadOnlyList(Of String)    ' p. ej. "sin variables"

CLASS AnalyzeResult                   ' RF-1
    Success As Boolean
    Template As TemplateInfo          ' válido si Success
    Errors As IReadOnlyList(Of String)

CLASS FillData                        ' RF-3 → RF-4
    SimpleValues As IReadOnlyDictionary(Of String, String)
    RowValues As IReadOnlyList(Of RowGroupValues)

CLASS RowGroupValues
    RowGroupId As Integer
    Rows As IReadOnlyList(Of IReadOnlyDictionary(Of String, String))

CLASS FillResult                      ' RF-4
    Success As Boolean
    Document As Byte()                ' .docx generado, si Success
    Errors As IReadOnlyList(Of String)

CLASS TemplateAnalyzer
    SHARED FUNCTION Analyze(docx As Byte()) As AnalyzeResult
        ' Entrada: bytes del .docx subido.
        ' Salida: Success=True con variables/grupos/avisos, o Success=False
        ' con errores (zip inválido, $ suelto, $$ sin cerrar, nombre vacío,
        ' nombre usado como simple y tabla a la vez).

CLASS DocumentFiller
    SHARED FUNCTION Fill(docx As Byte(), data As FillData) As FillResult
        ' Entrada: plantilla original + datos completos.
        ' Salida: Success=True con el .docx generado, o Success=False
        ' (plantilla inválida, valor ausente/vacío, grupo desconocido,
        ' más de MaxRowsPerGroup filas).
        ' Garantías: la entrada nunca se modifica; el resultado preserva el
        ' resto del documento y el formato de la plantilla.
```

## 5. Contrato de la API

Formato: JSON UTF-8 salvo subida (multipart) y descarga (binario). Errores
siempre `{"error": "..."}` en español. [RNF: idioma]

| Método y ruta | Entrada | Salida éxito | Errores | RF |
|---|---|---|---|---|
| `POST /api/templates` | multipart: `file` (.docx), `name` (texto, obligatorio) | `201` `{ templateId, name, schema, warnings }` | `400` no es .docx válido / sintaxis rota / nombre vacío; `409` nombre ya existe; `413` supera 10 MB | RF-1, RF-2, RF-6 |
| `GET /api/templates` | — | `200` `[ { templateId, name, variableCount, createdAt } ]` | — | RF-5 |
| `GET /api/templates/{id}` | — | `200` `{ templateId, name, schema }` | `404` no existe | RF-2, RF-3, RF-5 |
| `POST /api/templates/{id}/document` | JSON `FillRequest` (sección 2) | `200` binario .docx; `Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document`; `Content-Disposition: attachment; filename="<nombre plantilla>.docx"` (supuesto A3) | `400` campos ausentes/vacíos, grupo desconocido, >100 filas; `404` no existe | RF-3, RF-4, RF-6 |

Comportamiento de la Api al subir: comprueba tamaño → comprueba nombre
único → llama `TemplateAnalyzer.Analyze` → si éxito, persiste
(nombre, bytes, esquema JSON) y responde 201. El documento generado **nunca
se persiste** (solo se stream-ean los bytes devueltos por el Core). [RF-5]

Validación en dos capas: la Api valida para dar buenos mensajes HTTP; el
Core revalida defensivamente su contrato (D8). La plantilla guardada no se
modifica jamás: la generación parte siempre de los bytes originales. [RNF:
no corromper la plantilla]

## 6. Decisiones técnicas

| # | Decisión | Justificación | Alternativa descartada |
|---|---|---|---|
| D1 | OOXML directo con BCL: `System.IO.Compression.ZipArchive` + `System.Xml.Linq` | Respeta el principio 1 (Core solo BCL); .docx es zip+XML y el MVP solo lee/sustituye texto | **DocumentFormat.OpenXml**: dependencia externa, viola la constitución. **Word Interop**: requiere Word instalado en el servidor y dependencia COM |
| D2 | Detección sobre texto concatenado por párrafo con mapa carácter→fragmento | Cubre marcadores fragmentados en varios `<w:t>` (caso límite de la spec); los errores se localizan por número de párrafo | **Por `<w:t>` individual**: falla cuando Word parte el marcador. **A nivel de documento completo**: una variable entre párrafos no tiene sentido y difumina los errores |
| D3 | Grupo de filas = cada `<w:tr>` con variables; homónimos en filas distintas → secciones independientes identificadas por `rowGroupId` | Determinista y sin ambigüedad; resuelve la duda abierta 1 de la spec (supuesto A1) | **Campo único compartido entre tablas**: ambiguo, no se sabría a qué tabla van las filas |
| D4 | Un nombre usado como simple y como de tabla a la vez → plantilla rechazada | Coherente con RF-2 («nombre único = un campo del formulario») | **Permitir ambos usos**: rompería la correspondencia campo↔variable |
| D5 | Persistir el esquema JSON junto a los bytes en la subida | El formulario se sirve sin re-analizar; el esquema es inmutable mientras la plantilla no se re-suba | **Re-analizar en cada GET**: trabajo repetido innecesario. **Solo bytes**: obliga a re-analizar |
| D6 | El texto sustituido adopta el formato del primer run del marcador | Simple y predecible; la variable es una unidad conceptual en la plantilla | **Repartir formato por carácter**: complejidad alta sin valor perceptible en certificados |
| D7 | Resultados (`AnalyzeResult`/`FillResult`) en vez de excepciones | Errores en español explícitos y listables; fácil de testear y de revisar por Carlos (principio 2); mapeo limpio a códigos HTTP | **Excepciones de negocio**: control de flujo por excepción y try/catch sistemático en la Api |
| D8 | Validación duplicada: Api (UX) + Core (defensiva) | El Core nunca acepta datos incompletos aunque la Api falle; la Api da mensajes HTTP precisos | **Validar solo en una capa**: o el Core queda desprotegido o la UX empeora |

## 7. Estrategia de tests

### Core — tests automatizados (principio 4) [todos los RF del Core]
Proyecto `tests/CertifyDocx.Core.Tests` en VB.NET con xUnit. Los
`.docx` de prueba se construyen en memoria con un auxiliar `DocxBuilder`
(ZipArchive + XML generado por código): sin binarios en el repo, casos
deterministas y controlables (incluido el marcador fragmentado).

Detección y validación [RF-1, RF-2]:
- Variable simple detectada; 2 apariciones → 1 única variable.
- `$$Nombre$$` ≠ `$$nombre$$` (dos variables).
- Marcador repartido en 2 y 3 runs → una sola variable.
- `$` suelto → error; `$$` sin cerrar → error; `$ $$$`/nombre vacío → error.
- .doc sin zip / zip sin `document.xml` → error «no es .docx válido».
- Sin variables → éxito con aviso.
- Variables en fila de tabla → `RowGroupInfo` con nombres y posición.
- Nombre simple + tabla a la vez → error.
- Mismo nombre en dos filas → dos grupos.

Sustitución [RF-4]:
- Sustituye simple y todas las apariciones; el resto del texto intacto.
- El run resultante conserva el `<w:rPr>` del primer run afectado.
- 0 filas → la `<w:tr>` desaparece del resultado.
- 1 y N filas → N `<w:tr>` en orden con los valores correctos.
- Plantilla sin variables → salida idéntica a la entrada.
- La salida es un zip válido con `document.xml` bien formado.

Validación defensiva del Fill [RF-3, RF-6]:
- Valor ausente o vacío → error que nombra la variable.
- Grupo desconocido → error; > 100 filas → error que cita el límite.

### Api y Web — prueba manual del flujo completo (principio 4)
Checklist mínima (spec, criterios de finalización):
1. Subir plantilla real de Word con variables simples y una tabla → se
   lista, su esquema genera el formulario correcto.
2. Rellenar (incl. 2+ filas de tabla y luego 0 filas) → descargar → abrir
   en Word: valores en su sitio, fila eliminada cuando toca, formato
   conservado, nombre de archivo identificable.
3. Errores: archivo .txt/.doc → 400; `$$roto` → 400; nombre duplicado →
   409; archivo > 10 MB → 413; enviar formulario con campos vacíos → 400
   (y bloqueo en cliente); intentar fila 101 → bloqueado con mensaje.

### Matriz de cobertura RF → plan
| RF | Cubierto por |
|---|---|
| RF-1 Subida | Api `POST /api/templates` (sección 5), Core `Analyze` (secciones 3.1–3.2, 4), D1, tests de validación |
| RF-2 Detección | Core `Analyze` (3.2, 4), esquema JSON (2), D2–D4, tests de detección |
| RF-3 Formulario | Web (1), `GET /templates/{id}` + validación de envío (5), `FillData` (4), tests defensivos |
| RF-4 Generación/descarga | Core `Fill` (3.3, 4), endpoint de descarga (5), D6, tests de sustitución |
| RF-5 Persistencia | Api (1, 5), D5, Web lista/selección (1) |
| RF-6 Límites | Constantes del Core (4), validación Api/Web (1, 5), D8, tests de límites |

## 8. Supuestos sobre dudas abiertas de la spec
A1–A3 fueron adoptados durante la implementación (2026-08-31) y quedaron
reflejados en la spec; si Carlos discrepa de alguno, se ajusta la
implementación y la spec.
- **A1** (duda 1): variables de tabla homónimas en filas/tablas distintas se
  tratan como secciones de filas independientes (D3).
- **A2** (duda 2): longitud máxima de 1000 caracteres por campo; la Api
  responde 400 y la Web muestra contador.
- **A3** (duda 3): el documento se descarga como `<nombre de plantilla>.docx`
  (caracteres inválidos de nombre de archivo saneados).
