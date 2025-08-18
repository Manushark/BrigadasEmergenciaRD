using System;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class MedicionHilo
    {
        public int IdHilo { get; set; }                 // ID del hilo
        public int TareasCompletadas { get; set; }     // Número de tareas completadas por este hilo
        public TimeSpan TiempoTotalEjecucion { get; set; }  // Tiempo total de ejecución del hilo
        public double PorcentajeUso { get; set; }      // Porcentaje de uso del hilo (calculado)
    }
}
