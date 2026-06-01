using UnityEngine;

public class FloatAnimation : MonoBehaviour
{
    [Header("Floating Settings")]
    public float amplitude = 0.15f;      // Altura máxima del movimiento
    public float frequency = 1.5f;       // Velocidad de la animación
    public bool addRotation = false;     // Rotación opcional
    public float rotationSpeed = 45f;    // Grados por segundo (si addRotation = true)

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        // Movimiento sinusoidal arriba y abajo
        float newY = startPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);

        // Rotación opcional sobre el eje Y
        if (addRotation)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
}
