# Reto 2 — Patrones de Diseño Arquitectónico
### Del diseño correcto (SOLID) al diseño robusto (SOLID + Patrones) · Sistema Hacienda

**Equipo (máx. 4):** Abel · Miguel · Juanes · Juanjo
**Solicitud de cambio implementada:** SC‑1 — venta de productos derivados del ganado (lácteos, carne, piel).

---

## Índice

| # | Sección | Pág. |
|---|---|---|
| 1 | Contexto y punto de partida | 1 |
| 2 | Actividad 1 · Puntos de dolor | 2 |
| 3 | Actividad 2.1 · Decisión de patrones | 3 |
| 4 | Actividad 2.2 · Bitácora frente a la IA *(a completar por el equipo)* | 4 |
| 5 | Actividad 3.1 · Diseño TO‑BE (diagrama por capas) | 5 |
| 6 | Actividad 3.2 · Tabla de cambio estructural | 6 |
| 7 | Actividad 3.3 · Fichas de patrón | 7–8 |
| 8 | Actividad 4.1 · Matriz SOLID | 9 |
| 9 | Actividad 4.2 · Comportamiento preservado | 10 |
| 10 | Actividad 5 · Análisis de riesgos | 11 |
| 11 | Actividad 6 · Vista de negocio | 12 |
| 12 | Actividad 6 · Vista técnica (dónde tocar) | 13 |
| 13 | Anexos: deuda declarada · índice de archivos por patrón | 14–15 |

---

## 1. Contexto y punto de partida

El trimestre pasado entregamos un sistema **correcto**: SOLID resolvió *quién hace qué*. Lo que no
resolvió fue *quién decide qué objeto concreto se usa y cómo se ensamblan las cosas* — la siguiente
capa del problema.

Conviene decirlo con honestidad, porque enmarca este reto: **el Reto 1 ya traía patrones aplicados**,
aunque no siempre los nombráramos como tales. En la capa **As‑Is** del diagrama se ve **Factory Method**
funcionando en la creación de reses (`IFabricaRes` con `FabricaTernero/Cebon/Novillo`), y una **base
estructural de contratos** que sostiene el diseño: interfaces de repositorio que aíslan la persistencia
(DIP), un receptor de eventos abstracto (`IReceptorEventos`) y la validación segregada por tipo
(`IValidador<T>`). Con lo visto en clase este trimestre nos fue **más fácil reconocer esos patrones por
su nombre, medir su impacto y extenderlos**: llevamos Factory Method a las vacunas, y añadimos Strategy,
Observer y Composite exactamente donde teníamos puntos de dolor medidos. Es decir, no partimos de cero;
formalizamos y robustecimos algo que ya venía tomando forma.

**Límites respetados:** el comportamiento observable queda congelado; no cambiamos el estilo
arquitectónico (nada de clean/hexagonal/servicios/frameworks); SOLID no se rompe.

---
<!-- PÁGINA 2 -->

## 2. Actividad 1 · Puntos de dolor  *(Criterio 1 — 15%)*

> **Cómo se evalúa:** puntos reales, trazables a archivo y a un escenario de cambio; costo medido en
> nº de clases/archivos; priorización con criterio explícito; el punto no intervenido, argumentado.

**Criterio de prioridad (explícito):** *Alta* = bloquea una solicitud del Anexo B o cuesta ≥10 archivos;
*Media* = 5–9 archivos, o tiene puntos que el compilador no protege; *Baja* = <5.
**Cómo medimos el costo:** elegimos un cambio concreto, buscamos el tipo/cadena afectada en todo el
código y seguimos las llamadas hasta sus extremos. La cifra es el nº de clases y archivos que hay que abrir.

