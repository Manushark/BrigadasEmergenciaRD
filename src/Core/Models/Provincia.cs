using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    // Representa una provincia en Republica Dominicana
    public class Provincia
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public string Region { get; set; }
        public int Poblacion { get; set; }
        public double AreaKm2 { get; set; }
        public Coordenada Coordenadas { get; set; }
        public string VulnerabilidadClimatica { get; set; }

        public List<Municipio> Municipios { get; set; }
        public ConcurrentBag<Brigada> BrigadasDisponibles { get; set; }

        public Provincia()
        {
            Municipios = new List<Municipio>();  // Lista de municipios
            BrigadasDisponibles = new ConcurrentBag<Brigada>(); // Brigadas que se pueden enviar
        }

        public int ContarBarriosTotal()
        {
            int total = 0;
            foreach (var municipio in Municipios)
            {
                total += municipio.Barrios?.Count ?? 0;
            }
            return total;
        }

        public int ContarBrigadasDisponibles()
        {
            return BrigadasDisponibles.Count;
        }
    }
}