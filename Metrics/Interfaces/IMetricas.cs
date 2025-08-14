using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Metrics.Interfaces
{
    using BrigadasEmergenciaRD.Metrics.Models;

    // Interfaz para cualquier clase que recolecta metricas
    public interface IMetricas
    {
        // Marca inicio de una operacion (placeholder)
        void BeginOp();
        // Marca fin de una operacion con tiempo en ticks y si fue exitosa
        void EndOp(long elapsedTicks, bool success);
        // Suma bytes procesados
        void AddBytes(long bytes);
        // Devuelve resumen inmutable de metricas
        MetricSnapshot Snapshot();
        // Reinicia el estado interno
        void Reset();
    }
}