| ID | Dónde (archivo / clase) | Qué lo hace rígido o caro | Costo hoy | Prioridad |
|---|---|---|---|---|
| **P‑01** | `FabricaVacunas`, `VacunaService.cs:49`, `VacunaController.cs:95`, `MapeadorVacuna`, `Res.cs:168‑171` | Cada tipo de vacuna se escribe a mano en toda la cadena: un método por tipo en la fábrica, un `if` en el servicio, otro en el controlador, ramas en el mapeador y contadores en `Res` | **10 clases + 4 vistas = 14** | Alta |
| **P‑02** | `Venta.cs:13,16`, `ServicioVenta.cs:42‑46`, `ValidarVenta.cs:14`, `MapeadorVenta.cs:26` | La `Venta` exige una `Res` (campo obligatorio); vender **borra el animal** del potrero | **12 clases + 2 vistas = 14** · bloquea SC‑1 | Alta |
| **P‑03** | `Potrero.cs:21‑24`, `GestorReses.cs:30‑31`, `ServicioVacunacion.cs:29‑30` | 8 instanciaciones `new` de publicadores dentro del dominio; sin interfaz común ni colección sobre la que iterar | **3 clases de dominio / 8 `new`** no sustituibles en prueba | Media |
| **P‑04** | `Potrero.cs:15` (enum), `ResService.cs:233‑238`, 3 vistas | El discriminador de tipo de res es un `enum` dentro de la entidad `Potrero` y se persiste literal | **3 clases + 3 vistas = 6** (4 no los verifica el compilador) | Media — **no se interviene** |
| **P‑05** | `GuardadoValidado.cs`, `ValidarRes.cs:19`, `ResultadoValidacion.cs:23` | `GuardadoValidado` con 7 dependencias y 5 métodos iguales; cada validador colapsa todas sus reglas en un solo `if` que devuelve siempre el mismo texto | 2 clases nuevas + 3 servicios recompilan | Media |
| **P‑06** | `MapeadorRes/Vacuna/Venta/Potrero`, `RepositorioUsuariosArchivo` | Persistencia por posición de columna: 33 accesos `partes[n]` detrás de guardas que descartan la línea sin excepción | **8 archivos** + riesgo de pérdida silenciosa del histórico | **No se interviene** |

**Puntos no intervenidos, argumentados:**
- **P‑04:** el `enum l_tipos_potreros` se persiste **literal** en `Potreros.txt` (`Potrero_Terneros|ternero`) y `MapeadorPotrero.cs:31` lo reconstruye con `Enum.Parse`. Eliminarlo cambia el archivo de datos del cliente → **salida observable prohibida**. Queda como deuda con costo medido.
- **P‑06:** la cura real (cabecera/versión/serializador) reescribe los 6 archivos de datos del cliente (salida observable) y, aun conservando el formato, toca los 8 archivos de `Infraestructura`. **El remedio cuesta 8 archivos y arriesga el histórico; el problema cuesta 0 mientras se respete la regla "columnas nuevas solo al final".**

**Patrón común:** cinco de seis puntos responden a lo mismo — el Reto 1 resolvió *quién hace qué*, pero no *quién decide qué objeto concreto se usa*.

---
<!-- PÁGINA 3 -->

## 3. Actividad 2.1 · Decisión de patrones  *(Criterio 2 — 20%)*

> **Cómo se evalúa:** descartes que muestran comprensión real; cada patrón anclado a un P‑xx.

Evaluamos **10 patrones** (≥2 por familia) y adoptamos **4**, uno por cada punto priorizado que admite patrón.

| Patrón | Familia | P‑xx | Decisión | Por qué (síntesis) |
|---|---|---|---|---|
| **Factory Method** | Creacional | P‑01 | **Adoptado** | Un registro `IFabricaVacuna` con una implementación por tipo borra los 4 métodos fijos y los `if` de 3 capas. Ya probado en `IFabricaRes`. |
| **Strategy** | Comportamiento | P‑02 | **Adoptado** | Separa el efecto sobre el inventario: vender un animal lo retira; vender un derivado no. Hace viable SC‑1 sin duplicar el método. |
| **Observer** | Comportamiento | P‑03 | **Adoptado** | Interfaz común de aviso + contexto único; la colección se inyecta desde la raíz y borra los 8 `new`. Cubre 5 de 6 publicadores. |
| **Composite** | Estructural | P‑05 | **Adoptado** | `ValidadorCompuesto<T>` agrupa `IValidador<T>` y corta en la primera regla que falla; una regla nueva es una clase, sin tocar el `if` existente. |
| Abstract Factory | Creacional | P‑01 | Descartado | Una sola familia de productos → interfaz con una implementación = indirección sin variación. |
| Builder | Creacional | P‑01 | Descartado | No varían los parámetros sino 2 ejes cruzados (tipo × unidad/lote); además el texto de `ArgumentException` es salida observable. |
| Chain of Responsibility | Comportamiento | P‑05 | Descartado | Significa "el primero que pueda resuelve"; la validación necesita "todas aplican". Volvería significativo el orden. |
| Facade | Estructural | P‑02, P‑05 | Descartado | Los `*Service` ya son ese punto de entrada; una capa más es cambio de estilo y tiende a absorber lógica (rompe SRP). |
| Decorator | Estructural | P‑05 | Descartado | Hace significativo el orden de anidamiento y envuelve el contrato congelado `ResultadoValidacion`. |
| Proxy | Estructural | P‑05 | Descartado | Es lo que ADR‑03 desmontó (Castle DynamicProxy); reintroducirlo es volver a un framework. |

