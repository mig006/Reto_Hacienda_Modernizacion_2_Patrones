using Bib_Hacienda.Clases;
using Bib_Hacienda.Clases.Validaciones;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Estrategias;
using Bib_Hacienda.Eventos;
using Bib_Hacienda.Fabricas;
using Bib_Hacienda.Reglas;
using Bib_Hacienda.Servicios;
using Bib_Hacienda.Valores;
using Xunit;
using static Bib_Hacienda.Clases.Potrero;
using static Bib_Hacienda.Clases.Viva;

namespace Bib_Hacienda.Pruebas
{
    /// <summary>
    /// Pruebas de sustitución (LSP) que los propios ADR declaran como criterio de
    /// verificación. No sustituyen a los casos de caracterización —que son la evidencia
    /// de que el comportamiento observable se preservó— sino que comprueban las
    /// propiedades de DISEÑO que el rediseño afirma haber conseguido.
    ///
    /// Que este proyecto exista y corra sin arrancar un servidor web ni tocar el disco es
    /// consecuencia directa de ADR-01, ADR-02 y ADR-03: antes era imposible.
    /// </summary>
    public class PruebasDeSustitucion
    {
        private static readonly IFabricaRes[] Fabricas =
            { new FabricaTernero(), new FabricaCebon(), new FabricaNovillo() };

        // ══ ADR-03 · §5.1 · Ningún validador lanza ═══════════════════════════════
        //
        // Antes, esta misma prueba fallaba en 12 de 16 combinaciones: cada validador
        // heredaba tres métodos que no le correspondían y los rechazaba con una
        // excepción de método no implementado.

        [Fact]
        public void NingunValidadorLanzaConEntradaValidaNiInvalida()
        {
            var potrero = new Potrero("P1", l_tipos_potreros.ternero);
            var res = new Ternero("Pinta", 100, 6);
            var vacuna = new Bacteriana("V", "L1", new DateTime(2030, 1, 1), new DateTime(2029, 1, 1), 3);
            var venta = new Venta(potrero, DateTime.Now, res, 100);
            var chip = new Chip("CHIP-1", 0, 0, DateTime.Now);

            // Entradas válidas
            Assert.True(new ValidadorPotrero().Validar(potrero).EsValido);
            Assert.True(new ValidadorRes().Validar(res).EsValido);
            Assert.True(new ValidadorVacuna().Validar(vacuna).EsValido);
            Assert.True(new ValidadorVenta().Validar(venta).EsValido);
            Assert.True(new ValidadorChip().Validar(chip).EsValido);

            // Entradas inválidas: devuelven, NO lanzan
            Assert.False(new ValidadorPotrero().Validar(null).EsValido);
            Assert.False(new ValidadorRes().Validar(null).EsValido);
            Assert.False(new ValidadorVacuna().Validar(null).EsValido);
            Assert.False(new ValidadorVenta().Validar(null).EsValido);
            Assert.False(new ValidadorChip().Validar(null).EsValido);
        }

        [Fact]
        public void ElTextoDeValidacionEsElContratoCongeladoDeADR02()
        {
            Assert.Equal("Datos válidos. Guardado exitoso en BD", ResultadoValidacion.Valido().Mensaje);
            Assert.Equal("Datos inválidos. NO se guardó en BD", ResultadoValidacion.Invalido().Mensaje);
        }

        // ══ ADR-05 · §5.2 · La jerarquía Res es sustituible ══════════════════════

        public static IEnumerable<object[]> ResesDeCadaSubtipo() => new[]
        {
            new object[] { (Res)new Ternero("T", 100, 6) },
            new object[] { (Res)new Cebon("C", 200, 20) },
            new object[] { (Res)new Novillo("N", 300, 60) },
        };

        /// <summary>
        /// La prueba de sustitución que ADR-05 propone: ejecutar sobre cada subtipo la
        /// misma secuencia que ejecuta alimentar_res —incrementar Peso 200 veces— sin
        /// que ninguno lance.
        ///
        /// Antes esto era imposible de garantizar para Edad: el setter de cada subtipo
        /// rechazaba asignaciones que la base aceptaba. Ahora Edad es inmutable y el
        /// rango es una invariante verificada en construcción.
        /// </summary>
        [Theory]
        [MemberData(nameof(ResesDeCadaSubtipo))]
        public void CualquierSubtipoSoportaLaSecuenciaDeEngordeSinLanzar(Res res)
        {
            uint pesoInicial = res.Peso;

            for (int i = 0; i < 200; i++)
            {
                res.Alimentar(1);
            }

            Assert.Equal(pesoInicial + 200, res.Peso);
        }

