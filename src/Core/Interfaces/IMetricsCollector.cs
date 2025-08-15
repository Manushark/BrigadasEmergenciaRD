using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Core.Interfaces
{
    // Contrato para enviar datos de metricas al modulo de Ricardo
    // Solo define que el Core puede enviar, sin implementacion
    public interface IMetricsCollector
    {
        // Registra cuando se genera una emergencia
        void RegistrarEmergenciaGenerada(EmergenciaEvento emergencia);

        // Registra cuando una brigada atiende una emergencia
        void RegistrarEmergenciaAtendida(Brigada brigada, LlamadaEmergencia llamada, TimeSpan tiempoRespuesta);

        // Registra cambio de estado de una brigada
        void RegistrarCambioEstadoBrigada(int brigadaId, EstadoBrigada estadoAnterior, EstadoBrigada estadoNuevo);

        // Registra error o fallo en el sistema
        void RegistrarError(string operacion, string mensaje);
    }
}
