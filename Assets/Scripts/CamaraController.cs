using UnityEngine;

/// <summary>
/// Criterio C. Va en Main Camera. Calcula la distancia inicial (offset) entre cámara y jugador
/// y la mantiene siempre igual: la cámara nunca rota, solo se desplaza con suavidad.
/// </summary>
public class CamaraController : MonoBehaviour
{
    public Transform jugador;        // se arrastra el Jugador desde el Inspector
    public float suavizado = 8f;     // mayor valor = cámara más pegada al jugador

    private Vector3 offset;

    private void Start()
    {
        // Diferencia entre la posición de la cámara y la del jugador
        offset = transform.position - jugador.position;
    }

    // LateUpdate corre después de mover todo: es lo mejor para cámaras
    private void LateUpdate()
    {
        if (jugador == null) return;

        Vector3 destino = jugador.position + offset;

        // Si el jugador "salta" lejos (reaparición), la cámara lo sigue al instante
        if ((destino - transform.position).sqrMagnitude > 100f)
            transform.position = destino;
        else
            transform.position = Vector3.Lerp(transform.position, destino, suavizado * Time.deltaTime);
    }
}
