using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Metrics.Analyzers
{
    // Calculos de speedup y eficiencia
    public static class AnalizadorResultados
    {
        public static double Speedup(double tSecuencial, double tParalela)
        {
            return tParalela <= 0 ? 0 : tSecuencial / tParalela;
        }

        public static double Eficiencia(double speedup, int p)
        {
            return p <= 0 ? 0 : speedup / p;
        }

        public static (double speedup, double eficiencia) Comparar(double tSec, double tPar, int p)
        {
            var s = Speedup(tSec, tPar);
            return (s, Eficiencia(s, p));
        }
    }
}
