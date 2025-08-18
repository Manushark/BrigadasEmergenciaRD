using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Metrics.Models
{
    // POCO (Plain Old CLR Object) para transportar resultados de medicion.
    // No contiene logica, solo datos. Facil de serializar a CSV/Markdown.
    public sealed class MetricSnapshot
    {
        // Nombre del escenario (ej: CPU_Demo, IO_Demo, AsignacionBrigadas)
        public string Nombre { get; set; } = "";

        // Total de iteraciones observadas (Exitos + Fallos)
        public int Iteraciones { get; set; }

        // Contadores de estado por operacion
        public long Exitos { get; set; }
        public long Fallos { get; set; }

        // Duracion total del escenario en segundos (no por operacion)
        public double DuracionSeg { get; set; }

        // Throughput aproximado: Exitos / Duracion total en seg
        public double ThroughputOpsSeg { get; set; }

        // Latencia media por operacion en milisegundos
        public double LatenciaMediaMs { get; set; }

        // Percentiles de latencia: p95 y p99 en ms
        public double P95Ms { get; set; }
        public double P99Ms { get; set; }

        // Bytes acumulados procesados por las operaciones (si aplica)
        public long BytesProcesados { get; set; }

        // CPU total del proceso en seg durante el escenario (reservado para futuro)
        public double CpuProcesoSeg { get; set; }

        // Memoria del proceso (WorkingSet) al final del escenario en MB
        public double MemoriaMb { get; set; }

        // Grado de paralelismo con el que corrio el escenario
        public int GradoParalelismo { get; set; }

        // Marca temporal para ubicar el experimento en el tiempo
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    }
}
