using System;
using System.Threading;
using BrigadasEmergenciaRD.Metrics.Interfaces;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Collectors
{
    // Colector que implementa IMetricas.
    // Caracteristicas:
    // - Diseñado para uso desde multiples hilos.
    // - EndOp no bloquea: encola latencia y usa atomicos para contadores.
    // - Expone Start/Stop para medir ventana total del escenario (build de snapshot mas completo).
    public sealed class MetricasRendimiento : IMetricas
    {
        // Estado interno almacenado en un buffer mutable
        private readonly MetricBuffer _buffer = new();

        // Hook opcional al inicio de una operacion (sin logica por ahora)
        public void BeginOp() { }

        // Registra latencia y exito/fallo de una operacion.
        // - elapsedTicks: delta de Stopwatch.GetTimestamp()
        // - success: true = ok, false = fallo
        public void EndOp(long elapsedTicks, bool success)
        {
            _buffer.LatenciasTicks.Enqueue(elapsedTicks);
            if (success)
            {
                // Interlocked.Increment incrementa de forma atomica sin locks
                Interlocked.Increment(ref _buffer.Exitos);
            }
            else
            {
                Interlocked.Increment(ref _buffer.Fallos);
            }
        }

        // Suma bytes procesados (opcional segun escenario)
        public void AddBytes(long bytes)
        {
            // Interlocked.Add para sumar de forma atomica
            Interlocked.Add(ref _buffer.Bytes, bytes);
        }

        // Construye un snapshot con estadisticos agregados
        public MetricSnapshot Snapshot()
        {
            // iteraciones = exitos + fallos
            int iters = (int)(_buffer.Exitos + _buffer.Fallos);

            // grado = referencia; usamos cores del sistema como valor por defecto
            int grado = Math.Max(1, Environment.ProcessorCount);

            // delegamos el calculo detallado en MetricBuffer.ToSnapshot
            return MetricBuffer.ToSnapshot("default", _buffer, iters, grado);
        }

        // Limpia el estado interno para un nuevo escenario
        public void Reset()
        {
            // vaciar cola de latencias
            while (_buffer.LatenciasTicks.TryDequeue(out _)) { }
            // reiniciar contadores
            _buffer.Exitos = 0;
            _buffer.Fallos = 0;
            _buffer.Bytes = 0;
        }

        // Marca inicio de ventana de medicion
        public void Start()
        {
            _buffer.StartTimers();
        }

        // Marca fin de ventana de medicion y devuelve medidas brutas (cpu, mem, duracion)
        public (double cpuSeg, double memMb, double durSeg) Stop()
        {
            return _buffer.StopTimers();
        }
    }
}
