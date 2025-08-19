using System;
using System.IO;

namespace BrigadasEmergenciaRD.Metrics.Reporters
{
    /// <summary>
    /// Genera archivos .txt en la carpeta resultados-reportes sin timestamp.
    /// Por defecto sobrescribe el archivo si ya existe (comportamiento deseado
    /// cuando quieres tener "el ultimo reporte" con un nombre fijo).
    /// </summary>
    public static class TextReporter
    {
        // Carpeta de salida fija: <repo>/resultados-reportes
        public static readonly string OutDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "resultados-reportes")
        );

        /// <summary>
        /// Guarda un contenido de texto como .txt usando un nombre base sin fecha.
        /// Ejemplo: Save("sim_paralela_reporte", "...") -> resultados-reportes/sim_paralela_reporte.txt
        /// </summary>
        /// <param name="baseName">Nombre base sin extension</param>
        /// <param name="content">Contenido del archivo</param>
        /// <param name="overwrite">Si true, sobrescribe; si false, crea _v2, _v3, etc.</param>
        public static string Save(string baseName, string content, bool overwrite = true)
        {
            Directory.CreateDirectory(OutDir);

            // Nombre final sin timestamp
            var filePath = Path.Combine(OutDir, $"{Sanitize(baseName)}.txt");

            if (!overwrite && File.Exists(filePath))
                filePath = MakeUnique(filePath);

            File.WriteAllText(filePath, content);
            return filePath;
        }

        // Si overwrite=false y el archivo existe, genera nombre _v2, _v3, ...
        private static string MakeUnique(string path)
        {
            var dir = Path.GetDirectoryName(path)!;
            var file = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            int i = 2;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{file}_v{i}{ext}");
                i++;
            } while (File.Exists(candidate));
            return candidate;
        }

        // Limpia caracteres problematicos en nombres de archivo
        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim();
        }
    }
}