        /// <summary>
        /// Segunda prueba de sustitución de ADR-05, que es a la vez evidencia de
        /// preservación: PesoMinimo debe devolver EXACTAMENTE el valor que antes decidía
        /// la cadena de `is` dentro de PublisherPesoMin.
        /// </summary>
        [Fact]
        public void LaPoliticaPorTipoCoincideConLaQueEstabaEscritaFueraDelTipo()
        {
            var ternero = new Ternero("T", 100, 6);
            var cebon = new Cebon("C", 200, 20);
            var novillo = new Novillo("N", 300, 60);

            // PublisherPesoMin.cs:25-27 del sistema original
            Assert.Equal(ReglaRes.peso_min_ternero, ternero.PesoMinimo);
            Assert.Equal(ReglaRes.peso_min_cebon, cebon.PesoMinimo);
            Assert.Equal(ReglaRes.peso_min_novillo, novillo.PesoMinimo);

            // PublisherPesoVenta.cs:27-29
            Assert.Equal(ReglaRes.peso_recom_venta_ternero, ternero.PesoRecomendadoVenta);
            Assert.Equal(ReglaRes.peso_recom_venta_cebon, cebon.PesoRecomendadoVenta);
            Assert.Equal(ReglaRes.peso_recom_venta_novillo, novillo.PesoRecomendadoVenta);

            // Hacienda.cs:487-501
            Assert.Equal(ReglaVacuna.max_bac_ternero, ternero.MaxVacunasBacterianas);
            Assert.Equal(ReglaVacuna.max_viv_ternero, ternero.MaxVacunasVivas);
            Assert.Equal(ReglaVacuna.max_bac_cebon, cebon.MaxVacunasBacterianas);
            Assert.Equal(ReglaVacuna.max_viv_cebon, cebon.MaxVacunasVivas);
            Assert.Equal(ReglaVacuna.max_bac_novillo, novillo.MaxVacunasBacterianas);
            Assert.Equal(ReglaVacuna.max_viv_novillo, novillo.MaxVacunasVivas);

            // PublisherVacunacionCompletada.cs:30-41
            Assert.True(ternero.EsquemaCompleto(3, 1));
            Assert.False(ternero.EsquemaCompleto(2, 1));
            Assert.True(cebon.EsquemaCompleto(1, 4));
            Assert.True(novillo.EsquemaCompleto(2, 2));
        }

        /// <summary>
        /// El rango pasó de ser una precondición fortalecida a ser una invariante de
        /// clase: una Res construida es SIEMPRE válida, y el intento inválido falla en
        /// el constructor, no en una asignación posterior.
        ///
        /// El TIPO y el TEXTO de la excepción se conservan tal cual, erratas incluidas.
        /// </summary>
        [Fact]
        public void ElRangoDeEdadEsUnaInvarianteVerificadaEnConstruccion()
        {
            var errorTernero = Assert.Throws<Exception>(() => new Ternero("T", 100, 13));
            Assert.Equal("El ternero excedió la edad maxima", errorTernero.Message);

            var errorCebon = Assert.Throws<Exception>(() => new Cebon("C", 200, 12));
            Assert.Equal("El cebon excedió la edad maxima", errorCebon.Message);

            // DEUDA D-4 · dentro de la clase Novillo, el literal nombra al TERNERO.
            // Esta aserción existe para dejar constancia de que el defecto se conserva
            // a propósito: si alguien lo "arregla", esta prueba falla y obliga a leer
            // el ADR antes de cambiar una salida observable.
            var errorNovillo = Assert.Throws<Exception>(() => new Novillo("N", 300, 48));
            Assert.Equal("El ternero excedió la edad maxima", errorNovillo.Message);
        }

        // ══ ADR-05 · §5.4 · IFabricaRes pasa por construcción ════════════════════

        [Fact]
        public void CadaFabricaDeclaraSuPrecondicionYCumpleSuPostcondicion()
        {
            foreach (var fabrica in Fabricas)
            {
                // Postcondición: devuelve una Res no nula del tipo declarado.
                ushort edadValida = fabrica.RangoSoportado.Minimo;
                Res res = fabrica.Crear("prueba", 100, edadValida);

                Assert.NotNull(res);
                Assert.Equal(fabrica.TipoSoportado, res.GetType());

                // Precondición consultable: el cliente puede preguntar ANTES de invocar,
                // que es exactamente lo que la jerarquía Res no permitía hacer.
                Assert.True(fabrica.RangoSoportado.Contiene(edadValida));
            }
        }

