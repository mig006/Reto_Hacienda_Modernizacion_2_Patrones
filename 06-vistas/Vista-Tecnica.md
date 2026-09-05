# Vista para el equipo de desarrollo

Documento de consulta, no de lectura corrida. Si tienes que hacer un cambio, busca en la tabla
la fila que se parece al tuyo y sigue las tres columnas.

## La guía de dónde tocar

| Tu cambio | Crear | Modificar | No tocar |
|---|---|---|---|
| **Un tipo de vacuna nuevo** | 1 clase que implemente `IFabricaVacuna` | 1 línea en `RaizComposicion` | `FabricaVacunas`, `VacunaService`, `VacunaController`, `MapeadorVacuna` |
| **Un tipo de res nuevo** | 1 subclase de `Res` + 1 clase `IFabricaRes` | 1 línea en `RaizComposicion`; el diccionario de `ResService` y 2 vistas, que son presentación | El resto del dominio |
| **Vender un artículo nuevo** (leche, carne y piel ya están) | 1 clase `IArticuloVendible`; si su efecto sobre el inventario difiere de los dos que hay, 1 clase `IEfectoVenta` | 1 línea en `RaizComposicion` por cada una | `ServicioVenta` |
| **Un campo nuevo en algo que se guarda en disco** | — | El mapeador y el repositorio de esa entidad, en `p_mvcHacienda/Infraestructura/` | El orden de las columnas que ya existen |
| **Un agregado nuevo** (historia clínica, por ejemplo) | 1 clase de dominio, sus reglas `IValidador<T>`, 1 repositorio | `GuardadoValidado` y su constructor, que hace recompilar 3 servicios | Los 4 validadores que ya existen |
| **Una regla de validación nueva** | 1 clase que implemente `IValidador<T>` | 1 línea en el arreglo del `ValidadorCompuesto` correspondiente | El validador existente |
| **Un aviso nuevo** | 1 clase que implemente `IPublicadorEvento` | 1 línea en la lista de avisos que corresponda | `Potrero`, `GestorReses`, `ServicioVacunacion` |

Las tres solicitudes previstas caen en la tercera fila (vender derivados), la cuarta (chips de
geolocalización, ya implementada: añadió 4 columnas al final de la línea) y la quinta (historia
clínica).

## Dónde se ensambla el sistema

`p_mvcHacienda/Composicion/RaizComposicion.cs`, 241 líneas. Es el **único** lugar donde se elige
qué implementación satisface cada contrato, y por eso casi todas las filas de arriba dicen
«1 línea»: es esa.

La regla que lo sostiene: ninguna clase fuera de `p_mvcHacienda/Composicion/` recibe
`IServiceProvider` ni pide instancias en ejecución; todo entra por constructor. Si te ves
pidiéndole una instancia al contenedor desde un servicio, estás reintroduciendo el problema que
este diseño eliminó.

## Los cuatro patrones: dónde vive cada uno

| Patrón | Contrato | Implementaciones | Quién decide cuál se usa |
|---|---|---|---|
| Factory Method | `IFabricaVacuna` | `FabricaVacunaBacteriana`, `FabricaVacunaViva` | `FabricaVacunas`, por el tipo que llega del controlador |
| Strategy | `IEfectoVenta` | `EfectoVentaRetiroInventario`, `EfectoVentaSinEfecto` | `ServicioVenta`, buscando la que soporta el tipo del artículo |
| Observer | `IPublicadorEvento` + `ContextoAviso` | las cinco clases de `Bib_Hacienda/Eventos/` | El orden de la lista registrada en la raíz |
| Composite | `IValidador<T>` | `ValidadorCompuesto<T>` agrupando las reglas | La composición escrita en la raíz |

No se relacionan entre sí: se relacionan con el mismo sitio. Los cuatro trasladaron a la raíz de
composición una decisión que antes era un condicional dentro de un método, y por eso un tipo
nuevo en cualquiera de las cuatro familias cuesta una clase y una línea.

## Reglas que no se rompen, y por qué

1. **La salida observable está congelada**: texto de los mensajes, tipo de alerta, navegación y
   formato de los archivos de datos. Es restricción del cliente. Se verifica corriendo el arnés
   de `Caracterizacion-Rediseno`; el diff contra `04-verificacion/` debe salir vacío.
2. **El orden de los avisos es salida observable**: mitad, lleno, peso mínimo, peso de venta. Lo
   produce el orden de la lista en la raíz y ningún código lo protege.
3. **Cada aviso va solo en la lista cuyo contexto llena los campos que ese aviso lee.**
   `PublisherPotreroMitad` lee la cantidad de reses y el potrero: en la lista de alimentación,
   donde van vacíos, callaría sin error.
4. **Las columnas nuevas se anexan al final de la línea, nunca intercaladas**, o se reescribe el
   histórico del cliente. `MapeadorRes` pasó de 5 a 9 columnas así.
5. **Los cinco defectos de `PruebasDeDeudaCongelada` se conservan.** Esas pruebas afirman que el
   sistema se comporta mal y pasan porque se comporta mal. Si una falla, cambiaste algo que el
   operario ve.
6. **Nada de frameworks que resuelvan el problema**: el generador de proxies del sistema original
   se desmontó a propósito.

## Deuda que queda declarada

- El catálogo de tipos de potrero se guarda literalmente en disco: eliminarlo obligaría a
  reescribir el archivo de datos del cliente.
- La persistencia lee por posición de columna en 4 mapeadores y 4 repositorios, y una línea corta
  se descarta sin lanzar excepción. Es el riesgo de mayor exposición del registro.
- `GuardadoValidado` conserva 7 dependencias y 5 métodos casi idénticos: se anunció unificarlos
  con un método genérico y no se hizo.
- El agrupamiento de reglas se aplicó solo a `Res`. `ValidadorPotrero`, `ValidadorVacuna` y
  `ValidadorVenta` siguen resolviendo todo en un condicional único, así que en ellos la fila
  «una regla nueva» todavía no es cierta.
- `PublisherVacunaVencida` queda fuera del patrón a propósito: devuelve un booleano del que
  depende si se lanza una excepción, así que es guarda de flujo y no aviso.