**Los dos descartes más discutidos:**
- **Builder vs Factory Method (P‑01).** Lo que varía no es el nº de parámetros sino dos ejes independientes (qué tipo y si es unidad o lote). Builder resolvería solo uno y unificaría el mensaje de error, que llega a la pantalla del operario (`nameof(lote)` vs `nameof(lote_base)`). Ganó Factory Method.
- **Chain of Responsibility vs Composite (P‑05).** Ambos producen una lista de validadores; elegimos el que **dice la verdad sobre la intención**: las 4 condiciones de `ValidarRes.cs:19` son, juntas, la definición de res válida — no "la primera que pueda". El corte temprano se conserva como optimización.

---
<!-- PÁGINA 4 -->

## 4. Actividad 2.2 · Bitácora de decisiones frente a la IA  *(Criterio 2 — 20%)*

> ⚠️ **APARTADO A COMPLETAR POR EL EQUIPO.** Las filas siguientes son una **referencia con sentido**,
> alineadas a las decisiones reales del proyecto, para que cada integrante las **verifique, ajuste y
> respalde con capturas/enlaces reales** de sus sesiones con la herramienta. Reemplazar los textos entre
> «…» y confirmar cada evidencia antes de entregar. Mínimo 10 registros (regla: *sin bitácora → criterio 2 = 0.0*).
> Los registros escogidos al azar se defienden en vivo → **Abel y Miguel** son los responsables.

| ID | Qué consultamos | Qué propuso la herramienta | Qué hicimos | Argumento propio y evidencia |
|---|---|---|---|---|
| B‑01 | ¿Qué solicitud conviene, SC‑1 o SC‑3? | Sugirió SC‑3 por ser más acotada | **Corregimos** | SC‑1 aterriza sobre P‑02, el punto más caro (14 archivos); SC‑3 caía sobre P‑05 (Media). Ref. P‑02. |
| B‑02 | Costo real de la persistencia posicional (P‑06) | Contó 9 archivos | **Corregimos** | `RepositorioUsuariosArchivo` estaba contado dos veces; `ls Infraestructura` da 8. Ref. P‑06. |
| B‑03 | Patrón para crear vacunas (P‑01) | Abstract Factory | **Rechazamos** | Una sola familia; la interfaz quedaría con una implementación. Adoptamos Factory Method (ya en `IFabricaRes`). |
| B‑04 | Unificar las 4 sobrecargas de `FabricaVacunas` | Builder con API fluida | **Rechazamos** | 2 ejes cruzados; `FabricaVacunas.cs:53,118` lanzan con `nameof(lote)`/`nameof(lote_base)` → texto observable. |
| B‑05 | Patrón para las reglas de validación (P‑05) | Chain of Responsibility | **Corregimos** | La cadena "el primero resuelve"; las 4 reglas aplican todas. Adoptamos Composite. Ref. `ValidarRes.cs:19`. |
| B‑06 | ¿Fachada sobre los servicios? | Facade | **Rechazamos** | Los `*Service` ya son ese punto; una capa más es estilo y rompe SRP (`GuardadoValidado` ya tiende a ello). |
| B‑07 | Aplicar Observer a los 6 publicadores | Interfaz común para los 6 | **Corregimos** | Cubrimos 5; `PublisherVacunaVencida` devuelve `bool` y controla flujo (guarda), no notifica. |
| B‑08 | ¿Patrón para las 7 deps de `GuardadoValidado`? | Command (uno por operación) | **Rechazamos** | Los 5 métodos difieren solo en el tipo → método genérico. Command = sobre‑ingeniería. |
| B‑09 | ¿Eliminar el `enum` de P‑04 con un registro? | Sí, registro de cadenas | **Rechazamos** | Se persiste literal en `Potreros.txt`; cambiarlo es salida observable. Queda como deuda. |
| B‑10 | Modelar la venta de derivados (SC‑1) | Jerarquía de `Venta` por tipo | **Corregimos** | `Venta` con `IArticuloVendible` dentro + Strategy; una jerarquía forzaría discriminador en `Ventas.txt`. |
| B‑11 | ¿El conteo de 14 archivos de P‑02 es correcto? | Tomó la cifra del ADR (7) | **Corregimos** | Recontamos sobre el código: `Venta.cs:16` aún exige `Res`. Son 14. |
| B‑12 | ¿Cuántos patrones adoptar? | 3–5, sin recomendar | **Idea propia** | 4, uno por cada punto priorizado que admite patrón (P‑01/02/03/05). |
| B‑13 | Forma de la estrategia de venta (P‑02) | `IEfectoVenta` con 2 impls | **Aceptamos (verificado)** | Confirmamos que `ServicioVenta.cs:46` es el único punto que toca inventario. |
| B‑14 | Conservar el orden de disparo de avisos | Registrar en orden e iterar | **Aceptamos** | El orden mitad→lleno→peso mín→peso venta es salida observable; se reproduce sin código de ordenamiento. |
| B‑15 | ¿Decorator sobre los validadores? | Decorator | **Rechazamos** | Hace significativo el anidamiento y toca el contrato congelado `ResultadoValidacion` → rompería LSP. |

