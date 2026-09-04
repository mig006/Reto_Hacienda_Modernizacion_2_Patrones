using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Valores;

namespace p_mvcHacienda.Servicios
{
    /// <summary>
    /// ADR-02 + ADR-03 · Servicio de aplicación que ejecuta la secuencia
    /// VALIDAR → PERSISTIR sobre cada agregado y devuelve el resultado de la validación.
    ///
    /// ── Por qué existe ────────────────────────────────────────────────────────────
    /// ADR-02 saca la validación de la persistencia y la convierte en «una decisión
    /// explícita del servicio de aplicación». Esa decisión es idéntica en los cinco
    /// servicios y para cuatro de ellos recae sobre los mismos agregados, así que
    /// escribirla cinco veces sería duplicar el bucle de validación —justo el defecto
    /// que ADR-04 corrige en las cuatro sobrecargas de crear_vacuna—. Se concentra aquí.
    ///
    /// NOTA PARA EL DIAGRAMA TO-BE: esta clase NO figura en el inventario cerrado de
    /// ADRs.md §3.2, que se redactó antes de escribir el código. Es una enmienda
    /// consciente, registrada en 04-evidencia/enmiendas-ADRs.md, y debe aparecer en el
    /// diagrama para que se cumpla la regla de consistencia diagrama–código.
    ///
    /// ── Cómo reproduce el comportamiento observable ───────────────────────────────
    /// Antes, el resultado de cada validación se depositaba en
    /// HttpContext.Items["ResultadoValidacion"] y se leía de vuelta en once puntos de
    /// PersistenciaService (H-14). Ese diccionario vive lo que dura UNA petición, de
    /// modo que varios guardados seguidos comparten el último valor escrito.
    ///
    /// Aquí no hay diccionario global: cada método devuelve su ResultadoValidacion y
    /// es el servicio llamador quien lo encadena dentro de UNA operación, con una
    /// variable local. Devolver null significa «no se validó nada» —la colección
    /// estaba vacía—, que es el equivalente exacto de «Items nunca se escribió».
    ///
    /// El corte temprano se conserva: si una entidad no valida, NO se llama al
    /// repositorio. Por eso una sola res inválida sigue impidiendo que se escriba
    /// Reses.txt entero (PersistenciaService.cs:110-114), con la pérdida en disco de
    /// todas las reses de todos los potreros. Es comportamiento observable.
    /// </summary>
    public sealed class GuardadoValidado
    {
        private readonly IRepositorioPotreros _repositorioPotreros;
        private readonly IRepositorioVentas _repositorioVentas;
        private readonly IRepositorioCatalogoVacunas _repositorioCatalogoVacunas;
        private readonly IValidador<Potrero> _validadorPotrero;
        private readonly IValidador<Res> _validadorRes;
        private readonly IValidador<Vacuna> _validadorVacuna;
        private readonly IValidador<Venta> _validadorVenta;

        public GuardadoValidado(
            IRepositorioPotreros repositorioPotreros,
            IRepositorioVentas repositorioVentas,
            IRepositorioCatalogoVacunas repositorioCatalogoVacunas,
            IValidador<Potrero> validadorPotrero,
            IValidador<Res> validadorRes,
            IValidador<Vacuna> validadorVacuna,
            IValidador<Venta> validadorVenta)
        {
            _repositorioPotreros = repositorioPotreros;
            _repositorioVentas = repositorioVentas;
            _repositorioCatalogoVacunas = repositorioCatalogoVacunas;
            _validadorPotrero = validadorPotrero;
            _validadorRes = validadorRes;
            _validadorVacuna = validadorVacuna;
            _validadorVenta = validadorVenta;
        }

        // ── Guardados ─────────────────────────────────────────────────────

        public ResultadoValidacion? GuardarPotreros(List<Potrero> potreros)
        {
            ResultadoValidacion? ultima = null;
            foreach (var potrero in potreros)
            {
                ultima = _validadorPotrero.Validar(potrero);
                if (!ultima.EsValido) return ultima;   // corte temprano: no se escribe
            }

            _repositorioPotreros.GuardarPotreros(potreros);
            return ultima;
        }

