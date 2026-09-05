using Bib_Hacienda.Clases;
using Bib_Hacienda.Contratos;
using Bib_Hacienda.Servicios;

namespace p_mvcHacienda.Infraestructura
{
    /// <summary>
    /// ADR-02 · DI-1 · Implementación en archivos planos de <see cref="IRepositorioPotreros"/>.
    ///
    /// Es el módulo de BAJO NIVEL: sabe de rutas, de File.ReadAllLines y de formato de
    /// texto, y no sabe nada de reglas ganaderas. Los servicios de dominio dependen de
    /// la interfaz, no de esta clase; la flecha de dependencia apunta hacia arriba.
    ///
    /// Esta clase, junto con las otras tres, sustituye a PersistenciaService (643 líneas,
    /// grado de acoplamiento 21, el nodo más acoplado del mapa de dependencias · H-02).
    /// Lo que se le quitó respecto de aquella clase:
    ///   · la validación de negocio, que ahora decide el servicio de aplicación (ADR-03);
    ///   · el IHttpContextAccessor, del que leía y escribía resultados (H-14);
    ///   · la generación de proxies de Castle (PersistenciaService.cs:41-57).
    ///
    /// ADR-09 · El medio sigue siendo texto plano a propósito. Migrar a base de datos
    /// no lo pide ninguna solicitud de cambio y arriesgaría los datos históricos.
    /// Cuando se quiera, basta con implementar esta misma interfaz y cambiar una línea
    /// en RaizComposicion.
    /// </summary>
    public sealed class RepositorioPotrerosArchivo : IRepositorioPotreros
    {
        private readonly string _directorioArchivos;
        private readonly PoliticaCapacidadPotrero _politica;
        private readonly IReadOnlyCollection<IFabricaRes> _fabricas;
        private readonly IReadOnlyCollection<IPublicadorEvento> _avisosAltaDeRes;

        // La política, las fábricas y los avisos llegan del composition root porque hay
        // que entregárselos a Potrero.anadir_res, que es una entidad y no los puede pedir.
        // Los avisos SÍ importan aquí aunque el mensaje de retorno de anadir_res se
        // descarte (:92): son el mismo registro que usa GestorReses, y pasar uno vacío
        // solo porque hoy nadie lee el mensaje sería una fidelidad rota a propósito.
        public RepositorioPotrerosArchivo(string directorioArchivos,
            PoliticaCapacidadPotrero politica, IEnumerable<IFabricaRes> fabricas,
            IEnumerable<IPublicadorEvento> avisosAltaDeRes)
        {
            _directorioArchivos = directorioArchivos;
            _politica = politica;
            _fabricas = new List<IFabricaRes>(fabricas);
            _avisosAltaDeRes = new List<IPublicadorEvento>(avisosAltaDeRes);
            if (!Directory.Exists(_directorioArchivos))
            {
                Directory.CreateDirectory(_directorioArchivos);
            }
        }

        private string Ruta(string archivo) => Path.Combine(_directorioArchivos, archivo);

        // --- Carga --------------------------------------------------------

        public List<Potrero> CargarPotreros()
        {
            try
            {
                string rutaArchivo = Ruta("Potreros.txt");
                if (!File.Exists(rutaArchivo)) return new List<Potrero>();

                var potreros = new List<Potrero>();
                foreach (var linea in File.ReadAllLines(rutaArchivo))
                {
                    if (!MapeadorPotrero.TryDeLinea(linea, out var potrero)) continue;

                    // Evitar duplicados por identificación, sin distinguir mayúsculas.
                    if (!potreros.Any(p => string.Equals(p.Identificacion, potrero.Identificacion, StringComparison.OrdinalIgnoreCase)))
                    {
                        potreros.Add(potrero);
                    }
                }
                return potreros;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar potreros: {ex.Message}");
            }
        }

        public void CargarReses(List<Potrero> potreros)
        {
            try
            {
                string rutaArchivo = Ruta("Reses.txt");
                if (!File.Exists(rutaArchivo)) return;

                foreach (var linea in File.ReadAllLines(rutaArchivo))
                {
                    if (!MapeadorRes.TryDeLinea(linea, out var nombrePotrero, out var nombreRes, out var peso, out var edad, out var chip)) continue;

                    var potrero = potreros.FirstOrDefault(p => string.Equals(p.Identificacion, nombrePotrero, StringComparison.OrdinalIgnoreCase));
                    if (potrero != null)
                    {
                        // Se delega en el dominio, igual que hoy (PersistenciaService.cs:378):
                        // el subtipo de la res lo decide el potrero, no el archivo.
                        potrero.anadir_res(nombreRes, edad, peso, _politica, _fabricas, _avisosAltaDeRes);

                        // SC-2 · ADR-11 · El chip se conecta DESPUÉS de que la res existe,
                        // que es la misma secuencia que sigue el negocio. Por eso la carga
                        // no tuvo que cambiar de firma ni tocar `anadir_res`.
                        if (chip != null)
                        {
                            potrero.L_reses[potrero.L_reses.Count - 1].AsignarChip(chip);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar reses: {ex.Message}");
            }
        }

        public void CargarVacunasAplicadas(List<Potrero> potreros)
        {
            try
            {
                string rutaArchivo = Ruta("VacunasAplicadas.txt");
                if (!File.Exists(rutaArchivo)) return;

                foreach (var linea in File.ReadAllLines(rutaArchivo))
                {
                    if (!MapeadorVacuna.TryDeLineaAplicada(linea, out var nombrePotrero, out var nombreRes, out var vacuna)) continue;

                    var potrero = potreros.FirstOrDefault(p => string.Equals(p.Identificacion, nombrePotrero, StringComparison.OrdinalIgnoreCase));
                    if (potrero != null)
                    {
                        var res = potrero.buscar_res(nombreRes);
                        if (res != null)
                        {
                            res.L_vacunas_aplicadas.Add(vacuna);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar vacunas aplicadas: {ex.Message}");
            }
        }

        // --- Guardado -----------------------------------------------------
        //
        // Ya NO validan: la validación es una decisión explícita del servicio de
        // aplicación, que decide no llamar aquí si algo no valida (ADR-03). Esto
        // conserva el corte temprano de PersistenciaService.cs:110-114, donde una sola
        // res inválida impedía escribir Reses.txt entero.

        public void GuardarPotreros(List<Potrero> potreros)
        {
            try
            {
                var lineas = potreros.Select(MapeadorPotrero.ALinea);
                File.WriteAllLines(Ruta("Potreros.txt"), lineas);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar potreros: {ex.Message}", ex);
            }
        }

        public void GuardarReses(List<Potrero> potreros)
        {
            try
            {
                var lineas = new List<string>();
                foreach (var potrero in potreros)
                {
                    foreach (var res in potrero.L_reses)
                    {
                        lineas.Add(MapeadorRes.ALinea(potrero, res));
                    }
                }
                File.WriteAllLines(Ruta("Reses.txt"), lineas);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar reses: {ex.Message}", ex);
            }
        }

        public void GuardarVacunasAplicadas(List<Potrero> potreros)
        {
            try
            {
                var lineas = new List<string>();
                foreach (var potrero in potreros)
                {
                    foreach (var res in potrero.L_reses)
                    {
                        foreach (var vacuna in res.L_vacunas_aplicadas)
                        {
                            lineas.Add(MapeadorVacuna.ALineaAplicada(potrero, res, vacuna));
                        }
                    }
                }
                File.WriteAllLines(Ruta("VacunasAplicadas.txt"), lineas);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar vacunas aplicadas: {ex.Message}", ex);
            }
        }
    }
}
