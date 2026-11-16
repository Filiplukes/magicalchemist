using UnityEngine;

public class LoadingSpining : MonoBehaviour
{
    public float speed = 200f;

    void Update()
    {
        transform.Rotate(Vector3.back, speed * Time.deltaTime);
    }
}