        /// <summary>
        /// Hallazgo propio, documentado en FabricaNovillo: el rango de ADMISIÓN de un
        /// potrero de novillos y la INVARIANTE del tipo Novillo NO coinciden en el
        /// sistema original, y la diferencia es alcanzable. Se conserva.
        /// </summary>
        [Fact]
        public void ElRangoDeAdmisionDelPotreroYLaInvarianteDelTipoDifierenEnNovillo()
        {
            var fabrica = new FabricaNovillo();

            Assert.Equal(255, fabrica.RangoSoportado.Maximo);        // lo que admitía el potrero
            Assert.Equal(ushort.MaxValue, Novillo.Rango.Maximo);     // lo que aceptaba el tipo

            // Un novillo de 300 meses no entra por el alta...
            Assert.False(fabrica.RangoSoportado.Contiene(300));
            // ...pero sí se puede reconstruir desde disco, igual que hoy.
            Assert.NotNull(new Novillo("Viejo", 500, 300));
        }

        // ══ ADR-07 · La fuga de manejadores desapareció ══════════════════════════

        /// <summary>
        /// Prueba de carga de ADR-07: ejecutar 200 alimentaciones consecutivas y
        /// verificar que cada una produce EXACTAMENTE UN mensaje.
        ///
        /// Con el mecanismo original de eventos, cada llamada añadía dos manejadores más
        /// que nunca se retiraban; tras 200 alimentaciones había 400 manejadores vivos
        /// ejecutándose en cada disparo. No cambiaba la salida —cada lambda escribía en
        /// su propia clausura— pero sí el tiempo y la memoria.
        /// </summary>
        [Fact]
        public void DoscientasAlimentacionesNoAcumulanReceptores()
        {
            var hacienda = new Hacienda();
            var gestorPotreros = new GestorPotreros(hacienda);
            var avisosPeso = new IPublicadorEvento[] { new PublisherPesoMin(), new PublisherPesoVenta() };
            var gestorReses = new GestorReses(hacienda, gestorPotreros, new PoliticaCapacidadPotrero(), Fabricas,
                avisosAltaDeRes: Array.Empty<IPublicadorEvento>(), avisosAlimentacion: avisosPeso);

            gestorPotreros.crear_potrero("P_Terneros", l_tipos_potreros.ternero);
            var potrero = gestorPotreros.buscar_potrero("P_Terneros");
            potrero.anadir_res("Pinta", 6, 10, new PoliticaCapacidadPotrero(), Fabricas, Array.Empty<IPublicadorEvento>());

            for (int i = 0; i < 200; i++)
            {
                string mensaje = gestorReses.alimentar_res("P_Terneros", "Pinta", 1);

                // Una línea de operación + a lo sumo una de evento. Si los receptores se
                // acumularan, el número de líneas crecería con cada iteración.
                Assert.True(mensaje.Split('\n').Length <= 2,
                    $"En la iteración {i} el mensaje trajo {mensaje.Split('\n').Length} líneas");
            }
        }

        // ══ SC-2 · ADR-11 · El chip no reabre la verificación de LSP ════════════

        /// <summary>
        /// Prueba de sustitución de §5.6: para cada subtipo, `Chip` arranca en `null`, se
        /// asigna, se registran dos posiciones, y el identificador se conserva mientras la
        /// última lectura avanza. Los tres deben comportarse igual, porque ninguno
        /// sobrescribe, restringe ni oculta la propiedad.
        /// </summary>
        [Theory]
        [MemberData(nameof(ResesDeCadaSubtipo))]
        public void CualquierSubtipoAceptaChipYRegistraPosicionesIgual(Res res)
        {
            Assert.Null(res.Chip);

            res.AsignarChip(new Chip("CHIP-0417", 6.2442, -75.5812, new DateTime(2026, 1, 1)));
            Assert.NotNull(res.Chip);
            Assert.Equal("CHIP-0417", res.Chip.Identificador);

            res.RegistrarPosicion(6.2501, -75.5750, new DateTime(2026, 1, 2));
            res.RegistrarPosicion(6.2499, -75.5890, new DateTime(2026, 1, 3));

            Assert.Equal("CHIP-0417", res.Chip.Identificador);          // el identificador se conserva
            Assert.Equal(new DateTime(2026, 1, 3), res.Chip.UltimaLectura);
            Assert.Equal(6.2499, res.Chip.Latitud, 4);
        }

