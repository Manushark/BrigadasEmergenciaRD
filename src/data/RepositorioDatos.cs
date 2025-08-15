using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using BrigadasEmergenciaRD.Core.Models; 

public class RepositorioDatos
{
    private readonly string _rutaDatos;

    public RepositorioDatos(string rutaDatos = "./data/")
    {
        _rutaDatos = rutaDatos;
    }

    // Carga provincias desde JSON
    public List<Provincia> CargarProvincias()
    {
        string ruta = Path.Combine(_rutaDatos, "provincias.json");
        string json = File.ReadAllText(ruta);
        var provincias = JsonConvert.DeserializeObject<List<Provincia>>(json) ?? new List<Provincia>();

        Console.WriteLine($"Provincias cargadas: {provincias.Count}");
        foreach (var prov in provincias.Take(5))
            Console.WriteLine($"  - {prov.Codigo}: {prov.Nombre}");
        if (provincias.Count > 5) Console.WriteLine("  ...");

        return provincias;
    }

    // Carga municipios desde JSON y los asocia a provincias
    public void CargarMunicipios(List<Provincia> provincias)
    {
        string ruta = Path.Combine(_rutaDatos, "municipios.json");
        string json = File.ReadAllText(ruta);
        var municipios = JsonConvert.DeserializeObject<List<Municipio>>(json) ?? new List<Municipio>();

        foreach (var mun in municipios)
        {
            var prov = provincias.FirstOrDefault(p => p.Codigo == mun.Provincia);
            if (prov != null) prov.Municipios.Add(mun);
        }

        Console.WriteLine($"Municipios cargados: {municipios.Count}");
        foreach (var mun in municipios.Take(5))
            Console.WriteLine($"  - {mun.Codigo}: {mun.Nombre} (Prov: {mun.Provincia})");
        if (municipios.Count > 5) Console.WriteLine("  ...");
    }

    // Carga configuración desde JSON
    public ConfiguracionSistema CargarConfiguracion()
    {
        string ruta = Path.Combine(_rutaDatos, "configuracion.json");
        string json = File.ReadAllText(ruta);

        var contenedor = JsonConvert.DeserializeObject<Dictionary<string, ConfiguracionSistema>>(json);
        var config = contenedor?["configuracion"] ?? new ConfiguracionSistema();

        Console.WriteLine("Configuración cargada:");
        Console.WriteLine($"  Brigadas iniciales: {config.NumeroBrigadasInicial}");
        Console.WriteLine($"  Tiempo simulación: {config.TiempoSimulacionSegundos}");
        Console.WriteLine($"  Probabilidad emergencia: {config.ProbabilidadEmergenciaPorMinuto}");
        Console.WriteLine($"  Tipos emergencias: {string.Join(", ", config.Simulacion.TiposEmergencias)}");

        return config;
    }

    // Carga barrios desde JSON incluyendo subbarrios
    public List<Barrio> CargarBarriosDesdeJson(string archivoJson = "barrios.json")
    {
        try
        {
            string ruta = Path.Combine(_rutaDatos, archivoJson);
            string json = File.ReadAllText(ruta);

            var contenedor = JsonConvert.DeserializeObject<Dictionary<string, List<Barrio>>>(json);
            var barrios = contenedor?["barrios"] ?? new List<Barrio>();

            Console.WriteLine($"Barrios cargados: {barrios.Count}");
            foreach (var barrio in barrios.Take(5))
            {
                Console.WriteLine($"  - {barrio.Nombre} ({barrio.MunicipioNombre}) | Subbarrios: {barrio.SubBarrios.Count}");
            }
            if (barrios.Count > 5) Console.WriteLine("  ...");

            return barrios;
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Archivo JSON de barrios no encontrado.");
            return new List<Barrio>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error leyendo JSON de barrios: {ex.Message}");
            return new List<Barrio>();
        }
    }
}
