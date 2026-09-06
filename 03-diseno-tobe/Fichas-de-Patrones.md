# Actividad 3.3 — Ficha por patrón adoptado

Cuatro patrones, uno por cada punto de dolor priorizado que admite patrón (B-12). Los códigos
`B-nn` remiten a la bitácora de IA de la Actividad 2; los `E-nn`, a la tabla de cambio 3.2.

---

## Ficha 1 · Factory Method — catálogo de vacunas

| Campo | Contenido |
|---|---|
| **Patrón y punto de dolor** | Factory Method sobre **P-01**. Cada tipo concreto de vacuna estaba escrito a mano en toda la cadena: cuatro métodos fijos en `FabricaVacunas.cs`, un `if/else` en `VacunaService.cs:49-58`, otro en `VacunaController.cs:95-107` y cuatro ramas en `MapeadorVacuna.cs`. Costo medido en la Actividad 1: **5 clases y 2 vistas** por tipo nuevo. |
| **Alternativas evaluadas** | **(a) No hacer nada.** Descartada: SC-3 (historia clínica) abre una tercera familia de registros sanitarios y el costo se repetiría entero. **(b) Abstract Factory** (B-03). Descartada: solo existe una familia —el catálogo de vacunas—, así que la interfaz de familia tendría una sola implementación y no separaría nada. **(c) Builder** (B-04, el descarte más discutido). Descartada: las cuatro sobrecargas no cruzan parámetros opcionales sino **dos ejes independientes** —tipo de vacuna × unidad o lote—, y una API fluida unificaría los dos parámetros de lote, cambiando un mensaje de error que es salida observable. |
| **Qué sale y qué entra** | **Sale:** los cuatro métodos fijos de `FabricaVacunas` y las tres ramificaciones por tipo. **Entra:** `IFabricaVacuna` (creador, E-02), `FabricaVacunaBacteriana` y `FabricaVacunaViva` (creadores concretos, E-03) y `SolicitudVacuna` (E-04), el objeto que unifica la firma de `Crear` para que la interfaz no necesite una sobrecarga por tipo. |
| **Cómo se relaciona** | `RaizComposicion` construye las dos fábricas y se las inyecta a `FabricaVacunas`, que elige cuál usar en `ObtenerFabrica` (`FabricaVacunas.cs:116-117`) por el discriminador `tipoVacuna`, con fábrica por defecto en vez de excepción. `MapeadorVacuna` mantiene **su propio** registro estático (`MapeadorVacuna.cs:50`) porque la persistencia se construye fuera del contenedor: es duplicación consciente. No interactúa con los otros tres patrones. |
| **Impacto** | Creadas **4** (E-02…E-04). Modificadas **5**: `FabricaVacunas`, `VacunaService`, `VacunaController`, `MapeadorVacuna` y `RaizComposicion`. Eliminadas **0**. `FabricaVacunas.cs` encogió de 10 973 a 8 321 bytes. **Anexo B:** habilita SC-3 —un tipo sanitario nuevo pasa a costar una clase y una línea en la raíz—, y no toca SC-1 ni SC-2. |
| **Qué cuesta** | El patrón cubrió la cadena de **creación**, no la de **conteo**: `ServicioVacunacion.cs:73-96,118-122` sigue ramificando con `vac is Bacteriana` / `vac is Viva`, y `Res.cs` sigue declarando `MaxVacunasBacterianas` y `MaxVacunasVivas`. P-01 citaba los dos tramos y solo uno quedó cerrado; el otro es deuda declarada. Además, dos registros del mismo mapa en dos sitios (la raíz y el mapeador) que hay que mantener sincronizados. La fábrica por defecto convierte un discriminador desconocido en un objeto silencioso en vez de un error ruidoso. Y los textos observables pasaron a vivir dentro de cada fábrica: leerlos ya no es abrir un archivo, sino saber cuál. |
| **Origen** | Idea nuestra, contra dos propuestas de la herramienta que **rechazamos**: Abstract Factory (B-03) y Builder (B-04). |

---

## Ficha 2 · Strategy — efecto de la venta sobre el inventario (+ SC-1)