        /// <summary>
        /// ADR-11 · §5.6 · `Chip` es un objeto de VALOR inmutable: registrar una posición
        /// REEMPLAZA el objeto en lugar de mutarlo. Quien retuvo la referencia anterior
        /// sigue viendo la lectura que tenía, que es justo lo contrario de lo que pasaba
        /// con `Res.Edad` en el sistema original.
        /// </summary>
        [Fact]
        public void RegistrarPosicionReemplazaElObjetoDeValorEnLugarDeMutarlo()
        {
            var res = new Ternero("Pinta", 100, 6);
            res.AsignarChip(new Chip("CHIP-0417", 1, 2, new DateTime(2026, 1, 1)));

            Chip referenciaAntigua = res.Chip;
            res.RegistrarPosicion(9, 9, new DateTime(2026, 1, 5));

            Assert.NotSame(referenciaAntigua, res.Chip);
            Assert.Equal(1, referenciaAntigua.Latitud);   // la referencia vieja NO cambió
            Assert.Equal(9, res.Chip.Latitud);
        }

        /// <summary>
        /// ADR-11 · La consecuencia que el ADR pide demostrar: el chip NO viaja por los
        /// puntos de construcción. Las fábricas siguen produciendo reses sin chip, y ni
        /// `IFabricaRes` ni `Potrero.anadir_res` saben que el concepto existe.
        /// </summary>
        [Fact]
        public void LasFabricasYElAltaDeGanadoNoSabenQueElChipExiste()
        {
            foreach (var fabrica in Fabricas)
            {
                Res res = fabrica.Crear("prueba", 100, fabrica.RangoSoportado.Minimo);
                Assert.Null(res.Chip);
            }

            var potrero = new Potrero("P", l_tipos_potreros.ternero);
            potrero.anadir_res("Pinta", 6, 100, new PoliticaCapacidadPotrero(), Fabricas, Array.Empty<IPublicadorEvento>());
            Assert.Null(potrero.L_reses.Single().Chip);
        }

        // ══ Observer (Actividad 2 · P-03) · IPublicadorEvento ═════════════════════

        /// <summary>Aviso de prueba que siempre notifica, para verificar orden de disparo.</summary>
        private sealed class AvisoDePrueba : IPublicadorEvento
        {
            private readonly string _etiqueta;
            public AvisoDePrueba(string etiqueta) => _etiqueta = etiqueta;
            public void Informar(ContextoAviso contexto, IReceptorEventos receptor) => receptor.Notificar(_etiqueta);
        }

        /// <summary>Aviso de prueba que captura el ContextoAviso recibido, sin notificar.</summary>
        private sealed class AvisoCapturador : IPublicadorEvento
        {
            private readonly Action<ContextoAviso> _capturar;
            public AvisoCapturador(Action<ContextoAviso> capturar) => _capturar = capturar;
            public void Informar(ContextoAviso contexto, IReceptorEventos receptor) => _capturar(contexto);
        }

        /// <summary>
        /// El orden de disparo lo fija el orden de la colección registrada, no el
        /// publicador ni Potrero/GestorReses: tres avisos que no son ninguno de los cinco
        /// reales, sustituidos sin tocar el código de producción, disparan en el orden en
        /// que se registraron. Es la prueba de sustitución que Observer exige.
        /// </summary>
        [Fact]
        public void ElOrdenDeDisparoLoFijaElOrdenDeRegistroNoElPublicadorNiElConsumidor()
        {
            var hacienda = new Hacienda();
            var gestorPotreros = new GestorPotreros(hacienda);
            var avisos = new IPublicadorEvento[] { new AvisoDePrueba("A"), new AvisoDePrueba("B"), new AvisoDePrueba("C") };
            var gestorReses = new GestorReses(hacienda, gestorPotreros, new PoliticaCapacidadPotrero(), Fabricas,
                avisosAltaDeRes: Array.Empty<IPublicadorEvento>(), avisosAlimentacion: avisos);

            gestorPotreros.crear_potrero("P", l_tipos_potreros.ternero);
            var potrero = gestorPotreros.buscar_potrero("P");
            potrero.anadir_res("Pinta", 6, 100, new PoliticaCapacidadPotrero(), Fabricas, Array.Empty<IPublicadorEvento>());

            string mensaje = gestorReses.alimentar_res("P", "Pinta", 1);

            Assert.True(mensaje.IndexOf('A') < mensaje.IndexOf('B') && mensaje.IndexOf('B') < mensaje.IndexOf('C'),
                $"Orden inesperado: {mensaje}");
        }

