using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0,1 * Time.deltaTime * speed,0);
    }
}