| Campo | Contenido |
|---|---|
| **Patrón y punto de dolor** | Strategy sobre **P-02**, el punto de mayor costo medido. `ServicioVenta.cs:42-46` construía la venta y borraba la res del potrero en la misma línea; `Venta.cs:13,16` exigía una `Res`; `ValidarVenta.cs:14` la rechazaba si era nula y `MapeadorVenta.cs:26` escribía sus cuatro datos. Costo de SC-1 en la Actividad 1: **12 clases y 2 vistas = 14 archivos**. |
| **Alternativas evaluadas** | **(a) No hacer nada.** Descartada: SC-1 era literalmente inviable sin tocar los 14 archivos. **(b) Jerarquía de `Venta` con una subclase por tipo de producto** (B-10, propuesta de la herramienta). **Corregida**: multiplicaría la persistencia por tipo y obligaría a abrir columnas nuevas en `Ventas.txt`, que es formato observable. Mantuvimos `Venta` como un solo tipo con el artículo dentro. **(c) Un `if` sobre el tipo dentro de `vender_res`.** Descartada: es el condicional que P-02 identifica como el problema. |
| **Qué sale y qué entra** | **Sale:** la línea `...L_reses.Remove(res)` incrustada en `vender_res`, y la exigencia de que una venta tenga una res. **Entra:** `IEfectoVenta` (estrategia, E-05), `EfectoVentaRetiroInventario` y `EfectoVentaSinEfecto` (estrategias concretas, E-06), más la mitad de modelado que ningún patrón resuelve solo: `IArticuloVendible` (E-07) y `ProductoDerivado` (E-08). |
| **Cómo se relaciona** | `RaizComposicion` registra las dos estrategias; `ServicioVenta` las recibe por constructor (`:31`) y resuelve en `AplicarEfecto` (`:100-104`) por `TipoSoportado.IsAssignableFrom`, el mismo criterio que `IFabricaRes` usa en `Potrero.anadir_res`. `vender_res` y `vender_producto` usan **el mismo registro**, no ramas separadas. Comparte con el Factory Method la propiedad de resolverse en la raíz, pero no comparte objetos con ningún otro patrón. |
| **Impacto** | Creadas **5** (E-05…E-08) más la vista `VenderProducto.cshtml`. Modificadas **9**: `ServicioVenta`, `Venta`, `Res`, `ValidarVenta`, `MapeadorVenta`, `VentaService`, `VentaController`, `Views/Venta/Index.cshtml` y `RaizComposicion`. Eliminadas **0**. `ResService` **no se tocó**. **Anexo B:** implementa **SC-1** completa. No afecta a SC-2, ya entregada en el Reto 1. |
| **Qué cuesta** | La resolución es por tipo en ejecución: si se vende un artículo sin estrategia registrada, `First` lanza donde antes el compilador habría avisado. Es el riesgo **R-03** del registro. Además `MapeadorVenta` gana una conversión de ida y vuelta (`DescomponerArticulo`, `:49-58`) para meter dos formas distintas de venta en las mismas 7 columnas, en vez de abrir columnas nuevas. |
| **Origen** | Propuesta de la herramienta **aceptada tras verificarla** (B-13): confirmamos en el código que `ServicioVenta.cs:46` era el único punto del sistema que retiraba la res. La decisión de implementar SC-1 y no SC-3 es nuestra (B-01), y el modelado de `Venta` es una corrección nuestra (B-10). |

---

## Ficha 3 · Observer — publicadores de eventos del dominio

| Campo | Contenido |
|---|---|
| **Patrón y punto de dolor** | Observer sobre **P-03**. Los publicadores se creaban con `new` dentro del dominio: **8 instanciaciones en 3 clases** (`Potrero.cs:21-24,105-108`, `GestorReses.cs:30-31,77-78,188-189`, `ServicioVacunacion.cs:29-30`), sin interfaz común y con la secuencia de disparo escrita a mano. Ninguna prueba podía sustituirlos. |
| **Alternativas evaluadas** | **(a) No hacer nada.** Descartada: cambiar la política de avisos costaba 3 clases de dominio y 5 métodos, y las 8 instanciaciones eran intestables. **(b) Los eventos nativos de C# (`event`/`delegate`).** Descartada: el orden de invocación de un multicast no está garantizado por contrato y aquí el orden **es** salida observable. **(c) Una interfaz común para los seis publicadores** (B-07, propuesta de la herramienta). **Corregida a cinco**: `PublisherVacunaVencida` devuelve `bool` y `ServicioVacunacion` lanza según ese valor, así que es guarda de flujo, no aviso. |
| **Qué sale y qué entra** | **Sale:** las 8 instanciaciones con `new` y las seis firmas distintas de `Informar_*`. **Entra:** `IPublicadorEvento` (E-09) y `ContextoAviso` (E-10), el contexto único que hace posible una firma común. |
| **Cómo se relaciona** | `RaizComposicion` registra los cinco publicadores y construye **a mano** las listas que reciben `GestorReses` (dos listas distintas: alta de res y alimentación) y `ServicioVacunacion` (una). No se inyecta un `IEnumerable<IPublicadorEvento>` resuelto por el contenedor, porque cada lista es distinta y **el orden de cada lista es el orden de disparo**. `Potrero` es una entidad y recibe su lista por parámetro en `anadir_res` (`Potrero.cs:66`), no por constructor. |
| **Impacto** | Creadas **2** (E-09, E-10). Modificadas **10**: los 5 publicadores, `Potrero`, `GestorReses`, `ServicioVacunacion`, `RepositorioPotrerosArchivo` y `RaizComposicion`. Eliminadas **0**. **Anexo B:** no implementa ninguna solicitud, pero es la que hace verificable el resto: el Caso 19 de `04-verificacion/salida-patrones.txt` agrega en vivo un aviso que ninguno de los cinco publicadores conoce, sin tocar el dominio. |
| **Qué cuesta** | `ContextoAviso` es un contexto compartido del que cada publicador lee dos o tres campos e ignora el resto: desperdicio declarado. Peor, un publicador registrado en la lista equivocada lee campos vacíos y **calla sin error** — es el riesgo **R-05**, y `PublisherPotreroMitad` es el caso concreto. Y depurar un aviso ya no es leer una llamada: es saber en qué lista de la raíz está registrado y en qué posición. |
| **Origen** | Propuesta de la herramienta **corregida** en el alcance (B-07: cinco, no seis) y **aceptada** en el mecanismo de orden (B-14: registrar en orden en la raíz e iterar la colección), después de comprobar que el orden mitad → lleno → peso mínimo → peso de venta de `Potrero.cs:105-108` es salida observable. |

