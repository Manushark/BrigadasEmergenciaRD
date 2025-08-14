using System.IO;
using System.Text;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Reporters
{
    // Resumen en Markdown para el informe
    public static class MarkdownReporter
    {
        public static void Save(string path, MetricSnapshot sec, MetricSnapshot par, double speedup, double eficiencia)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var sb = new StringBuilder();
            sb.AppendLine("# Resultados de rendimiento");
            sb.AppendLine();
            sb.AppendLine($"- Speedup: {speedup:0.00}");
            sb.AppendLine($"- Eficiencia: {eficiencia:P2}");
            sb.AppendLine();
            sb.AppendLine("## Secuencial");
            sb.AppendLine(Bullets(sec));
            sb.AppendLine("## Paralela");
            sb.AppendLine(Bullets(par));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string Bullets(MetricSnapshot m)
        {
            var s = new StringBuilder();
            s.AppendLine($"- Iteraciones: {m.Iteraciones}");
            s.AppendLine($"- Exitos: {m.Exitos}");
            s.AppendLine($"- Duracion (s): {m.DuracionSeg:0.000}");
            s.AppendLine($"- Throughput (ops/s): {m.ThroughputOpsSeg:0.00}");
            s.AppendLine($"- Latencia media (ms): {m.LatenciaMediaMs:0.00}");
            s.AppendLine($"- P95 (ms): {m.P95Ms:0.00}");
            s.AppendLine($"- P99 (ms): {m.P99Ms:0.00}");
            s.AppendLine($"- Memoria (MB): {m.MemoriaMb:0.0}");
            s.AppendLine($"- Paralelismo: {m.GradoParalelismo}");
            return s.ToString();
        }
    }
}