> **Nota para el equipo:** el detalle ampliado de cada decisión está en
> `02-decision-patrones/Decision-de-Patrones.md`. Añadan al menos una **captura por integrante** que
> muestre el intercambio real con la herramienta y péguenla como evidencia.

---
<!-- PÁGINA 5 -->

## 5. Actividad 3.1 · Diseño TO‑BE — diagrama por capas  *(Criterio 3 — 20%)*

> **Cómo se evalúa:** los dos diagramas correctos, indican patrón y papel de cada clase.

Entregamos **un solo archivo `.drawio` con dos capas superpuestas** (As‑Is / To‑Be):
`03-diseno-tobe/Diagrama_AsIs_ToBe_Capas.drawio`.

- **As‑Is** = línea base del Reto 1 (SOLID). **To‑Be** = As‑Is + los 4 patrones.
- Las **50 clases comunes** conservan exactamente la misma posición entre capas; las **13 clases nuevas**
  se colocan alrededor de aquellas con las que se relacionan. Al alternar capas se ve el sistema
  **evolucionar**, no un diagrama distinto.
- **Leyenda:** azul = interfaz · naranja = abstracta · verde = entidad · amarillo = enum · morado = objeto de valor · rojo = fábrica/estrategia/publicador/validador · negro = se conserva sin cambios.

*(Insertar 3 capturas: (a) solo As‑Is, (b) solo To‑Be, (c) ambas superpuestas.)*

**Qué sale / qué entra / qué se conserva, por patrón:**
- **Factory Method (P‑01):** *entra* `IFabricaVacuna` + `FabricaVacunaBacteriana`/`FabricaVacunaViva` + `SolicitudVacuna`; `FabricaVacunas` pasa a orquestar por diccionario `tipo→fábrica`.
- **Strategy (P‑02, habilita SC‑1):** *entra* `IEfectoVenta` + `EfectoVentaRetiroInventario`/`EfectoVentaSinEfecto` + `IArticuloVendible` + `ProductoDerivado`; `Venta` guarda `IArticuloVendible` (antes `Res`).
- **Observer (P‑03):** *entra* `IPublicadorEvento` + `ContextoAviso`; 5 publicadores lo implementan; `PublisherVacunaVencida` queda fuera a propósito (guarda de flujo).
- **Composite (P‑05):** *entra* `ValidadorCompuesto<T>`; los 5 validadores quedan como **hojas** (su `if` no cambia).
- **Se conserva en negro:** jerarquías `Res`/`Vacuna`, reglas, repositorios (DIP), autenticación.

---
<!-- PÁGINA 6 -->

## 6. Actividad 3.2 · Tabla de cambio estructural  *(Criterio 3 — 20%)*

> **Cómo se evalúa:** de cada elemento se sabe qué pasó con él y cómo se reconectó.

