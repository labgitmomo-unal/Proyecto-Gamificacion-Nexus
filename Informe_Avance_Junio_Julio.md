# Informe de Avance — Proyecto Nexus
## Período: 5 de Junio al 5 de Julio de 2026

---

### 1. Pantalla de Inicio de Sesión

Se creó la escena base donde el jugador inicia sesión o se registra antes de entrar al juego. Se implementó la lógica completa de autenticación (formularios de login y registro, campos de texto, botones), y se configuró la transición entre esta pantalla y la escena principal del juego usando carga en segundo plano, de modo que la transición sea fluida.

---

### 2. Optimización y Rendimiento

Se trabajó intensamente en mejorar los FPS y la fluidez general del juego:

- Ajustes de rendimiento general para mantener una tasa de cuadros estable.
- Se agregó un sistema que reduce automáticamente la calidad gráfica (resolución de renderizado, distancia de sombras) cuando los FPS bajan, y la recupera cuando el rendimiento mejora.
- Se implementó un optimizador específico para la cinemática inicial que prepara los vehículos y el audio antes de que comience la escena, evitando tirones.
- Se precargaron texturas y sonidos al iniciar para evitar pausas durante el juego.

---

### 3. Cinemática de Apertura

Se realizaron múltiples mejoras a la escena cinematográfica inicial:

- Se corrigió una distorsión visual que ocurría en el waypoint 13 (un punto específico del recorrido de la cámara).
- Se hicieron ajustes a los tiempos y transiciones de la cámara virtual.
- Se implementó un sistema para que el jugador pueda saltar la cinemática presionando un botón del control VR.
- Se activó la aparición de un edificio en el momento justo durante la cinemática.

---

### 4. Sistema de Audio y Sonido

Se añadió sonido a múltiples partes del juego que antes eran silenciosas:

- **Reto 1 (Panel_1)**: Ahora cuando el jugador llega al panel, suena una voz explicando el desafío. Solo después de que termina la explicación, comienza la cuenta regresiva. También se agregó una alerta sonora que suena al 75%, 50% y 25% del tiempo restante, y un sonido especial cuando el desafío se completa (ya sea por éxito o por tiempo agotado).
- **Reto 2 (Panel_2)**: Se aplicó la misma mecánica: audio de explicación al llegar, alertas en los mismos porcentajes de tiempo, y sonido de finalización.
- Se agregó un indicador sonoro especial que guía al jugador hacia el Panel_2 una vez completado el primer reto.

---

### 5. Navegación entre Paneles

- Se creó un collider (zona de detección) que al tocarlo activa la interfaz del Panel_2.
- Se implementó un indicador visual y auditivo que le dice al jugador hacia dónde dirigirse después de completar el Panel_1.

---

### 6. Dinamismo en el Juego

- Se agregó aleatoriedad al orden en que aparecen los elementos en ambos paneles. Cada vez que el jugador inicia un reto, los ítems se mezclan al azar, haciendo que cada partida sea diferente y evitando que el jugador memorice posiciones.

---

### Resumen

| Área | Progreso |
|------|----------|
| Inicio de sesión | Completado |
| Rendimiento y optimización | Avanzado |
| Cinemática inicial | Corregida y ajustada |
| Sonido en Reto 1 | Completado |
| Sonido en Reto 2 | Completado |
| Navegación Panel_1 → Panel_2 | Completado |
| Aleatoriedad en retos | Completado |
