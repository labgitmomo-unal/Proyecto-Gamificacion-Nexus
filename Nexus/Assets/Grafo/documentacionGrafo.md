# Documentación Holograma

Resumen y documentación técnica del módulo `Grafo` (Assets/Grafo).

## Propósito

El conjunto de scripts bajo `Assets/Grafo` implementa un mini-sistema de "grafo holográfico" para VR: nodos representados por esferas agarrables, bases/socket donde anclar nodos, y conexiones visuales (LineRenderers) que se actualizan según ocupación y estado.

## Componentes principales añadidos

- `GraphNodeBall.cs`: Bola agarrable que representa un nodo. Maneja estados visuales (idle, hover, en base). En la implementación actual cada nodo crea una instancia de `Material` vía `_renderer.material` y escribe el color de emisión directamente en ese material (no se usa `MaterialPropertyBlock`). Esto garantiza emisión visible por nodo a costa de instanciar materiales por objeto.

- `BaseSlot.cs`: Wrapper ligero sobre `XRSocketInteractor` que detecta si una `GraphNodeBall` está anclada. Expone la propiedad `IsOccupied` y delega en `XRSocketInteractor.hasSelection`. En la implementación actual **no** expone eventos `OnBallPlaced`/`OnBallRemoved`; otros componentes (por ejemplo `BaseConnectionManager`) evalúan el estado por polling.

- `BaseConnectionManager.cs`: Descubre automáticamente `BaseSlot` hijos y `LineRenderer` bajo el hijo `Conexiones`. Mantiene pares de conexiones y actualiza posición, gradiente y ancho de las líneas cuando detecta que ambas bases están ocupadas. Nota: la implementación actual usa polling en `Update()` para evaluar `IsOccupied` y solo modifica `LineRenderer` cuando el estado cambia; no depende de eventos desde `BaseSlot`.

## Otros componentes relacionados

- `GraphManager.cs`, `HologramNodeFeedback.cs`, `MejoraMovilidad.cs`, `NodoSnapZone.cs`, `HologramaDataLoader.cs`, `IGraphManager.cs` (descrito en el documento previo). Estos forman la capa de reglas, mejoras y carga de datos del sistema holográfico.

## Configuración y parámetros importantes

- `BaseConnectionManager` Inspector: `_alturaOffset`, `_anchoInactivo`, `_anchoActivo`, `_gradienteInactivo`, `_gradienteActivo`.
- `GraphNodeBall` Inspector: `_renderer`, `_colorBase`, `_colorHover`, `_colorEnBase`, `_intensidadBase`, `_intensidadHover`, `_intensidadEnBase`.
- `BaseSlot` no expone parámetros públicos relevantes; se basa en el `XRSocketInteractor` del mismo GameObject.

## Buenas prácticas y notas de integración

- Mantener el hijo `Conexiones` bajo el GameObject que tiene `BaseConnectionManager` y nombrar cada línea con el formato `Conexion_BX_BY` (p. ej. `Conexion_B1_B2`).
- Nombrar los GameObjects de los slots como `Base1`, `Base2`, ... para que el auto-descubrimiento empareje correctamente.
- Si se renombra este archivo de documentación, conservar la `.meta` de Unity para preservar el GUID del asset.

## Ejemplo rápido de uso

1. Crear un GameObject vacío `Grafo` y añadir `BaseConnectionManager`.
2. Añadir hijos `Base1`…`BaseN` con `BaseSlot` (cada uno con `XRSocketInteractor`).
3. Crear un hijo `Conexiones` y bajo él LineRenderers llamados `Conexion_B1_B2`.
4. Añadir `GraphNodeBall` prefabs a la escena para interactuar con las bases.

---

Archivo renombrado/copiado desde `documentaciónHolograma.md` → `documentacionHolograma.md` (sin tilde) para evitar problemas de codificación/consistencia en rutas.
