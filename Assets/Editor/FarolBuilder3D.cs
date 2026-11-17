#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Arma TODO el juego "El Faro de Bruma 3D" automáticamente:
/// carpetas, materiales, prefabs, escenario, jugador, cámara, luz, HUD y audio.
/// Se ejecuta solo la primera vez que se abre el proyecto.
/// Si algo falla o quieres reconstruir:  menú  Farol 3D > 1. Construir el juego
/// </summary>
[InitializeOnLoad]
public static class FarolBuilder3D
{
    private const string RutaEscena = "Assets/Scenes/Nivel1_FarolDeBruma3D.unity";
    private const string DirMateriales = "Assets/Materiales";
    private const string DirPrefabs = "Assets/Prefabs";
    private const string DirEscenas = "Assets/Scenes";
    private const string ClaveSesion = "FarolBuilder3D_intentoAuto";

    private static readonly Color ColorNiebla = new Color(0.10f, 0.14f, 0.22f);

    private static Shader shader;
    private static Material mPasto, mPiedra, mMadera, mNox, mOro, mCristal,
                            mFaroBlanco, mFaroRojo, mLuzFaro, mMar;

    // ------------------------------------------------------------ ARRANQUE AUTOMÁTICO

    static FarolBuilder3D()
    {
        EditorApplication.update += AutoConstruir;
    }

    private static void AutoConstruir()
    {
        // Esperamos a que Unity termine de importar y compilar
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode) return;

        EditorApplication.update -= AutoConstruir;

        if (SessionState.GetBool(ClaveSesion, false)) return;   // solo un intento por sesión
        SessionState.SetBool(ClaveSesion, true);

