# Vista para el negocio

## De qué sistema hablamos

Es el programa con el que la hacienda lleva el día a día del ganado. El personal entra desde el
navegador con su usuario y su clave, y ahí registra los potreros —cada uno admite hasta 150
animales—, los animales que hay en cada potrero con su nombre, peso y edad, las vacunas que se
les aplican y las ventas. Toda la información queda guardada en archivos dentro del propio
servidor de la finca: no hay base de datos ni servicios de terceros.

Lo que el programa aporta, más allá de guardar datos, es que conoce las reglas de la hacienda y
avisa solo. Sabe que un animal pasa por tres etapas —ternero hasta los doce meses, después cebón
y novillo—, que cada etapa tiene un peso mínimo saludable y un peso recomendado para vender, y
cuántas vacunas de cada tipo admite un animal según la etapa en la que esté. Con eso le avisa al
operario cuando un potrero va por la mitad, cuando se llena, cuando un animal está por debajo de
su peso saludable, cuando alcanza el peso de venta, cuando termina su plan de vacunación y
cuando una vacuna se vence. El trimestre pasado se le añadió además el seguimiento por chip de
localización de cada animal.

## Qué le vamos a hacer al sistema y qué no cambia

Cambiamos cómo está armado el programa por dentro. Por fuera no cambia nada: los mismos
mensajes, las mismas pantallas, los mismos datos guardados y las mismas reglas de la hacienda.
Quien usa el sistema todos los días no va a notar la diferencia, y eso lo comprobamos
ejecutando el programa antes y después y comparando lo que imprime, línea por línea.

Lo único nuevo es lo que el negocio pidió y autorizó: la hacienda ya puede vender leche, carne
y piel, no solo el animal completo.

## Dónde se está yendo hoy el tiempo y el dinero

El costo no está en escribir lo que se pide. Está en encontrar todos los sitios donde hay que
escribirlo.

Hoy, dar de alta un tipo de vacuna nuevo obliga a revisar **catorce puntos distintos** del
programa. Ninguno de ellos avisa si alguien se olvida de otro: el programa se pone en marcha
igual, y la falla aparece semanas después, cuando alguien nota que un animal nunca apareció en
un listado. Vender un producto que no fuera el animal completo costaba otros catorce, y por eso
esa petición llevaba tiempo sin poder atenderse.

## Qué gana el negocio

| | Antes | Después |
|---|---|---|
| Un tipo de vacuna nuevo | 14 puntos que revisar | 1 |
| Vender un producto nuevo | 14 puntos que revisar | 1 |
| Una regla nueva sobre los animales | Reescribir una regla existente | 1 pieza aparte |
| Un aviso nuevo para el operario | 3 sitios del programa | 1 |

Dos ganancias, y la segunda importa más que la primera. La obvia es que el trabajo se acorta.
La que cuenta es que **el sitio donde hay que tocar es siempre el mismo y está señalado**: hoy
una petición así no se puede prometer para una fecha, porque nadie sabe si va a aparecer un
punto olvidado; a partir de ahora sí se puede.

## Qué cuesta

Tres cosas, y las declaramos de frente:

El programa tiene ahora más piezas, cada una más pequeña. Es más fácil cambiar una, y hay más
que conocer al llegar.

Seguir la pista de un error cuesta un paso más: hay que mirar dónde ocurre y también dónde se
decidió que ocurriera, que ahora son sitios distintos.

Todo el armado pasa por un único punto del programa. Es la razón de que los cambios sean
baratos, y también un sitio que hay que vigilar: si crece sin control, se convierte en el
próximo cuello de botella.

## Qué riesgos hay

El más caro: **la forma en que el programa guarda la información en disco no se tocó**, porque
cambiarla obligaba a reescribir los datos históricos de la hacienda. Mientras siga así, si
alguien añade un dato nuevo a una ficha sin el cuidado debido, se pueden perder registros
viejos sin que el programa se queje. Sabemos cómo enterarnos: los archivos de datos quedan con
menos líneas que antes, y eso se revisa en cada cambio.

Hay **cinco fallas menores que el cliente ya conoce** y que se conservan tal cual, porque
corregirlas cambiaría lo que el operario ve y eso no está autorizado. Dejamos pruebas
automáticas que avisan si alguien las corrige por su cuenta.

Y un riesgo silencioso: **un aviso mal conectado deja de aparecer sin que el programa falle**.
No da error, simplemente calla. Por eso el orden y la conexión de los avisos quedaron por
escrito y se revisan en cada entrega.

## Qué necesitamos del negocio

Poco, y nada costoso: que la autorización para vender productos derivados siga en pie, y que no
entre otra petición mientras dura el cambio. Si en algún momento se quiere corregir alguna de
las cinco fallas conocidas, hace falta una autorización expresa, porque cambia lo que el
operario ve en pantalla.

## Qué pasa si no se hace

La petición de vender productos derivados no se podía atender: no era una cuestión de tiempo,
era que el programa daba por hecho que lo único vendible era el animal entero. La siguiente
petición que llegue costará lo mismo que costaba ésta, y el riesgo de perder registros
históricos crece con cada dato nuevo que se agregue.

## Qué entendió una persona ajena al equipo

> **PENDIENTE** · Presentar esta vista a una persona sin formación técnica, anotar aquí con sus
> palabras qué entendió que se le va a hacer al sistema, qué gana la hacienda y qué riesgo
> corre. Grabarlo para el video.
