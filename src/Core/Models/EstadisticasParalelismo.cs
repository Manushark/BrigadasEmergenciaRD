using System;
using System.Collections.Generic;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class EstadisticasParalelismo
    {
        public int TotalTareasEjecutadas { get; set; }       // Total de tareas procesadas
        public int MaximoHilosConcurrentes { get; set; }     // Número máximo de hilos concurrentes
        public int TareasEjecutadasEnParalelo { get; set; }  // Número de tareas ejecutadas en paralelo
        public List<MedicionHilo> MedicionesPorHilo { get; set; } = new List<MedicionHilo>();
    }
}
