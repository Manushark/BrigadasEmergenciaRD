using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class RecursosEmergencia
    {
        public int BrigadasDisponibles { get; set; }
        public int Hospitales { get; set; }
        public int Refugios { get; set; }
        public int VehiculosEmergencia { get; set; }
        public int CapacidadRefugio { get; set; }

        public RecursosEmergencia()
        {
            BrigadasDisponibles = 5;
            Hospitales = 1;
            Refugios = 2;
            VehiculosEmergencia = 3;
            CapacidadRefugio = 200;
        }
    }
}