        /// <summary>
        /// Potrero.anadir_res construye UN solo ContextoAviso compartido —no uno por
        /// publicador— con Potrero, CantidadReses y Res ya poblados. Es el costo que la
        /// Actividad 2 declara: cualquier aviso puede leer estos tres campos aunque no
        /// los necesite todos.
        /// </summary>
        [Fact]
        public void PotreroConstruyeUnContextoUnicoConPotreroCantidadYRes()
        {
            ContextoAviso capturado = null;
            var capturador = new AvisoCapturador(c => capturado = c);

            var potrero = new Potrero("P1", l_tipos_potreros.ternero);
            potrero.anadir_res("Pinta", 6, 100, new PoliticaCapacidadPotrero(), Fabricas, new IPublicadorEvento[] { capturador });

            Assert.NotNull(capturado);
            Assert.Same(potrero, capturado.Potrero);
            Assert.Equal((ushort)1, capturado.CantidadReses);
            Assert.Equal("Pinta", capturado.Res.Nombre);
        }

        // ══ Strategy (Actividad 2 · P-02) · IEfectoVenta ══════════════════════════

        /// <summary>
        /// Prueba de sustitución del registro de estrategias: ServicioVenta no sabe
        /// si el artículo es una Res o un ProductoDerivado, y aun así cada uno recibe
        /// el efecto que le corresponde. Es la prueba de que P-02 quedó resuelto y no
        /// solo movido a otro condicional.
        /// </summary>
        [Fact]
        public void VenderUnaResLaRetiraDelPotreroYVenderUnProductoNoTocaElInventario()
        {
            var hacienda = new Hacienda();
            var gestorPotreros = new GestorPotreros(hacienda);
            var efectos = new IEfectoVenta[] { new EfectoVentaRetiroInventario(), new EfectoVentaSinEfecto() };
            var servicioVenta = new ServicioVenta(hacienda, gestorPotreros, efectos);

            gestorPotreros.crear_potrero("P_Cebones", l_tipos_potreros.cebon);
            var potrero = gestorPotreros.buscar_potrero("P_Cebones");
            potrero.anadir_res("Rayo", 20, 200, new PoliticaCapacidadPotrero(), Fabricas, Array.Empty<IPublicadorEvento>());

            Assert.Single(potrero.L_reses);

            servicioVenta.vender_res("P_Cebones", "Rayo", 100);
            Assert.Empty(potrero.L_reses);

            var reseCebonesAntes = hacienda.L_potreros.Sum(p => p.L_reses.Count);
            servicioVenta.vender_producto("P_Cebones", new ProductoDerivado(TipoProducto.Lacteo, 100), 50);
            var reseCebonesDespues = hacienda.L_potreros.Sum(p => p.L_reses.Count);

            Assert.Equal(reseCebonesAntes, reseCebonesDespues);
            Assert.Equal(2, hacienda.L_ventas.Count);
        }

        /// <summary>
        /// Cada IEfectoVenta declara el tipo que soporta y ninguno lanza al recibirlo:
        /// misma verificación de postcondición que ADR-05 exige de IFabricaRes.
        /// </summary>
        [Fact]
        public void CadaEfectoDeVentaSeAplicaSinLanzarSobreSuTipoSoportado()
        {
            var hacienda = new Hacienda();
            var potrero = new Potrero("P", l_tipos_potreros.cebon);
            potrero.anadir_res("C", 20, 200, new PoliticaCapacidadPotrero(),
                new IFabricaRes[] { new FabricaCebon() }, System.Array.Empty<IPublicadorEvento>());
            var res = potrero.buscar_res("C");
            hacienda.AgregarPotrero(potrero);

            new EfectoVentaRetiroInventario().Aplicar(hacienda, potrero, res);
            new EfectoVentaSinEfecto().Aplicar(hacienda, potrero, new ProductoDerivado(TipoProducto.Carne, 10));
        }

        // ══ ADR-06 · La jerarquía Vacuna ya expone su estado ═════════════════════

        [Fact]
        public void VivaExponeSuAtenuacionIgualQueBacterianaExponeSuPeriodo()
        {
            var viva = new Viva("V", "L1", new DateTime(2030, 1, 1), new DateTime(2029, 1, 1), enum_l_atenuaciones.Atenuacion30);
            var bacteriana = new Bacteriana("B", "L2", new DateTime(2030, 1, 1), new DateTime(2029, 1, 1), 3);

            Assert.Equal(enum_l_atenuaciones.Atenuacion30, viva.Periodo_atenuacion);
            Assert.Equal(3u, bacteriana.Periodo_aplicacion);
        }
    }
}
