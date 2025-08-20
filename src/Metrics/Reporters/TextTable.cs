// src/Metrics/Reporters/TextTable.cs
using System;
using System.Linq;
using System.Text;

namespace BrigadasEmergenciaRD.Metrics.Reporters
{
    // Construye tablas ascii simples
    public static class TextTable
    {
        // title puede ser null o vacio
        public static string Build(string title, string[] headers, string[][] rows)
        {
            int[] widths = new int[headers.Length];
            for (int c = 0; c < headers.Length; c++)
                widths[c] = Math.Max(headers[c]?.Length ?? 0, rows.Length > 0 ? rows.Max(r => (r[c]?.Length ?? 0)) : 0);

            string Line(char corner, char dash)
            {
                var sb = new StringBuilder();
                sb.Append(corner);
                for (int c = 0; c < widths.Length; c++)
                {
                    sb.Append(new string(dash, widths[c] + 2));
                    sb.Append(corner);
                }
                return sb.ToString();
            }

            string Row(string[] cols)
            {
                var sb = new StringBuilder();
                sb.Append("|");
                for (int c = 0; c < widths.Length; c++)
                {
                    var cell = cols[c] ?? "";
                    sb.Append(" " + cell.PadRight(widths[c]) + " ");
                    sb.Append("|");
                }
                return sb.ToString();
            }

            var top = Line('+', '=');
            var sep = Line('+', '-');

            var outSb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(title))
            {
                outSb.AppendLine(title);
                outSb.AppendLine(new string('=', Math.Max(title.Length, 10)));
            }

            outSb.AppendLine(top);
            outSb.AppendLine(Row(headers));
            outSb.AppendLine(sep);
            foreach (var r in rows) outSb.AppendLine(Row(r));
            outSb.AppendLine(top);

            return outSb.ToString();
        }
    }
}