        public ResultadoValidacion? GuardarReses(List<Potrero> potreros)
        {
            ResultadoValidacion? ultima = null;
            foreach (var potrero in potreros)
            {
                foreach (var res in potrero.L_reses)
                {
                    ultima = _validadorRes.Validar(res);
                    if (!ultima.EsValido) return ultima;
                }
            }

            _repositorioPotreros.GuardarReses(potreros);
            return ultima;
        }

        /// <summary>
        /// Valida cada res y, dentro de cada una, cada vacuna aplicada. El orden importa:
        /// es el que fija PersistenciaService.cs:230-260, y determina cuál es el último
        /// resultado registrado cuando todo valida.
        /// </summary>
        public ResultadoValidacion? GuardarVacunasAplicadas(List<Potrero> potreros)
        {
            ResultadoValidacion? ultima = null;
            foreach (var potrero in potreros)
            {
                foreach (var res in potrero.L_reses)
                {
                    ultima = _validadorRes.Validar(res);
                    if (!ultima.EsValido) return ultima;

                    foreach (var vacuna in res.L_vacunas_aplicadas)
                    {
                        ultima = _validadorVacuna.Validar(vacuna);
                        if (!ultima.EsValido) return ultima;
                    }
                }
            }

            _repositorioPotreros.GuardarVacunasAplicadas(potreros);
            return ultima;
        }

        public ResultadoValidacion? GuardarVacunas(List<Vacuna> vacunas)
        {
            ResultadoValidacion? ultima = null;
            foreach (var vacuna in vacunas)
            {
                ultima = _validadorVacuna.Validar(vacuna);
                if (!ultima.EsValido) return ultima;
            }

            _repositorioCatalogoVacunas.GuardarVacunas(vacunas);
            return ultima;
        }

        public ResultadoValidacion? GuardarVentas(List<Venta> ventas)
        {
            ResultadoValidacion? ultima = null;
            foreach (var venta in ventas)
            {
                ultima = _validadorVenta.Validar(venta);
                if (!ultima.EsValido) return ultima;
            }

            _repositorioVentas.GuardarVentas(ventas);
            return ultima;
        }

        // ── Contrato de texto congelado (ADR-02) ──────────────────────────

        /// <summary>
        /// Traduce el resultado acumulado de una operación al texto que ve el operario.
        /// Cuando no se validó nada —colección vacía— devuelve el respaldo, igual que
        /// hacía el <c>?? "Guardado exitosamente"</c> de PersistenciaService.cs:86.
        /// </summary>
        public static string Texto(ResultadoValidacion? ultima, string respaldo = TextoGuardadoExitoso)
            => ultima?.Mensaje ?? respaldo;

        /// <summary>Literal de PersistenciaService.cs:86.</summary>
        public const string TextoGuardadoExitoso = "Guardado exitosamente";

        // Los cinco respaldos de error de PersistenciaService.cs:77, 113, 150, 190 y 239.
        //
        // Se conservan como parte del contrato de texto congelado de ADR-02, pero hay
        // que decir con precisión qué les pasó: en el sistema original solo se
        // alcanzaban cuando NO había HttpContext, es decir fuera de una petición, y
        // dentro de una petición eran inalcanzables porque una validación fallida
        // siempre escribía el diccionario. En el rediseño ya no hay HttpContext y la
        // ausencia de resultado significa «no se validó nada», caso en el que nada
        // pudo fallar. Quedan, por tanto, INALCANZABLES POR CONSTRUCCIÓN.
        //
        // Su 'ó' aparece como carácter de reemplazo porque PersistenciaService.cs está
        // escrito en ISO-8859 sin BOM y el compilador lo lee como UTF-8 (ADRs.md §8.5):
        // lo que el programa emite hoy es esto, no la palabra bien acentuada.
        public const string RespaldoErrorPotrero = "Error de validaci�n en potrero";
        public const string RespaldoErrorRes = "Error de validaci�n en res";
        public const string RespaldoErrorVenta = "Error de validaci�n en venta";
        public const string RespaldoErrorVacuna = "Error de validaci�n en vacuna";
        public const string RespaldoErrorVacunaAplicada = "Error de validaci�n en vacuna aplicada";
    }
}
