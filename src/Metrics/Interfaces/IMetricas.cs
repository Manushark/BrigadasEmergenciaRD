using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Metrics.Interfaces
{
    using BrigadasEmergenciaRD.Metrics.Models;

    // Interface base del modulo de metricas.
    // Esta interface define un contrato minimo para medir operaciones.
    // Ventajas:
    // - Bajo acoplamiento: no obliga a usar una clase concreta.
    // - Facil de simular en pruebas unitarias si lo necesitas.
    public interface IMetricas
    {
        // Marca el inicio de una operacion (opcional).
        // Se deja por si en el futuro se desea medir costos de setup por operacion.
        void BeginOp();

        // Marca el final de una operacion.
        // Parametros:
        // - elapsedTicks: tiempo transcurrido en ticks de Stopwatch (alta resolucion).
        // - success: true si la operacion termino correctamente, false si hubo error.
        void EndOp(long elapsedTicks, bool success);

        // Suma bytes procesados asociados a la operacion (opcional).
        // Ejemplo: lectura/escritura de archivos o red.
        void AddBytes(long bytes);

        // Devuelve un snapshot inmutable con agregados listos para reportes.
        // El snapshot contiene: media, p95, p99, throughput, memoria, etc.
        MetricSnapshot Snapshot();

        // Resetea el estado interno del colector:
        // limpia colas de latencias y pone contadores en cero.
        void Reset();
    }
}
