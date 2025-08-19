using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Parallelism
{
    public sealed class ConfigParalelo
    {
        public int MaxGradoParalelismo { get; init; } = Environment.ProcessorCount;
        public int CapacidadCola { get; init; } = 1000;
        public bool HabilitarMetricas { get; init; } = true;
        public int TimeoutMs { get; init; } = 30000;
    }

    public sealed class PoolRecursos : IDisposable
    {
        private readonly SemaphoreSlim _semaforo;
        public int Disponibles => _semaforo.CurrentCount;

        public PoolRecursos(int capacidad)
        {
            _semaforo = new SemaphoreSlim(capacidad, capacidad);
        }

        public async Task<IDisposable> AdquirirAsync(CancellationToken ct = default)
        {
            await _semaforo.WaitAsync(ct);
            return new Liberador(_semaforo);
        }

        private class Liberador : IDisposable
        {
            private readonly SemaphoreSlim _sem;
            private int _liberado = 0;

            public Liberador(SemaphoreSlim sem) => _sem = sem;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _liberado, 1) == 0)
                    _sem.Release();
            }
        }

        public void Dispose() => _semaforo.Dispose();
    }

    public sealed class GestorParaleloExtendido : IDisposable
    {
        private readonly ConfigParalelo _config;
        private readonly BlockingCollection<EmergenciaEvento> _colaEmergencias;
        private readonly PoolRecursos _poolRecursos;
        private readonly CancellationTokenSource _ctsPrincipal = new();

        // Datos compartidos thread-safe
        public ConcurrentBag<(string emergenciaId, string brigadaId, TimeSpan duracion)> Resultados { get; } = new();
        public ConcurrentDictionary<string, int> LlamadosPorZona { get; } = new();
        public ConcurrentDictionary<string, bool> EstadoBarrio { get; } = new();
        public ConcurrentDictionary<int, int> TareasPorHilo { get; } = new();
        public ConcurrentDictionary<int, TimeSpan> TiempoPorHilo { get; } = new();

        public GestorParaleloExtendido(ConfigParalelo? config = null, int recursosDisponibles = 10)
        {
            _config = config ?? new ConfigParalelo();
            _colaEmergencias = new BlockingCollection<EmergenciaEvento>(_config.CapacidadCola);
            _poolRecursos = new PoolRecursos(recursosDisponibles);
        }

        public void EncolarEmergencia(EmergenciaEvento emergencia)
        {
            if (_colaEmergencias.IsAddingCompleted) return;

            _colaEmergencias.Add(emergencia);

            // Trackear por zona geográfica
            var zona = $"{emergencia.ProvinciaId}:{emergencia.MunicipioId}:{emergencia.BarrioId}";
            LlamadosPorZona.AddOrUpdate(zona, 1, (k, v) => v + 1);
            EstadoBarrio[zona] = true;
        }

        public async Task<(TimeSpan tiempo, int procesadas)> ProcesarEnParaleloAsync(
            Func<EmergenciaEvento, CancellationToken, Task<(string brigadaId, TimeSpan duracion)>> procesador,
            CancellationToken ctExterno = default)
        {
            using var ctsCombinadoSrc = CancellationTokenSource.CreateLinkedTokenSource(ctExterno, _ctsPrincipal.Token);
            var ct = ctsCombinadoSrc.Token;

            _colaEmergencias.CompleteAdding(); // No más emergencias después de iniciar

            var cronometro = Stopwatch.StartNew();
            int totalProcesadas = 0;

            // Crear workers paralelos
            var workers = Enumerable.Range(0, _config.MaxGradoParalelismo)
                .Select(workerId => Task.Run(async () =>
                {
                    var hiloId = Thread.CurrentThread.ManagedThreadId;
                    var cronometroHilo = Stopwatch.StartNew();
                    int tareasDeEsteHilo = 0;

                    try
                    {
                        // Consumir emergencias de la cola thread-safe
                        foreach (var emergencia in _colaEmergencias.GetConsumingEnumerable(ct))
                        {
                            ct.ThrowIfCancellationRequested();

                            // Adquirir recurso (brigada) de manera thread-safe
                            using var recurso = await _poolRecursos.AdquirirAsync(ct);

                            try
                            {
                                // Procesar emergencia de manera asíncrona
                                var (brigadaId, duracion) = await procesador(emergencia, ct);

                                // Guardar resultado thread-safe
                                Resultados.Add((emergencia.Id.ToString(), brigadaId, duracion));
                                Interlocked.Increment(ref totalProcesadas);
                                tareasDeEsteHilo++;
                            }
                            catch (OperationCanceledException)
                            {
                                break; // Cancelación limpia
                            }
                            catch (Exception ex)
                            {
                                // Log error pero continuar
                                Resultados.Add((emergencia.Id.ToString(), "ERROR", TimeSpan.Zero));
                                Console.WriteLine($"⚠️ Error procesando {emergencia.Id}: {ex.Message}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancelación esperada
                    }
                    finally
                    {
                        cronometroHilo.Stop();

                        // Guardar métricas del hilo
                        if (_config.HabilitarMetricas)
                        {
                            TareasPorHilo[hiloId] = tareasDeEsteHilo;
                            TiempoPorHilo[hiloId] = cronometroHilo.Elapsed;
                        }
                    }
                }, ct))
                .ToArray();

            try
            {
                // Esperar a que todos los workers terminen
                await Task.WhenAll(workers);
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada
            }

            cronometro.Stop();
            return (cronometro.Elapsed, totalProcesadas);
        }

        public EstadisticasParalelismo ObtenerEstadisticas()
        {
            return new EstadisticasParalelismo
            {
                TotalTareasEjecutadas = TareasPorHilo.Values.Sum(),
                MaximoHilosConcurrentes = TareasPorHilo.Count,
                TareasEjecutadasEnParalelo = Resultados.Count,
                DuracionTotal = TiempoPorHilo.Values.Any() ? TiempoPorHilo.Values.Max() : TimeSpan.Zero,
                MedicionesPorHilo = TareasPorHilo.Select(kvp => new MedicionHilo
                {
                    IdHilo = kvp.Key,
                    TareasCompletadas = kvp.Value,
                    TiempoTotalEjecucion = TiempoPorHilo.GetValueOrDefault(kvp.Key, TimeSpan.Zero)
                }).ToList()
            };
        }

        public void Cancelar() => _ctsPrincipal.Cancel();

        public void Dispose()
        {
            _ctsPrincipal.Cancel();
            _colaEmergencias?.Dispose();
            _poolRecursos?.Dispose();
            _ctsPrincipal?.Dispose();
        }
    }
}