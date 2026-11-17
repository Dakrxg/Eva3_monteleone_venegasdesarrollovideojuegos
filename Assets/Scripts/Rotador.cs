using UnityEngine;

/// <summary>
/// Criterio D. Gira el objeto continuamente. Cada Prefab tiene su propia velocidad de giro
/// (grados por segundo en X, Y, Z), así el normal y el especial se distinguen.
/// </summary>
public class Rotador : MonoBehaviour
{
    public Vector3 velocidadGiro = new Vector3(30f, 60f, 90f);

    private void Update()
    {
        // Time.deltaTime hace que gire igual en cualquier computador
        transform.Rotate(velocidadGiro * Time.deltaTime);
    }
}
