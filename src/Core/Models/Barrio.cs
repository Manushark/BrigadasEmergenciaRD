using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    // Representa un barrio dentro de un municipio
    public class Barrio
    {
        public int Id { get; set; }
        public int MunicipioId { get; set; }
        public string Nombre { get; set; }
        public int Poblacion { get; set; }
        public Coordenada Coordenadas { get; set; }
        public bool EnEmergencia { get; set; }   // Estado de emergencia

        public ConcurrentQueue<LlamadaEmergencia> LlamadasPendientes { get; set; } // Cola de llamadas
        public Municipio Municipio { get; set; }

        public Barrio()
        {
            LlamadasPendientes = new ConcurrentQueue<LlamadaEmergencia>();
            EnEmergencia = false;
        }

        public void AgregarLlamadaEmergencia(LlamadaEmergencia llamada)
        {
            LlamadasPendientes.Enqueue(llamada);
            EnEmergencia = true;
        }

        public bool TenerLlamadaPendiente(out LlamadaEmergencia llamada)
        {
            return LlamadasPendientes.TryDequeue(out llamada);
        }
    }

}
