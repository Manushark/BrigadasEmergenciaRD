using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;

namespace BrigadasEmergenciaRD.Core.Models
{
    // Representa una brigada de emergencia
    public class Brigada : IBrigada
    {
        public int Id { get; set; } // ID único de la brigada
        public string Nombre { get; set; } // Nombre descriptivo
        public TipoBrigada Tipo { get; set; } // Tipo (Rescate, Médica, Bomberos, etc.)
        public EstadoBrigada Estado { get; set; }  
        public Coordenada UbicacionActual { get; set; } // Posición actual
        public int CapacidadMaxima { get; set; } // Cuántas personas puede atender a la vez
        public DateTime? UltimaActivacion { get; set; } // Última vez que se activó

        public Brigada()
        {
            Estado = EstadoBrigada.Disponible;
            UbicacionActual = new Coordenada();
        }

        // Simula atender una emergencia
        public async Task<bool> AtenderEmergenciaAsync(LlamadaEmergencia llamada)
        {
            if (!PuedeAtender(llamada.TipoEmergencia) || Estado != EstadoBrigada.Disponible)
                return false;

            CambiarEstado(EstadoBrigada.EnRuta);

            // Simular tiempo de traslado
            var tiempoRespuesta = await CalcularTiempoRespuestaAsync(llamada.Ubicacion);
            await Task.Delay((int)tiempoRespuesta.TotalMilliseconds / 100); // Escala de tiempo acelerada

            CambiarEstado(EstadoBrigada.AtendendoEmergencia);

            // Simular tiempo de atención
            var tiempoAtencion = CalcularTiempoAtencion(llamada.TipoEmergencia);
            await Task.Delay((int)tiempoAtencion.TotalMilliseconds / 100);

            // Actualizar ubicación
            UbicacionActual = llamada.Ubicacion;
            UltimaActivacion = DateTime.Now;

            CambiarEstado(EstadoBrigada.Disponible);
            return true;
        }

        // Calcula tiempo de respuesta basado en distancia y velocidad promedio
        public async Task<TimeSpan> CalcularTiempoRespuestaAsync(Coordenada destino)
        {
            var distanciaKm = UbicacionActual.CalcularDistanciaKm(destino);
            var velocidadPromedioKmh = ObtenerVelocidadPromedio();

            var tiempoHoras = distanciaKm / velocidadPromedioKmh;
            return TimeSpan.FromHours(tiempoHoras);
        }

        // Verifica si la brigada puede atender un tipo de emergencia
        public bool PuedeAtender(TipoEmergencia tipoEmergencia)
        {
            return Tipo switch
            {
                TipoBrigada.Rescate => tipoEmergencia == TipoEmergencia.PersonasAtrapadas ||
                                     tipoEmergencia == TipoEmergencia.DeslizamientoTierra,
                TipoBrigada.Medica => tipoEmergencia == TipoEmergencia.EmergenciaMedica ||
                                    tipoEmergencia == TipoEmergencia.AccidenteVehicular,
                TipoBrigada.Bomberos => tipoEmergencia == TipoEmergencia.IncendioEstructural,
                TipoBrigada.DefensaCivil => true, // Puede atender cualquier tipo
                TipoBrigada.PoliciaNacional => tipoEmergencia == TipoEmergencia.AccidenteVehicular,
                TipoBrigada.CruzRoja => tipoEmergencia == TipoEmergencia.EmergenciaMedica,
                TipoBrigada.Especializada => true,
                _ => false
            };
        }

        public void CambiarEstado(EstadoBrigada nuevoEstado)
        {
            Estado = nuevoEstado;
        }

        private double ObtenerVelocidadPromedio()
        {
            return Tipo switch
            {
                TipoBrigada.Rescate => 45.0,
                TipoBrigada.Medica => 60.0,
                TipoBrigada.Bomberos => 50.0,
                TipoBrigada.DefensaCivil => 40.0,
                TipoBrigada.PoliciaNacional => 55.0,
                TipoBrigada.CruzRoja => 50.0,
                TipoBrigada.Especializada => 35.0,
                _ => 40.0
            };
        }

        private TimeSpan CalcularTiempoAtencion(TipoEmergencia tipoEmergencia)
        {
            return tipoEmergencia switch
            {
                TipoEmergencia.PersonasAtrapadas => TimeSpan.FromMinutes(45),
                TipoEmergencia.EmergenciaMedica => TimeSpan.FromMinutes(30),
                TipoEmergencia.IncendioEstructural => TimeSpan.FromMinutes(60),
                TipoEmergencia.Inundacion => TimeSpan.FromMinutes(90),
                TipoEmergencia.DeslizamientoTierra => TimeSpan.FromMinutes(120),
                TipoEmergencia.VientosFuertes => TimeSpan.FromMinutes(40),
                TipoEmergencia.CorteEnergia => TimeSpan.FromMinutes(180),
                TipoEmergencia.AccidenteVehicular => TimeSpan.FromMinutes(35),
                _ => TimeSpan.FromMinutes(30)
            };
        }
    }
}

