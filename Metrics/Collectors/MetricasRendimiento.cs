using System;
using System.Threading;
using BrigadasEmergenciaRD.Metrics.Interfaces;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Collectors
{
    // Colector basico de metricas
    public sealed class MetricasRendimiento : IMetricas
    {
        private readonly MetricBuffer _buffer = new();

        public void BeginOp() { }

        public void EndOp(long elapsedTicks, bool success)
        {
            _buffer.LatenciasTicks.Enqueue(elapsedTicks);
            if (success) Interlocked.Increment(ref _buffer.Exitos);
            else Interlocked.Increment(ref _buffer.Fallos);
        }

        public void AddBytes(long bytes) { Interlocked.Add(ref _buffer.Bytes, bytes); }

        public MetricSnapshot Snapshot()
        {
            return MetricBuffer.ToSnapshot("default", _buffer, (int)(_buffer.Exitos + _buffer.Fallos), Environment.ProcessorCount);
        }

        public void Reset()
        {
            while (_buffer.LatenciasTicks.TryDequeue(out _)) { }
            _buffer.Exitos = 0;
            _buffer.Fallos = 0;
            _buffer.Bytes = 0;
        }

        public void Start() { _buffer.StartTimers(); }
        public (double cpuSeg, double memMb, double durSeg) Stop() { return _buffer.StopTimers(); }
    }
}
