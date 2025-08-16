using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Simulation
{
    /// <summary>
    /// Simula la generación de eventos durante una tormenta y los convierte en llamadas de emergencia.
    /// - Recorre el territorio (provincias ? municipios ? barrios)
    /// - En cada “tick” evalúa la probabilidad de evento en cada barrio
    /// - Si ocurre, crea un EmergenciaEvento y lo convierte en LlamadaEmergencia
    /// - La llamada se encola en la cola concurrente del Barrio (Barrio.AgregarLlamadaEmergencia)
    ///
    /// Nota: Esta clase NO asigna brigadas. Eso lo hará el GestorEmergencias.
    /// </summary>
    public class SimuladorTormenta
    {
        // Dependencias
        private readonly IDataProvider _data;
        private readonly ParametrosSimulacion _cfg;

        // Estado interno de datos
        private List<Provincia> _provincias = new();
        private List<Municipio> _municipios = new();
        private List<Barrio> _barrios = new();

        // Estado interno de ejecución
        private readonly Random _rng;
        private CancellationTokenSource _cts;
        private Task _loopTask;

        // Contador de llamadas generadas (para tope máximo)
        private int _llamadasGeneradas = 0;
        /// <summary>Lectura del total de llamadas generadas en esta ejecución.</summary>
        public int LlamadasGeneradas => _llamadasGeneradas;

        /// <summary>
        /// Intensidad actual de la tormenta (ajustable “en caliente”).
        /// </summary>
        public IntensidadTormenta IntensidadActual { get; private set; } = IntensidadTormenta.Moderada;

        // Hooks opcionales para métricas / logging
        public event Action<LlamadaEmergencia> OnLlamadaGenerada;
        public event Action<EmergenciaEvento> OnEventoGenerado;

        public SimuladorTormenta(IDataProvider data, ParametrosSimulacion cfg)
        {
            _data = data;
            _cfg = cfg ?? new ParametrosSimulacion();
            _cfg.Validar();

            _rng = _cfg.SemillaAleatoria.HasValue
                ? new Random(_cfg.SemillaAleatoria.Value)
                : new Random();
        }

        /// <summary>Permite cambiar la intensidad “en caliente”.</summary>
        public void EstablecerIntensidad(IntensidadTormenta nuevaIntensidad)
        {
            IntensidadActual = nuevaIntensidad;
        }

        /// <summary>
        /// Carga y prepara los datos territoriales (provincias, municipios y barrios).
        /// Debe llamarse antes de iniciar el bucle de simulación.
        /// </summary>
        public async Task PrepararDatosAsync()
        {
            // 1) Provincias
            _provincias = (await _data.ObtenerProvinciasAsync())?.ToList() ?? new List<Provincia>();

            // 2) Municipios (derivados de Provincias)
            _municipios.Clear();
            foreach (var prov in _provincias)
            {
                var munis = await _data.ObtenerMunicipiosAsync(prov.Id);
                if (munis != null) _municipios.AddRange(munis);
            }

            // 3) Barrios (derivados de Municipios)
            _barrios.Clear();
            foreach (var muni in _municipios)
            {
                var brrs = await _data.ObtenerBarriosAsync(muni.Id);
                if (brrs != null) _barrios.AddRange(brrs);
            }

            // Reiniciar contador de llamadas para una nueva corrida
            _llamadasGeneradas = 0;
        }

        /// <summary>Inicia el bucle de simulación (tick periódico).</summary>
        public async Task IniciarAsync(CancellationToken externalToken = default)
        {
            if (_loopTask != null)
                throw new InvalidOperationException("El simulador ya está en ejecución.");

            // Asegura datos cargados
            if (_barrios.Count == 0) await PrepararDatosAsync();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            _loopTask = Task.Run(() => LoopAsync(_cts.Token), _cts.Token);
        }

        /// <summary>Señal de parada y espera de finalización del bucle.</summary>
        public async Task DetenerAsync()
        {
            try
            {
                _cts?.Cancel();
                if (_loopTask != null)
                    await _loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada
            }
            finally
            {
                _loopTask = null;
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>Bucle principal: ejecuta "ticks" hasta que se cancele la simulación.</summary>
        private async Task LoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await TickAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    // Salir silenciosamente si fue cancelado en mitad del tick
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Simulador] Error en tick: {ex.Message}");
                }

                // Ritmo de la simulación
                await Task.Delay(_cfg.DuracionTickMs, ct);
            }
        }

        /// <summary>
        /// Un “tick” de simulación: para cada barrio, decide si ocurre un evento y genera llamada.
        /// Respeta el tope de llamadas configurado en ParametrosSimulacion.MaximoLlamadas.
        /// </summary>
        public async Task TickAsync(CancellationToken ct = default)
        {
            if (_barrios.Count == 0) return;

            // Si ya alcanzamos el máximo, no generamos más
            if (_cfg.MaximoLlamadas > 0 && _llamadasGeneradas >= _cfg.MaximoLlamadas)
                return;

            // Precalcular factor por intensidad de la tormenta
            var factorIntensidad = _cfg.FactorPorIntensidad.TryGetValue(IntensidadActual, out var fI)
                ? fI : 1.0;

            // Subconjunto aleatorio de barrios por tick (reduce costo si hay muchos)
            var porcentajeBarrioRevisado = 0.5; // 50% de barrios por tick
            var totalARevisar = Math.Max(1, (int)(_barrios.Count * porcentajeBarrioRevisado));

            var barriosMuestra = _barrios
                .OrderBy(_ => _rng.Next())
                .Take(totalARevisar)
                .ToList();

            foreach (var barrio in barriosMuestra)
            {
                ct.ThrowIfCancellationRequested();

                // Si ya llegamos al tope dentro del mismo tick, salimos
                if (_cfg.MaximoLlamadas > 0 && _llamadasGeneradas >= _cfg.MaximoLlamadas)
                    return;

                // Probabilidad efectiva = base × intensidad × vulnerabilidad
                var probBase = _cfg.ProbabilidadBaseEventoPorBarrio;
                var factorVulnerabilidad = ObtenerFactorVulnerabilidad(barrio);
                var probEfectiva = probBase * factorIntensidad * factorVulnerabilidad;

                if (SiguienteEvento(probEfectiva))
                {
                    // Elegir tipo de emergencia según pesos configurados
                    var tipo = SortearTipoEmergencia();

                    // Generar datos del evento
                    var evento = new EmergenciaEvento
                    {
                        Tipo = tipo,
                        Intensidad = IntensidadActual,
                        Ubicacion = barrio.Coordenadas ?? new Coordenada(0, 0),
                        PersonasAfectadas = _rng.Next(_cfg.PersonasAfectadasMin, _cfg.PersonasAfectadasMax + 1),
                        Descripcion = GenerarDescripcion(tipo),
                        ProvinciaId = barrio?.Municipio?.ProvinciaId ?? 0,
                        MunicipioId = barrio?.MunicipioId ?? 0,
                        BarrioId = barrio.Id
                    };

                    OnEventoGenerado?.Invoke(evento);

                    // Convertir a llamada y encolarla en el barrio
                    var llamada = evento.ConvertirALlamada();
                    llamada.Barrio = barrio; // para logs con nombres
                    barrio.AgregarLlamadaEmergencia(llamada);
                    OnLlamadaGenerada?.Invoke(llamada);

                    // Incrementar contador y cortar si se alcanzó el tope
                    if (_cfg.MaximoLlamadas > 0)
                    {
                        _llamadasGeneradas++;
                        if (_llamadasGeneradas >= _cfg.MaximoLlamadas)
                            return;
                    }

                    // “Yield” cooperativo para no monopolizar CPU si la muestra es grande
                    await Task.Yield();
                }
            }
        }

        // ----------------------
        //    Métodos auxiliares
        // ----------------------

        /// <summary>
        /// Devuelve el multiplicador de vulnerabilidad configurado para la provincia del barrio.
        /// Si no hay dato, retorna 1.0 (neutro).
        /// </summary>
        private double ObtenerFactorVulnerabilidad(Barrio barrio)
        {
            var provincia = _provincias.FirstOrDefault(p => p.Id == (barrio?.Municipio?.ProvinciaId ?? -1));
            if (provincia == null) return 1.0;

            var codigo = !string.IsNullOrWhiteSpace(provincia.Codigo)
                ? provincia.Codigo
                : provincia.Id.ToString("00");

            if (_cfg.FactorPorProvinciaCodigo != null &&
                _cfg.FactorPorProvinciaCodigo.TryGetValue(codigo, out var factor) &&
                factor > 0)
            {
                return factor;
            }

            return 1.0;
        }

        /// <summary>Decide si ocurre un evento dado una probabilidad (0..1).</summary>
        private bool SiguienteEvento(double probabilidad)
        {
            if (probabilidad <= 0) return false;
            if (probabilidad >= 1) return true;
            return _rng.NextDouble() < probabilidad;
        }

        /// <summary>Sortea el tipo de emergencia usando los pesos configurados en parámetros.</summary>
        private TipoEmergencia SortearTipoEmergencia()
        {
            var pesos = _cfg.PesoPorTipoEmergencia
                .Where(kv => kv.Value > 0)
                .ToList();

            if (pesos.Count == 0)
                return TipoEmergencia.Inundacion; // fallback por si la config viene vacía

            var suma = pesos.Sum(kv => kv.Value);
            var r = _rng.NextDouble() * suma;

            double acumulado = 0;
            foreach (var kv in pesos)
            {
                acumulado += kv.Value;
                if (r <= acumulado)
                    return kv.Key;
            }

            return pesos.Last().Key; // Fallback por redondeos
        }

        /// <summary>
        /// Genera una descripción “humana” aleatoria para la llamada.
        /// No afecta la lógica, pero hace más realista la simulación.
        /// </summary>
        private string GenerarDescripcion(TipoEmergencia tipo)
        {
            if (_rng.NextDouble() > _cfg.ProbabilidadDescripcion)
                return string.Empty;

            return tipo switch
            {
                TipoEmergencia.Inundacion => "Calles anegadas y vehículos atrapados.",
                TipoEmergencia.EmergenciaMedica => "Persona inconsciente por golpe; se requiere ambulancia.",
                TipoEmergencia.PersonasAtrapadas => "Vecinos atrapados en vivienda colapsada.",
                TipoEmergencia.DeslizamientoTierra => "Desprendimiento bloqueando acceso al barrio.",
                TipoEmergencia.AccidenteVehicular => "Colisión múltiple con heridos.",
                TipoEmergencia.IncendioEstructural => "Humo denso saliendo de edificio residencial.",
                TipoEmergencia.VientosFuertes => "Postes caídos y techos desprendidos.",
                TipoEmergencia.CorteEnergia => "Sector sin electricidad desde hace horas.",
                _ => "Se reporta situación de emergencia por la tormenta."
            };
        }
    }
}
