# Actividad 4 — Demostrar que SOLID sigue en pie

## 4.1 Matriz de verificación

Una fila por patrón adoptado, una columna por principio. **Refuerza** — el patrón resuelve
exactamente el problema de ese principio. **Neutro** — el patrón no lo toca, para bien ni
para mal. **Tensionado pero compensado** — el patrón introduce un riesgo real sobre ese
principio, declarado aquí junto con lo que lo compensa. **Roto** — no aplica en ninguna
celda de esta tabla: no dejamos ningún principio roto sin compensación.

| Patrón adoptado | SRP | OCP | LSP | ISP | DIP |
|---|---|---|---|---|---|
| **Factory Method** (P-01, catálogo de vacunas) | Refuerza | Refuerza | Tensionado pero compensado | Neutro | Refuerza |
| **Strategy** (P-02, efecto de venta / SC-1) | Refuerza | Refuerza | Tensionado pero compensado | Neutro | Refuerza |
| **Observer** (P-03, publicadores de eventos) | Refuerza | Refuerza | Tensionado pero compensado | Neutro | Refuerza |
| **Composite** (P-05, validadores) | Refuerza | Refuerza | Tensionado pero compensado | Neutro | Refuerza |

### El hilo conductor de la columna LSP

Los cuatro patrones comparten la MISMA tensión, no cuatro tensiones distintas por
casualidad: cada uno introduce una familia de implementaciones (`IFabricaVacuna`,
`IEfectoVenta`, `IPublicadorEvento`, `IValidador<Res>` dentro del compuesto) donde **una
implementación individual asume una entrada más estrecha de la que su contrato declara**.
Eso es, con precisión, precondición fortalecida — lo que LSP prohíbe. Ninguna de las
cuatro está rota en el sistema que se entrega, porque en los cuatro casos la seguridad no
la da la implementación: la da **el único punto que las invoca**, que siempre les entrega
una entrada compatible. Es una propiedad de la composición, no del tipo — y por eso queda
declarada aquí y no escondida.

- **Factory Method.** `FabricaVacunaBacteriana.Crear` lanza `ArgumentException` si
  `SolicitudVacuna.PeriodoAplicacion` no llegó; `FabricaVacunaViva.Crear` exige
  `GradoAtenuacion`. `SolicitudVacuna` declara los dos campos opcionales — nada en el tipo
  impide pedir una Bacteriana sin período. **Compensación:** `FabricaVacunas.ObtenerFabrica`
  resuelve la fábrica por el discriminador `tipoVacuna` que ya viene explícito desde
  `VacunaController`, y este último SIEMPRE completa el campo correspondiente antes de
  llamar (`Contratos/IFabricaVacuna.cs`, comentario de `Crear`). La guarda es defensiva y
  hoy inalcanzable, exactamente el mismo argumento que sostiene ADR-05 para `IFabricaRes`.

- **Strategy.** `EfectoVentaRetiroInventario.Aplicar` hace `(Res)articulo`: si alguien lo
  invoca con un `ProductoDerivado`, lanza `InvalidCastException`. La firma de
  `IEfectoVenta.Aplicar(Hacienda, Potrero, IArticuloVendible)` no promete que el artículo
  sea una `Res`. **Compensación:** `ServicioVenta.AplicarEfecto` nunca invoca una estrategia
  al azar — resuelve `_efectos.First(e => e.TipoSoportado.IsAssignableFrom(tipoArticulo))`
  antes de llamar, así que el cast siempre es seguro en la práctica
  (`Bib_Hacienda/Servicios/ServicioVenta.cs`). Probado en
  `VenderUnaResLaRetiraDelPotreroYVenderUnProductoNoTocaElInventario`.

- **Observer.** `PublisherPotreroMitad.Informar` lee `contexto.Potrero.Identificacion`
  cuando `CantidadReses` coincide con la capacidad máxima; si se registrara este aviso en
  `avisosAlimentacion` (donde `ContextoAviso.Potrero` nunca se llena), la condición numérica
  nunca coincidiría — funciona por una coincidencia del umbral de negocio, no por una
  garantía del tipo. **Compensación:** la raíz de composición registra cada publicador SOLO
  en la lista cuyo contexto sí llena los campos que necesita (`RaizComposicion.cs`,
  bloque «Publicadores de eventos»), y el CASO 19 (`salida-patrones.txt`) demuestra que un
  aviso nuevo, correctamente registrado, se dispara sin tocar `Potrero.cs` ni
  `GestorReses.cs`.

