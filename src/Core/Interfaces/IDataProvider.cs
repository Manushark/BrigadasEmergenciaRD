using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models;

namespace BrigadasEmergenciaRD.Core.Interfaces
{
    // Contrato para obtener datos desde el modulo de Netanel
    // Solo define que necesita el Core, sin implementacion
    public interface IDataProvider
    {
        // Obtiene todas las provincias registradas
        Task<IEnumerable<Provincia>> ObtenerProvinciasAsync();

        // Obtiene municipios de una provincia especifica
        Task<IEnumerable<Municipio>> ObtenerMunicipiosAsync(int provinciaId);

        // Obtiene barrios de un municipio especifico
        Task<IEnumerable<Barrio>> ObtenerBarriosAsync(int municipioId);

        // Obtiene brigadas disponibles en una provincia
        Task<IEnumerable<Brigada>> ObtenerBrigadasDisponiblesAsync(int provinciaId);
    }
}
// Este contrato define los metodos que el Core necesita para interactuar con el modulo de Netanel.
// Esto permite al Core enfocarse en su logica de negocio sin preocuparse por detalles de acceso a datos.
