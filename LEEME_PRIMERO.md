# El Faro de Bruma 3D — Evaluación Sumativa N°03 (TILE22)

Evolución en 3D del juego 2D "El Faro de Bruma": Nox (ahora una esfera de luz) recorre un
acantilado 3D recolectando fragmentos de luz para encender el faro. Reutiliza el audio del 2D.

## Cómo abrirlo (Unity Hub, Unity 6000.2.3f1)
1. Descomprime el zip. Unity Hub > **Añadir > Añadir proyecto desde el disco** > elige la carpeta `FarolBruma3D`.
2. Ábrelo con 6000.2.3f1 (la primera vez demora varios minutos).
3. Cuando termine de importar, **la escena se construye sola** y aparece un aviso "Juego construido".
   Si no apareció: menú **Farol 3D > 1. Construir el juego**.
4. Abre `Assets/Scenes/Nivel1_FarolDeBruma3D` (menú Farol 3D > 2) y presiona **Play**.

Alternativa: copia las carpetas `Assets` (Scripts, Editor, Audio) dentro de tu proyecto `Sumativa_3`
(URP o Built-in, ambos funcionan) y se construye igual.

Controles: WASD/flechas mover · Espacio saltar (solo apoyado) · Shift correr.
Meta: 12 puntos (normal +1, especial +3) → reinicia la partida completa.

## Rúbrica → dónde está
| Criterio | Implementación |
|---|---|
| A. Escenario | Plataforma baja (Y=0), alta (Y=4) e isla (Y=4) unidas por **Rampa**; muros en `Escenario/Paredes` con bordes abiertos (lado este de la plataforma alta y puerta norte) → caída al vacío. 10 materiales en `Assets/Materiales`, luz direccional ajustada (rot 50,-35,0), modelo compuesto: **Faro** (8 piezas) y **Especial** (3 piezas). |
| B. Movimiento | `Movimiento.cs`: `AddForce` con variable `velocidad`; **salto** solo si `EstaEnSuelo()` (SphereCast); **sprint** con Shift. |
| C. Cámara y caída | `CamaraController.cs`: offset fijo en `LateUpdate`, sin rotar. `Movimiento.Reaparecer()`: si Y < -10 vuelve al inicio con velocidad 0. |
| D. Coleccionables | Prefabs `Coleccionable` (tag `Coleccionable`, 11 instancias, giro 30/60/90) y `Especial` (tag `Especial`, 4 instancias, otro material y giro 0/220/40). Trigger + Rigidbody kinematic. |
| E. Interacción | `Recolector.OnTriggerEnter`: desactiva el objeto, suma 1 ó 3, `Debug.Log` con detalle y puntaje acumulado; al llegar a 12 recarga la escena. |

## Si hay errores
Copia el mensaje rojo completo de la consola y envíalo.
