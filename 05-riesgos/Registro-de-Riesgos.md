# Actividad 5 — Análisis de riesgos

Riesgos de incorporar los cuatro patrones: los de ejecutar el cambio y los de convivir con el
diseño resultante. Cada uno se ancla al archivo donde vive, y la consecuencia está escrita en
lenguaje de operación porque quien decide sobre estos riesgos no lee código.

**Cómo puntuamos.** La escala se fija antes de puntuar, para que la nota sea defendible y no
una impresión.

*Probabilidad* — **1** exige violar a propósito una regla escrita · **2** solo si alguien
nuevo toca sin leer la vista técnica · **3** ocurre en el próximo cambio ordinario ·
**4** ya ocurrió una vez en este repositorio · **5** está ocurriendo hoy.

*Impacto* — **1** molestia local · **2** retrabajo de un archivo · **3** cambia una salida
observable, que está congelada por el encargo · **4** pérdida de datos recuperable ·
**5** pérdida silenciosa del histórico del cliente.

*Exposición* = Probabilidad × Impacto. La tabla va ordenada por exposición descendente.

## 5.1 Registro de riesgos

| ID | Riesgo (si ocurre X, entonces Y) | Prob | Imp | Exp | Qué hacemos para evitarlo | Cómo nos enteramos de que está pasando |
|---|---|---|---|---|---|---|
| **R-01** | Si alguien añade un campo a una entidad que se guarda en disco, entonces una línea mal formada se descarta sin error y el histórico del cliente pierde registros sin que nadie lo note | 3 | 5 | **15** | P-06 queda declarado fuera de alcance. Todo cambio en `Infraestructura/` obliga a correr el arnés de caracterización antes de integrarlo, y a anexar la columna nueva al final de la línea, nunca intercalada | Los archivos de datos que deja el arnés en `04-verificacion/datos-rediseñado/` traen menos líneas que la corrida anterior |
| **R-02** | Si alguien corrige uno de los cinco defectos que el cliente ya conoce y que se conservan a propósito, entonces la pantalla del operario cambia sin autorización | 3 | 3 | **9** | Los cinco defectos están fijados por pruebas escritas al revés: afirman que el sistema se comporta mal. Quien las vea fallar debe leer la decisión que las sostiene antes de tocar nada | Falla una de las cinco pruebas de `Pruebas/PruebasDeDeudaCongelada.cs` |
| **R-03** | Si se agrega un artículo vendible nuevo y se olvida registrar qué le pasa al inventario al venderlo, entonces la venta no falla al compilar: falla delante del operario la primera vez que la intenta | 3 | 3 | **9** | El artículo nuevo y su efecto de inventario entran en el mismo cambio. La guía de dónde tocar de la vista técnica lo declara como una sola fila, no dos | El operario recibe «Error inesperado en el método vender_producto» al vender el artículo nuevo |
| **R-04** | Si el diseño publicado nombra elementos que el código no tiene, entonces quien entre al equipo abre el archivo equivocado y el cambio cuesta el doble | 4 | 2 | **8** | La vista técnica se redacta leyendo el código, no los diagramas. Antes de publicar el documento se coteja nombre por nombre lo dibujado contra `src/` | Un nombre que aparece en el diseño publicado no devuelve resultados al buscarlo en `src/` |
| **R-05** | Si un aviso se registra en la lista equivocada al ensamblar el sistema, entonces el operario deja de recibir esa alerta y nadie lo nota, porque el sistema no falla: simplemente calla | 2 | 3 | **6** | Cada aviso se registra solo en la lista cuyo contexto llena los datos que ese aviso lee. La restricción está escrita junto al registro y en la matriz de verificación | El aviso desaparece de `04-verificacion/salida-patrones.txt`: el caso 19 reporta menos avisos que antes |
| **R-06** | Si se reordenan los avisos en el punto donde se ensambla el sistema, entonces los mensajes le llegan al operario en otro orden, y ese orden es parte de lo que el cliente ya da por hecho | 2 | 3 | **6** | El orden —mitad, lleno, peso mínimo, peso de venta— es el de la colección registrada, está documentado ahí mismo y no se toca sin autorización | `git diff 04-verificacion/salida-rediseñada.txt` deja de salir vacío tras correr el arnés |

