using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BrigadasEmergenciaRD.Core.Interfaces;
using BrigadasEmergenciaRD.Core.Models;
using BrigadasEmergenciaRD.Core.Enums;
using System.Collections.Concurrent;

namespace BrigadasEmergenciaRD.Data
{
    /// <summary>
    /// Carga Provincias, Municipios y Barrios desde ./data/{provincias,municipios,barrios}.json
    /// - Mapea códigos ONE (string) a Ids int
    /// - Completa población y coordenadas con valores aleatorios razonables si faltan
    /// - Genera algunas brigadas por provincia para que el Gestor pueda asignar
    /// </summary>
    public class JsonDataProvider : IDataProvider
    {
        private readonly string _rutaDatos;
        private readonly Random _rng;
        private List<Provincia>? _provinciasCache; // cache en memoria para no releer disco cada vez

        public JsonDataProvider(string? rutaDatos = "./data/", int? semilla = null)
        {
            _rutaDatos = rutaDatos ?? Path.Combine(AppContext.BaseDirectory, "data");
            _rng = semilla.HasValue ? new Random(semilla.Value) : new Random();
        }

        // --------- API pública (IDataProvider) -------------------------

        public async Task<IEnumerable<Provincia>> ObtenerProvinciasAsync()
        {
            if (_provinciasCache == null) await CargarTodoAsync();
            return _provinciasCache;
        }

        public async Task<IEnumerable<Municipio>> ObtenerMunicipiosAsync(int provinciaId)
        {
            if (_provinciasCache == null) await CargarTodoAsync();
            return _provinciasCache.FirstOrDefault(p => p.Id == provinciaId)?.Municipios ?? new List<Municipio>();
        }

        public async Task<IEnumerable<Barrio>> ObtenerBarriosAsync(int municipioId)
        {
            if (_provinciasCache == null) await CargarTodoAsync();
            return _provinciasCache.SelectMany(p => p.Municipios)
                                   .FirstOrDefault(m => m.Id == municipioId)?.Barrios ?? new List<Barrio>();
        }

        public async Task<IEnumerable<Brigada>> ObtenerBrigadasDisponiblesAsync(int provinciaId)
        {
            if (_provinciasCache == null) await CargarTodoAsync();
            var p = _provinciasCache!.FirstOrDefault(pv => pv.Id == provinciaId);

            // Devolver IEnumerable<Brigada> sin pelear con tipos del ??
            return p != null ? (IEnumerable<Brigada>)p.BrigadasDisponibles : Array.Empty<Brigada>();
        }

        // --------- Carga y mapeo desde JSON ---------------------------

