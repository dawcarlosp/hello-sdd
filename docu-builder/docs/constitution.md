# Constitution — CertifyDocx

## Principios
1. Núcleo sin dependencias externas: el Core VB.NET solo usa la BCL, no toca
   disco ni base de datos — recibe bytes y datos, devuelve bytes.
2. Aprendizaje activo en el Core: cada tarea sobre el Core se divide en
   pasos pequeños explicados antes de escribirse, y Carlos escribe o revisa
   cada parte — es la pieza nueva. En Api y Web (stacks que ya domina) puede
   avanzar con más autonomía.
3. Spec antes que código: ninguna funcionalidad se implementa sin una spec
   activa en `specs/`.
4. Tests como criterio de "hecho": una tarea del Core no está terminada
   hasta que sus tests pasan; una tarea de Api/Web no está terminada hasta
   que se ha probado manualmente el flujo subir → rellenar → descargar.
5. Separación de capas: Web solo habla con Api por HTTP; Api es la única
   capa que toca SQL Server y el Core; el Core no sabe que existen ni la web
   ni la base de datos.