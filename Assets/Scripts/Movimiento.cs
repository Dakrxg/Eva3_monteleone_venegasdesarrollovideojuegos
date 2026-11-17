using UnityEngine;

/// <summary>
/// Criterio B y C (parte de caída). Va en el objeto Jugador (esfera con Rigidbody).
///  - Movimiento por FÍSICAS (AddForce) en los ejes X y Z, con variable "velocidad".
///  - Mecánica adicional 1: SALTO solo si está apoyado en una superficie (sin saltos infinitos).
///  - Mecánica adicional 2: SPRINT mientras se mantiene Shift.
///  - Si cae bajo "limiteCaida" vuelve al punto inicial SIN velocidad residual.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Movimiento : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 14f;            // fuerza que se aplica al mover
    public float velocidadMaxima = 7f;       // tope de rapidez horizontal
    public float frenado = 4f;               // freno suave cuando no se presiona nada

    [Header("Sprint (mantener Shift)")]
    public float multiplicadorSprint = 1.7f;

    [Header("Salto (Espacio)")]
    public float fuerzaSalto = 6.5f;
    public float toleranciaSuelo = 0.15f;    // margen para detectar el suelo

    [Header("Caída al vacío")]
    public float limiteCaida = -10f;         // si Y baja de este valor, reaparece

    [Header("Audio")]
    public AudioClip sonidoSalto;
    public AudioClip sonidoCaida;

    private Rigidbody rb;
    private AudioSource fuenteAudio;
    private Recolector recolector;
    private Vector3 puntoInicio;
    private float radio;
    private bool saltoPedido;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fuenteAudio = GetComponent<AudioSource>();
        recolector = GetComponent<Recolector>();
        puntoInicio = transform.position;             // guardamos dónde empieza el jugador

        SphereCollider sc = GetComponent<SphereCollider>();
        radio = (sc != null ? sc.radius : 0.5f) * transform.lossyScale.x;
    }

    private void Update()
    {
        // Las teclas se leen en Update; la física se aplica en FixedUpdate
        if (Entrada.SaltoPresionado() && EstaEnSuelo())
        {
            saltoPedido = true;
        }

        if (transform.position.y < limiteCaida)
        {
            Reaparecer();
        }
    }

    // FixedUpdate sincroniza con el motor de físicas
    private void FixedUpdate()
    {
        float h = Entrada.Horizontal();
        float v = Entrada.Vertical();

        Vector3 direccion = new Vector3(h, 0f, v);
        if (direccion.sqrMagnitude > 1f) direccion.Normalize();

        float factor = Entrada.SprintPresionado() ? multiplicadorSprint : 1f;

        // Movimiento basado en físicas: se aplica una fuerza, no se mueve el transform
        rb.AddForce(direccion * velocidad * factor);

        // Limitamos solo la rapidez horizontal (la vertical la controla la gravedad)
        Vector3 velHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float maxima = velocidadMaxima * factor;
        if (velHorizontal.magnitude > maxima)
        {
            velHorizontal = velHorizontal.normalized * maxima;
            rb.linearVelocity = new Vector3(velHorizontal.x, rb.linearVelocity.y, velHorizontal.z);
        }

        // Freno suave si el jugador suelta las teclas
        if (direccion.sqrMagnitude < 0.01f && EstaEnSuelo())
        {
            rb.AddForce(-velHorizontal * frenado);
        }

        // Salto: impulso hacia arriba una sola vez
        if (saltoPedido)
        {
            saltoPedido = false;
            if (EstaEnSuelo())
            {
                rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
                Sonar(sonidoSalto);
            }
        }
    }

    // Lanza una esfera pequeña hacia abajo; ignora los triggers (coleccionables)
    private bool EstaEnSuelo()
    {
        float distancia = radio * 0.1f + toleranciaSuelo;
        return Physics.SphereCast(transform.position, radio * 0.9f, Vector3.down,
                                  out RaycastHit golpe, distancia, ~0,
                                  QueryTriggerInteraction.Ignore);
    }

    private void Reaparecer()
    {
        // Quitamos TODA la velocidad para que no conserve el impulso de la caída
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = puntoInicio;
        transform.position = puntoInicio;
        saltoPedido = false;

        Debug.Log("[CAIDA] El jugador cayó al vacío y reapareció en el punto inicial.");
        Sonar(sonidoCaida);
        if (recolector != null) recolector.MostrarMensaje("Nox cayó al vacío... ¡vuelve a intentarlo!", 2f);
    }

    private void Sonar(AudioClip clip)
    {
        if (clip != null) fuenteAudio.PlayOneShot(clip);
    }
}
