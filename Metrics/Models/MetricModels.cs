using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace BrigadasEmergenciaRD.Metrics.Models
{
    // Resumen de metricas de un escenario
    public sealed record MetricSnapshot
    {
        public string Nombre { get; init; } = "";
        public int Iteraciones { get; init; }
        public long Exitos { get; init; }
        public long Fallos { get; init; }
        public double DuracionSeg { get; init; }
        public double ThroughputOpsSeg { get; init; }
        public double LatenciaMediaMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public long BytesProcesados { get; init; }
        public double CpuProcesoSeg { get; init; }
        public double MemoriaMb { get; init; }
        public int GradoParalelismo { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    }

    // Buffer interno para latencias, contadores y timers
    internal sealed class MetricBuffer
    {
        public readonly ConcurrentQueue<long> LatenciasTicks = new();
        public long Exitos;
        public long Fallos;
        public long Bytes;
        public readonly Stopwatch Reloj = new();
        public readonly Process Proc = Process.GetCurrentProcess();
        public TimeSpan CpuInicio;
        public double MemoriaMb;

        public void StartTimers()
        {
            Proc.Refresh();
            CpuInicio = Proc.TotalProcessorTime;
            Reloj.Restart();
        }

        public (double cpuSeg, double memMb, double durSeg) StopTimers()
        {
            Reloj.Stop();
            Proc.Refresh();
            var cpuSeg = (Proc.TotalProcessorTime - CpuInicio).TotalSeconds;
            MemoriaMb = Proc.WorkingSet64 / (1024.0 * 1024.0);
            return (cpuSeg, MemoriaMb, Reloj.Elapsed.TotalSeconds);
        }

        public static MetricSnapshot ToSnapshot(string nombre, MetricBuffer b, int iters, int grado)
        {
            var lat = b.LatenciasTicks.ToArray();
            Array.Sort(lat);
            double ticksToMs = 1000.0 / Stopwatch.Frequency;
            double media = lat.Length == 0 ? 0 : lat.Average() * ticksToMs;
            double p95 = lat.Length == 0 ? 0 : lat[(int)Math.Floor(0.95 * (lat.Length - 1))] * ticksToMs;
            double p99 = lat.Length == 0 ? 0 : lat[(int)Math.Floor(0.99 * (lat.Length - 1))] * ticksToMs;
            var dur = b.Reloj.Elapsed.TotalSeconds;

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
                CpuProcesoSeg = 0,
                MemoriaMb = b.MemoriaMb,
                GradoParalelismo = grado,
                Timestamp = DateTimeOffset.Now
            };
        }
    }
}

