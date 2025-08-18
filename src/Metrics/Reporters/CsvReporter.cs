using System.Globalization;
using System.IO;
using System.Text;
using BrigadasEmergenciaRD.Metrics.Models;

namespace BrigadasEmergenciaRD.Metrics.Reporters
{
    // Exporta un MetricSnapshot a formato CSV simple.
    // Ventajas:
    // - Sin dependencias externas
    // - Abrible en Excel o Google Sheets
    // - Culture invariante para evitar problemas de coma/punto
    public static class CsvReporter
    {
        // Guarda un archivo CSV en la ruta indicada.
        // Si la carpeta no existe, se crea.
        public static void Save(string path, MetricSnapshot s)
        {
            // Asegurar carpeta destino
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            // StringBuilder para construir el contenido CSV
            var sb = new StringBuilder();

            // Cabecera con nombres de columnas (una sola fila)
            sb.AppendLine("Nombre,Iteraciones,Exitos,Fallos,DuracionSeg,ThroughputOpsSeg,LatenciaMediaMs,P95Ms,P99Ms,Bytes,MemoriaMb,GradoParalelismo");

            // Fila con los datos del snapshot
            sb.AppendLine(Row(s));

            // Escribir al disco en UTF-8
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        // Convierte un MetricSnapshot a una linea CSV usando culture invariante
        private static string Row(MetricSnapshot m) => string.Join(",",
            m.Nombre,
            m.Iteraciones.ToString(CultureInfo.InvariantCulture),
            m.Exitos.ToString(CultureInfo.InvariantCulture),
            m.Fallos.ToString(CultureInfo.InvariantCulture),
            m.DuracionSeg.ToString(CultureInfo.InvariantCulture),
            m.ThroughputOpsSeg.ToString(CultureInfo.InvariantCulture),
            m.LatenciaMediaMs.ToString(CultureInfo.InvariantCulture),
            m.P95Ms.ToString(CultureInfo.InvariantCulture),
            m.P99Ms.ToString(CultureInfo.InvariantCulture),
            m.BytesProcesados.ToString(CultureInfo.InvariantCulture),
            m.MemoriaMb.ToString(CultureInfo.InvariantCulture),
            m.GradoParalelismo.ToString(CultureInfo.InvariantCulture)
        );
    }
}
