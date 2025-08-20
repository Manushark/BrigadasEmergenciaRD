using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    // Representa una ubicación geográfica con latitud y longitud
    public class Coordenada
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }

        public Coordenada() { }

        public Coordenada(double latitud, double longitud)
        {
            Latitud = latitud;
            Longitud = longitud;
        }

        // Calcula distancia entre esta coordenada y otra usando fórmula Haversine
        public double CalcularDistanciaKm(Coordenada destino)
        {
            const double radioTierraKm = 6371;

            var lat1Rad = DegreesToRadians(Latitud);
            var lat2Rad = DegreesToRadians(destino.Latitud);
            var deltaLat = DegreesToRadians(destino.Latitud - Latitud);
            var deltaLon = DegreesToRadians(destino.Longitud - Longitud);

            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return radioTierraKm * c;
        }

        private double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    }
}