## Evidencia

**R-01.** `MapeadorRes.cs:103` descarta la línea con `return false` cuando trae menos columnas
de las esperadas: no lanza excepción ni deja rastro, y son 4 mapeadores y 4 repositorios con
la misma forma. Se suma que `GuardadoValidado` conserva 7 dependencias y 5 métodos casi
idénticos, así que un agregado nuevo cambia la firma del constructor y recompila tres
servicios. Es P-06 completo más la mitad de P-05 que esta entrega no cerró: deuda medida, no
olvido.

**R-02.** `PruebasDeDeudaCongelada.cs` fija D-1 a D-5. Dos de ellos —un alta correcta de
usuario que se reporta como error, un lote duplicado que se reporta como éxito— se leen como
errores evidentes, así que el riesgo de que alguien los «arregle» de buena fe no es teórico.

**R-03.** `ServicioVenta.cs:103` resuelve la estrategia con
`_efectos.First(e => e.TipoSoportado.IsAssignableFrom(tipoArticulo))`. Si ningún efecto
registrado corresponde al artículo, `First` lanza en ejecución. Es el precio declarado de
mover la decisión fuera del método: el compilador ya no la vigila.

**R-04.** Se puntúa en 4 porque ya se materializó **dos veces** durante esta entrega, y las dos
se detectaron con la misma señal que aparece en la tabla. La segunda es la más ilustrativa: el
Composite entró en el código el 4 de septiembre de 2026 a las 23:14 (commit `d809d6d`) y el
diagrama definitivo se publicó 21 horas después (commit `2bd8a36`) con una nota que afirmaba
«en el código actual no existe: no hay `ValidadorCompuesto` ni reglas atómicas por clase».
Dibujaba `ValidadorRes`, clase que el propio commit `d809d6d` había eliminado. La divergencia
no vino de un descuido de redacción sino del desfase entre dos personas trabajando sobre el
mismo entregable en días distintos, que es exactamente el mecanismo que describe la fila.
La corrección es previa a publicar el documento.

**R-05.** `PublisherPotreroMitad.cs` lee del contexto la cantidad de reses y el potrero. En la
lista de avisos de alimentación esos datos van vacíos, así que la condición numérica nunca se
cumple y el aviso calla sin error. La compensación está declarada en la fila LSP de Observer
de `04-verificacion/Matriz-SOLID.md`.

**R-06.** El orden de disparo quedó documentado como salida observable desde el reto anterior y
se reproduce iterando la colección registrada en `RaizComposicion.cs`. No hay código de
ordenamiento que lo proteja: lo protege el orden de esas cuatro líneas.

## 5.2 Plan de cambio

**Un patrón por entrega, en orden de riesgo creciente.** Se adoptaron en cuatro pasos
independientes, no en un cambio único:

1. **Catálogo de vacunas** — primero: replica un mecanismo que el sistema ya tenía funcionando
   para el ganado. No se inventa nada, se repite lo probado.
2. **Efecto de venta** — segundo: trae la solicitud autorizada, y llega cuando el equipo ya
   practicó con el primero.
3. **Avisos del dominio** — tercero: toca muchos archivos pero no agrega comportamiento, así
   que su verificación es un diff que debe salir vacío.
4. **Validación por reglas** — último: es el único que elimina un archivo existente.

**Puerta de verificación entre paso y paso.** Ninguno se integra sin las tres comprobaciones:
pruebas en verde, arnés de caracterización corrido y diff de las salidas guardadas saliendo
vacío. Si el diff no sale vacío, el paso no entra.

**Cómo se revierte.** Cada patrón es un cambio aislado en la historia del repositorio, así que
deshacer uno no arrastra a los otros tres. Esa es la razón de no haberlos integrado juntos.

**Qué necesitamos del negocio.** Que la autorización de vender productos derivados siga en pie
y que ninguna otra solicitud entre mientras dure la adopción. El resto del sistema queda igual.
