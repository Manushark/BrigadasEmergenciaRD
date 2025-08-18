using System.IO;
using System.Text;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Reporters
{
    // Exporta un MetricSnapshot a un archivo Markdown (.md).
    // Util para informes o README tecnicos.
    public static class MarkdownReporter
    {
        // Escribe un archivo .md con un resumen de los campos clave
        public static void Save(string path, MetricSnapshot s)
        {
            // Asegurar carpeta destino
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // Construccion del markdown
            var sb = new StringBuilder();

            // Titulo y pares clave-valor
            sb.AppendLine("# Resumen de rendimiento");
            sb.AppendLine($"- Nombre: {s.Nombre}");
            sb.AppendLine($"- Iteraciones: {s.Iteraciones}");
            sb.AppendLine($"- Exitos: {s.Exitos}  |  Fallos: {s.Fallos}");
            sb.AppendLine($"- Duracion (s): {s.DuracionSeg:0.000}");
            sb.AppendLine($"- Throughput (ops/s): {s.ThroughputOpsSeg:0.00}");
            sb.AppendLine($"- Latencia media (ms): {s.LatenciaMediaMs:0.00}");
            sb.AppendLine($"- P95 (ms): {s.P95Ms:0.00}  |  P99 (ms): {s.P99Ms:0.00}");
            sb.AppendLine($"- Memoria (MB): {s.MemoriaMb:0.0}");
            sb.AppendLine($"- Grado de paralelismo: {s.GradoParalelismo}");

            // Escribir al disco
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }
}
