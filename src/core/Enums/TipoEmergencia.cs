using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergencia.Core.Enums
{
    // Esta clase define los tipos de emergencias que puede haber en el sistema
    public enum TipoEmergencia
    {
        Inundacion = 1,
        DeslizamientoTierra = 2,
        VientosFuertes = 3,
        PersonasAtrapadas = 4,
        CorteEnergia = 5,
        AccidenteVehicular = 6,
        IncendioEstructural = 7,
        EmergenciaMedica = 8
    }
}