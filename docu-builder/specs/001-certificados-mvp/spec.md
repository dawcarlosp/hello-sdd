# Spec 001 — Certificados MVP

## Contexto y objetivo
Organizaciones (academias, organizadores de eventos, departamentos de
formación) emiten grandes volúmenes de certificados casi idénticos que solo
difieren en nombres, fechas o notas. Editar un documento Word por persona es
lento y propenso a errores de copiado.

El objetivo del MVP es permitir que un usuario suba una plantilla Word con
variables marcadas como `$$variable$$` (simples y de tabla), el sistema
detecte esas variables y genere un formulario, y al completarlo el usuario
descargue el documento ya rellenado. El valor: emisión de certificados en
segundos y sin errores de transcripción.

## Usuarios
- Personal administrativo o de formación que emite certificados. No se
  asumen conocimientos técnicos más allá de usar un navegador y manejar
  archivos Word.
- El MVP no contempla cuentas, roles ni multi-organización.

## Historias de usuario
1. Como administrativo, quiero subir una plantilla Word con variables para
   que el sistema reconozca los campos a rellenar.
2. Como administrativo, quiero que el sistema genere un formulario a partir
   de las variables detectadas para introducir los datos sin tocar Word.
3. Como administrativo, quiero añadir filas para las variables de tabla
   (p. ej. asignaturas y notas) para reflejar listas de elementos en el
   certificado.
4. Como administrativo, quiero descargar el documento rellenado para
   entregárselo al destinatario.
5. Como administrativo, quiero guardar mis plantillas con un nombre para
   reutilizarlas sin volver a subirlas.
6. Como administrativo, quiero recibir errores claros cuando la plantilla
   sea inválida para saber cómo corregirla.

## Requisitos funcionales

### RF-1 Subida de plantillas
El sistema deberá permitir subir un documento .docx asignándole un nombre.

Criterios de aceptación:
- Cuando el usuario suba una plantilla válida, el sistema deberá validarla,
  detectar sus variables y confirmar la subida.
- Si el archivo no es un .docx válido (otro formato, corrupto o protegido
  con contraseña), entonces el sistema deberá rechazar la subida con un
  mensaje claro en español que indique el motivo, sin generar formulario.
- Si el .docx contiene un marcador `$` suelto, `$$` sin cerrar o un nombre
  de variable vacío, entonces el sistema deberá rechazar la subida con un
  mensaje claro.
- Cuando el .docx sea válido pero no contenga ninguna variable, el sistema
  deberá aceptarlo y avisar de que el documento generado será idéntico a la
  plantilla.
- Si ya existe una plantilla guardada con el mismo nombre, entonces el
  sistema deberá rechazar la subida con un mensaje claro.

### RF-2 Detección de variables
El sistema deberá detectar variables simples (`$$nombre$$`) y variables de
tabla dentro de la plantilla.

Criterios de aceptación:
- El sistema deberá detectar variables aunque el texto del marcador tenga
  formatos distintos dentro del documento (negrita, fuentes, etc.).
- Los nombres de variable distinguirán mayúsculas y minúsculas:
  `$$Nombre$$` y `$$nombre$$` serán variables distintas.
- Cada nombre de variable único corresponderá a un único campo del
  formulario, independientemente de sus apariciones en la plantilla.

### RF-3 Formulario generado
Cuando el usuario seleccione una plantilla, el sistema deberá mostrar un
formulario con un campo de texto libre por cada variable simple y una
sección de filas por cada variable de tabla.

Criterios de aceptación:
- Todos los campos serán obligatorios; si el usuario intenta enviar el
  formulario con campos vacíos, entonces el sistema deberá bloquear el
  envío e indicar qué campos faltan.
- El sistema deberá limitar a 1000 caracteres la longitud de cada valor
  introducido en el formulario; si se supera, entonces deberá rechazar el
  envío con un mensaje claro.
- El sistema deberá permitir añadir y eliminar filas en cada variable de
  tabla.

### RF-4 Generación y descarga
Cuando el usuario envíe el formulario completo, el sistema deberá generar un
documento .docx con cada variable sustituida por su valor y ofrecerlo en
descarga.

