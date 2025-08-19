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

    #region Análisis de Speedup
    static async Task EjecutarAnalisisSpeedupAsync()
    {
        Console.Clear();
        MostrarTitulo("ANALISIS DE SPEEDUP CON MULTIPLES NUCLEOS");

        var cantidadEmergencias = SolicitarCantidadEmergencias();
        var emergencias = GenerarEmergenciasReales(cantidadEmergencias);
        var configuracionesNucleos = new[] { 1, 2, 4, 8, 16, Environment.ProcessorCount }
            .Where(n => n <= Environment.ProcessorCount).Distinct().OrderBy(x => x).ToArray();

        Console.WriteLine($"Sistema: {Environment.ProcessorCount} núcleos | Configuraciones: {string.Join(", ", configuracionesNucleos)}\n");

        // Baseline secuencial
        Console.WriteLine("Midiendo tiempo secuencial...");
        var tiempoSecuencial = await MedirTiempoSecuencialAsync(emergencias);

        var resultados = new List<ResultadoSpeedup>();

        // Probar cada configuración
        foreach (var nucleos in configuracionesNucleos)
        {
            Console.WriteLine($"Probando con {nucleos} núcleo{(nucleos == 1 ? "" : "s")}...");

            try
            {
                var (tiempoParalelo, stats) = await MedirTiempoParaleloConNucleosAsync(emergencias, nucleos);
                var speedup = tiempoSecuencial.TotalSeconds / tiempoParalelo.TotalSeconds;
                var eficiencia = speedup / nucleos * 100;

                resultados.Add(new ResultadoSpeedup
                {
                    Nucleos = nucleos,
                    TiempoSecuencial = tiempoSecuencial,
                    TiempoParalelo = tiempoParalelo,
                    Speedup = speedup,
                    Eficiencia = eficiencia,
                    Estadisticas = stats
                });

                Console.WriteLine($"   {nucleos} núcleos: {tiempoParalelo.TotalSeconds:F2}s | Speedup: {speedup:F2}x | Eficiencia: {eficiencia:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error con {nucleos} núcleos: {ex.Message}");
            }
        }

        MostrarAnalisisSpeedupCompleto(resultados, tiempoSecuencial, cantidadEmergencias);
    }

    static void MostrarAnalisisSpeedupCompleto(List<ResultadoSpeedup> resultados, TimeSpan tiempoSecuencial, int totalEmergencias)
    {
        Console.WriteLine("\n" + new string('=', 80));
        MostrarTitulo("ANALISIS COMPLETO DE SPEEDUP Y EFICIENCIA");

        // Tabla de resultados
        Console.WriteLine("┌─────────┬──────────────┬──────────────┬───────────┬─────────────┐");
        Console.WriteLine("│ Núcleos │   Tiempo (s) │   Speedup    │ Eficiencia │  Throughput │");
        Console.WriteLine("├─────────┼──────────────┼──────────────┼───────────┼─────────────┤");

        foreach (var resultado in resultados.OrderBy(r => r.Nucleos))
        {
            var throughput = totalEmergencias / resultado.TiempoParalelo.TotalSeconds;
            Console.WriteLine($"│ {resultado.Nucleos,7} │ {resultado.TiempoParalelo.TotalSeconds,12:F2} │ {resultado.Speedup,12:F2}x │ {resultado.Eficiencia,9:F1}% │ {throughput,11:F1}/s │");
        }

        Console.WriteLine("└─────────┴──────────────┴──────────────┴───────────┴─────────────┘\n");

        // Análisis y recomendaciones
        MostrarRecomendaciones(resultados);
    }

    static void MostrarRecomendaciones(List<ResultadoSpeedup> resultados)
    {
        var puntoOptimo = resultados.OrderByDescending(r => r.Speedup / r.Nucleos).First();
        var mejorSpeedup = resultados.OrderByDescending(r => r.Speedup).First();

        Console.WriteLine("RECOMENDACIONES");
        Console.WriteLine("===============");
        Console.WriteLine($"Configuración óptima: {puntoOptimo.Nucleos} núcleos (mejor ratio costo/beneficio)");
        Console.WriteLine($"Mejor speedup absoluto: {mejorSpeedup.Speedup:F2}x con {mejorSpeedup.Nucleos} núcleos");

        var escalabilidad = mejorSpeedup.Speedup / mejorSpeedup.Nucleos;
        var evaluacion = escalabilidad > 0.8 ? "Excelente" : escalabilidad > 0.6 ? "Buena" : escalabilidad > 0.4 ? "Regular" : "Limitada";
        Console.WriteLine($"Escalabilidad del sistema: {evaluacion} ({escalabilidad:P1})");
    }

    private class ResultadoSpeedup
    {
        public int Nucleos { get; set; }
        public TimeSpan TiempoSecuencial { get; set; }
        public TimeSpan TiempoParalelo { get; set; }
        public double Speedup { get; set; }
        public double Eficiencia { get; set; }
        public EstadisticasParalelismo Estadisticas { get; set; }
    }
    #endregion

    #region Métodos de Utilidad
    static int SolicitarEntero(string mensaje, int min, int max, int defaultValue)
    {
        Console.Write(mensaje);
        return int.TryParse(Console.ReadLine(), out int valor) && valor >= min && valor <= max ? valor : defaultValue;
    }

    static int SolicitarCantidadEmergencias()
    {
        while (true)
        {
            Console.Write("Cantidad de emergencias a simular (50-1000): ");
            if (int.TryParse(Console.ReadLine(), out int cantidad) && cantidad >= 50 && cantidad <= 1000)
                return cantidad;
            Console.WriteLine("Por favor ingresa un número entre 50 y 1000");
        }
    }

    static List<EmergenciaEvento> GenerarEmergenciasReales(int cantidad)
    {
        var emergencias = new List<EmergenciaEvento>();
        var tipos = Enum.GetValues<TipoEmergencia>();

        for (int i = 0; i < cantidad; i++)
        {
            var provincia = _provincias[_random.Next(_provincias.Count)];
            var municipio = provincia.Municipios[_random.Next(provincia.Municipios.Count)];
            var barrio = municipio.Barrios.Count > 0 ? municipio.Barrios[_random.Next(municipio.Barrios.Count)] : null;
            var coordenadas = barrio?.Coordenadas ?? municipio.Coordenadas;
            var tipoEmergencia = tipos[_random.Next(tipos.Length)];

            emergencias.Add(new EmergenciaEvento
            {
                Id = i + 1,
                Tipo = tipoEmergencia,
                ProvinciaId = provincia.Id,
                MunicipioId = municipio.Id,
                BarrioId = barrio?.Id ?? municipio.Id,
                PersonasAfectadas = GenerarPersonasAfectadas(tipoEmergencia),
                Intensidad = GenerarIntensidad(provincia.VulnerabilidadClimatica),
                Ubicacion = new Coordenada(
                    coordenadas.Latitud + (_random.NextDouble() - 0.5) * 0.01,
                    coordenadas.Longitud + (_random.NextDouble() - 0.5) * 0.01),
                Descripcion = $"{tipoEmergencia} en {barrio?.Nombre ?? municipio.Nombre}, {municipio.Nombre}",
                Timestamp = DateTime.Now.AddMinutes(-_random.Next(0, 180))
            });
        }
        return emergencias;
    }

    static EmergenciaEvento GenerarEmergenciaAleatoria()
    {
        var tipos = Enum.GetValues<TipoEmergencia>();
        var provincia = _provincias[_random.Next(_provincias.Count)];
        var municipio = provincia.Municipios[_random.Next(provincia.Municipios.Count)];
        var barrio = municipio.Barrios.Count > 0 ? municipio.Barrios[_random.Next(municipio.Barrios.Count)] : null;
        var coordenadas = barrio?.Coordenadas ?? municipio.Coordenadas;
        var tipoEmergencia = tipos[_random.Next(tipos.Length)];

        return new EmergenciaEvento
        {
            Id = _random.Next(100000, 999999),
            Tipo = tipoEmergencia,
            ProvinciaId = provincia.Id,
            MunicipioId = municipio.Id,
            BarrioId = barrio?.Id ?? municipio.Id,
            PersonasAfectadas = GenerarPersonasAfectadas(tipoEmergencia),
            Intensidad = GenerarIntensidad(provincia.VulnerabilidadClimatica),
            Ubicacion = new Coordenada(
                coordenadas.Latitud + (_random.NextDouble() - 0.5) * 0.01,
                coordenadas.Longitud + (_random.NextDouble() - 0.5) * 0.01),
            Descripcion = $"{tipoEmergencia} en {barrio?.Nombre ?? municipio.Nombre}, {municipio.Nombre}",
            Timestamp = DateTime.Now
        };
    }

    static string ObtenerUbicacionCompleta(EmergenciaEvento emergencia)
    {
        var provincia = _provincias.FirstOrDefault(p => p.Id == emergencia.ProvinciaId);
        var municipio = provincia?.Municipios.FirstOrDefault(m => m.Id == emergencia.MunicipioId);
        var barrio = municipio?.Barrios.FirstOrDefault(b => b.Id == emergencia.BarrioId);

        return $"{provincia?.Nombre} -> {municipio?.Nombre} -> {barrio?.Nombre ?? "N/A"}";
    }

    static int GenerarPersonasAfectadas(TipoEmergencia tipo) => tipo switch
    {
        TipoEmergencia.PersonasAtrapadas => _random.Next(1, 8),
        TipoEmergencia.IncendioEstructural => _random.Next(5, 25),
        TipoEmergencia.EmergenciaMedica => _random.Next(1, 3),
        TipoEmergencia.Inundacion => _random.Next(10, 100),
        TipoEmergencia.DeslizamientoTierra => _random.Next(3, 20),
        TipoEmergencia.AccidenteVehicular => _random.Next(1, 6),
        _ => _random.Next(1, 10)
    };

    static IntensidadTormenta GenerarIntensidad(string vulnerabilidadClimatica)
    {
        var intensidades = Enum.GetValues<IntensidadTormenta>();
        return vulnerabilidadClimatica switch
        {
            "Alta" => intensidades[_random.Next(2, 4)],
            "Media" => intensidades[_random.Next(1, 3)],
            _ => intensidades[_random.Next(0, 2)]
        };
    }

    static async Task<(string brigada, TimeSpan tiempo)> ProcesarEmergenciaConDatosRealesAsync(EmergenciaEvento emergencia, CancellationToken ct)
    {
        var brigadaMasCercana = EncontrarBrigadaMasCercana(emergencia);
        var tiempoBase = CalcularTiempoRespuesta(emergencia, brigadaMasCercana);
        var tiempoFinal = Math.Max(50, tiempoBase + _random.Next(-50, 150));

        await Task.Delay(tiempoFinal, ct);
        return (brigadaMasCercana.Nombre, TimeSpan.FromMilliseconds(tiempoFinal));
    }

    static Brigada EncontrarBrigadaMasCercana(EmergenciaEvento emergencia)
    {
        var brigadasCapaces = _todasBrigadas
            .Where(b => b.Estado == EstadoBrigada.Disponible && b.PuedeAtender(emergencia.Tipo))
            .ToList();

        if (!brigadasCapaces.Any())
            brigadasCapaces = _todasBrigadas.Where(b => b.Estado == EstadoBrigada.Disponible).ToList();

        if (!brigadasCapaces.Any())
            return _todasBrigadas.First();

        var brigada = brigadasCapaces.OrderBy(b => b.UbicacionActual.CalcularDistanciaKm(emergencia.Ubicacion)).First();
        brigada.CambiarEstado(EstadoBrigada.AtendendoEmergencia);

        _ = Task.Run(async () =>
        {
            await Task.Delay(_random.Next(5000, 15000));
            brigada.CambiarEstado(EstadoBrigada.Disponible);
        });

        return brigada;
    }

    static int CalcularTiempoRespuesta(EmergenciaEvento emergencia, Brigada brigada)
    {
        var tiempoBase = emergencia.Tipo switch
        {
            TipoEmergencia.PersonasAtrapadas => 300,
            TipoEmergencia.IncendioEstructural => 400,
            TipoEmergencia.EmergenciaMedica => 200,
            TipoEmergencia.Inundacion => 350,
            TipoEmergencia.DeslizamientoTierra => 450,
            TipoEmergencia.AccidenteVehicular => 250,
            TipoEmergencia.VientosFuertes => 300,
            TipoEmergencia.CorteEnergia => 500,
            _ => 250
        };

        var distancia = brigada.UbicacionActual.CalcularDistanciaKm(emergencia.Ubicacion);
        var ajusteDistancia = (int)(distancia * 10);
        var ajusteIntensidad = (int)emergencia.Intensidad * 50;

        return tiempoBase + ajusteDistancia + ajusteIntensidad;
    }

    static void MostrarResultadosSimulacion(string modo, TimeSpan tiempo, int procesadas, int total)
    {
        Console.WriteLine($"\nRESULTADOS - MODO {modo}");
        Console.WriteLine("=" + new string('=', 20 + modo.Length));
        Console.WriteLine($"Total emergencias: {total}");
        Console.WriteLine($"Procesadas: {procesadas}");
        Console.WriteLine($"Tasa de éxito: {(double)procesadas / total * 100:F1}%");
        Console.WriteLine($"Tiempo total: {tiempo.TotalSeconds:F2} segundos");
        Console.WriteLine($"Throughput: {procesadas / Math.Max(tiempo.TotalSeconds, 0.1):F1} emergencias/segundo");
    }

    static void MostrarEstadisticasParalelismo(EstadisticasParalelismo stats)
    {
        Console.WriteLine($"\nESTADISTICAS DE PARALELISMO");
        Console.WriteLine("===========================");
        Console.WriteLine($"Hilos concurrentes: {stats.MaximoHilosConcurrentes}");
        Console.WriteLine($"Tareas totales: {stats.TotalTareasEjecutadas}");
        Console.WriteLine($"Tareas en paralelo: {stats.TareasEjecutadasEnParalelo}");

        if (stats.MedicionesPorHilo.Any())
        {
            var minTareas = stats.MedicionesPorHilo.Min(m => m.TareasCompletadas);
            var maxTareas = stats.MedicionesPorHilo.Max(m => m.TareasCompletadas);
            var desbalance = maxTareas > 0 ? (double)(maxTareas - minTareas) / maxTareas * 100 : 0;
            Console.WriteLine($"Balance de carga: {desbalance:F1}% desbalance");
        }
    }

    static async Task<TimeSpan> MedirTiempoSecuencialAsync(List<EmergenciaEvento> emergencias)
    {
        var cronometro = Stopwatch.StartNew();
        foreach (var emergencia in emergencias)
        {
            try { await ProcesarEmergenciaConDatosRealesAsync(emergencia, CancellationToken.None); }
            catch { }
        }
        cronometro.Stop();
        return cronometro.Elapsed;
    }

    static async Task<TimeSpan> MedirTiempoParaleloAsync(List<EmergenciaEvento> emergencias)
    {
        var config = new ConfigParalelo { MaxGradoParalelismo = Environment.ProcessorCount, HabilitarMetricas = true };
        using var gestor = new GestorParaleloExtendido(config, recursosDisponibles: _todasBrigadas.Count);

        emergencias.ForEach(gestor.EncolarEmergencia);
        var (tiempo, _) = await gestor.ProcesarEnParaleloAsync(ProcesarEmergenciaConDatosRealesAsync);
        return tiempo;
    }

    static async Task<(TimeSpan tiempo, EstadisticasParalelismo stats)> MedirTiempoParaleloConNucleosAsync(List<EmergenciaEvento> emergencias, int nucleos)
    {
        var config = new ConfigParalelo { MaxGradoParalelismo = nucleos, HabilitarMetricas = true };
        using var gestor = new GestorParaleloExtendido(config, recursosDisponibles: _todasBrigadas.Count);

        emergencias.ForEach(gestor.EncolarEmergencia);
        var (tiempo, _) = await gestor.ProcesarEnParaleloAsync(ProcesarEmergenciaConDatosRealesAsync);
        return (tiempo, gestor.ObtenerEstadisticas());
    }

    // Métodos de formato y colores
    static void MostrarTitulo(string titulo)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(titulo);
        Console.WriteLine(new string('=', titulo.Length));
        Console.ResetColor();
    }

    static void MostrarInfo(string mensaje)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(mensaje);
        Console.ResetColor();
    }

    static void MostrarExito(string mensaje)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(mensaje);
        Console.ResetColor();
    }

    static void MostrarError(string mensaje)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(mensaje);
        Console.ResetColor();
    }
    #endregion
}