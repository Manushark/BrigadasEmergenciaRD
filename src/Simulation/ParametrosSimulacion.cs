using System;
using System.Collections.Generic;
using BrigadasEmergenciaRD.Core.Enums;

namespace BrigadasEmergenciaRD.Simulation
{
	/// <summary>
	/// Configuración central de la simulación.
	/// Aquí se definen ritmos, probabilidades y rangos que usará el simulador para generar eventos y llamadas.
	/// La idea es poder ajustar el “comportamiento” de la tormenta sin tocar la lógica del simulador.
	/// </summary>
	public class ParametrosSimulacion
	{
		// --- Tiempo y ritmo general ---

		/// <summary>
		/// Duración de un “tick” (paso) de la simulación en milisegundos.
		/// Mientras más pequeño, más “rápida” se sentirá la simulación.
		/// </summary>
		public int DuracionTickMs { get; set; } = 500;

		/// <summary>
		/// Semilla opcional para el generador aleatorio (útil para reproducir escenarios).
		/// Si es null, se usará una semilla basada en la hora actual.
		/// </summary>
		public int? SemillaAleatoria { get; set; } = null;

		// --- Probabilidades base ---

		/// <summary>
		/// Probabilidad base (0..1) de que ocurra al menos un evento en un barrio durante un tick.
		/// Esta probabilidad luego se modula por la intensidad de la tormenta y la vulnerabilidad.
		/// </summary>
		public double ProbabilidadBaseEventoPorBarrio { get; set; } = 0.05;

		/// <summary>
		/// Factor multiplicador por nivel de intensidad de la tormenta.
		/// Ejemplo: con Intensidad ALTA, un valor de 2.0 duplicaría la probabilidad efectiva.
		/// </summary>
		public Dictionary<IntensidadTormenta, double> FactorPorIntensidad { get; set; } =
			new Dictionary<IntensidadTormenta, double>
			{
				{ IntensidadTormenta.Baja, 0.7 },
				{ IntensidadTormenta.Moderada, 1.0 },
				{ IntensidadTormenta.Alta, 1.6 },
				{ IntensidadTormenta.Extrema, 2.3 }
			};

		/// <summary>
		/// Distribución (no necesariamente normalizada) de tipos de emergencia.
		/// El simulador normalizará internamente estos pesos para sortear el tipo.
		/// </summary>
		public Dictionary<TipoEmergencia, double> PesoPorTipoEmergencia { get; set; } =
			new Dictionary<TipoEmergencia, double>
			{
				{ TipoEmergencia.Inundacion, 1.0 },
				{ TipoEmergencia.EmergenciaMedica, 0.9 },
				{ TipoEmergencia.PersonasAtrapadas, 0.6 },
				{ TipoEmergencia.DeslizamientoTierra, 0.5 },
				{ TipoEmergencia.AccidenteVehicular, 0.7 },
				{ TipoEmergencia.IncendioEstructural, 0.4 },
				{ TipoEmergencia.VientosFuertes, 0.8 },
				{ TipoEmergencia.CorteEnergia, 0.3 }
			};

		// --- Severidad / personas afectadas ---

		/// <summary>
		/// Rango mínimo y máximo de personas afectadas que puede generar un evento.
		/// Estos valores se usan al crear la Llamada de Emergencia.
		/// </summary>
		public int PersonasAfectadasMin { get; set; } = 1;

		public int PersonasAfectadasMax { get; set; } = 30;

		/// <summary>
		/// Probabilidad (0..1) de que un evento genere descripción adicional (ruidos, colapso, etc.).
		/// No afecta la lógica, solo el realismo del contenido de la llamada.
		/// </summary>
		public double ProbabilidadDescripcion { get; set; } = 0.35;

		// --- Vulnerabilidad territorial ---

		/// <summary>
		/// Mapa opcional de multiplicadores por “vulnerabilidad climática” de la provincia.
		/// Si no hay valor para una provincia, se asume 1.0 (sin efecto).
		/// Clave sugerida: Provincia.Codigo (ej. "01", "02", ...).
		/// </summary>
		public Dictionary<string, double> FactorPorProvinciaCodigo { get; set; } =
			new Dictionary<string, double>();

		// --- Modos de encolado ---

		/// <summary>
		/// Si true, tu diseño podría usar una cola global prioritaria.
		/// Si false (por defecto), se usa la cola concurrente por barrio (ya existente en el modelo).
		/// </summary>
		public bool UsarColaGlobal { get; set; } = false;

        
		/// <summary>
		/// Maximo de llamadas
		/// </summary>
		public int MaximoLlamadas { get; set; } = 0; // 0 = ilimitadas


        // --- Utilidades ---

        /// <summary>
        /// Asegura que los valores numéricos sean coherentes (clamps) y evita configuraciones inválidas.
        /// Llama a este método al iniciar la simulación.
        /// </summary>
        public void Validar()
		{
			if (DuracionTickMs < 1) DuracionTickMs = 1;
			if (ProbabilidadBaseEventoPorBarrio < 0) ProbabilidadBaseEventoPorBarrio = 0;
			if (ProbabilidadBaseEventoPorBarrio > 1) ProbabilidadBaseEventoPorBarrio = 1;

			if (PersonasAfectadasMin < 0) PersonasAfectadasMin = 0;
			if (PersonasAfectadasMax < PersonasAfectadasMin) PersonasAfectadasMax = PersonasAfectadasMin;

			if (ProbabilidadDescripcion < 0) ProbabilidadDescripcion = 0;
			if (ProbabilidadDescripcion > 1) ProbabilidadDescripcion = 1;

			// Remueve factores inválidos por intensidad
			var intensidades = new List<IntensidadTormenta>(FactorPorIntensidad.Keys);
			foreach (var k in intensidades)
			{
				if (FactorPorIntensidad[k] <= 0) FactorPorIntensidad[k] = 1.0;
			}

			// Normaliza pesos de tipos si todos fueran <= 0 (evita división por cero luego)
			var suma = 0.0;
			foreach (var kv in PesoPorTipoEmergencia) suma += Math.Max(0, kv.Value);
			if (suma <= 0)
			{
				// Restaura valores por defecto si estaban mal configurados
				PesoPorTipoEmergencia = new Dictionary<TipoEmergencia, double>
				{
					{ TipoEmergencia.Inundacion, 1.0 },
					{ TipoEmergencia.EmergenciaMedica, 0.9 },
					{ TipoEmergencia.PersonasAtrapadas, 0.6 },
					{ TipoEmergencia.DeslizamientoTierra, 0.5 },
					{ TipoEmergencia.AccidenteVehicular, 0.7 },
					{ TipoEmergencia.IncendioEstructural, 0.4 },
					{ TipoEmergencia.VientosFuertes, 0.8 },
					{ TipoEmergencia.CorteEnergia, 0.3 }
				};
			}
		}
	}
}