        if (File.Exists(RutaEscena)) return;                    // ya está construido
        Construir();
    }

    [MenuItem("Farol 3D/1. Construir el juego")]
    public static void ConstruirDesdeMenu()
    {
        Construir();
    }

    [MenuItem("Farol 3D/2. Abrir la escena del juego")]
    public static void AbrirEscena()
    {
        if (File.Exists(RutaEscena) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(RutaEscena);
    }

    // ------------------------------------------------------------ CONSTRUCCIÓN

    public static void Construir()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(DirMateriales);
        Directory.CreateDirectory(DirPrefabs);
        Directory.CreateDirectory(DirEscenas);
        AssetDatabase.Refresh();

        AsegurarTag("Coleccionable");
        AsegurarTag("Especial");

        shader = ElegirShader();
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Faro de Bruma 3D",
                "No se encontró un shader válido (Standard / URP Lit).", "Cerrar");
            return;
        }

        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CrearMateriales();
        GameObject prefabNormal = CrearPrefabNormal();
        GameObject prefabEspecial = CrearPrefabEspecial();

        ConfigurarAmbiente();
        Transform escenario = ConstruirEscenario();
        ConstruirFaro(escenario);
        ColocarColeccionables(prefabNormal, prefabEspecial);
        GameObject jugador = CrearJugador();
        CrearCamara(jugador.transform);
        CrearLuzDireccional();
        CrearHUD(jugador.GetComponent<Recolector>());
        CrearMusica();

        EditorSceneManager.SaveScene(escena, RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
        PlayerSettings.productName = "El Faro de Bruma 3D";
        Selection.activeGameObject = jugador;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("El Faro de Bruma 3D",
            "Juego construido.\n\nEscena: " + RutaEscena + "\n\nPresiona Play para jugar.", "Entendido");
    }

    // ------------------------------------------------------------ MATERIALES

    private static Shader ElegirShader()
    {
        bool urp = GraphicsSettings.currentRenderPipeline != null ||
                   GraphicsSettings.defaultRenderPipeline != null;

        Shader s = urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
        return s;
    }

    private static Material CrearMaterial(string nombre, Color color, float metalico, float brillo, float emision)
    {
        Material m = new Material(shader);
        m.name = nombre;

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);   // URP
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);           // Standard
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", brillo);   // URP
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", brillo);   // Standard

        if (emision > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(color.r * emision, color.g * emision, color.b * emision, 1f));
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        string ruta = DirMateriales + "/" + nombre + ".mat";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    private static void CrearMateriales()
    {
        mPasto      = CrearMaterial("Pasto",      new Color(0.28f, 0.55f, 0.30f), 0f,   0.15f, 0f);
        mPiedra     = CrearMaterial("Piedra",     new Color(0.45f, 0.47f, 0.52f), 0f,   0.20f, 0f);
        mMadera     = CrearMaterial("Madera",     new Color(0.55f, 0.36f, 0.20f), 0f,   0.10f, 0f);
        mNox        = CrearMaterial("Nox",        new Color(0.70f, 0.95f, 1.00f), 0f,   0.60f, 0.5f);
        mOro        = CrearMaterial("Oro",        new Color(1.00f, 0.78f, 0.10f), 0.5f, 0.50f, 0f);
        mCristal    = CrearMaterial("Cristal",    new Color(0.20f, 0.90f, 1.00f), 0f,   0.90f, 1.5f);
        mFaroBlanco = CrearMaterial("FaroBlanco", new Color(0.92f, 0.92f, 0.90f), 0f,   0.25f, 0f);
        mFaroRojo   = CrearMaterial("FaroRojo",   new Color(0.80f, 0.15f, 0.12f), 0f,   0.25f, 0f);
        mLuzFaro    = CrearMaterial("LuzFaro",    new Color(1.00f, 0.90f, 0.40f), 0f,   0.50f, 2.0f);
        mMar        = CrearMaterial("Mar",        new Color(0.08f, 0.22f, 0.40f), 0.1f, 0.80f, 0f);
    }

    // ------------------------------------------------------------ PREFABS (coleccionables)

    private static GameObject CrearPrefabNormal()
    {
        // Cubo dorado pequeño que gira lento
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Coleccionable";
        go.transform.rotation = Quaternion.Euler(45f, 45f, 45f);
        go.transform.localScale = Vector3.one * 0.5f;
        go.GetComponent<MeshRenderer>().sharedMaterial = mOro;
        go.tag = "Coleccionable";

        BoxCollider caja = go.GetComponent<BoxCollider>();
        caja.isTrigger = true;                      // detecta contacto sin bloquear
        caja.size = Vector3.one * 1.6f;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Rotador rot = go.AddComponent<Rotador>();
        rot.velocidadGiro = new Vector3(30f, 60f, 90f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, DirPrefabs + "/Coleccionable.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject CrearPrefabEspecial()
    {
        // Modelo COMPUESTO por 3 piezas: cristal + anillo + punta. Gira mucho más rápido.
        GameObject raiz = new GameObject("Especial");
        raiz.tag = "Especial";

        SphereCollider esfera = raiz.AddComponent<SphereCollider>();
        esfera.isTrigger = true;
        esfera.radius = 0.9f;

        Rigidbody rb = raiz.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        Rotador rot = raiz.AddComponent<Rotador>();
        rot.velocidadGiro = new Vector3(0f, 220f, 40f);

        Pieza("Cristal", PrimitiveType.Sphere, raiz.transform, Vector3.zero, Vector3.one * 0.8f, mCristal, false);
        Pieza("Anillo", PrimitiveType.Cylinder, raiz.transform, Vector3.zero, new Vector3(1.4f, 0.03f, 1.4f), mOro, false, new Vector3(65f, 0f, 0f));
        Pieza("Punta", PrimitiveType.Cube, raiz.transform, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.3f, mOro, false, new Vector3(45f, 45f, 0f));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, DirPrefabs + "/Especial.prefab");
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ------------------------------------------------------------ ESCENARIO

    private static void ConfigurarAmbiente()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.34f, 0.42f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = ColorNiebla;
        RenderSettings.fogDensity = 0.012f;
    }

    private static Transform ConstruirEscenario()
    {
        Transform escenario = Vacio("Escenario", null);
        Transform suelo = Vacio("Suelo", escenario);
        Transform paredes = Vacio("Paredes", escenario);
        Transform decoracion = Vacio("Decoracion", escenario);

        // --- Tres plataformas: baja (Y=0), alta (Y=4) e isla del faro (Y=4)
        Pieza("Plataforma_Baja", PrimitiveType.Cube, suelo, new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f), mPasto);
        Pieza("Plataforma_Alta", PrimitiveType.Cube, suelo, new Vector3(0f, 3.5f, 32f), new Vector3(20f, 1f, 20f), mPasto);
        Pieza("Isla_Faro", PrimitiveType.Cube, suelo, new Vector3(0f, 3.5f, 51f), new Vector3(10f, 1f, 10f), mPasto);

        // --- Rampa inclinada: sube 4 m a lo largo de 12 m (de Z=10 a Z=22)
        float z0 = 10f, z1 = 22f, alto = 4f, ancho = 8f, grosor = 1f;
        float largoH = z1 - z0;
        float angulo = Mathf.Atan2(alto, largoH) * Mathf.Rad2Deg;
        float largo = Mathf.Sqrt(largoH * largoH + alto * alto);
        float rad = angulo * Mathf.Deg2Rad;
        Vector3 medioSuperficie = new Vector3(0f, alto / 2f, (z0 + z1) / 2f);
        Vector3 normal = new Vector3(0f, Mathf.Cos(rad), -Mathf.Sin(rad));
        Vector3 centroRampa = medioSuperficie - normal * (grosor / 2f);
        Pieza("Rampa", PrimitiveType.Cube, escenario, centroRampa, new Vector3(ancho, grosor, largo), mMadera, true, new Vector3(-angulo, 0f, 0f));

        // --- Muros del perímetro (altura 2). Los bordes sin muro quedan ABIERTOS (caída al vacío)
        // Plataforma baja: muros Oeste, Este, Sur y Norte (con hueco para la rampa)
        Muro(paredes, "Muro_Baja_Oeste", new Vector3(-9.75f, 1f, 0f), new Vector3(0.5f, 2f, 20f));
        Muro(paredes, "Muro_Baja_Este", new Vector3(9.75f, 1f, 0f), new Vector3(0.5f, 2f, 20f));
        Muro(paredes, "Muro_Baja_Sur", new Vector3(0f, 1f, -9.75f), new Vector3(20f, 2f, 0.5f));
        Muro(paredes, "Muro_Baja_Norte_Izq", new Vector3(-7f, 1f, 9.75f), new Vector3(6f, 2f, 0.5f));
        Muro(paredes, "Muro_Baja_Norte_Der", new Vector3(7f, 1f, 9.75f), new Vector3(6f, 2f, 0.5f));

        // Plataforma alta: Oeste, Sur (hueco de rampa) y Norte (con puerta hacia el vacío).
        // El lado ESTE queda totalmente abierto.
        Muro(paredes, "Muro_Alta_Oeste", new Vector3(-9.75f, 5f, 32f), new Vector3(0.5f, 2f, 20f));
        Muro(paredes, "Muro_Alta_Sur_Izq", new Vector3(-7f, 5f, 22.25f), new Vector3(6f, 2f, 0.5f));
        Muro(paredes, "Muro_Alta_Sur_Der", new Vector3(7f, 5f, 22.25f), new Vector3(6f, 2f, 0.5f));
        Muro(paredes, "Muro_Alta_Norte_Izq", new Vector3(-6.5f, 5f, 41.75f), new Vector3(7f, 2f, 0.5f));
        Muro(paredes, "Muro_Alta_Norte_Der", new Vector3(6.5f, 5f, 41.75f), new Vector3(7f, 2f, 0.5f));

        // --- Mar decorativo bajo el escenario (sin collider: el jugador cae "a través")
        Pieza("Mar", PrimitiveType.Plane, decoracion, new Vector3(0f, -7f, 30f), new Vector3(30f, 1f, 30f), mMar, false);

        return decoracion;   // el faro se agrupa en Decoracion
    }

    private static void Muro(Transform padre, string nombre, Vector3 pos, Vector3 escala)
    {
        Pieza(nombre, PrimitiveType.Cube, padre, pos, escala, mPiedra);
    }

    // Modelo 3D COMPUESTO por varias piezas: el Faro de Bruma, sobre la isla
    private static void ConstruirFaro(Transform decoracion)
    {
        Transform faro = Vacio("Faro", decoracion);
        faro.position = new Vector3(0f, 4f, 51f);

        Pieza("Base", PrimitiveType.Cylinder, faro, new Vector3(0f, 0.5f, 0f), new Vector3(4f, 0.5f, 4f), mPiedra, false);
        Pieza("Torre_1", PrimitiveType.Cylinder, faro, new Vector3(0f, 2.5f, 0f), new Vector3(3f, 1.5f, 3f), mFaroBlanco, false);
        Pieza("Torre_2", PrimitiveType.Cylinder, faro, new Vector3(0f, 5.5f, 0f), new Vector3(2.6f, 1.5f, 2.6f), mFaroRojo, false);
        Pieza("Torre_3", PrimitiveType.Cylinder, faro, new Vector3(0f, 8.5f, 0f), new Vector3(2.2f, 1.5f, 2.2f), mFaroBlanco, false);
        Pieza("Galeria", PrimitiveType.Cylinder, faro, new Vector3(0f, 10.2f, 0f), new Vector3(3.2f, 0.2f, 3.2f), mPiedra, false);
        Pieza("Lampara", PrimitiveType.Sphere, faro, new Vector3(0f, 11.2f, 0f), Vector3.one * 1.6f, mLuzFaro, false);
        Pieza("Techo", PrimitiveType.Cylinder, faro, new Vector3(0f, 12.15f, 0f), new Vector3(2f, 0.15f, 2f), mFaroRojo, false);
        Pieza("Punta", PrimitiveType.Sphere, faro, new Vector3(0f, 12.5f, 0f), Vector3.one * 0.5f, mOro, false);

        // Luz cálida en la lámpara
        GameObject luzGO = new GameObject("LuzLampara");
        luzGO.transform.SetParent(faro, false);
        luzGO.transform.localPosition = new Vector3(0f, 11.2f, 0f);
        Light luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.9f, 0.5f);
        luz.intensity = 3f;
        luz.range = 18f;

        // Un solo collider simple para que el jugador no atraviese el faro
        BoxCollider caja = faro.gameObject.AddComponent<BoxCollider>();
        caja.center = new Vector3(0f, 6.5f, 0f);
        caja.size = new Vector3(4f, 13f, 4f);
    }

    // ------------------------------------------------------------ COLECCIONABLES

    private static void ColocarColeccionables(GameObject prefabNormal, GameObject prefabEspecial)
    {
        Transform raiz = Vacio("Coleccionables", null);
        Transform normales = Vacio("Normales", raiz);
        Transform especiales = Vacio("Especiales", raiz);

        // Coleccionable NORMAL (+1): 11 instancias
        Vector3[] posNormales =
        {
            new Vector3(-6f, 1f, -5f), new Vector3(6f, 1f, -5f),
            new Vector3(-6f, 1f, 5f),  new Vector3(6f, 1f, 5f),
            new Vector3(0f, 2f, 13f),  new Vector3(0f, 4f, 19f),       // sobre la rampa
            new Vector3(-6f, 5f, 28f), new Vector3(6f, 5f, 28f),
            new Vector3(-6f, 5f, 38f), new Vector3(6f, 5f, 38f),
            new Vector3(-3.5f, 5f, 51f)                                // en la isla (hay que saltar)
        };
        for (int i = 0; i < posNormales.Length; i++)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefabNormal, normales);
            g.name = "Normal_" + (i + 1).ToString("00");
            g.transform.position = posNormales[i];
        }

        // Coleccionable ESPECIAL (+3): 4 instancias
        Vector3[] posEspeciales =
        {
            new Vector3(-8f, 1f, -8f),     // esquina de la plataforma baja
            new Vector3(0f, 5f, 30f),      // centro de la plataforma alta
            new Vector3(8.3f, 5f, 40f),    // junto al borde abierto (riesgoso)
            new Vector3(3.5f, 5f, 51f)     // en la isla del faro
        };
        for (int i = 0; i < posEspeciales.Length; i++)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefabEspecial, especiales);
            g.name = "Especial_" + (i + 1).ToString("00");
            g.transform.position = posEspeciales[i];
        }
    }

    // ------------------------------------------------------------ JUGADOR, CÁMARA, LUZ

    private static GameObject CrearJugador()
    {
        GameObject jug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        jug.name = "Jugador";
        jug.tag = "Player";
        jug.transform.position = new Vector3(0f, 0.55f, -6f);
        jug.GetComponent<MeshRenderer>().sharedMaterial = mNox;

        Rigidbody rb = jug.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Movimiento mov = jug.AddComponent<Movimiento>();
        mov.sonidoSalto = CargarAudio("sfx_jump");
        mov.sonidoCaida = CargarAudio("sfx_hurt");

        Recolector rec = jug.AddComponent<Recolector>();
        rec.sonidoNormal = CargarAudio("sfx_pickup");
        rec.sonidoEspecial = CargarAudio("sfx_checkpoint");
        rec.sonidoMeta = CargarAudio("sfx_door");

        AudioSource fuente = jug.GetComponent<AudioSource>();
        if (fuente == null) fuente = jug.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;

        return jug;
    }

    private static void CrearCamara(Transform jugador)
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColorNiebla;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;

        // Misma idea de la guía: cámara arriba y atrás, inclinada 45 grados
        camGO.transform.position = jugador.position + new Vector3(0f, 9f, -9f);
        camGO.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

        CamaraController control = camGO.AddComponent<CamaraController>();
        control.jugador = jugador;
    }

    private static void CrearLuzDireccional()
    {
        GameObject go = new GameObject("Directional Light");
        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = new Color(0.78f, 0.86f, 1f);       // luz de luna fría
        luz.intensity = 1.2f;
        luz.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        RenderSettings.sun = luz;
    }

    private static void CrearMusica()
    {
        AudioClip clip = CargarAudio("bgm_level");
        GameObject go = new GameObject("Musica");
        AudioSource fuente = go.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.loop = true;
        fuente.volume = 0.35f;
        fuente.spatialBlend = 0f;
        fuente.playOnAwake = true;
    }

    // ------------------------------------------------------------ INTERFAZ (HUD)

    private static void CrearHUD(Recolector recolector)
    {
        GameObject canvasGO = new GameObject("Canvas_HUD");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler escalador = canvasGO.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        recolector.textoPuntaje = CrearTexto(canvasGO.transform, fuente, "Texto_Puntaje", "Puntaje: 0 / 12", 52,
            TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(900f, 90f));

        recolector.textoMensaje = CrearTexto(canvasGO.transform, fuente, "Texto_Mensaje", "", 46,
            TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1500f, 220f));

        CrearTexto(canvasGO.transform, fuente, "Texto_Titulo", "EL FARO DE BRUMA 3D", 34,
            TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(900f, 60f));

        CrearTexto(canvasGO.transform, fuente, "Texto_Controles",
            "WASD / Flechas: mover   |   Espacio: saltar   |   Shift: correr", 30,
            TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(1500f, 60f));
    }

    private static Text CrearTexto(Transform padre, Font fuente, string nombre, string contenido, int tamano,
                                   TextAnchor alineacion, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        Text t = go.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = tamano;
        t.alignment = alineacion;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        Outline borde = go.AddComponent<Outline>();
        borde.effectColor = Color.black;
        borde.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    // ------------------------------------------------------------ UTILIDADES

    private static Transform Vacio(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre);
        if (padre != null) go.transform.SetParent(padre, false);
        return go.transform;
    }

    private static GameObject Pieza(string nombre, PrimitiveType tipo, Transform padre, Vector3 pos,
                                    Vector3 escala, Material material, bool conCollider = true, Vector3? rotacion = null)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        if (padre != null) go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        if (rotacion.HasValue) go.transform.localEulerAngles = rotacion.Value;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;

        if (!conCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return go;
    }

    private static AudioClip CargarAudio(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + nombre + ".wav");
    }

    private static void AsegurarTag(string tag)
    {
        foreach (string t in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (t == tag) return;
        }
        UnityEditorInternal.InternalEditorUtility.AddTag(tag);
    }
}
#endif
