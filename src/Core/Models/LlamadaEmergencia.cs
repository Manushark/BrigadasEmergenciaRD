using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;

namespace BrigadasEmergenciaRD.Core.Models
{
    // Representa una llamada de emergencia que llega a la central
    public class LlamadaEmergencia
    {
        public int Id { get; set; }
        public TipoEmergencia TipoEmergencia { get; set; }
        public string Descripcion { get; set; }
        public Coordenada Ubicacion { get; set; }
        public DateTime TimestampLlamada { get; set; } // Momento en que se recibe
        public int PersonasAfectadas { get; set; }
        public int Prioridad { get; set; }  // 0-100
        public string NombreReportante { get; set; }
        public string TelefonoContacto { get; set; }

        public int BarrioId { get; set; }
        public Barrio Barrio { get; set; }

        public LlamadaEmergencia()
        {
            TimestampLlamada = DateTime.Now;
            Id = new Random().Next(10000, 99999);  // ID aleatorio temporal
        }

        public TimeSpan TiempoEspera()
        {
            return DateTime.Now - TimestampLlamada;
        }

        public bool EsUrgente()
        {
            return Prioridad >= 80 ||
                   TiempoEspera().TotalMinutes > 30 ||
                   PersonasAfectadas > 10;
        }
    }

}
