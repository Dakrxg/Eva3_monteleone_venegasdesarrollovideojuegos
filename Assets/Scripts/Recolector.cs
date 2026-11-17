using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Criterio E. Va en el objeto Jugador.
///  - Detecta el contacto con los dos tipos de coleccionable (por TAG).
///  - Los desactiva, suma el puntaje según su valor y lo imprime en consola.
///  - Al llegar a la meta (12 puntos) reinicia la partida completa recargando la escena.
/// </summary>
public class Recolector : MonoBehaviour
{
    [Header("Reglas")]
    public int puntosNormal = 1;       // tag "Coleccionable"
    public int puntosEspecial = 3;     // tag "Especial"
    public int metaPuntos = 12;
    public float esperaReinicio = 2.5f;

    [Header("Interfaz")]
    public Text textoPuntaje;
    public Text textoMensaje;

    [Header("Audio")]
    public AudioClip sonidoNormal;
    public AudioClip sonidoEspecial;
    public AudioClip sonidoMeta;

    private int puntaje;
    private bool reiniciando;
    private float ocultarMensajeEn;
    private AudioSource fuenteAudio;

    private void Awake()
    {
        fuenteAudio = GetComponent<AudioSource>();
    }

    private void Start()
    {
        ActualizarPuntaje();
        MostrarMensaje("Nox: reúne " + metaPuntos + " puntos de luz para encender el faro", 5f);
        Debug.Log("[INICIO] Partida iniciada. Meta: " + metaPuntos + " puntos.");
    }

    private void Update()
    {
        // Borra el mensaje cuando se cumple su tiempo
        if (!reiniciando && textoMensaje != null && Time.time > ocultarMensajeEn)
            textoMensaje.text = "";
    }

    // Se ejecuta cuando la esfera entra en un collider marcado como Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (reiniciando) return;

        if (other.CompareTag("Coleccionable"))
        {
            Recoger(other.gameObject, "Fragmento de luz", puntosNormal, sonidoNormal);
        }
        else if (other.CompareTag("Especial"))
        {
            Recoger(other.gameObject, "Cristal de Bruma", puntosEspecial, sonidoEspecial);
        }
    }

    private void Recoger(GameObject objeto, string nombre, int valor, AudioClip sonido)
    {
        objeto.SetActive(false);               // desaparece de la escena
        puntaje += valor;

        string signo = valor >= 0 ? "+" : "";
        Debug.Log("[RECOLECTADO] " + nombre + " (objeto: " + objeto.name + ", tag: " + objeto.tag + ") "
                  + signo + valor + " punto(s) | Puntaje acumulado: " + puntaje + "/" + metaPuntos);

        if (sonido != null) fuenteAudio.PlayOneShot(sonido);
        ActualizarPuntaje();
        MostrarMensaje(nombre + "  " + signo + valor, 1.5f);

        if (puntaje >= metaPuntos)
        {
            StartCoroutine(ReiniciarPartida());
        }
    }

    private IEnumerator ReiniciarPartida()
    {
        reiniciando = true;
        Debug.Log("[META] ¡Meta alcanzada con " + puntaje + " puntos! Reiniciando la partida completa...");

        if (sonidoMeta != null) fuenteAudio.PlayOneShot(sonidoMeta);
        if (textoMensaje != null) textoMensaje.text = "¡El faro vuelve a brillar!\nReiniciando partida...";

        yield return new WaitForSeconds(esperaReinicio);

        // Recargar la escena deja TODO como al inicio: jugador, coleccionables y puntaje
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ActualizarPuntaje()
    {
        if (textoPuntaje != null)
            textoPuntaje.text = "Puntaje: " + puntaje + " / " + metaPuntos;
    }

    public void MostrarMensaje(string mensaje, float segundos)
    {
        if (textoMensaje == null) return;
        textoMensaje.text = mensaje;
        ocultarMensajeEn = Time.time + segundos;
    }
}
