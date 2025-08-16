using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrigadasEmergenciaRD.src.Simulation.UI
{
    public static class ConsoleUi
    {
        public static void Header(string title)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine($"   {title}");
            Console.WriteLine(new string('=', 60) + "\n");
        }

        public static void Llamada(string tipo, int barrioId, int prioridad, string extra = "")
        {
            Console.WriteLine($"[Simulador] Llamada: {tipo} (prio {prioridad}) en Barrio {barrioId}");
            if (!string.IsNullOrWhiteSpace(extra))
                Console.WriteLine($"           » {extra}");
        }

        public static void Asignada(string brigada, string tipo, int barrioId)
        {
            Console.WriteLine($"[Gestor] Asignada {brigada} a {tipo} (Barrio {barrioId})");
        }

        public static void Atendida(string brigada, double segundos)
        {
            Console.WriteLine($"[Gestor] Atendida por {brigada} en {segundos:F1}s");
        }

        public static void Reencolada(string tipo, int barrioId)
        {
            Console.WriteLine($"[Gestor] Reencolada (sin brigada): {tipo} en Barrio {barrioId}");
        }
    }

}
