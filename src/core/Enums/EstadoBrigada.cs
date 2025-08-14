using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergencia.Core.Enums
{
    // Enum que define el estado de una brigada
    public enum EstadoBrigada
    {
        Disponible = 1,
        EnRuta = 2,
        AtendendoEmergencia = 3,
        Regresando = 4,
        FueraServicio = 5,
        Mantenimiento = 6
    }
}