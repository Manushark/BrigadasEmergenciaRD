using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace BrigadasEmergenciaRD.Metrics.Models
{
    // Estructura interna mutable para acumular datos mientras corre el escenario.
    // Responsabilidades:
    // - Guardar latencias de cada operacion en una cola thread-safe.
    // - Llevar contadores atomicos de exitos/fallos/bytes.
    // - Medir duracion total del escenario con Stopwatch.
    // - Leer memoria del proceso al finalizar para reportarla.
    internal sealed class MetricBuffer
    {
        // Cola concurrente con latencias por operacion (unidad: ticks de Stopwatch).
        // Uso de ConcurrentQueue minimiza contencion al registrar muestras desde varios hilos.
        public readonly ConcurrentQueue<long> LatenciasTicks = new();

        // Contadores atomicos que se actualizan con Interlocked en el colector.
        public long Exitos;
        public long Fallos;
        public long Bytes;

        // Cronometro de la ventana total de medicion.
        public readonly Stopwatch Reloj = new();

        // Proceso actual: se usa para consultar WorkingSet (memoria) y CPU (si lo habilitas).
        public readonly Process Proc = Process.GetCurrentProcess();

        // CPU acumulada al inicio (guardada por si se decide exponerlo en el futuro).
        public TimeSpan CpuInicio;

        // Memoria en MB al final del escenario (WorkingSet64 / 1024^2).
        public double MemoriaMb;

        // Inicia la ventana de medicion del escenario completo.
        // - Refresh del proceso por si hay datos obsoletos.
        // - Guarda la CPU inicial.
        // - Reinicia el cronometro.
        public void StartTimers()
        {
            Proc.Refresh();
            CpuInicio = Proc.TotalProcessorTime;
            Reloj.Restart();
        }

        // Detiene la ventana de medicion y devuelve medidas brutas:
        // - cpuSeg: CPU de proceso usada durante el escenario (aprox).
        // - memMb: WorkingSet en MB al final.
        // - durSeg: duracion total en segundos.
        public (double cpuSeg, double memMb, double durSeg) StopTimers()
        {
            Reloj.Stop();
            Proc.Refresh();
            var cpuSeg = (Proc.TotalProcessorTime - CpuInicio).TotalSeconds;
            MemoriaMb = Proc.WorkingSet64 / (1024.0 * 1024.0);
            return (cpuSeg, MemoriaMb, Reloj.Elapsed.TotalSeconds);
        }

        // Crea un MetricSnapshot agregando estadisticos desde lo acumulado:
        // - convierte ticks a ms
        // - calcula media, p95, p99
        // - calcula throughput
        public static MetricSnapshot ToSnapshot(string nombre, MetricBuffer b, int iters, int grado)
        {
            // Se copia la cola a un arreglo para ordenar y calcular percentiles
            var lat = b.LatenciasTicks.ToArray();
            Array.Sort(lat);

            // Conversion de ticks a milisegundos:
            //   ms = ticks * (1000 / Stopwatch.Frequency)
            double ticksToMs = 1000.0 / Stopwatch.Frequency;

            // Estadisticos con manejo de caso vacio (sin muestras)
            double media = lat.Length == 0 ? 0 : lat.Average() * ticksToMs;
            double p95 = lat.Length == 0 ? 0 : lat[(int)Math.Floor(0.95 * (lat.Length - 1))] * ticksToMs;
            double p99 = lat.Length == 0 ? 0 : lat[(int)Math.Floor(0.99 * (lat.Length - 1))] * ticksToMs;

            // Duracion total medida por el cronometro del escenario
            var dur = b.Reloj.Elapsed.TotalSeconds;

            // Ensambla el snapshot listo para exportar
            return new MetricSnapshot
            {
                Nombre = nombre,
                Iteraciones = iters,
                Exitos = b.Exitos,
                Fallos = b.Fallos,
                DuracionSeg = dur,
                ThroughputOpsSeg = dur <= 0 ? 0 : b.Exitos / dur,
                LatenciaMediaMs = media,
                P95Ms = p95,
                P99Ms = p99,
                BytesProcesados = b.Bytes,
                CpuProcesoSeg = 0,  // reservado; por simplicidad no lo exponemos
                MemoriaMb = b.MemoriaMb,
                GradoParalelismo = grado,
                Timestamp = DateTimeOffset.Now
            };
        }
    }
}
