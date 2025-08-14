using System.Globalization;
using System.IO;
using System.Text;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Reporters
{
    // Reporte CSV con datos crudos de ambos escenarios
    public static class CsvReporter
    {
        public static void Save(string path, MetricSnapshot sec, MetricSnapshot par, double speedup, double eficiencia)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var sb = new StringBuilder();
            sb.AppendLine("Escenario,Iteraciones,Exitos,DuracionSeg,ThroughputOpsSeg,LatenciaMediaMs,P95Ms,P99Ms,Bytes,CPU_Seg,MemoriaMb,GradoParalelismo,Speedup,Eficiencia");
            sb.AppendLine(Row("Secuencial", sec, speedup, eficiencia));
            sb.AppendLine(Row("Paralela", par, speedup, eficiencia));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string Row(string nombre, MetricSnapshot m, double s, double e)
        {
            return string.Join(",",
                nombre,
                m.Iteraciones.ToString(CultureInfo.InvariantCulture),
                m.Exitos.ToString(CultureInfo.InvariantCulture),
                m.DuracionSeg.ToString(CultureInfo.InvariantCulture),
                m.ThroughputOpsSeg.ToString(CultureInfo.InvariantCulture),
                m.LatenciaMediaMs.ToString(CultureInfo.InvariantCulture),
                m.P95Ms.ToString(CultureInfo.InvariantCulture),
                m.P99Ms.ToString(CultureInfo.InvariantCulture),
                m.BytesProcesados.ToString(CultureInfo.InvariantCulture),
                m.CpuProcesoSeg.ToString(CultureInfo.InvariantCulture),
                m.MemoriaMb.ToString(CultureInfo.InvariantCulture),
                m.GradoParalelismo.ToString(CultureInfo.InvariantCulture),
                s.ToString(CultureInfo.InvariantCulture),
                e.ToString(CultureInfo.InvariantCulture)
            );
        }
    }
}
