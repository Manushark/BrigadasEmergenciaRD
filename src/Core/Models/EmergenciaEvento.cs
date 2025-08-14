using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;

namespace BrigadasEmergenciaRD.Core.Models
{
    // Evento de emergencia generado por el simulador
    public class EmergenciaEvento
    {
        public int Id { get; set; }
        public TipoEmergencia Tipo { get; set; }
        public IntensidadTormenta Intensidad { get; set; }
        public Coordenada Ubicacion { get; set; }
        public DateTime Timestamp { get; set; }
        public int PersonasAfectadas { get; set; }
        public string Descripcion { get; set; }

        public int ProvinciaId { get; set; }
        public int MunicipioId { get; set; }
        public int BarrioId { get; set; }

        public EmergenciaEvento()
        {
            Timestamp = DateTime.Now;
            Id = new Random().Next(100000, 999999);
        }

        public LlamadaEmergencia ConvertirALlamada()
        {
            return new LlamadaEmergencia
            {
                TipoEmergencia = Tipo,
                Ubicacion = Ubicacion,
                TimestampLlamada = Timestamp,
                PersonasAfectadas = PersonasAfectadas,
                Descripcion = Descripcion,
                BarrioId = BarrioId,
                Prioridad = CalcularPrioridad()
            };
        }

        private int CalcularPrioridad()
        {
            var prioridad = 0;

            // Prioridad por tipo
            prioridad += Tipo switch
            {
                TipoEmergencia.PersonasAtrapadas => 100,
                TipoEmergencia.EmergenciaMedica => 95,
                TipoEmergencia.IncendioEstructural => 90,
                TipoEmergencia.DeslizamientoTierra => 85,
                TipoEmergencia.Inundacion => 80,
                TipoEmergencia.AccidenteVehicular => 75,
                TipoEmergencia.VientosFuertes => 60,
                TipoEmergencia.CorteEnergia => 40,
                _ => 30
            };

            // Prioridad por intensidad
            prioridad += (int)Intensidad * 10;

            // Prioridad por personas afectadas
            prioridad += Math.Min(PersonasAfectadas / 5, 50);

            return Math.Min(prioridad, 200);
        }
    }
}
