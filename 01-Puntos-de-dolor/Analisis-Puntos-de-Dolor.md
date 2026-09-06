# Actividad 1 — Puntos de dolor de la arquitectura actual

Analizamos el diseño rediseñado del Reto 1 (`03-src/Modificado`), que es el AS-IS de este reto. Un punto
de dolor aquí no es una violación de SOLID —esas ya se cerraron—, sino un sitio donde el diseño es
correcto y aun así cambiarlo obliga a abrir muchos archivos, o donde el compilador no avisa si alguien
se olvida de uno. Cada punto indica qué queda de lo diagnosticado en el Reto 1 y qué es nuevo, porque
varios de estos sitios ya se tocaron y el rediseño los dejó a medio camino o creó el problema actual.

**Cómo medimos el costo.** Para cada punto elegimos un cambio concreto, buscamos en todo el código el
tipo o la cadena afectada y seguimos las llamadas hasta sus extremos. La cifra es el número de clases y
archivos que hay que abrir. Las vistas `.cshtml` se cuentan aparte porque el compilador no las verifica.

**Criterio de prioridad.** Alta: bloquea una solicitud del Anexo B o cuesta 10 archivos o más.
Media: entre 5 y 9 archivos, o tiene puntos de cambio que el compilador no protege. Baja: menos de 5.

## Tabla consolidada

| ID | Dónde | Qué lo hace rígido o caro | Escenario de cambio y costo hoy | Prioridad | Origen |
|---|---|---|---|---|---|
| **P-01** | `FabricaVacunas.cs`, `VacunaService.cs:49-58`, `VacunaController.cs:95-107`, `MapeadorVacuna.cs`, `ServicioVacunacion.cs:60-84`, `Res.cs:168-171` | Cada tipo concreto de vacuna está escrito a mano en toda la cadena: un método por tipo en la fábrica, un `if/else` en el servicio, otro en el controlador, cuatro ramas en el mapeador y dos contadores en la vacunación. Hasta `Res` declara `MaxVacunasBacterianas` y `MaxVacunasVivas`. | Un tercer tipo de vacuna: **10 clases modificadas y 4 vistas** = 14 archivos, más 1 clase nueva | Alta | Conteo verificado con IA |
| **P-02** | `Venta.cs:13,16`, `ServicioVenta.cs:42-46`, `ValidarVenta.cs:14`, `MapeadorVenta.cs:26` | Una `Venta` exige una `Res`: campo obligatorio del constructor, el validador la rechaza si es nula y la línea de disco escribe sus cuatro datos. Además vender borra el animal del potrero. | SC-1, vender derivados: **12 clases modificadas y 2 vistas** = 14 archivos. Hoy la solicitud es inviable sin tocarlas todas | Alta | Hallazgo propio |
| **P-03** | `Potrero.cs:21-24,105-108`, `GestorReses.cs:30-31,77-78,188-189`, `ServicioVacunacion.cs:29-30` | Los seis publicadores de avisos se crean con `new` dentro del dominio: 8 instanciaciones en 3 clases. No hay interfaz común de publicador y la secuencia de disparo está escrita a mano. | Cambiar la política de avisos: **3 clases de dominio y 5 métodos**, más **8 instanciaciones que ninguna prueba puede sustituir** | Media | Hallazgo propio |
| **P-04** | `Potrero.cs:15`, `ResService.cs:233-238`, `MapeadorRes.cs:155-156`, 3 vistas | Un cuarto tipo de res obliga a añadir un valor al `enum` que vive dentro de la entidad `Potrero`, más un diccionario de presentación y tres vistas. | Un cuarto tipo de res: **3 clases modificadas y 3 vistas** = 6 archivos, más 2 clases nuevas. **4 de los 6 no los verifica el compilador** | Media | Hallazgo propio |
| **P-05** | `GuardadoValidado.cs:49-65,69-149`, `ValidarRes.cs:19`, `ResultadoValidacion.cs:23` | `GuardadoValidado` recibe 7 dependencias y repite 5 veces el mismo bucle validar → cortar → persistir. Cada validador mete todas sus reglas en un solo `if` que devuelve siempre el mismo texto. | Un agregado nuevo (producto de SC-1, historia clínica de SC-3): **2 clases nuevas, 2 archivos modificados y 3 servicios que recompilan** por el cambio de firma del constructor | Media | Conteo verificado con IA |
| **P-06** | `MapeadorRes.cs`, `MapeadorVacuna.cs`, `MapeadorVenta.cs`, `MapeadorPotrero.cs`, `RepositorioUsuariosArchivo.cs` | La persistencia lee por posición de columna: 33 accesos `partes[n]` detrás de guardas que descartan la línea sin lanzar excepción. | Añadir un campo a cualquier entidad persistida: **4 mapeadores y 4 repositorios** = 8 archivos, con riesgo de pérdida silenciosa del histórico | **No se interviene** | Conteo verificado con IA |

## Evidencia

