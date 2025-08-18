using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Models;
using BrigadasEmergenciaRD.Parallelism;

namespace BrigadasEmergenciaRD.Tests
{
    /// <summary>
    /// Pruebas específicas para el GestorParalelo
    /// </summary>
    public static class PruebasParalelismo
    {
        /// <summary>
        /// Método principal que puede llamarse desde Program.cs
        /// </summary>
        public static async Task EjecutarPruebasAsync()
        {
            Console.WriteLine("🧵 PRUEBAS DE PARALELISMO");
            Console.WriteLine("==================================");

            await PruebaBasica();
            await PruebaVelocidad();
            await PruebaRecursos();
            
            Console.WriteLine("\n✅ Pruebas completadas");
        }

        /// <summary>
        /// Prueba 1: Verificar que el sistema paralelo funciona
        /// </summary>
        private static async Task PruebaBasica()
        {
            Console.WriteLine("\n🔧 PRUEBA BÁSICA");
            Console.WriteLine("----------------");

            var config = new ConfigParalelo { MaxGradoParalelismo = 4 };
            using var gestor = new GestorParaleloExtendido(config, recursosDisponibles: 10);

            var emergencias = GenerarEmergenciasPrueba(50);
            
            async Task<(string, TimeSpan)> ProcesarEmergencia(EmergenciaEvento e, CancellationToken ct)
            {
                await Task.Delay(10, ct);
                return ($"Brigada_{e.Id % 5}", TimeSpan.FromMilliseconds(10));
            }

            foreach (var e in emergencias)
                gestor.EncolarEmergencia(e);

            var (tiempo, atendidas) = await gestor.ProcesarEnParaleloAsync(ProcesarEmergencia);

            Console.WriteLine($"✅ Procesadas {atendidas} emergencias en {tiempo.TotalMilliseconds:F0}ms");
            Console.WriteLine($"📊 Throughput: {atendidas / tiempo.TotalSeconds:F0} emergencias/segundo");

            var resultados = gestor.Resultados.ToList();
            var exitosos = resultados.Count(r => r.brigadaId != "ERROR");
            
            Console.WriteLine($"✅ Éxito: {exitosos}/{resultados.Count} ({(double)exitosos/resultados.Count*100:F1}%)");
        }

        /// <summary>
        /// Prueba 2: Comparar velocidad secuencial vs paralelo
        /// </summary>
        private static async Task PruebaVelocidad()
        {
            Console.WriteLine("\n⚡ PRUEBA DE VELOCIDAD");
            Console.WriteLine("---------------------");

            var emergencias = GenerarEmergenciasPrueba(200);
            
            async Task<(string, TimeSpan)> TrabajoPesado(EmergenciaEvento e, CancellationToken ct)
            {
                var delay = e.Tipo switch
                {
                    TipoEmergencia.PersonasAtrapadas => 50,
                    TipoEmergencia.IncendioEstructural => 75,
                    TipoEmergencia.EmergenciaMedica => 25,
                    _ => 30
                };
                
                await Task.Delay(delay, ct);
                return ($"Brigada_{e.Tipo}", TimeSpan.FromMilliseconds(delay));
            }

            var configuraciones = new[] { 1, 2, 4, Environment.ProcessorCount };
            var resultados = new List<(int hilos, TimeSpan tiempo, int atendidas)>();

            foreach (var numHilos in configuraciones)
            {
                var config = new ConfigParalelo { MaxGradoParalelismo = numHilos };
                using var gestor = new GestorParaleloExtendido(config, recursosDisponibles: 15);

                foreach (var e in emergencias)
                    gestor.EncolarEmergencia(e);

                var (tiempo, atendidas) = await gestor.ProcesarEnParaleloAsync(TrabajoPesado);
                resultados.Add((numHilos, tiempo, atendidas));

                Console.WriteLine($"  {numHilos} hilos: {tiempo.TotalMilliseconds:F0}ms");
            }

            var tiempoBase = resultados[0].tiempo.TotalMilliseconds;
            Console.WriteLine("\n📈 Análisis de Speedup:");
            foreach (var (hilos, tiempo, _) in resultados)
            {
                var speedup = tiempoBase / tiempo.TotalMilliseconds;
                var eficiencia = speedup / hilos * 100;
                Console.WriteLine($"  {hilos} hilos -> Speedup: {speedup:F2}x, Eficiencia: {eficiencia:F1}%");
            }
        }