| ID | Elemento | Estado | Qué hacía antes | Qué hace ahora | Quién dependía / cómo se reconecta |
|---|---|---|---|---|---|
| E‑01 | `IFabricaVacuna` | Entra | — | Contrato de creación de vacuna por tipo | `FabricaVacunas` la consume; 2 impls registradas en la raíz |
| E‑02 | `FabricaVacunaBacteriana` / `Viva` | Entra | — | Crean `Bacteriana`/`Viva` desde `SolicitudVacuna` | Resueltas por diccionario `tipo→fábrica` |
| E‑03 | `SolicitudVacuna` | Entra | — | Objeto de valor con los parámetros de creación | `IFabricaVacuna`, `FabricaVacunas` |
| E‑04 | `FabricaVacunas` | Se transforma | 4 métodos fijos (`CrearBacteriana`…`CrearLoteVivo`) | `Crear`/`CrearLote` delegando por diccionario | `VacunaService` la usa igual (fachada de aplicación) |
| E‑05 | `IEfectoVenta` (+2 impls) | Entra | — | Efecto de la venta sobre el inventario | `ServicioVenta` selecciona por tipo del artículo |
| E‑06 | `IArticuloVendible` | Entra | — | Lo que puede figurar en una `Venta` | `Res` y `ProductoDerivado` lo implementan; `Venta` lo referencia |
| E‑07 | `ProductoDerivado` (+`TipoProducto`) | Entra | — | Artículo vendible no‑animal (SC‑1) | `ServicioVenta.vender_producto` |
| E‑08 | `Venta` | Se transforma | Campo `Res res` obligatorio | Campo `IArticuloVendible articulo` | `MapeadorVenta`, `ValidadorVenta`, `IRepositorioVentas` |
| E‑09 | `ServicioVenta` | Se transforma | `vender_res` con `Remove(res)` inline | `vender_res` + `vender_producto`; efecto vía `IEfectoVenta` | `VentaService` |
| E‑10 | `IPublicadorEvento` (+`ContextoAviso`) | Entra | — | Contrato común de aviso | `Potrero`/`GestorReses`/`ServicioVacunacion` lo reciben inyectado |
| E‑11 | 5 publicadores | Se transforman | Clases concretas creadas con `new` en el dominio | Implementan `Informar(ctx, receptor)`; inyectados desde la raíz | El dominio ya no hace `new` |
| E‑12 | `ValidadorCompuesto<T>` | Entra | — | Agrupa `IValidador<T>` y corta en la 1.ª que falla | Envuelto en la raíz; `GuardadoValidado`/`ResService` lo consumen vía `IValidador<T>` |
| E‑13 | 5 validadores | **Se conservan** | Reglas en un `if` | Idénticos, ahora como **hojas** del Composite | Sin cambios; su `if` no se toca |

---
<!-- PÁGINA 7 -->

## 7. Actividad 3.3 · Fichas de patrón  *(Criterio 3 — 20%)*

> **Cómo se evalúa:** alternativas reales (una debe ser "no hacer nada") y costo declarado.

### Factory Method — P‑01
| Campo | Contenido |
|---|---|
| Punto de dolor | `FabricaVacunas` con 4 métodos fijos + decisión de tipo repartida en 3 capas |
| Alternativas | **No hacer nada** (queda la duplicación) · Abstract Factory (1 familia, descartado) · Builder (2 ejes, descartado) |
| Sale / entra | Entran `IFabricaVacuna`, `FabricaVacunaBacteriana/Viva`, `SolicitudVacuna`; `FabricaVacunas` → diccionario |
| Cómo se relaciona | `FabricaVacunas` construye por `tipo→fábrica`; `VacunaService` la usa igual; registrado en `RaizComposicion` |
| Impacto | +4 clases; un tipo nuevo pasa de 10 archivos a **1 clase + 1 línea** en la raíz |
| Qué cuesta | Un salto de indirección al leer dónde se crea una vacuna |
| Origen | Idea propia (replica `IFabricaRes`); ver B‑03, B‑04 |

### Strategy — P‑02 (habilita SC‑1)
| Campo | Contenido |
|---|---|
| Punto de dolor | `ServicioVenta.cs:46` retira la res inline; vender un derivado daría de baja el animal |
| Alternativas | **No hacer nada** (solo se vende el animal) · Jerarquía de `Venta` (B‑10, forzaría discriminador en disco) |
| Sale / entra | Entran `IEfectoVenta`, 2 efectos, `IArticuloVendible`, `ProductoDerivado`; `Venta` guarda `IArticuloVendible` |
| Cómo se relaciona | `ServicioVenta` elige el efecto por tipo (`TipoSoportado.IsAssignableFrom`); registrado en la raíz |
| Impacto | +5 participantes; `vender_producto` nuevo; SC‑1 viable sin duplicar el método |
| Qué cuesta | El efecto de una venta se lee en la raíz, no en el método |
| Origen | Aceptado tras verificar (B‑13) |

---
<!-- PÁGINA 8 -->

### Observer — P‑03
| Campo | Contenido |
|---|---|
| Punto de dolor | 8 `new` de publicadores dentro del dominio; sin interfaz común |
| Alternativas | **No hacer nada** (el dominio fabrica sus avisos) · Meter los 6 en la interfaz (B‑07: rompería la guarda) |
| Sale / entra | Entran `IPublicadorEvento`, `ContextoAviso`; 5 publicadores lo implementan; `PublisherVacunaVencida` fuera |
| Cómo se relaciona | La raíz inyecta la colección **en orden**; el dominio itera y ya no hace `new` |
| Impacto | 8 `new` → 0; un aviso nuevo = 1 clase + 1 registro |
| Qué cuesta | Un `ContextoAviso` que algunos avisos ignoran; el orden depende del registro |
| Origen | Corregido sobre la IA (B‑07): cubrimos 5, no 6 |

