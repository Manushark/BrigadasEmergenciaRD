using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models; // Usar modelos del Core en lugar de "Core"

namespace BrigadasEmergenciaRD.Parallelism
{
    public sealed class ConfigParalelo
    {
        public int MaxGradoParalelismo { get; init; } = Math.Max(2, Environment.ProcessorCount);
        public int CapacidadCola { get; init; } = 10_000;
        public bool HabilitarMetricas { get; init; } = true;
        public bool UsarPrioridades { get; init; } = false;
        public int TimeoutRecursosMs { get; init; } = 30000;
    }

    public sealed class PoolRecursos : IDisposable
    {
        private readonly SemaphoreSlim _sem;
        private readonly ConcurrentDictionary<int, long> _usoRecursos; // 🆕 Tracking de uso
        
        public int CapacidadTotal { get; }
        public int RecursosDisponibles => _sem.CurrentCount; // 🆕 Propiedad útil

        public PoolRecursos(int capacidad)
        {
            if (capacidad <= 0) throw new ArgumentOutOfRangeException(nameof(capacidad));
            CapacidadTotal = capacidad;
            _sem = new SemaphoreSlim(capacidad, capacidad);
            _usoRecursos = new ConcurrentDictionary<int, long>();
        }

        public async Task<IDisposable> ReservarAsync(CancellationToken ct)
        {
            await _sem.WaitAsync(ct).ConfigureAwait(false);
            
            //Tracking de hilos
            var hiloId = Thread.CurrentThread.ManagedThreadId;
            _usoRecursos.AddOrUpdate(hiloId, 1, (_, count) => count + 1);
            
            return new Releaser(_sem, hiloId, _usoRecursos);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _s;
            private readonly int _hiloId;
            private readonly ConcurrentDictionary<int, long> _tracking;
            private int _done;
            
            public Releaser(SemaphoreSlim s, int hiloId, ConcurrentDictionary<int, long> tracking)
            {
                _s = s;
                _hiloId = hiloId;
                _tracking = tracking;
            }
            
            public void Dispose()
            {
                if (Interlocked.Exchange(ref _done, 1) == 0)
                {
                    _s.Release();
                    _tracking.AddOrUpdate(_hiloId, 0, (_, count) => Math.Max(0, count - 1));
                }
            }
        }

        public Dictionary<int, long> ObtenerUsoRecursos() => new(_usoRecursos);

        public void Dispose() => _sem.Dispose();
    }
    public sealed class GestorParaleloExtendido : IDisposable
    {
        private readonly ConfigParalelo _cfg;
        private BlockingCollection<EmergenciaEvento> _cola; // mutable para reiniciar en comparaciones
        private readonly PoolRecursos _pool;
        private readonly CancellationTokenSource _cts = new();

        // Datos compartidos thread-safe
        public ConcurrentDictionary<string, int> LlamadosPorZona { get; } = new();
        public ConcurrentDictionary<string, bool> EstadoBarrio { get; } = new();
        public ConcurrentBag<(string emergenciaId, string brigadaId, TimeSpan duracion)> Resultados { get; } = new();
        public ConcurrentDictionary<int, int> TareasPorHilo { get; } = new();
        public ConcurrentDictionary<int, TimeSpan> TiempoPorHilo { get; } = new();
        
        private readonly object _lockCritico = new();

        public GestorParaleloExtendido(ConfigParalelo? cfg = null, int recursosDisponibles = 12)
        {
            _cfg = cfg ?? new ConfigParalelo();
            _cola = new BlockingCollection<EmergenciaEvento>(_cfg.CapacidadCola);
            _pool = new PoolRecursos(recursosDisponibles);
        }

        public void EncolarEmergencia(EmergenciaEvento e)
        {
            _cola.Add(e);
            var clave = $"{e.ProvinciaId}:{e.MunicipioId}:{e.BarrioId}";
            LlamadosPorZona.AddOrUpdate(clave, 1, (_, v) => v + 1);
            EstadoBarrio.AddOrUpdate(clave, true, (_, __) => true);
        }

        public async Task IniciarProductorAsync(IAsyncEnumerable<EmergenciaEvento> fuente, CancellationToken ct = default)
        {
            using var link = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            await foreach (var e in fuente.WithCancellation(link.Token))
                EncolarEmergencia(e);
            _cola.CompleteAdding();
        }

        public async Task<(TimeSpan tiempo, int atendidas)> ProcesarSecuencialAsync(
            Func<EmergenciaEvento, CancellationToken, Task<(string brigadaId, TimeSpan duracion)>> atender,
            CancellationToken ct = default)
        {
            using var link = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            var sw = Stopwatch.StartNew();
            int atendidas = 0;

            foreach (var e in _cola.GetConsumingEnumerable(link.Token))
            {
                var (brigadaId, dur) = await atender(e, link.Token).ConfigureAwait(false);
                Resultados.Add((e.Id.ToString(), brigadaId, dur));
                atendidas++;
            }

            sw.Stop();
            return (sw.Elapsed, atendidas);
        }

