# Sistema de Brigadas de Emergencia RD

Sistema de simulación de emergencias para República Dominicana que permite comparar el rendimiento entre procesamiento secuencial y paralelo.

## ¿Qué hace?

Simula emergencias reales en las 32 provincias de RD y despacha brigadas (Bomberos, Defensa Civil, Cruz Roja, AMET) para atenderlas. Compara qué tan rápido se resuelven las emergencias usando procesamiento secuencial vs paralelo.

## Cómo usarlo

### Instalación
```bash
git clone https://github.com/Manushark/BrigadasEmergenciaRD.git
cd BrigadasEmergenciaRD
dotnet run
```

### Menú Principal
```
1. Simulación EN VIVO - Ve emergencias generándose en tiempo real
2. Simulación Secuencial - Procesa emergencias una por una  
3. Simulación Paralela - Usa todos los núcleos del procesador
4. Comparar Rendimiento - Secuencial vs Paralelo
5. Análisis de Speedup - Prueba con diferentes núcleos
6. Ver Reportes - Abre los archivos .txt generados
```

## Ejemplo de uso

**Simulación en vivo:**
```
EMERGENCIA: IncendioEstructural en Santiago
   Despachando: Bomberos Santiago | Afectadas: 12 personas
   Emergencia en Santiago resuelta por Bomberos Santiago
```

**Comparación de rendimiento:**
```
Tiempo secuencial: 45.23 segundos
Tiempo paralelo: 13.45 segundos
Speedup: 3.36x más rápido
```

## Requisitos
- .NET 8.0 o superior
- Windows, macOS o Linux

---
**Proyecto educativo para demostrar paralelismo en C#** 🇩🇴
