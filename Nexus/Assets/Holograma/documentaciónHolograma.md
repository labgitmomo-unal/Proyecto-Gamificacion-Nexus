# Documentación Holograma

## Componentes

- [GraphManager.cs](GraphManager.cs): controla el grafo holográfico. Administra rutas, calcula grosor y color de las líneas, aplica y revierte mejoras, maneja el hover de los nodos y el presupuesto de medidas activas.
- [HologramNodeFeedback.cs](HologramNodeFeedback.cs): representa el feedback visual e interactivo de cada nodo. Maneja el brillo al pasar el cursor, el pulso de confirmación y el factor de prioridad del nodo.
- [MejoraMovilidad.cs](MejoraMovilidad.cs): componente de cada mejora o pivote que el jugador puede agarrar. Guarda el tipo de solución, sus factores de reducción y su estado de aplicación.
- [NodoSnapZone.cs](NodoSnapZone.cs): zona de anclaje de cada nodo. Valida la capa de interacción, revisa el presupuesto y llama a `GraphManager` para aplicar o revertir la mejora.
- [HologramaDataLoader.cs](HologramaDataLoader.cs): carga datos locales del JSON y los aplica al grafo y al sistema de tráfico.
- [IGraphManager.cs](IGraphManager.cs): interfaz que desacopla a `MejoraMovilidad` de la implementación concreta de `GraphManager`.

## Datos y configuración

- `rutas`: lista de conexiones del grafo.
- `volumenMaxReferencia`: valor de referencia para calcular el grosor de las líneas.
- `anchoMinimo` y `anchoMaximo`: límites visuales del grosor.
- `gradienteCongestión`: gradiente usado para pintar la congestión de verde a rojo.
- `densidadMaxReferencia`: referencia global para normalizar la congestión de todas las rutas.
- `intensidadHoverLinea` y `colorHoverLinea`: configuran el resaltado visual al hacer hover.
- `factorReduccionSemaforo` y `factorReduccionPeaje`: definen cuánto reduce cada tipo de mejora.
- `maxMedidas`: límite de mejoras activas simultáneas.