        public async Task<(TimeSpan tiempo, int atendidas)> ProcesarEnParaleloAsync(
            Func<EmergenciaEvento, CancellationToken, Task<(string brigadaId, TimeSpan duracion)>> atender,
            CancellationToken ct = default)
        {
            using var link = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
            var token = link.Token;
            var sw = Stopwatch.StartNew();
            int atendidas = 0;

            var workers = Enumerable.Range(0, _cfg.MaxGradoParalelismo)
                .Select(workerId => Task.Run(async () =>
                {
                    var hiloId = Thread.CurrentThread.ManagedThreadId;
                    var swHilo = Stopwatch.StartNew();
                    int tareasHilo = 0;

                    try
                    {
                        foreach (var e in _cola.GetConsumingEnumerable(token))
                        {
                            token.ThrowIfCancellationRequested();

                            using var _res = await _pool.ReservarAsync(token).ConfigureAwait(false);

                            try
                            {
                                var (brigadaId, dur) = await atender(e, token).ConfigureAwait(false);
                                Resultados.Add((e.Id.ToString(), brigadaId, dur));
                                Interlocked.Increment(ref atendidas);
                                tareasHilo++;
                            }
                            catch (Exception ex)
                            {
                                lock (_lockCritico)
                                {
                                    Console.WriteLine($"⚠️ Error procesando emergencia {e.Id}: {ex.Message}");
                                }
                                Resultados.Add((e.Id.ToString(), "ERROR", TimeSpan.Zero));
                            }
                        }
                    }
                    finally
                    {
                        swHilo.Stop();
                        if (_cfg.HabilitarMetricas)
                        {
                            TareasPorHilo.TryAdd(hiloId, tareasHilo);
                            TiempoPorHilo.TryAdd(hiloId, swHilo.Elapsed);
                        }
                    }
                }));

            try { await Task.WhenAll(workers).ConfigureAwait(false); }
            catch (OperationCanceledException) { /* cancelado OK */ }

            sw.Stop();
            return (sw.Elapsed, atendidas);
        }

        //Estadísticas
        public EstadisticasParalelismo ObtenerEstadisticas()
        {
            var stats = new EstadisticasParalelismo
            {
                TotalTareasEjecutadas = TareasPorHilo.Values.Sum(),
                MaximoHilosConcurrentes = TareasPorHilo.Count,
                TareasEjecutadasEnParalelo = Resultados.Count
            };

            foreach (var (hiloId, tareas) in TareasPorHilo)
            {
                var tiempo = TiempoPorHilo.GetValueOrDefault(hiloId, TimeSpan.Zero);
                stats.MedicionesPorHilo.Add(new MedicionHilo
                {
                    IdHilo = hiloId,
                    TareasCompletadas = tareas,
                    TiempoTotalEjecucion = tiempo,
                    PorcentajeUso = tiempo.TotalMilliseconds > 0 ?
                        (double)tareas / tiempo.TotalSeconds * 100 : 0
                });
            }

            return stats;
        }

        //Comparación secuencial vs paralelo
        public async Task<ResultadoComparacion> CompararRendimientoAsync(
            Func<EmergenciaEvento, CancellationToken, Task<(string brigadaId, TimeSpan duracion)>> atender,
            List<EmergenciaEvento> eventos)
        {
            // secuencial
            foreach (var e in eventos) EncolarEmergencia(e);
            _cola.CompleteAdding();

            var (tiempoSeq, _) = await ProcesarSecuencialAsync(atender);

            // resetear para paralelo
            _cola = new BlockingCollection<EmergenciaEvento>(_cfg.CapacidadCola);
            Resultados.Clear();

            foreach (var e in eventos) EncolarEmergencia(e);
            _cola.CompleteAdding();

            var (tiempoPar, _) = await ProcesarEnParaleloAsync(atender);

            var resultado = new ResultadoComparacion
            {
                TiempoEjecucionSecuencial = tiempoSeq,
                TiempoEjecucionParalela = tiempoPar,
                NucleosProcesadorUtilizados = _cfg.MaxGradoParalelismo,
                EstrategiaParalelizacion = "TPL Workers",
                DetallesParalelismo = ObtenerEstadisticas()
            };

            resultado.CalcularMetricas();
            return resultado;
        }

        public void CancelarTodo() => _cts.Cancel();

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cola.Dispose();
            _pool.Dispose();
        }
    }
}