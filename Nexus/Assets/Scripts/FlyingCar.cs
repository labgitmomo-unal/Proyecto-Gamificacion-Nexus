using UnityEngine;
public class FlyingCar : MonoBehaviour {
    public Vector3 center = new Vector3(50, 15, -50);
    public float radius = 5f;
    public float speed = 2f;
    public float heightBob = 1f;
    private float angle;
    private float heightOffset;
    void Start() {
        angle = Random.Range(0f, 360f);
        heightOffset = Random.Range(0f, 6.28f);
    }
    void Update() {
        angle += speed * Time.deltaTime;
        float x = center.x + Mathf.Cos(angle) * radius;
        float z = center.z + Mathf.Sin(angle) * radius;
        float y = center.y + Mathf.Sin(angle * 0.7f + heightOffset) * heightBob;
        transform.position = new Vector3(x, y, z);
        transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(angle), 0, Mathf.Cos(angle)));
    }
}
