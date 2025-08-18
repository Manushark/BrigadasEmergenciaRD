using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using BrigadasEmergenciaRD.Metrics.Collectors;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Comparisons
{
    // Runner generico para ejecutar una operacion N veces
    // en modo secuencial o paralelo, y producir un MetricSnapshot.
    // Uso:
    //   var s = BenchmarkRunner.Run("Escenario", i => MiOperacion(i), 1000, 4);
    // Donde:
    //   - "i" es el indice de la iteracion (0..N-1)
    //   - "MiOperacion" puede ser async (Task) o sincrona envuelta con Task.CompletedTask
    public static class BenchmarkRunner
    {
        // Parametros:
        // - nombre: identificador del escenario (aparece en reportes)
        // - op: delegado por iteracion (puede hacer CPU, IO o logica real del sistema)
        // - iteraciones: cuantas veces se ejecuta "op"
        // - gradoParalelismo: 1 = secuencial; >1 = paralelo con MaxDegreeOfParallelism
        public static MetricSnapshot Run(
            string nombre,
            Func<int, Task> op,
            int iteraciones,
            int gradoParalelismo)
        {
            // Instancia del colector de metricas.
            // Reset para arrancar desde cero y Start para abrir ventana de medicion.
            var m = new MetricasRendimiento();
            m.Reset();
            m.Start();

            // Caso 1: modo secuencial
            if (gradoParalelismo <= 1)
            {
                // Bucle simple de 0 a iteraciones-1
                for (int i = 0; i < iteraciones; i++)
                {
                    // Tomamos timestamp inicial en ticks de Stopwatch
                    var t0 = Stopwatch.GetTimestamp();
                    try
                    {
                        // Ejecutar operacion; si es async, se espera con .Wait()
                        op(i).Wait();

                        // Registrar fin exitoso con delta de ticks
                        m.EndOp(Stopwatch.GetTimestamp() - t0, true);
                    }
                    catch
                    {
                        // Registrar fin con fallo (delta de ticks igual se mide)
                        m.EndOp(Stopwatch.GetTimestamp() - t0, false);
                    }
                }
            }
            // Caso 2: modo paralelo
            else
            {
                // Parallel.For con MaxDegreeOfParallelism controla concurrencia
                Parallel.For(0, iteraciones,
                    new ParallelOptions { MaxDegreeOfParallelism = gradoParalelismo },
                    i =>
                    {
                        var t0 = Stopwatch.GetTimestamp();
                        try
                        {
                            op(i).Wait();
                            m.EndOp(Stopwatch.GetTimestamp() - t0, true);
                        }
                        catch
                        {
                            m.EndOp(Stopwatch.GetTimestamp() - t0, false);
                        }
                    });
            }

            // Cerrar ventana de medicion y tomar medidas brutas (cpu, mem, duracion)
            var (_, mem, dur) = m.Stop();

            // Armar snapshot inmutable con agregados
            var s = m.Snapshot();
            s.Nombre = nombre;                  // nombre de escenario para reportes
            s.DuracionSeg = dur;                // duracion total real del escenario
            s.MemoriaMb = mem;                  // memoria del proceso al final
            s.GradoParalelismo = Math.Max(1, gradoParalelismo); // guarda el grado usado
            return s;
        }
    }
}