---

## Ficha 4 · Composite — reglas de validación de la res

| Campo | Contenido |
|---|---|
| **Patrón y punto de dolor** | Composite sobre **P-05**. `ValidarRes.cs:19` metía nulidad, nombre, peso y edad en un solo `if` que devolvía siempre el mismo texto (`ResultadoValidacion.cs:23`), y `GuardadoValidado.cs:49-65,69-149` repetía cinco veces el mismo bucle validar → cortar → persistir con 7 dependencias. |
| **Alternativas evaluadas** | **(a) No hacer nada.** Descartada: una regla nueva obliga a editar el `if`, y SC-1 y SC-3 traen agregados nuevos que validar. **(b) Chain of Responsibility** (B-05, propuesta de la herramienta). **Corregida**: la cadena declara «el primero que pueda, resuelve»; aquí las cuatro condiciones aplican todas al mismo objeto y ninguna sustituye a otra. Elegimos el patrón que dice la verdad sobre la intención. **(c) Decorator** (B-15). Descartada: envolver hace significativo el orden de anidamiento y obliga a leer cuatro constructores anidados para saber qué se valida. **(d) Command** para `GuardadoValidado` (B-08). Descartada: los cinco métodos difieren solo en el tipo, y un método genérico los unifica sin patrón. |
| **Qué sale y qué entra** | **Sale:** `ValidadorRes` entera (E-01), la única clase eliminada en todo el reto. **Entra:** `ValidadorCompuesto<T>` (compuesto, E-11) y las cuatro hojas de `ReglasRes/` (E-12), una condición por clase. |
| **Cómo se relaciona** | La composición y su orden se fijan en `RaizComposicion.cs:91-97`, y solo ahí. `GuardadoValidado` sigue recibiendo un `IValidador<Res>` y no distingue un compuesto de una hoja: esa indistinguibilidad **es** el patrón. Interactúa con el Strategy de forma indirecta: `ValidadorVenta` valida `venta.Articulo` (E-26), el campo que el Strategy generalizó. |
| **Impacto** | Creadas **5** (E-11, E-12). Modificadas **2**: `GuardadoValidado` y `RaizComposicion` —`ValidarVenta` también cambió, pero lo cambió SC-1, no este patrón—. Eliminadas **1** (`ValidarRes.cs`). De paso, el bucle repetido de `GuardadoValidado` se unificó en el genérico `Validar<T>` (`:88`). **Anexo B:** habilita SC-1 y SC-3 —un agregado nuevo se valida componiendo reglas en vez de escribiendo otro `if`. |
| **Qué cuesta** | Más clases pequeñas: cuatro archivos donde había cuatro operandos de un `\|\|`. El orden de registro pasa a ser semántico y frágil —`ReglaResNoNula` **debe** ir primera porque las otras tres asumen que la res no es nula— y nada en el compilador lo protege. Y depurar una validación fallida exige recorrer la lista de la raíz, no leer una línea. |
| **Origen** | Propuesta de la herramienta **corregida** (B-05, de cadena a compuesto) y dos propuestas **rechazadas** (B-15 Decorator, B-08 Command). |

---

## Alcance declarado

Dos de los cuatro patrones cerraron su punto de dolor a medias, y conviene decirlo antes de que lo pregunten.

El **Factory Method** cerró la cadena de creación de vacunas pero no la de conteo: `ServicioVacunacion` sigue distinguiendo bacterianas de vivas con `is`, y `Res` sigue declarando un tope por tipo. Cerrarlo obligaría a mover ese conteo detrás de la fábrica, y el orden en que se acumulan los contadores decide el mensaje que ve el operario.

El **Composite** se aplicó **solo a `Res`**. `ValidadorPotrero`, `ValidadorVacuna`, `ValidadorVenta` y
`ValidadorChip` siguen resolviendo todo en un único método `Validar()`. Es deuda declarada, no un
olvido: son los cuatro validadores que ninguna solicitud del Anexo B obliga a extender hoy, y
componerlos habría añadido diez clases sin un punto de dolor que las justifique —exactamente lo que
la Actividad 2 se comprometió a no hacer.