        /// <summary>
        /// Prueba 3: Monitoreo de recursos y estadísticas
        /// </summary>
        private static async Task PruebaRecursos()
        {
            Console.WriteLine("\n📊 PRUEBA DE RECURSOS");
            Console.WriteLine("--------------------");

            var config = new ConfigParalelo 
            { 
                MaxGradoParalelismo = Environment.ProcessorCount,
                HabilitarMetricas = true 
            };
            
            using var gestor = new GestorParaleloExtendido(config, recursosDisponibles: 8);

            var emergencias = GenerarEmergenciasPrueba(300);
            
            async Task<(string, TimeSpan)> TrabajoVariado(EmergenciaEvento e, CancellationToken ct)
            {
                var delay = new Random().Next(10, 100);
                await Task.Delay(delay, ct);
                
                var brigadaId = $"{e.Tipo}_{Thread.CurrentThread.ManagedThreadId}";
                return (brigadaId, TimeSpan.FromMilliseconds(delay));
            }

            foreach (var e in emergencias)
                gestor.EncolarEmergencia(e);

            var memoriaInicial = GC.GetTotalMemory(false);
            var (tiempo, atendidas) = await gestor.ProcesarEnParaleloAsync(TrabajoVariado);
            var memoriaFinal = GC.GetTotalMemory(false);

            var stats = gestor.ObtenerEstadisticas();

            Console.WriteLine($"⏱️ Tiempo total: {tiempo.TotalSeconds:F2}s");
            Console.WriteLine($"🎯 Emergencias procesadas: {atendidas}");
            Console.WriteLine($"💾 Memoria usada: {(memoriaFinal - memoriaInicial) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"🧵 Hilos activos: {stats.MaximoHilosConcurrentes}");
            Console.WriteLine($"📈 Total tareas: {stats.TotalTareasEjecutadas}");

            Console.WriteLine("\n🔍 Distribución por hilo:");
            foreach (var medicion in stats.MedicionesPorHilo.OrderByDescending(m => m.TareasCompletadas))
            {
                Console.WriteLine($"  Hilo {medicion.IdHilo}: {medicion.TareasCompletadas} tareas " +
                                $"({medicion.TiempoTotalEjecucion.TotalMilliseconds:F0}ms)");
            }

            var tareasMinimas = stats.MedicionesPorHilo.Min(m => m.TareasCompletadas);
            var tareasMaximas = stats.MedicionesPorHilo.Max(m => m.TareasCompletadas);
            var desbalance = (double)(tareasMaximas - tareasMinimas) / tareasMaximas * 100;
            
            Console.WriteLine($"\n⚖️ Balance de carga: {desbalance:F1}% desbalance");
            if (desbalance < 20)
                Console.WriteLine("✅ Buen balance de carga");
            else
                Console.WriteLine("⚠️ Considerar optimizar distribución");

            Console.WriteLine($"\n🗺️ Zonas con más emergencias:");
            var topZonas = gestor.LlamadosPorZona
                .OrderByDescending(z => z.Value)
                .Take(5);
            
            foreach (var zona in topZonas)
            {
                Console.WriteLine($"  {zona.Key}: {zona.Value} llamados");
            }
        }

        private static List<EmergenciaEvento> GenerarEmergenciasPrueba(int cantidad)
        {
            var emergencias = new List<EmergenciaEvento>();
            var random = new Random();
            var tipos = Enum.GetValues<TipoEmergencia>();

            for (int i = 0; i < cantidad; i++)
            {
                emergencias.Add(new EmergenciaEvento
                {
                    Id = i,
                    Tipo = tipos[random.Next(tipos.Length)],
                    ProvinciaId = random.Next(1, 33),
                    MunicipioId = random.Next(1, 200),
                    BarrioId = random.Next(1, 1000),
                    PersonasAfectadas = random.Next(1, 25),
                    Intensidad = (IntensidadTormenta)random.Next(1, 5),
                    Ubicacion = new Coordenada(
                        18.0 + random.NextDouble() * 2,
                        -70.0 - random.NextDouble() * 2
                    ),
                    Descripcion = $"Emergencia de prueba #{i}"
                });
            }

            return emergencias;
        }

        public static async Task GenerarReporteEntregaAsync()
        {
            Console.WriteLine("\n📋 GENERANDO REPORTE DE ENTREGA");
            Console.WriteLine("===============================");

            var reporte = $@"
SISTEMA DE PARALELISMO
======================
Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}

COMPONENTES ENTREGADOS:
✅ Clase GestorParalelo - Orquestador principal
✅ Clase PoolRecursos - Gestión de recursos con SemaphoreSlim  
✅ Clase ConfigParalelo - Configuración del sistema
✅ Sistema de colecciones concurrentes (ConcurrentDictionary, ConcurrentBag)
✅ Implementación TPL (Task Parallel Library)
✅ Sistema de sincronización (locks, semáforos, CancellationToken)
✅ Pruebas de velocidad (secuencial vs paralelo)

TECNOLOGÍAS UTILIZADAS:
🔧 Task Parallel Library (TPL)
🔧 SemaphoreSlim para control de recursos
🔧 BlockingCollection para colas thread-safe
🔧 ConcurrentDictionary/ConcurrentBag para datos compartidos
🔧 CancellationToken para cancelación coordinada
🔧 Locks para secciones críticas
🔧 Patrón Disposable para resource management

MÉTRICAS DE RENDIMIENTO:
📊 Speedup esperado: 2-4x con 4+ cores
📊 Eficiencia esperada: >75% hasta 8 hilos
📊 Throughput esperado: >100 operaciones/segundo
📊 Balance de carga: <20% desbalance entre hilos

PRÓXIMOS PASOS SUGERIDOS:
🚀 Integrar con sistema principal de brigadas
🚀 Agregar más estrategias de paralelización
🚀 Implementar tolerancia a fallos avanzada
🚀 Optimizar para casos específicos de RD
";

            Console.WriteLine(reporte);
            
            try
            {
                await System.IO.File.WriteAllTextAsync("reporte_paralelismo.txt", reporte);
                Console.WriteLine("💾 Reporte guardado en: reporte_paralelismo.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ No se pudo guardar reporte: {ex.Message}");
            }
        }
    }
}