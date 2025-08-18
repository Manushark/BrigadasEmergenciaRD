using System;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Simulation;
using BrigadasEmergenciaRD.Data;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.src.Simulation.UI;
using BrigadasEmergenciaRD.Tests;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        ConsoleUi.Header("Simulación de Brigadas de Emergencia RD (PRUEBA)");

        // ===============================================
        // FLAG --metrics
        // ===============================================
        // Si el primer argumento es --metrics, ejecutamos las mediciones
        // y salimos sin correr la simulacion normal.
        // Nota: RunMetrics() es una funcion local mas abajo en este mismo metodo.
        if (args.Length > 0 && string.Equals(args[0], "--metrics", StringComparison.OrdinalIgnoreCase))
        {
            RunMetrics(); // genera archivos en carpeta resultados-reportes/
            return;       // no seguir con la simulacion normal
        }

        // Configuracion base de la simulacion
        var cfg = new ParametrosSimulacion
        {
            DuracionTickMs = 800,                   // 0.8s por tick
            ProbabilidadBaseEventoPorBarrio = 0.08, // 8% por barrio y por tick (ajustalo si quieres ver mas/menos eventos)
            MaximoLlamadas = 15                     // solo 15 llamadas
        };
        cfg.Validar();

        // Data provider oficial (JsonDataProvider)
        // Ruta a la carpeta src/data (sube 3 niveles desde bin\Debug\netX.0\ hasta el repo)
        var rutaData = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", "data")
        );

        // Instancia del proveedor de datos real usando los JSON
        IDataProvider dataProvider = new JsonDataProvider(rutaData);

        Console.WriteLine($"[Datos] Leyendo JSON desde: {rutaData}");

        // Instanciar simulador y gestor
        var simulador = new SimuladorTormenta(dataProvider, cfg);
        var gestor = new GestorEmergencias(dataProvider);

        // Intensidad mas “movida” para ver actividad
        simulador.EstablecerIntensidad(IntensidadTormenta.Alta);

        // Logs bonitos usando ConsoleUi
        simulador.OnLlamadaGenerada += llamada =>
        {
            string extra = $"{llamada.Barrio?.Nombre} / {llamada.Barrio?.Municipio?.Nombre} / {llamada.Barrio?.Municipio?.Provincia?.Nombre}";
            ConsoleUi.Llamada(llamada.TipoEmergencia.ToString(), llamada.BarrioId, llamada.Prioridad, extra);
        };

        gestor.OnBrigadaAsignada += (llamada, brigada) =>
            ConsoleUi.Asignada(brigada.Nombre, llamada.TipoEmergencia.ToString(), llamada.BarrioId);

        gestor.OnLlamadaAtendida += (llamada, brigada, dt) =>
            ConsoleUi.Atendida(brigada.Nombre, dt.TotalSeconds);

        gestor.OnLlamadaReencolada += llamada =>
            ConsoleUi.Reencolada(llamada.TipoEmergencia.ToString(), llamada.BarrioId);

        using var cts = new CancellationTokenSource();

        // Iniciar simulacion
        await simulador.PrepararDatosAsync();
        await gestor.PrepararDatosAsync();

        await simulador.IniciarAsync(cts.Token);
        await gestor.IniciarAsync(cts.Token);

        // Correr 15 segundos y cortar
        await Task.Delay(TimeSpan.FromSeconds(15));
        cts.Cancel();

        // Detener limpiamente
        await simulador.DetenerAsync();
        await gestor.DetenerAsync();

        ConsoleUi.Header("FIN DE PRUEBA");

        // Mensajes de ayuda para el usuario (solo texto, no afectan la logica)
        Console.WriteLine();
        Console.WriteLine("[Ayuda] Puedes ejecutar solo los reportes con: dotnet run -- --metrics");
        Console.WriteLine("[Ayuda] O puedes generarlos ahora respondiendo 's' a la pregunta de abajo.");

        // ===============================================
        // PREGUNTA AL FINAL: quieres ver las metrics?
        // ===============================================
        // Bucle que valida la entrada: solo s/y o n.
        while (true)
        {
            Console.WriteLine();
            Console.Write("Deseas ver los reportes de metrics ahora? (s/n): ");
            var resp = char.ToLowerInvariant(Console.ReadKey(intercept: true).KeyChar);
            Console.WriteLine();

            if (resp == 's' || resp == 'y')
            {
                RunMetrics(); // ejecuta mediciones y muestra el menu persistente
                break;        // salir del bucle despues de cerrar el menu de metrics
            }
            else if (resp == 'n')
            {
                Console.WriteLine("OK: fin del programa.");
                break;        // no correr metrics
            }
            else
            {
                Console.WriteLine("Entrada invalida: solo puede ser s o n.");
            }
        }

        // ===============================================
        // FUNCION RunMetrics: CREA ESCENARIOS Y EXPORTA RESULTADOS
        // ===============================================
        // Nota: funcion local dentro de Main. Cambia la ruta de salida,
        // crea un indice y agrega un menu persistente para abrir reportes.
        static void RunMetrics()
        {
            // Diagnostico de rutas
            Console.WriteLine("cwd = " + Directory.GetCurrentDirectory());
            // AppContext.BaseDirectory => bin/Debug/netX.X/
            // Subimos 3 niveles hasta el proyecto y creamos "resultados-reportes"
            var outDir = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "resultados-reportes")
            );
            Console.WriteLine("outDir = " + outDir);

            // Crear carpeta de salida si no existe
            Directory.CreateDirectory(outDir);

            // Parametros basicos de medicion
            int iters = 200;                                 // iteraciones por escenario
            int p = Math.Max(2, Environment.ProcessorCount); // paralelismo por defecto

            // Escenario CPU (trabajo CPU-bound)
            var cpu = BrigadasEmergenciaRD.Metrics.Comparisons.BenchmarkRunner.Run(
                "CPU_Demo",
                i =>
                {
                    double x = 0;
                    for (int k = 0; k < 200000; k++) x += Math.Sqrt(k + 1);
                    return Task.CompletedTask;
                },
                iters,
                p
            );

            // Escenario IO (simula IO con delay fijo)
            var io = BrigadasEmergenciaRD.Metrics.Comparisons.BenchmarkRunner.Run(
                "IO_Demo",
                i => Task.Delay(5),
                iters,
                p
            );

            // Rutas de salida (siempre bajo resultados-reportes/)
            var cpuCsv = Path.Combine(outDir, "cpu_demo.csv");
            var cpuMd = Path.Combine(outDir, "cpu_demo.md");
            var ioCsv = Path.Combine(outDir, "io_demo.csv");
            var ioMd = Path.Combine(outDir, "io_demo.md");

            // Guardar reportes (usa las firmas actuales de tus reporters: path + snapshot)
            BrigadasEmergenciaRD.Metrics.Reporters.CsvReporter.Save(cpuCsv, cpu);
            BrigadasEmergenciaRD.Metrics.Reporters.MarkdownReporter.Save(cpuMd, cpu);
            BrigadasEmergenciaRD.Metrics.Reporters.CsvReporter.Save(ioCsv, io);
            BrigadasEmergenciaRD.Metrics.Reporters.MarkdownReporter.Save(ioMd, io);

            // Indice simple en Markdown con links relativos
            var indexPath = Path.Combine(outDir, "README_resultados.md");
            File.WriteAllText(indexPath,
$@"# Resultados de metricas
Generado: {DateTime.Now:yyyy-MM-dd HH:mm}

- [CPU CSV](cpu_demo.csv)
- [CPU MD](cpu_demo.md)
- [IO  CSV](io_demo.csv)
- [IO  MD](io_demo.md)
");

            // ===== MENU PERSISTENTE PARA ABRIR REPORTES =====
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("===================================");
                Console.WriteLine(" Reportes generados en: " + outDir);
                Console.WriteLine(" 1) Abrir carpeta de reportes");
                Console.WriteLine(" 2) Abrir cpu_demo.csv");
                Console.WriteLine(" 3) Abrir io_demo.csv");
                Console.WriteLine(" 4) Abrir cpu_demo.md");
                Console.WriteLine(" 5) Abrir io_demo.md");
                Console.WriteLine(" 6) Abrir README_resultados.md");
                Console.WriteLine(" 0) Salir");
                Console.Write(" Elige opcion: ");

                var key = Console.ReadKey(intercept: true).KeyChar;
                Console.WriteLine();

                void Open(string path)
                {
                    try
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true // abre con app por defecto (Excel, VSCode, etc.)
                        };
                        System.Diagnostics.Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("No se pudo abrir: " + path + " -> " + ex.Message);
                    }
                }

                switch (key)
                {
                    case '1': Open(outDir); break;
                    case '2': Open(cpuCsv); break;
                    case '3': Open(ioCsv); break;
                    case '4': Open(cpuMd); break;
                    case '5': Open(ioMd); break;
                    case '6': Open(indexPath); break;
                    case '0': Console.WriteLine("OK: resultados listos en " + outDir); return;
                    default: Console.WriteLine("Opcion invalida"); break;
                }
            }

        }
        
        // ===============================
        //     Pruebas de paralelismo
        // ===============================
        Console.WriteLine("\n\n==================================");
        Console.WriteLine("🚀 INICIANDO PRUEBAS DE PARALELISMO");
        Console.WriteLine("==================================\n");

        await PruebasParalelismo.EjecutarPruebasAsync();
        await PruebasParalelismo.GenerarReporteEntregaAsync();

        Console.WriteLine("\n==================================");
        Console.WriteLine("✅ TODAS LAS PRUEBAS COMPLETADAS");
        Console.WriteLine("==================================");
    }
}
