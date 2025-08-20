using System;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Parallelism
{
    public class ResultadoComparacion
    {
        public TimeSpan TiempoEjecucionSecuencial { get; set; }
        public TimeSpan TiempoEjecucionParalela { get; set; }
        public int NucleosProcesadorUtilizados { get; set; }
        public string EstrategiaParalelizacion { get; set; }
        public EstadisticasParalelismo DetallesParalelismo { get; set; }

        // Método para calcular métricas adicionales como aceleración o eficiencia
        public void CalcularMetricas()
        {
            // Ejemplo simple: se pueden calcular ratios
            double aceleracion = TiempoEjecucionSecuencial.TotalMilliseconds /
                                 TiempoEjecucionParalela.TotalMilliseconds;
            double eficiencia = aceleracion / NucleosProcesadorUtilizados * 100;

            // Solo para demo, puedes agregar propiedades para guardarlo
            Console.WriteLine($"Aceleración: {aceleracion:F2}");
            Console.WriteLine($"Eficiencia: {eficiencia:F2}%");
        }
    }
}
