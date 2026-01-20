using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private float _speed = 12f;

    public Vector3 TransposePoint(Vector3 localPoint, float time)
    {
        var angle = time * _speed;
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        
        return transform.position + rotation * localPoint;
    }

    void Update()
    {
        transform.Rotate(0,1 * Time.deltaTime * _speed,0);
    }
}
