using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Enums
{
    // Enum que define la intensidad de la tormenta para medir su gravedad
    public enum IntensidadTormenta
    {
        Baja = 1,      // Vientos 39-73 km/h
        Moderada = 2,  // Vientos 74-95 km/h  
        Alta = 3,      // Vientos 96-110 km/h
        Extrema = 4    // Vientos >110 km/h
    }
}