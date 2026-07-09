using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MortarShadow : MonoBehaviour
{
    [SerializeField] private DecalProjector decalProjector;
    [SerializeField] private float maxSize;
    [SerializeField] private float fallDuration;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / fallDuration; 
        
        Vector2 currentSize = Vector2.Lerp(Vector2.zero, Vector2.one * maxSize, t);
        decalProjector.size = new Vector3(currentSize.x, currentSize.y, decalProjector.size.z);
    }

    public void UpdatePositionOverTerrain(Vector3 shellXZPosition)
    {
        Ray ray = new Ray(shellXZPosition + Vector3.up * 50f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            transform.position = hit.point + Vector3.up * 0.5f; 
        }
    }
}