        private async Task CargarTodoAsync()
        {
            // 1) Provincias
            var provinciasPath = Path.Combine(_rutaDatos, "provincias.json");
            var municipiosPath = Path.Combine(_rutaDatos, "municipios.json");
            var barriosPath = Path.Combine(_rutaDatos, "barrios.json");

            if (!File.Exists(provinciasPath) || !File.Exists(municipiosPath) || !File.Exists(barriosPath))
                throw new FileNotFoundException(
                                $"No se encontraron JSON en: '{_rutaDatos}'. " +
                                $"Esperado: {Path.GetFullPath(provinciasPath)}, {Path.GetFullPath(municipiosPath)}, {Path.GetFullPath(barriosPath)}"
                            );


            var provWrapper = JsonConvert.DeserializeObject<ProvinciasWrapper>(await File.ReadAllTextAsync(provinciasPath))
                              ?? new ProdinciasWrapperFallback();
            var muniWrapper = JsonConvert.DeserializeObject<MunicipiosWrapper>(await File.ReadAllTextAsync(municipiosPath))
                              ?? new MunicipiosWrapper { Municipios = new List<MunicipioDto>() };
            var barriosList = JsonConvert.DeserializeObject<List<ProvinciaBarriosDto>>(await File.ReadAllTextAsync(barriosPath))
                              ?? new List<ProvinciaBarriosDto>();

            // Mapa codigoProvincia(string) -> Provincia (objeto)
            var provincias = new List<Provincia>();
            foreach (var p in provWrapper.Provincias)
            {
                var idInt = SafeParseInt(p.Codigo, fallback: _rng.Next(1, 999)); // Id a partir del código
                var prov = new Provincia
                {
                    Id = idInt,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    Region = "Región " + _rng.Next(1, 3),
                    Poblacion = _rng.Next(200_000, 2_000_000),
                    Coordenadas = CoordenadaAleatoriaRD(),
                    Municipios = new List<Municipio>(),
                    BrigadasDisponibles = new ConcurrentBag<Brigada>(),
                    VulnerabilidadClimatica = RandomVulnerabilidad()
                };
                provincias.Add(prov);
            }

            // 2) Municipios: vienen como arreglo plano con "provincia":"01", "codigo":"01", "nombre":"..."
            // Creamos Municipios y los metemos en su Provincia por coincidencia de CODIGO.
            var muniIdCounter = 1;
            foreach (var m in muniWrapper.Municipios)
            {
                var prov = provincias.FirstOrDefault(px => px.Codigo == m.Provincia);
                if (prov == null) continue;

                var muni = new Municipio
                {
                    Id = muniIdCounter++,
                    ProvinciaId = prov.Id,
                    Nombre = m.Nombre,
                    Codigo = m.Codigo,
                    Poblacion = _rng.Next(20_000, 250_000),
                    Coordenadas = CoordenadaAleatoriaRD(),
                    Barrios = new List<Barrio>(),
                    Provincia = prov
                };
                prov.Municipios.Add(muni);
            }

            // 3) Barrios: estructura anidada por provincia -> municipios -> barrios
            // Debemos casarlos por NOMBRE de provincia y de municipio (según tus ejemplos)
            var barrioIdCounter = 1;
            foreach (var provDto in barriosList)
            {
                var prov = provincias.FirstOrDefault(p => p.Nombre == provDto.Provincia);
                if (prov == null) continue;

                foreach (var muniDto in provDto.Municipios ?? Enumerable.Empty<MunicipioBarriosDto>())
                {
                    var muni = prov.Municipios.FirstOrDefault(m => m.Nombre == muniDto.Nombre);
                    if (muni == null) continue;

                    foreach (var b in muniDto.Barrios ?? Enumerable.Empty<BarrioDto>())
                    {
                        // Si el JSON trae id como string (010101), intentamos usarlo; si no, autoincremental
                        var idInt = SafeParseInt(b.Id, fallback: barrioIdCounter++);

                        var barrio = new Barrio
                        {
                            Id = idInt,
                            MunicipioId = muni.Id,
                            Nombre = b.Nombre,
                            Poblacion = _rng.Next(800, 25_000),
                            Coordenadas = CoordenadaAleatoriaRD(),
                            Municipio = muni
                        };
                        muni.Barrios.Add(barrio);
                    }
                }
            }

            // 4) Brigadas por provincia (al menos 2-3 para que la simulación fluya)
            foreach (var prov in provincias)
            {
                var baseCoord = prov.Coordenadas ?? CoordenadaAleatoriaRD();

                prov.BrigadasDisponibles.Add(new Brigada
                {
                    Id = _rng.Next(1000, 9999),
                    Nombre = $"Ambulancia {prov.Nombre}",
                    Tipo = TipoBrigada.Medica,
                    Estado = EstadoBrigada.Disponible,
                    UbicacionActual = Perturbar(baseCoord, 0.05),
                    CapacidadMaxima = 4
                });
                prov.BrigadasDisponibles.Add(new Brigada
                {
                    Id = _rng.Next(1000, 9999),
                    Nombre = $"Bomberos {prov.Nombre}",
                    Tipo = TipoBrigada.Bomberos,
                    Estado = EstadoBrigada.Disponible,
                    UbicacionActual = Perturbar(baseCoord, 0.05),
                    CapacidadMaxima = 5
                });
                prov.BrigadasDisponibles.Add(new Brigada
                {
                    Id = _rng.Next(1000, 9999),
                    Nombre = $"Defensa Civil {prov.Nombre}",
                    Tipo = TipoBrigada.DefensaCivil,
                    Estado = EstadoBrigada.Disponible,
                    UbicacionActual = Perturbar(baseCoord, 0.05),
                    CapacidadMaxima = 8
                });
            }

            _provinciasCache = provincias;
        }
        public async Task<List<Provincia>> ObtenerDatosCompletosAsync()
        {
            var provincias = (await ObtenerProvinciasAsync()).ToList();

            foreach (var provincia in provincias)
            {
                // Cargar municipios
                var municipios = (await ObtenerMunicipiosAsync(provincia.Id)).ToList();
                provincia.Municipios = municipios;

                // Para cada municipio, cargar sus barrios
                foreach (var municipio in municipios)
                {
                    municipio.Provincia = provincia;
                    var barrios = (await ObtenerBarriosAsync(municipio.Id)).ToList();
                    municipio.Barrios = barrios;

                    // Establecer referencia al municipio en cada barrio
                    foreach (var barrio in barrios)
                    {
                        barrio.Municipio = municipio;
                    }
                }

                // Cargar brigadas
                var brigadas = await ObtenerBrigadasDisponiblesAsync(provincia.Id);
                foreach (var brigada in brigadas)
                {
                    provincia.BrigadasDisponibles.Add(brigada);
                }
            }

            return provincias;
        }