### P-01 · Un tipo de vacuna nuevo se escribe catorce veces

`FabricaVacunas` tiene cuatro métodos fijos (`CrearBacteriana`, `CrearViva`, `CrearLoteBacteriano`,
`CrearLoteVivo`), y la decisión de qué tipo se crea está repartida en tres capas que resuelven lo mismo
de tres maneras distintas:

- `VacunaController.cs:95` compara una cadena: `if (tipoVacuna == "Bacteriana") … else // Viva`
- `VacunaService.cs:49` mira qué parámetros vienen llenos: `if (periodoAplicacion.HasValue && !atenuacion.HasValue)`
- `MapeadorVacuna.cs:73,140` lee la quinta columna del archivo, y una vez la compara ignorando mayúsculas y otra vez no

El síntoma más caro está en `Res.cs:168-171`: toda res, presente y futura, debe declarar cuántas
vacunas *bacterianas* y *vivas* admite. Dos miembros abstractos con el nombre de una subclase concreta,
así que un tercer tipo de vacuna termina cambiando la jerarquía del ganado.

Archivos: `FabricaVacunas`, `ServicioVacunacion`, `Res`, `Ternero`, `Cebon`, `Novillo`,
`PublisherVacunacionCompletada`, `VacunaService`, `VacunaController`, `MapeadorVacuna`, más
`Vacuna/Create`, `Vacuna/Index`, `Vacuna/Aplicar` y `Res/DetalleVacunas`.

**Qué cambió desde el Reto 1.** H-16 señalaba la interfaz `ICreacionVacuna` con cuatro firmas, y esa
interfaz desapareció. Lo que no desapareció es la duplicación: los cuatro métodos siguen ahí, ahora
dentro de `FabricaVacunas`, y el rediseño añadió un tercer punto de decisión —`VacunaService.cs:49`—
que antes no existía, porque la capa de aplicación tuvo que traducir el tipo a parámetros opcionales.

### P-02 · La venta solo sabe vender el animal entero

```csharp
// ServicioVenta.cs:42-46
Venta venta = new Venta(potrero, DateTime.Now, res, monto);
_hacienda.L_ventas.Add(venta);
_hacienda.L_potreros.Where(p => p == potrero).FirstOrDefault().L_reses.Remove(res);
```

Vender leche o piel con este flujo daría de baja la vaca. Y el acoplamiento no está solo ahí: `Venta`
guarda una `Res` como campo obligatorio (`Venta.cs:13,16`), `ValidadorVenta` la exige explícitamente
(`ValidarVenta.cs:14`), `MapeadorVenta.cs:26` escribe sus cuatro datos en la línea de disco y
`RepositorioVentasArchivo` recibe las fábricas de res (`RaizComposicion.cs:58-60`) solo para reconstruir
el animal de cada venta histórica.

Archivos: `Venta`, `ServicioVenta`, `ValidarVenta`, `IRepositorioVentas`, `MapeadorVenta`,
`RepositorioVentasArchivo`, `VentaService`, `VentaController`, `ResService`, `GuardadoValidado`,
`RaizComposicion`, `CargadorInicial`, más `Venta/Create` y `Venta/Index`.

**Qué cambió desde el Reto 1.** Nada. El ADR §3.3.3 diseñó una `Venta` polimórfica y proyectó que SC-1
bajaría de 15-17 archivos a 7, pero esa proyección nunca se implementó: el trimestre pasado se ejecutó
SC-2. Los 14 archivos de arriba son el recuento sobre el código real, que es exactamente lo que el
propio §9.3 exigía hacer.

### P-03 · El dominio fabrica sus propios avisos

Ocho instanciaciones directas de clases concretas dentro del dominio: cuatro en `Potrero.cs:21-24`, dos
en `GestorReses.cs:30-31` y dos en `ServicioVacunacion.cs:29-30`.

Los publicadores son clases concretas soldadas al dominio y cada una tiene su propia firma
—`Informar_Peso_Min(Res, IReceptorEventos)` frente a `Informar_Potrero_Mitad(ushort, Potrero, IReceptorEventos)`—,
así que no existe ninguna colección sobre la que iterar. La secuencia se escribe a mano en cinco
métodos y el orden es comportamiento observable (`Potrero.cs:102-108`), lo que convierte cualquier
inserción en una decisión delicada. `GestorReses` ya paga el precio: repite la misma pareja de llamadas
en sus dos sobrecargas de `alimentar_res` (`:77-78` y `:188-189`).

**Qué cambió desde el Reto 1.** H-12 denunciaba la fuga de memoria por suscripciones con `+=` que nunca
se deshacían, y ADR-07 la cerró entregando el receptor por parámetro. Ese problema está resuelto. El de
ahora es el opuesto y quedó intacto: `IReceptorEventos` invirtió quién *recibe* el aviso, pero nadie
invirtió quién lo *emite*.

### P-04 · El catálogo de tipos de res vive dentro de una entidad

