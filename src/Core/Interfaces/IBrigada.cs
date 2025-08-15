using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Core.Interfaces
{
    // Esta interfaz define el contrato que debe cumplir cualquier clase que represente una brigada de emergencia.
    // Define el "contrato" de lo que debe hacer una brigada.
    // No implementa nada, solo establece las reglas que luego una clase Brigada cumplira.
    public interface IBrigada
    {
        int Id { get; }
        string Nombre { get; }
        TipoBrigada Tipo { get; }   // Tipo (medica, bomberos, rescate, etc.)
        EstadoBrigada Estado { get; }  // Estado actual (disponible, en ruta, etc.)
        Coordenada UbicacionActual { get; set; } // Ubicacion actual de la brigada

        // Metodo para asignar una emergencia a esta brigada, usando datos de una llamada de emergencia.
        Task<bool> AtenderEmergenciaAsync(LlamadaEmergencia llamada);
        Task<TimeSpan> CalcularTiempoRespuestaAsync(Coordenada destino);
        bool PuedeAtender(TipoEmergencia tipoEmergencia);
        void CambiarEstado(EstadoBrigada nuevoEstado); // Metodo que actualiza el estado de la brigada segun su actividad actual.
    }
}
