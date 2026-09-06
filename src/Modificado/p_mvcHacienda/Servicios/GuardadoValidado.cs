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
        //
        // P-05 (Actividad 1) · Actividad 2 · La otra mitad de P-05, la que NO es un
        // patrón: cuatro de los cinco métodos hacían exactamente el mismo bucle
        // validar → cortar → persistir, cambiando solo el tipo. Ahora ese bucle vive
        // una sola vez en Validar<T>; cada método arma su lista, valida y decide si
        // persiste. Un agregado nuevo dejó de costar "otro método con el mismo cuerpo".
        //
        // GuardarVacunasAplicadas NO entra en el genérico y se queda con su propio
        // bucle: valida DOS TIPOS distintos intercalados —una res y, si es válida, sus
        // vacunas, antes de pasar a la siguiente res—, y ese orden intercalado es
        // comportamiento observable (determina cuál es el último resultado registrado
        // cuando todo valida). Validar<T> generaliza "una lista, un tipo, un validador";
        // forzarlo aquí habría exigido una lista mixta o dos pasadas, y una segunda
        // pasada valida res que la primera ya había cortado. Se documenta como el
        // límite honesto del refactor, no como un olvido.

        /// <summary>
        /// El bucle único: valida cada elemento en orden y corta en el primero que
        /// falla, sin persistir. Reemplaza el cuerpo que se repetía en cuatro métodos.
        /// </summary>
        private static ResultadoValidacion? Validar<T>(IEnumerable<T> elementos, IValidador<T> validador)
        {
            ResultadoValidacion? ultima = null;
            foreach (var elemento in elementos)
            {
                ultima = validador.Validar(elemento);
                if (!ultima.EsValido) return ultima;   // corte temprano: no se escribe
            }
            return ultima;
        }

        public ResultadoValidacion? GuardarPotreros(IReadOnlyList<Potrero> potreros)
        {
            var resultado = Validar(potreros, _validadorPotrero);
            if (resultado == null || resultado.EsValido) _repositorioPotreros.GuardarPotreros(potreros);
            return resultado;
        }

        public ResultadoValidacion? GuardarReses(IReadOnlyList<Potrero> potreros)
        {
            var resultado = Validar(potreros.SelectMany(p => p.L_reses), _validadorRes);
            if (resultado == null || resultado.EsValido) _repositorioPotreros.GuardarReses(potreros);
            return resultado;
        }

        /// <summary>
        /// Valida cada res y, dentro de cada una, cada vacuna aplicada. El orden importa:
        /// es el que fija PersistenciaService.cs:230-260, y determina cuál es el último
        /// resultado registrado cuando todo valida. Ver la nota de §Guardados sobre por
        /// qué este método, a diferencia de los otros cuatro, no usa Validar&lt;T&gt;.
        /// </summary>
        public ResultadoValidacion? GuardarVacunasAplicadas(IReadOnlyList<Potrero> potreros)
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

        public ResultadoValidacion? GuardarVacunas(IReadOnlyList<Vacuna> vacunas)
        {
            var resultado = Validar(vacunas, _validadorVacuna);
            if (resultado == null || resultado.EsValido) _repositorioCatalogoVacunas.GuardarVacunas(vacunas);
            return resultado;
        }

        public ResultadoValidacion? GuardarVentas(IReadOnlyList<Venta> ventas)
        {
            var resultado = Validar(ventas, _validadorVenta);
            if (resultado == null || resultado.EsValido) _repositorioVentas.GuardarVentas(ventas);
            return resultado;
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
