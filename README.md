# Reto_Hacienda_Modernizacion_2_Patrones

Segunda evolución del sistema de gestión de una hacienda ganadera: del diseño
correcto (SOLID, Reto 1) al diseño robusto (SOLID + patrones, Reto 2).
**Mismo comportamiento observable, salvo las excepciones autorizadas del Anexo B.**

---

## Roles del equipo

**Enlace al video de sustentación:** _pendiente_

| Rol | Integrante | Responde por |
|---|---|---|
| Arquitecto Líder | *(por confirmar)* | Puntos rígidos, selección y descarte de patrones, diseño TO-BE |
| Arquitecto de Verificación | *Juan José Mesa Cardona* | Que SOLID siga en pie y que el comportamiento no cambió |
| Arquitecto de Riesgos y despliegue | *(por confirmar)* | Análisis de riesgo y plan de cambio |
| Arquitecto de comunicación gráfica | *(por confirmar)* | Las dos vistas, la bitácora de decisiones, el documento |

---

## Cómo ejecutar

Requiere **.NET SDK 8.0 o superior**. Todo se ejecuta desde la raíz del repositorio.

### 1 · Compilar el sistema

```bash
cd src/Modificado
dotnet build Hacienda.sln
```

### 2 · Levantar la aplicación web

```bash
cd src/Modificado
dotnet run --project p_mvcHacienda
```

Abrir la URL que imprime la consola. Credenciales de `Datos/Usuarios.txt` — por ejemplo `santi` / `santi11`.

### 3 · Programa principal de demostración

```bash
cd src/Modificado/Demostracion
dotnet run
```

Recorre los escenarios de uso más importantes sin servidor web y sin tocar los datos del repositorio, incluida la solicitud de cambio implementada (SC-1).

### 4 · Evidencia de preservación del comportamiento

```bash
cd src/Modificado/Caracterizacion-Rediseno
dotnet run
```

Deja en `04-verificacion/` tres salidas (no contra el sistema legacy original: en el Reto 2
la línea base es el propio rediseño SOLID que se entregó el trimestre pasado):

- `salida-rediseñada.txt` — los 15 casos del Reto 1, diff vacío contra antes de los patrones.
- `salida-sc2.txt` — los 3 casos de SC-2 (Reto 1), también diff vacío.
- `salida-patrones.txt` — 4 casos nuevos (19–22) que ejercitan Observer, Strategy y
  Composite; sin línea base porque el Reto 1 no tenía estos casos.

Matriz de verificación SOLID completa en [`04-verificacion/Matriz-SOLID.md`](04-verificacion/Matriz-SOLID.md).

### 5 · Pruebas de diseño

```bash
cd src/Modificado/Pruebas
dotnet test
```

---

## Estructura del entregable

| Carpeta | Contenido | Actividad |
|---|---|---|
| `01-Puntos-de-dolor/` | Puntos rígidos del diseño SOLID del Reto 1, medidos en clases y archivos | 1 |
| `02-decision-patrones/` | Tabla de decisión de patrones evaluados/adoptados y bitácora de IA | 2 |
| `03-diseno-tobe/` | Diagramas de lo que sale y lo que entra, tabla de cambio estructural, fichas por patrón | 3 |
| `04-verificacion/` | Matriz SOLID por patrón adoptado, evidencia de preservación del comportamiento | 4 |
| `05-riesgos/` | Registro de riesgos de incorporar los patrones | 5 |
| `06-vistas/` | Vista de negocio y vista técnica | 6 |
| `src/` | Código fuente rediseñado, programa principal y casos de caracterización | 6.2 |
| `entrega/` | Documento de sustentación (PDF, máx. 15 páginas) | 6.1 |

### Dentro de `src/Modificado/`