### Composite — P‑05  *(el más discutido)*
| Campo | Contenido |
|---|---|
| Punto de dolor | `ValidarRes.cs:19`: 4 reglas colapsadas en un `if`; validación cerrada a un tipo de regla |
| Alternativas | **No hacer nada** (una regla nueva modifica la clase) · Chain of Responsibility (B‑05) · Decorator (B‑15) |
| Sale / entra | Entra `ValidadorCompuesto<T> : IValidador<T>`; los 5 validadores quedan como **hojas** (su `if` no cambia) |
| Cómo se relaciona | Se instancia en `RaizComposicion` (y en los 3 arneses); envuelve cada validador; `GuardadoValidado` lo consume vía `IValidador<T>` (DIP) |
| Impacto | +1 clase, 0 modificadas en el dominio, 5 registros cambiados en la raíz |
| Qué cuesta | Más clases pequeñas; el efecto de la validación se lee en la raíz; hoy cada compuesto envuelve una sola hoja (indirección cuya variación aún no se ejerce, pero es el punto de extensión declarado) |
| Origen | Corregido sobre la IA (B‑05: Chain → Composite) |

---
<!-- PÁGINA 9 -->

## 8. Actividad 4.1 · Matriz SOLID  *(Criterio 4 — 15%)*

> **Cómo se evalúa:** matriz completa con evidencia; todo principio tensionado, declarado y compensado; ninguna celda Rota sin justificar.

| Patrón | SRP | OCP | LSP | ISP | DIP |
|---|---|---|---|---|---|
| **Factory Method** | Refuerza | Refuerza | Neutro | Refuerza | Refuerza |
| **Strategy** | Refuerza | Refuerza | Neutro | Neutro | Refuerza |
| **Observer** | Refuerza | Refuerza | **Tensionado/compensado** | Refuerza | Refuerza |
| **Composite** | Refuerza | Refuerza | Neutro | Neutro | Refuerza |

**Evidencia (una línea por celda ≠ Neutro):**
- **FM·SRP** — las fábricas saben crear un tipo; `FabricaVacunas` solo orquesta.
- **FM·OCP** — tipo nuevo = 1 clase + 1 línea; desaparecen los `if` de `VacunaService.cs:49` y `VacunaController.cs:95`.
- **FM·ISP/DIP** — `IFabricaVacuna` cohesiva; se depende de la abstracción, no de `Bacteriana`/`Viva`.
- **STR·SRP/OCP** — el `Remove(res)` sale a `EfectoVentaRetiroInventario`; vender un derivado no toca inventario, sin modificar `ServicioVenta`.
- **STR·DIP** — `ServicioVenta` depende de `IEfectoVenta` (colección inyectada).
- **OBS·SRP/OCP/DIP** — 5 publicadores tras `IPublicadorEvento`; el dominio ya no hace `new`; aviso nuevo = 1 clase + 1 registro.
- **OBS·LSP (única tensión, declarada y compensada)** — `PublisherVacunaVencida` **no** implementa `IPublicadorEvento`: devuelve `bool` y `ServicioVacunacion` lanza según ese valor (guarda de flujo). Forzarlo dentro del contrato `void Informar(...)` rompería LSP; se deja fuera y se inyecta como tipo concreto.
- **OBS·ISP** — `IPublicadorEvento (Informar)` separada de `IReceptorEventos (Notificar)`.
- **CMP·SRP** — `ValidadorCompuesto<T>` solo compone e itera.
- **CMP·OCP** — regla nueva = 1 clase + 1 línea, sin tocar `ValidarRes.cs:19`.
- **CMP·DIP** — `GuardadoValidado` depende de `IValidador<T>`; recibe el compuesto sin enterarse.

---
<!-- PÁGINA 10 -->

## 9. Actividad 4.2 · Comportamiento preservado  *(Criterio 4 — 15%)*

> **Cómo se evalúa:** los doce casos pasan y las salidas coinciden con las de antes; el código corresponde al diagrama.

**Evidencia reproducible (ejecutable en vivo en la sustentación):**
- `dotnet build Hacienda.sln` → **0 errores**.
- `dotnet test Pruebas/Bib_Hacienda.Pruebas.csproj` → **30/30 pruebas pasan** (deuda congelada, formato de datos, sustitución/LSP).
- Arnés de caracterización → `04-verificacion/salida-rediseñada.txt` y `salida-sc2.txt` **byte‑idénticos**
  al baseline previo a los patrones (incluida la corrida **a través del Composite**); los 6 `.txt` de datos, idénticos.
