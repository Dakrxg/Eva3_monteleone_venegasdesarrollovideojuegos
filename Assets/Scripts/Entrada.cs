using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lee el teclado sin importar si el proyecto usa el Input Manager antiguo,
/// el Input System nuevo o "Both". Así el juego funciona en cualquier configuración.
/// </summary>
public static class Entrada
{
    // Eje horizontal: A/D o flechas (-1 a 1)
    public static float Horizontal()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetAxis("Horizontal");
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return 0f;
        float v = 0f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) v -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) v += 1f;
        return v;
#else
        return 0f;
#endif
    }

    // Eje vertical: W/S o flechas (-1 a 1)
    public static float Vertical()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetAxis("Vertical");
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return 0f;
        float v = 0f;
        if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
        if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
        return v;
#else
        return 0f;
#endif
    }

    // Se llama en Update: true solo en el frame en que se presiona Espacio
    public static bool SaltoPresionado()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.spaceKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // true mientras se mantiene Shift
    public static bool SprintPresionado()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#elif ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && k.shiftKey.isPressed;
#else
        return false;
#endif
    }
}
