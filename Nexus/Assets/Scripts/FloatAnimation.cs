using UnityEngine;

public class FloatAnimation : MonoBehaviour
{
    [Header("Floating Settings")]
    public float amplitude = 0.15f;      // Altura máxima del movimiento
    public float frequency = 1.5f;       // Velocidad de la animación
    public bool addRotation = false;     // Rotación opcional
    public float rotationSpeed = 45f;    // Grados por segundo (si addRotation = true)

    private float baseY;

    void Start()
    {
        baseY = transform.position.y;
    }

    void Update()
    {
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = pos;

        if (addRotation)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void OnDisable()
    {
        if (Mathf.Abs(baseY) > 0.001f)
        {
            Vector3 pos = transform.position;
            pos.y = baseY;
            transform.position = pos;
        }
    }
}
