using System;
using System.Threading;
using System.Threading.Tasks;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Simulation;
using BrigadasEmergenciaRD.Data;
using BrigadasEmergenciaRD.Core.Enums;
using BrigadasEmergenciaRD.src.Simulation.UI;
using System.IO;


class Program
{
    static async Task Main(string[] args)
    {
        ConsoleUi.Header("Simulación de Brigadas de Emergencia RD (PRUEBA)");

        // Configuración base de la simulación
        var cfg = new ParametrosSimulacion
        {
            DuracionTickMs = 800,                  // 0.8s por tick
            ProbabilidadBaseEventoPorBarrio = 0.08, // 8% por barrio y por tick (ajústalo si quieres ver más/menos eventos)
            MaximoLlamadas = 15   // solo 15 llamadas

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



        // Intensidad más “movida” para ver actividad
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

        // Iniciar simulación
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
    }
}
