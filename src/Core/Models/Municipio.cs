using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    // Representa un municipio dentro de una provincia
    public class Municipio
    {
        public int Id { get; set; }
        public int ProvinciaId { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public int Poblacion { get; set; }
        public Coordenada Coordenadas { get; set; }

        public List<Barrio> Barrios { get; set; } // Barrios dentro del municipio
        public Provincia Provincia { get; set; }    // Provincia a la que pertenece

        public Municipio()
        {
            Barrios = new List<Barrio>();
        }

        public int ContarBarriosEnEmergencia()
        {
            int count = 0;
            foreach (var barrio in Barrios)
            {
                if (barrio.EnEmergencia) count++;
            }
            return count;
        }
    }
}