El discriminador de tipo es un `enum` declarado dentro de la entidad `Potrero` (`Potrero.cs:15`):

```csharp
public enum l_tipos_potreros { ternero, novillo, cebon };
```

`IFabricaRes.TipoPotreroSoportado` depende de él, así que agregar un tipo obliga a modificar una entidad
del dominio antes de poder escribir la fábrica nueva. A eso se suman el diccionario de presentación de
`ResService.cs:233-238` y las vistas `Potrero/Create.cshtml:46-48`, `Res/Index.cshtml:46-48` y
`Res/Create.cshtml:65`; esos cuatro el compilador no los verifica, así que si alguien se olvida la
aplicación compila igual y la pantalla muestra un contador de menos. Agravante en
`MapeadorRes.cs:155-156`: un tipo desconocido se convierte en `Ternero` sin avisar, en lugar de fallar.

**Qué cambió desde el Reto 1.** H-06 medía 9 puntos en 8 archivos y el rediseño los bajó a 6, con los
tres puntos de presentación declarados como deuda consciente en ADR §9.2.1. Lo que no estaba en H-06 es
el `enum`: antes el tipo se resolvía con un `switch` sobre cadena en `Potrero.cs:88-102`, y al
sustituirlo por `IFabricaRes` el enum pasó de detalle interno a **contrato público del que cuelgan las
tres fábricas**. El punto de cambio no se movió de sitio, se endureció.

### P-05 · Un solo punto valida y guarda todo lo que existe

Concentrar la secuencia validar → persistir evitó escribirla cinco veces, pero la clase creció hasta
7 dependencias por constructor y 5 métodos con el mismo cuerpo. Un agregado nuevo obliga a crear el
validador y el repositorio, modificar `GuardadoValidado` y `RaizComposicion`, y cambiar la firma del
constructor, con lo que recompilan `PotreroService`, `ResService` y `VacunaService`.

Un nivel más abajo, los validadores colapsan todas sus reglas en un `if`:

```csharp
// ValidarRes.cs:19 — clase ValidadorRes, del diseño anterior
if (res == null || string.IsNullOrWhiteSpace(res.Nombre) || res.Peso <= 0 || res.Edad <= 0)
    return ResultadoValidacion.Invalido();
```

`Invalido()` devuelve siempre el mismo texto (`ResultadoValidacion.cs:23`), así que una regla nueva
obliga a modificar la clase y el operario nunca sabe cuál de las cuatro condiciones falló.
Ese archivo ya no está en `src/`: el Composite de la Actividad 3 lo eliminó y repartió sus cuatro
condiciones en una clase cada una.

**Qué cambió desde el Reto 1.** `GuardadoValidado` no existía: se creó durante el rediseño, como
enmienda registrada en `04-evidencia/enmiendas-ADRs.md`, para no repetir el bucle en cinco servicios.
Es un punto de dolor nacido de la solución, no una deuda heredada.

### P-06 · Persistencia por posición de columna — no se interviene

33 accesos `partes[n]` repartidos en cinco archivos (`MapeadorVacuna` 14, `MapeadorRes` 9,
`MapeadorVenta` 7, `MapeadorPotrero` 2, `RepositorioUsuariosArchivo` 1). Los `.txt` no tienen cabecera
ni número de versión, y la guarda `if (partes.Length < N) return false` descarta la línea sin lanzar
excepción; `MapeadorVacuna.cs:75-78` llega a omitir en silencio toda bacteriana con período fuera de `[2,4]`.

No lo intervenimos por tres razones. La cura real —cabecera, versión o un serializador— reescribe los
seis archivos de datos del cliente, que es cambio de salida observable y está prohibido. Aun
conservando el formato byte a byte habría que tocar la carpeta `Infraestructura` entera —cuatro
mapeadores y cuatro repositorios, 8 archivos— para producir exactamente lo mismo que hoy. Y la regla que rige
el proyecto —columnas nuevas solo al final, nunca intercalar; un registro de forma distinta va a un
archivo nuevo (`MapeadorRes.cs:37-40`)— contiene el problema con costo cero y ya sobrevivió a SC-2 sin
perder una línea del histórico. El problema cuesta hoy cero archivos mientras se respete la regla; el
remedio cuesta ocho y arriesga los datos del cliente.

**Qué cambió desde el Reto 1.** El formato posicional venía del sistema original y ADR-12 lo documentó
como riesgo de datos al diseñar SC-2, pero nunca se midió. Aquí queda medido: 33 accesos, 5 archivos.

## Patrón común

Cinco de los seis puntos responden a lo mismo: el Reto 1 resolvió quién hace qué, pero no quién decide
qué objeto concreto se usa. En P-01 y P-04 esa decisión está repartida en condicionales, cadenas de
texto y enums; en P-03 el propio dominio construye sus colaboradores con `new`; en P-02 y P-05 los
modelos y servicios están cerrados a un solo tipo de cosa —una venta es una res, un validador es un `if`—.