Criterios de aceptación:
- Todas las apariciones de una variable se sustituirán por el mismo valor
  introducido.
- Si una variable de tabla no tiene filas, entonces el sistema deberá
  eliminar del documento generado la fila de la plantilla que la contenía.
- El documento generado deberá conservar el formato de la plantilla
  original.
- El documento descargado deberá ser identificable por el nombre de la
  plantilla usada (se descarga como «<nombre de la plantilla>.docx»).

### RF-5 Persistencia y reutilización de plantillas
El sistema deberá guardar las plantillas subidas con su nombre para su uso
en sesiones posteriores.

Criterios de aceptación:
- El sistema deberá listar las plantillas guardadas y permitir seleccionar
  una para generar su formulario.
- Los documentos generados no se guardarán: solo existirán como descarga
  inmediata.

### RF-6 Límites
El sistema deberá limitar el tamaño de plantilla a 10 MB y las filas de cada
variable de tabla a 100.

Criterios de aceptación:
- Si la plantilla supera 10 MB, entonces el sistema deberá rechazarla con
  un mensaje claro.
- Cuando el usuario alcance 100 filas en una variable de tabla, el sistema
  deberá impedir añadir más y explicar el límite.

## Requisitos no funcionales
- Todos los textos de interfaz y mensajes de error en español.
- La generación de un documento del caso típico deberá completarse en pocos
  segundos (sin percepción de bloqueo).
- Un fallo durante la generación nunca deberá corromper ni perder la
  plantilla original guardada.
- Utilizable desde navegador moderno sin instalación ni formación previa.
- Los datos introducidos en el formulario se usarán únicamente para generar
  el documento descargado; no se almacenarán.

## Casos límite
- Variable de tabla con 0 filas: se elimina la fila de plantilla en el
  documento generado.
- Variable simple repetida varias veces: un solo campo; todas las
  apariciones reciben el mismo valor.
- Variables que solo difieren en mayúsculas/minúsculas: campos
  independientes.
- Plantilla sin variables: aceptada con aviso; genera documento idéntico.
- Marcador de variable fragmentado por formato dentro del documento:
  detectado como una única variable.
- Nombre de plantilla duplicado: subida rechazada con mensaje.
- Plantilla de más de 10 MB o tabla de más de 100 filas: rechazado o
  bloqueado con mensaje.
- Valor de formulario de más de 1000 caracteres: rechazado con mensaje.
- Variables de tabla homónimas en filas o tablas distintas: se tratan
  como secciones de filas independientes en el formulario.

## Fuera de alcance (MVP)
- Cuentas de usuario, roles, permisos y multi-organización.
- Historial o almacenamiento de documentos generados.
- Edición o borrado de plantillas ya subidas.
- Tipos de dato distintos de texto libre (fechas, números, imágenes).
- Variables opcionales.
- Otros formatos de entrada (.doc) o de salida (PDF).
- Generación por lotes (varios destinatarios de una vez).
- Previsualización del documento antes de descargar.

## Criterios de finalización
- Todos los RF implementados y sus criterios de aceptación verificados.
- El Core dispone de tests automatizados que pasan, cubriendo: detección de
  variables (incl. marcador fragmentado por formato), sustitución de
  apariciones múltiples, distinción de mayúsculas, y gestión de filas de
  tabla con 0, 1 y N filas.
- El flujo completo subir → rellenar → descargar se ha probado manualmente
  con éxito, incluidos los caminos de error (archivo inválido, sintaxis
  rota, campos vacíos, límites superados).
- No quedan dudas [NECESITA ACLARACIÓN] sin resolver.

## Dudas abiertas
Resueltas el 2026-08-31 (decisiones adoptadas en el plan §8 y verificadas
durante la implementación):
- Duda 1 resuelta: si dos filas o tablas distintas usan variables de tabla
  con el mismo nombre, se tratan como secciones de filas independientes en
  el formulario (una por grupo de filas).
- Duda 2 resuelta: la longitud máxima de cada valor del formulario es de
  1000 caracteres.
- Duda 3 resuelta: el documento se descarga como
  «<nombre de la plantilla>.docx».
