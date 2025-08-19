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