using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Core.Models;
using BrigadasEmergenciaRD.Data;
using BrigadasEmergenciaRD.Parallelism;

class Program
{
    private static List<Provincia> _provincias = new();
    private static List<Brigada> _todasBrigadas = new();
    private static Random _random = new();
    private static readonly object _lockConsole = new object();

    // Estadísticas en tiempo real
    private static int _emergenciasGeneradas = 0;
    private static int _emergenciasAtendidas = 0;
    private static int _brigadasEnServicio = 0;
    private static DateTime _inicioSimulacion;

    static async Task Main(string[] args)
    {
        Console.Clear();
        MostrarEncabezado();

        try
        {
            await InicializarSistemaAsync();
            await MostrarMenuPrincipalAsync();
        }
        catch (Exception ex)
        {
            MostrarError($"Error crítico: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nGracias por usar el sistema de Brigadas RD");
    }

    static void MostrarEncabezado()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("SISTEMA DE BRIGADAS DE EMERGENCIA - REPUBLICA DOMINICANA");
        Console.WriteLine("=========================================================");
        Console.ResetColor();
        Console.WriteLine();
    }

    static async Task InicializarSistemaAsync()
    {
        MostrarInfo("Inicializando sistema...");

        var rutaData = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "src", "data"));
        var dataProvider = new JsonDataProvider(rutaData);

        _provincias = await dataProvider.ObtenerDatosCompletosAsync();
        _todasBrigadas = _provincias.SelectMany(p => p.BrigadasDisponibles).ToList();

        MostrarExito($"Sistema inicializado: {_provincias.Count} provincias, {_todasBrigadas.Count} brigadas\n");
    }

    static async Task MostrarMenuPrincipalAsync()
    {
        while (true)
        {
            Console.Clear();
            MostrarTitulo("MENU PRINCIPAL");
            Console.WriteLine("1. Simulación EN VIVO con datos reales");
            Console.WriteLine("2. Simulación con datos reales (Secuencial)");
            Console.WriteLine("3. Simulación con datos reales (Paralela)");
            Console.WriteLine("4. Comparar rendimiento (Secuencial vs Paralela)");
            Console.WriteLine("5. Análisis de Speedup con múltiples núcleos");
            Console.WriteLine("0. Salir");
            Console.Write("\nElige una opción: ");

            var opcion = Console.ReadKey().KeyChar;
            Console.WriteLine("\n");

            switch (opcion)
            {
                case '1': await EjecutarSimulacionEnVivoAsync(); break;
                case '2': await EjecutarSimulacionSecuencialAsync(); break;
                case '3': await EjecutarSimulacionParalelaAsync(); break;
                case '4': await CompararRendimientoAsync(); break;
                case '5': await EjecutarAnalisisSpeedupAsync(); break;
                case '0': return;
                default: MostrarError("Opción inválida"); break;
            }

            if (opcion != '1')
            {
                Console.WriteLine("\nPresiona cualquier tecla para continuar...");
                Console.ReadKey();
            }
        }
    }

    #region Simulación en Vivo
    static async Task EjecutarSimulacionEnVivoAsync()
    {
        Console.Clear();
        MostrarTitulo("SIMULACION EN TIEMPO REAL");

        var duracionSegundos = SolicitarEntero("Duración de la simulación en segundos (30-300): ", 30, 300, 60);
        var intervaloMs = SolicitarEntero("Intervalo entre emergencias en ms (500-5000): ", 500, 5000, 2000);

        Console.Clear();
        await MostrarSimulacionEnVivoAsync(duracionSegundos, intervaloMs);
    }

    static async Task MostrarSimulacionEnVivoAsync(int duracionSegundos, int intervaloMs)
    {
        ReiniciarEstadisticas();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(duracionSegundos));
        Console.CursorVisible = false;

        try
        {
            await Task.WhenAll(
                GenerarEmergenciasEnVivoAsync(intervaloMs, cts.Token),
                MostrarPantallaEnVivoAsync(cts.Token)
            );
        }
        catch (OperationCanceledException) { }
        finally
        {
            Console.CursorVisible = true;
            MostrarResumenFinal();
        }
    }

    static void ReiniciarEstadisticas()
    {
        _inicioSimulacion = DateTime.Now;
        _emergenciasGeneradas = _emergenciasAtendidas = _brigadasEnServicio = 0;
    }

    static async Task GenerarEmergenciasEnVivoAsync(int intervaloMs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var emergencia = GenerarEmergenciaAleatoria();
                _ = Task.Run(() => ProcesarEmergenciaEnVivoAsync(emergencia, ct), ct);
                Interlocked.Increment(ref _emergenciasGeneradas);
                await Task.Delay(intervaloMs + _random.Next(-500, 500), ct);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    static async Task ProcesarEmergenciaEnVivoAsync(EmergenciaEvento emergencia, CancellationToken ct)
    {
        try
        {
            var brigada = EncontrarBrigadaMasCercana(emergencia);
            if (brigada != null)
            {
                Interlocked.Increment(ref _brigadasEnServicio);
                MostrarDespachoEnVivo(emergencia, brigada);

                var tiempoRespuesta = CalcularTiempoRespuesta(emergencia, brigada);
                await Task.Delay(tiempoRespuesta / 10, ct);

                Interlocked.Increment(ref _emergenciasAtendidas);
                Interlocked.Decrement(ref _brigadasEnServicio);
                MostrarEmergenciaResuelta(emergencia, brigada);
            }
            else
            {
                MostrarEmergenciaSinBrigada(emergencia);
            }
        }
        catch (OperationCanceledException) { }
    }

    static async Task MostrarPantallaEnVivoAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                MostrarEstadisticasEnVivo();
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    static void MostrarEstadisticasEnVivo()
    {
        lock (_lockConsole)
        {
            var pos = (Console.CursorTop, Console.CursorLeft);
            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;

            var tiempoTranscurrido = DateTime.Now - _inicioSimulacion;
            var brigadasDisponibles = _todasBrigadas.Count(b => b.Estado == EstadoBrigada.Disponible);

            Console.WriteLine($" BRIGADAS RD - EN VIVO {DateTime.Now:HH:mm:ss} | T: {tiempoTranscurrido:mm\\:ss} | Gen: {_emergenciasGeneradas} | Atend: {_emergenciasAtendidas} | Servicio: {_brigadasEnServicio} | Disp: {brigadasDisponibles}                    ");
            Console.WriteLine($"                                                                                                                                                                   ");

            Console.ResetColor();
            Console.SetCursorPosition(pos.CursorLeft, Math.Max(3, pos.CursorTop));
        }
    }

    static void MostrarDespachoEnVivo(EmergenciaEvento emergencia, Brigada brigada)
    {
        lock (_lockConsole)
        {
            var ubicacion = ObtenerUbicacionCompleta(emergencia);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"EMERGENCIA: ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{emergencia.Tipo}");
            Console.ResetColor();
            Console.Write($" en {ubicacion}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"   Despachando: {brigada.Nombre} ({brigada.Tipo}) | Afectadas: {emergencia.PersonasAfectadas} | Intensidad: {emergencia.Intensidad}");
            Console.ResetColor();
        }
    }

    static void MostrarEmergenciaResuelta(EmergenciaEvento emergencia, Brigada brigada)
    {
        lock (_lockConsole)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   Emergencia #{emergencia.Id} resuelta por {brigada.Nombre}");
            Console.ResetColor();
        }
    }

    static void MostrarEmergenciaSinBrigada(EmergenciaEvento emergencia)
    {
        lock (_lockConsole)
        {
            var provincia = _provincias.FirstOrDefault(p => p.Id == emergencia.ProvinciaId);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"   Sin brigadas disponibles para emergencia en {provincia?.Nombre}");
            Console.ResetColor();
        }
    }

    static void MostrarResumenFinal()
    {
        Console.WriteLine("\n");
        MostrarTitulo("RESUMEN DE SIMULACION EN VIVO");

        var tiempoTotal = DateTime.Now - _inicioSimulacion;
        var tasaExito = _emergenciasGeneradas > 0 ? (double)_emergenciasAtendidas / _emergenciasGeneradas * 100 : 0;

        Console.WriteLine($"Duración total: {tiempoTotal:mm\\:ss}");
        Console.WriteLine($"Emergencias generadas: {_emergenciasGeneradas}");
        Console.WriteLine($"Emergencias atendidas: {_emergenciasAtendidas}");
        Console.WriteLine($"Tasa de éxito: {tasaExito:F1}%");
        Console.WriteLine($"Promedio por minuto: {_emergenciasGeneradas / Math.Max(tiempoTotal.TotalMinutes, 1):F1}");

        Console.WriteLine("\nPresiona cualquier tecla para volver al menú...");
        Console.ReadKey();
    }
    #endregion

    #region Simulaciones Secuencial y Paralela
    static async Task EjecutarSimulacionSecuencialAsync()
    {
        Console.WriteLine("SIMULACION SECUENCIAL CON DATOS REALES");
        Console.WriteLine("======================================\n");

        var cantidadEmergencias = SolicitarCantidadEmergencias();
        var emergencias = GenerarEmergenciasReales(cantidadEmergencias);

        Console.WriteLine($"Procesando {emergencias.Count} emergencias de forma SECUENCIAL...\n");

        var cronometro = Stopwatch.StartNew();
        var procesadas = 0;

        foreach (var emergencia in emergencias)
        {
            try
            {
                await ProcesarEmergenciaConDatosRealesAsync(emergencia, CancellationToken.None);
                procesadas++;

                if (procesadas % (emergencias.Count / 4) == 0 || procesadas == emergencias.Count)
                    Console.WriteLine($"Progreso: {procesadas}/{emergencias.Count} ({(double)procesadas / emergencias.Count * 100:F1}%)");
            }
            catch { }
        }

        cronometro.Stop();
        MostrarResultadosSimulacion("SECUENCIAL", cronometro.Elapsed, procesadas, emergencias.Count);
    }

    static async Task EjecutarSimulacionParalelaAsync()
    {
        Console.WriteLine("SIMULACION PARALELA CON DATOS REALES");
        Console.WriteLine("====================================\n");

        var cantidadEmergencias = SolicitarCantidadEmergencias();
        var emergencias = GenerarEmergenciasReales(cantidadEmergencias);
        var nucleos = Environment.ProcessorCount;

        var config = new ConfigParalelo
        {
            MaxGradoParalelismo = nucleos,
            HabilitarMetricas = true,
            CapacidadCola = cantidadEmergencias + 100
        };

        Console.WriteLine($"Usando {nucleos} núcleos | Procesando {emergencias.Count} emergencias...\n");

        using var gestorParalelo = new GestorParaleloExtendido(config, recursosDisponibles: _todasBrigadas.Count);
        emergencias.ForEach(gestorParalelo.EncolarEmergencia);

        var (tiempo, procesadas) = await gestorParalelo.ProcesarEnParaleloAsync(ProcesarEmergenciaConDatosRealesAsync);
        var stats = gestorParalelo.ObtenerEstadisticas();

        MostrarResultadosSimulacion("PARALELA", tiempo, procesadas, emergencias.Count);
        MostrarEstadisticasParalelismo(stats);
    }

    static async Task CompararRendimientoAsync()
    {
        Console.WriteLine("COMPARACION DE RENDIMIENTO");
        Console.WriteLine("==========================\n");

        var cantidadEmergencias = SolicitarCantidadEmergencias();
        var emergencias = GenerarEmergenciasReales(cantidadEmergencias);

        Console.WriteLine("Ejecutando versión secuencial...");
        var tiempoSecuencial = await MedirTiempoSecuencialAsync(emergencias);

        Console.WriteLine("Ejecutando versión paralela...");
        var tiempoParalelo = await MedirTiempoParaleloAsync(emergencias);

        MostrarComparacionResultados(tiempoSecuencial, tiempoParalelo);
    }

    static void MostrarComparacionResultados(TimeSpan tiempoSecuencial, TimeSpan tiempoParalelo)
    {
        Console.WriteLine("\nRESULTADOS DE COMPARACION");
        Console.WriteLine("========================");
        Console.WriteLine($"Tiempo secuencial:  {tiempoSecuencial.TotalSeconds:F2} segundos");
        Console.WriteLine($"Tiempo paralelo:    {tiempoParalelo.TotalSeconds:F2} segundos");

        if (tiempoParalelo.TotalSeconds > 0)
        {
            var speedup = tiempoSecuencial.TotalSeconds / tiempoParalelo.TotalSeconds;
            var eficiencia = speedup / Environment.ProcessorCount * 100;

            Console.WriteLine($"Aceleración (Speedup): {speedup:F2}x");
            Console.WriteLine($"Eficiencia: {eficiencia:F1}%");
            Console.WriteLine($"Núcleos utilizados: {Environment.ProcessorCount}");

            var evaluacion = speedup > 1.5 ? "mejora significativa" :
                           speedup > 1.0 ? "mejora modesta" : "sin ventajas";
            Console.WriteLine($"El paralelismo ofrece {evaluacion}");
        }
    }
    #endregion