        // --------- Utilidades ----------------------------------------

        private int SafeParseInt(string s, int fallback)
        {
            if (int.TryParse(s, out var val)) return val;
            // si tiene ceros a la izquierda (p.ej. "0101"), intentamos removerlos
            if (!string.IsNullOrWhiteSpace(s))
            {
                var trimmed = s.TrimStart('0');
                if (int.TryParse(string.IsNullOrEmpty(trimmed) ? "0" : trimmed, out val))
                    return val == 0 ? fallback : val;
            }
            return fallback;
        }

        private Coordenada CoordenadaAleatoriaRD()
        {
            // Aproximación de RD: Lat ~ [17.5, 19.9], Lon ~ [-71.8, -68.3]
            var lat = 17.5 + _rng.NextDouble() * (19.9 - 17.5);
            var lon = -71.8 + _rng.NextDouble() * (-68.3 + 71.8); // suma porque lon es negativa
            return new Coordenada(lat, lon);
        }

        private Coordenada Perturbar(Coordenada c, double maxDelta)
        {
            return new Coordenada(
                c.Latitud + (_rng.NextDouble() * 2 - 1) * maxDelta,
                c.Longitud + (_rng.NextDouble() * 2 - 1) * maxDelta
            );
        }

        private string RandomVulnerabilidad()
        {
            var vals = new[] { "Baja", "Media", "Alta" };
            return vals[_rng.Next(vals.Length)];
        }

        // --------- DTOs para deserializar tus JSON --------------------

        private class ProdinciasWrapperFallback : ProvinciasWrapper
        {
            public ProdinciasWrapperFallback() { Provincias = new List<ProvinciaDto>(); }
        }

        private class ProvinciasWrapper
        {
            [JsonProperty("provincias")]
            public List<ProvinciaDto> Provincias { get; set; }
        }

        private class ProvinciaDto
        {
            [JsonProperty("codigo")] public string Codigo { get; set; }
            [JsonProperty("nombre")] public string Nombre { get; set; }
        }

        private class MunicipiosWrapper
        {
            [JsonProperty("municipios")]
            public List<MunicipioDto> Municipios { get; set; }
        }

        private class MunicipioDto
        {
            [JsonProperty("provincia")] public string Provincia { get; set; } // "01"
            [JsonProperty("codigo")] public string Codigo { get; set; }    // "01"
            [JsonProperty("nombre")] public string Nombre { get; set; }
        }

        private class ProvinciaBarriosDto
        {
            [JsonProperty("id")] public string Id { get; set; }         // "01" (no lo usamos)
            [JsonProperty("provincia")] public string Provincia { get; set; }  // "Distrito Nacional"
            [JsonProperty("municipios")] public List<MunicipioBarriosDto> Municipios { get; set; }
        }

        private class MunicipioBarriosDto
        {
            [JsonProperty("id")] public string Id { get; set; }      // "0101" (no imprescindible)
            [JsonProperty("nombre")] public string Nombre { get; set; }  // "Santo Domingo de Guzmán"
            [JsonProperty("barrios")] public List<BarrioDto> Barrios { get; set; }
        }

        private class BarrioDto
        {
            [JsonProperty("id")] public string Id { get; set; }      // "010101" (opcional)
            [JsonProperty("nombre")] public string Nombre { get; set; }
        }
    }
}
