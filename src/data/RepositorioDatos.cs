using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using BrigadasEmergenciaRD.Core.Models;

// Clase para manejar la carga de datos desde archivos
public class RepositorioDatos
{
    // Ruta base donde se encuentran los archivos de datos
    private readonly string _rutaDatos;

    // Constructor que inicializa la ruta de datos y crea el directorio si no existe
    public RepositorioDatos(string rutaDatos = "./data/")
    {
        _rutaDatos = rutaDatos;
        Directory.CreateDirectory(_rutaDatos);
    }

    // Carga provincias, municipios y barrios desde JSON
    public List<Provincia> CargarProvincias()
    {
        try
        {
            // Construye la ruta al archivo barrios.json
            string ruta = Path.Combine(_rutaDatos, "barrios.json");
            // Lee el contenido del archivo
            string json = File.ReadAllText(ruta);
            // Deserializa el JSON directamente a una lista de provincias
            var provincias = JsonConvert.DeserializeObject<List<Provincia>>(json) ?? new List<Provincia>();
            // Asegura que todas las listas estén inicializadas para evitar null references
            foreach (var prov in provincias)
            {
                prov.Municipios = prov.Municipios ?? new List<Municipio>();
                foreach (var mun in prov.Municipios)
                {
                    mun.Barrios = mun.Barrios ?? new List<Barrio>();
                }
            }
            // Muestra información de las provincias cargadas
            Console.WriteLine($"Provincias cargadas: {provincias.Count}");
            foreach (var prov in provincias.Take(5))
            {
                Console.WriteLine($"  - {prov.Id}: {prov.Nombre} (Municipios: {prov.Municipios.Count})");
                foreach (var mun in prov.Municipios.Take(2))
                {
                    Console.WriteLine($"    - {mun.Id}: {mun.Nombre} (Barrios: {mun.Barrios.Count})");
                    foreach (var barrio in mun.Barrios.Take(2))
                    {
                        Console.WriteLine($"      - {barrio.Id}: {barrio.Nombre}");
                    }
                }
            }
            if (provincias.Count > 5) Console.WriteLine("  ...");
            return provincias;
        }
        catch (FileNotFoundException)
        {
            // Maneja el caso en que el archivo no se encuentra
            Console.WriteLine("Archivo JSON de barrios no encontrado.");
            return new List<Provincia>();
        }
        catch (JsonException ex)
        {
            // Maneja errores de deserialización JSON
            Console.WriteLine($"Error leyendo JSON de barrios: {ex.Message}");
            return new List<Provincia>();
        }
        catch (Exception ex)
        {
            // Maneja cualquier otro error inesperado
            Console.WriteLine($"Error al cargar provincias: {ex.Message}");
            return new List<Provincia>();
        }
    }
}