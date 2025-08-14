using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;

namespace BrigadasEmergenciaRD.Core.Models
{
    public class ConfiguracionSistema
    {
        public ParallelismConfig Paralelismo { get; set; }
        public SimulacionConfig Simulacion { get; set; }
        public BrigadasConfig Brigadas { get; set; }
        public MetricasConfig Metricas { get; set; }

        public ConfiguracionSistema()
        {
            Paralelismo = new ParallelismConfig();
            Simulacion = new SimulacionConfig();
            Brigadas = new BrigadasConfig();
            Metricas = new MetricasConfig();
        }
    }

    public class ParallelismConfig
    {
        public int MaxGradoParalelismo { get; set; } = Environment.ProcessorCount;
        public int TamanoColaBrigadas { get; set; } = 1000;
        public int TimeoutOperacionMs { get; set; } = 30000;
        public bool HabilitarBalanceoCarga { get; set; } = true;
    }

    public class SimulacionConfig
    {
        public int DuracionSimulacionMinutos { get; set; } = 60;
        public int IntervaloEventosSegundos { get; set; } = 10;
        public double ProbabilidadEmergenciasPorMinuto { get; set; } = 0.3;
        public string[] TiposEmergenciaActivos { get; set; } =
        {
            "Inundacion", "DeslizamientoTierra", "VientosFuertes",
            "PersonasAtrapadas", "EmergenciaMedica"
        };
        public IntensidadTormenta IntensidadMinima { get; set; } = IntensidadTormenta.Baja;
    }

    public class BrigadasConfig
    {
        public int MaximoBrigadasPorProvincia { get; set; } = 10;
        public int TiempoMaximoRespuestaMinutos { get; set; } = 30;
        public Dictionary<string, int> BrigadasPorTipo { get; set; } = new()
        {
            { "DefensaCivil", 3 },
            { "Bomberos", 2 },
            { "Medica", 2 },
            { "Rescate", 2 },
            { "PoliciaNacional", 1 }
        };
    }

    public class MetricasConfig
    {
        public bool HabilitarRecoleccionTiempoReal { get; set; } = true;
        public int IntervaloRecoleccionMs { get; set; } = 1000;
        public bool GenerarGraficasAutomaticas { get; set; } = true;
        public string RutaReportes { get; set; } = "./metrics/";
    }
}