- **El código corresponde al diagrama:** `Clases/Validaciones/ValidadorCompuesto.cs` ↔ nodo `ValidadorCompuesto<T>` de la capa To‑Be.

**Los doce casos (categorías observables):** 1 texto · 2 tipo de alerta · 3 navegación · 4 archivos · 5 listados · 6 consola.
*(Insertar la tabla antes/después de los 12 casos, lado a lado, mostrando que coinciden.)*

> **Nota de entorno:** los proyectos apuntan a `net8.0`; en una máquina con runtime .NET 10 se ejecutan con `DOTNET_ROLL_FORWARD=LatestMajor`.

---
<!-- PÁGINA 11 -->

## 10. Actividad 5 · Análisis de riesgos  *(Criterio 5 — 15%)*

> **Cómo se evalúa:** riesgos como condición→consecuencia, con señal de alerta observable y acción concreta.
> *(El equipo ajusta Prob/Imp de 1 a 5; Exp = P×I.)*

| ID | Riesgo (si ocurre X, entonces Y) | Prob | Imp | Exp | Qué hacen para evitarlo | Señal observable |
|---|---|---|---|---|---|---|
| R‑01 | Si la colección de `IPublicadorEvento` se registra en otro orden en la raíz, entonces cambia el orden observable de avisos (mitad→lleno→peso mín→peso venta) y rompe salida congelada | 2 | 4 | 8 | Registrar en orden en `RaizComposicion`; test de caracterización que compara la salida | `diff` no vacío en `salida-rediseñada.txt` / falla el test de orden |
| R‑02 | Si entra un `IArticuloVendible` sin un `IEfectoVenta` que lo cubra, entonces `_efectos.First(...)` lanza y la venta falla | 2 | 3 | 6 | `EfectoVentaSinEfecto` como efecto por defecto para no‑animales; test por cada tipo | Excepción "sequence contains no matching element" en `vender_*` |
| R‑03 | Si una regla nueva se añade al `ValidadorCompuesto` en un orden que altere el corte temprano, entonces cambia el primer mensaje de fallo observable | 2 | 3 | 6 | Mismo texto congelado `ResultadoValidacion.Invalido()`; orden documentado en la raíz | La caracterización de guardado cambia de mensaje |

---
<!-- PÁGINA 12 -->

## 11. Actividad 6 · Vista de negocio  *(Criterio 6 — 15%)*

> **Para:** la Líder Técnica y quien aprueba el presupuesto. **Prohibido aquí:** nombres de patrones, de clases, UML, siglas sin desarrollar (incluida SOLID) y las palabras *refactorizar / desacoplar / inyección de dependencias*.

**Qué le vamos a hacer al sistema.** Hoy la finca solo sabe vender el animal completo. Vamos a permitir
que **venda también productos derivados** —leche, carne, piel— **sin dar de baja al animal**, y a dejar
el sistema preparado para agregar **nuevos tipos de vacuna, avisos y reglas de control** de forma rápida.

**Qué NO cambia.** Todo lo que hoy funciona sigue igual: los mismos cálculos, los mismos mensajes, los
mismos reportes. No tocamos el comportamiento que el negocio ya conoce.

**Dónde se está yendo hoy el tiempo y el dinero.** Cada novedad de este tipo obligaba a modificar
**hasta 14 puntos distintos** del sistema, muchos sin relación aparente entre sí. Eso es lento, caro y
propenso a romper algo que ya funcionaba.

**Qué gana el negocio.** Con el cambio, una novedad pasa de "tocar muchas partes" a "agregar una pieza
nueva sin mover el resto": **menos tiempo de respuesta** ante una solicitud y **menos riesgo** de romper
lo que ya opera.

**Qué cuesta.** El sistema queda con más piezas pequeñas y ordenadas. Es un costo que se paga una vez y
se recupera en cada cambio futuro.

**Qué necesitamos del negocio.** Confirmar **qué producto derivado** se prioriza para salir primero.

**Qué pasa si no se hace.** Cada nueva forma de vender o cada nueva vacuna seguirá siendo cara y frágil,
y el equipo de soporte seguirá pagando ese sobrecosto en cada solicitud.

> **Prueba de que sirve (evidencia en el video):** presentamos esta vista a **[persona no técnica]**, que
> resumió lo entendido así: *"[frase que dijo]"*. *(Completar con la evidencia real grabada.)*

---
<!-- PÁGINA 13 -->

## 12. Actividad 6 · Vista técnica — guía de "dónde tocar"  *(Criterio 6 — 15%)*

> **Para:** el ingeniero que entra en seis meses y debe cambiar algo sin romper nada.
> **Punto de ensamblaje del sistema:** `p_mvcHacienda/Composicion/RaizComposicion.cs`.

