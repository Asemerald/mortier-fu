using UnityEngine;

public class FaceCamera : MonoBehaviour {
    private Camera _cam;
    
    private void Start() {
        _cam = Camera.main;
    }
    
    private void LateUpdate() {
        if (_cam == null) return;
        
        transform.rotation = Quaternion.LookRotation(_cam.transform.forward, Vector3.up);
    }
}