- **Composite.** `ReglaNombreObligatorio`, `ReglaPesoPositivo` y `ReglaEdadPositiva`
  asumen `res != null` — llamadas sueltas con `null` lanzarían `NullReferenceException`,
  aunque `IValidador<Res>.Validar(T entidad)` no exige no-nulidad. **Compensación:**
  `ValidadorCompuesto<Res>` corta en la primera regla inválida y `ReglaResNoNula` se
  registra SIEMPRE primero (`RaizComposicion.cs`, bloque «Composite»); las otras tres nunca
  se evalúan sobre `null`. Probado en `ValidadorCompuestoCortaEnLaPrimeraReglaInvalidaSin-
  EvaluarLasSiguientes` y `ElOrdenDeLasCuatroReglasDeResEvitaNullReferenceException`.

### Por qué ISP queda Neutro y no Refuerza

Las cuatro interfaces nuevas (`IFabricaVacuna`, `IEfectoVenta`, `IPublicadorEvento`) y la
reutilizada (`IValidador<T>`) ya nacían de un solo método relevante por implementación
desde el Reto 1 (ADR-03). Ninguna implementación de estos cuatro patrones se ve forzada a
escribir un método que no usa, pero tampoco había un contrato gordo que este trimestre
haya partido. Marcarlo "Refuerza" habría sido apuntarse un mérito que ya era de ADR-03.

### Confirmación contra los errores típicos del Anexo A

Ninguno de los cuatro patrones cae en los errores que el enunciado marca como frecuentes:

| Error típico | ¿Aplica aquí? |
|---|---|
| Fábrica que crece con un condicional por tipo nuevo | No — `FabricaVacunas` y `ServicioVenta` resuelven por registro (`ObtenerFabrica`, `AplicarEfecto`), cero `if` por tipo |
| Fachada que absorbe lógica de negocio | No — Facade se evaluó y se descartó (Decision-de-Patrones.md) |
| Punto de acceso global de instancia única | No — Singleton no se adoptó |
| Método plantilla con paso vacío en una subclase | No — Template Method no se adoptó |
| Envoltura que cambia el contrato de lo envuelto | No — Decorator se evaluó para P-05 y se descartó precisamente por este riesgo |

## 4.2 Evidencia de preservación del comportamiento

- **Los quince casos heredados del Reto 1** corren sin modificar contra el código con los
  cuatro patrones y producen un **diff vacío** contra la salida guardada antes de tocar
  cada patrón: `04-verificacion/salida-rediseñada.txt`.
- **Los tres casos de SC-2** (Reto 1, chips) también dan diff vacío:
  `04-verificacion/salida-sc2.txt`.
- **Cuatro casos nuevos** (19 a 22), uno o dos por patrón, en
  `04-verificacion/salida-patrones.txt` — no comparan contra el Reto 1 porque ejercitan
  comportamiento que el Reto 1 no tenía (SC-1, el registro de avisos, la ausencia de un
  `if` en la validación de `Res`):
  - **19 · Observer** — un aviso nuevo (`AvisoDeAuditoria`) se dispara sin una línea de
    código en `Potrero.cs` ni `GestorReses.cs`.
  - **20 y 21 · Strategy** — vender un producto derivado no toca el inventario; vender una
    res sí lo retira. Mismo `ServicioVenta`, sin un `if` que distinga los dos casos.
  - **22 · Composite** — una res con edad 0 se agrega en memoria pero no se guarda en
    disco; el texto que ve el operario es el mismo `Invalido()` congelado de siempre.
- **32 pruebas xUnit** en `src/Modificado/Pruebas` (`dotnet test`; eran 26 al cierre del
  Reto 1). Las 6 nuevas prueban exactamente las compensaciones de LSP citadas arriba:
  sustitución y tipo de `IEfectoVenta` (2), orden de disparo y contexto de
  `IPublicadorEvento` (2), y corte por la primera regla inválida de
  `ValidadorCompuesto<Res>` (2).
