using UnityEngine;

public class UpDownMotion : MonoBehaviour
{
    public float amplitude = 0.05f;
    public float frequency = 4f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