```
Hacienda.sln
Bib_Hacienda/              DOMINIO · net8.0 · sin dependencias de framework web
  Clases/                    entidades, jerarquías, ProductoDerivado (SC-1)
  Contratos/                 IFabricaRes · IFabricaVacuna · IEfectoVenta · IArticuloVendible · IValidador<T>...
  Estrategias/                EfectoVentaRetiroInventario · EfectoVentaSinEfecto (Strategy, P-02)
  Fabricas/                   IFabricaRes ← FabricaTernero/Cebon/Novillo · IFabricaVacuna ← FabricaVacunaBacteriana/Viva
  Servicios/ Valores/ Eventos/ Reglas/
p_mvcHacienda/             WEB
  Composicion/               RaizComposicion (composition root)
  Infraestructura/ Servicios/ Controllers/ Views/ Datos/
Demostracion/              Programa principal de demostración
Caracterizacion-Rediseno/  Arnés de caracterización
Pruebas/                   xUnit
```

---

## Patrones adoptados (Actividad 2)

Los cuatro patrones que decidió el equipo están **adoptados y verificados** — código,
pruebas y evidencia de comportamiento incluidos.

| Patrón | Familia | Punto de dolor | Estado |
|---|---|---|---|
| Factory Method | Creacional | P-01 · catálogo de vacunas | ✅ Adoptado |
| Strategy | Comportamiento | P-02 · efecto de venta sobre el inventario | ✅ Adoptado — implementa además SC-1 |
| Observer | Comportamiento | P-03 · publicadores de eventos del dominio | ✅ Adoptado |
| Composite | Estructural | P-05 · validadores | ✅ Adoptado — sobre `Res`; los otros cuatro validadores siguen monolíticos (deuda declarada) |

Detalle completo de la decisión, alternativas evaluadas y descartes en
[`02-decision-patrones/Decision-de-Patrones.md`](02-decision-patrones/Decision-de-Patrones.md).

**Solicitud de cambio implementada:** SC-1, venta de productos derivados del ganado
(lácteos, carne, piel), distinta de la SC-2 (chips de geolocalización) que ya
implementó el Reto 1. Aterriza sobre P-02, el punto de mayor costo medido.

### Impacto medido por patrón

Clases creadas frente a archivos modificados, contados sobre los commits reales —no
estimados—, la misma evidencia empírica que pide el criterio 5 de la rúbrica:

| Patrón | Clases nuevas | Archivos modificados | Archivos eliminados |
|---|---|---|---|
| Factory Method (P-01) | 4 | 8 | 0 |
| Strategy (P-02) + SC-1 | 5 (+1 vista) | 12 (+1 vista) | 0 |
| Observer (P-03) | 2 | 15 | 0 |
| Composite (P-05) | 5 | 6 | 1 (`ValidarRes.cs`, el `if` de cuatro condiciones) |

El patrón común: en los cuatro casos, **un tipo nuevo dentro de la familia del patrón
cuesta una clase y una línea en `RaizComposicion.cs`** — cero condicionales nuevos en
`ServicioVenta`, `Potrero`, `GestorReses`, `ServicioVacunacion` ni `GuardadoValidado`.
Es la prueba de OCP que pide la Actividad 4; el Caso 19 de
[`04-verificacion/salida-patrones.txt`](04-verificacion/salida-patrones.txt) lo demuestra
en vivo con un aviso que ninguno de los cinco publicadores del Anexo A conoce.

### Verificación de SOLID (Actividad 4)

- **Matriz de verificación** por patrón × principio, con evidencia y compensación
  declarada para cada tensión encontrada:
  [`04-verificacion/Matriz-SOLID.md`](04-verificacion/Matriz-SOLID.md).
- **Comportamiento preservado**: diff vacío de los 15 casos del Reto 1
  ([`salida-rediseñada.txt`](04-verificacion/salida-rediseñada.txt)) y de los 3 de SC-2
  ([`salida-sc2.txt`](04-verificacion/salida-sc2.txt)) contra el estado justo antes de
  cada patrón.
- **4 casos nuevos** que ejercitan lo que los patrones tocan:
  [`salida-patrones.txt`](04-verificacion/salida-patrones.txt).
- **32 pruebas xUnit** (`cd src/Modificado/Pruebas && dotnet test`), 6 nacidas de este
  reto y ligadas una a una a las compensaciones de LSP de la matriz.