| Tipo de cambio previsible | Qué CREAR | Qué MODIFICAR | Qué NO tocar |
|---|---|---|---|
| **Nuevo tipo de vacuna** | 1 clase que implemente `IFabricaVacuna` | 1 línea de registro en `RaizComposicion` | `FabricaVacunas`, `VacunaService`, los `if` (ya no existen) |
| **Nuevo producto / artículo vendible (SC‑1)** | `ProductoDerivado` (o una nueva `IEfectoVenta` si el efecto difiere) | 1 línea en la raíz | `ServicioVenta`, `Venta` |
| **Nuevo aviso de dominio** | 1 clase que implemente `IPublicadorEvento` | Añadirla al arreglo de avisos en la raíz **respetando el orden** | Publicadores existentes, `ContextoAviso` |
| **Nueva regla de validación (p. ej. historia clínica, SC‑3)** | 1 clase que implemente `IValidador<T>` | Añadirla a la lista del `ValidadorCompuesto` en la raíz | El `if` de `ValidarRes`, `GuardadoValidado` |
| **Nuevo tipo de res** | 1 clase que implemente `IFabricaRes` | El `enum l_tipos_potreros` + el registro en la raíz | La jerarquía `Res` |
| **Chips / geolocalización (SC‑2, ya implementado)** | — | — | El chip viaja con la res; solo referencia |

**Reglas que no se deben romper y por qué:**
1. **El orden de los avisos es salida observable** — regístralos en el mismo orden en la raíz.
2. **El texto de `ResultadoValidacion` está congelado** — una regla nueva no cambia el mensaje.
3. **Columnas nuevas en los `.txt`, solo al final** — nunca intercalar (contiene P‑06 a costo 0).

**Deuda pendiente declarada:** P‑04 (el tipo de res es un `enum` persistido literal) y P‑06 (persistencia por posición de columna).

---
<!-- PÁGINA 14–15 -->

## 13. Anexos

### 13.1 Deuda declarada (no intervenida, con costo medido)
- **P‑04 — enum de tipo de res dentro de la entidad.** Cambiarlo altera `Potreros.txt` (salida observable). Costo si se interviniera: 6 archivos.
- **P‑06 — persistencia por posición de columna.** 33 accesos `partes[n]` en 8 archivos; el remedio reescribe datos del cliente. Contenido a costo 0 por la regla "columnas solo al final".

### 13.2 Índice de archivos por patrón (para ubicar rápido en el repo)
- **Factory Method** → `Contratos/IFabricaVacuna.cs`, `Fabricas/FabricaVacuna*.cs`, `Servicios/FabricaVacunas.cs`, `Valores/SolicitudVacuna.cs`
- **Strategy** → `Contratos/IEfectoVenta.cs`, `Contratos/IArticuloVendible.cs`, `Estrategias/*.cs`, `Clases/ProductoDerivado.cs`, `Servicios/ServicioVenta.cs`
- **Observer** → `Contratos/IPublicadorEvento.cs`, `Valores/ContextoAviso.cs`, `Eventos/Publisher*.cs`
- **Composite** → `Clases/Validaciones/ValidadorCompuesto.cs`, `Clases/Validaciones/Validar*.cs`, `Composicion/RaizComposicion.cs`

### 13.3 Nota sobre el punto de partida (patrones previos)
El Reto 1 ya aplicaba **Factory Method** en las reses (`IFabricaRes`) y una **estructura de contratos**
(repositorios/DIP, `IReceptorEventos`, validadores segregados), visibles en la capa As‑Is. El Reto 2, con
lo aprendido en clase, los **reconoció, midió y extendió** (Factory Method a las vacunas) y añadió Strategy,
Observer y Composite. La robustez no se improvisó: se construyó sobre una base que ya existía.

---

### Checklist de reglas que anulan criterios *(revisar antes de convertir a PDF)*
- [ ] Bitácora completada con evidencia real (si falta → criterio 2 = 0.0).
- [ ] Vista de negocio sin patrones/clases/UML/SOLID (si no → criterio 6 ≤ 3.0).
- [ ] Diagramas corresponden al código (verificado: capas As‑Is/To‑Be fieles; Composite implementado).
- [ ] Cada patrón anclado a un P‑xx (los 4 lo están).
- [ ] Comportamiento observable sin cambios (verificado: 30/30 + caracterización byte‑idéntica).
- [ ] Sin cambio de estilo arquitectónico ni frameworks nuevos.
- [ ] Documento paginado, con índice, ≤ 15 páginas.
