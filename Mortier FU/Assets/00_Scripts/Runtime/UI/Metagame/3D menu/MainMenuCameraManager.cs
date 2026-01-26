using UnityEngine;

public class MainMenuCameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform[] cameraPositions;
    [SerializeField] private float transitionDuration = 2f;
    
    

    [ContextMenu("Initialize Camera")]
    private void Start()
    {
        if (cameraPositions.Length > 0)
        {
            mainCamera.transform.position = cameraPositions[0].position;
            mainCamera.transform.rotation = cameraPositions[0].rotation;
        }
        
        MoveToPosition(1);
    }
    
    public void MoveToPosition(int index)
    {
        if (index < 0 || index >= cameraPositions.Length)
            return;

        StopAllCoroutines();
        StartCoroutine(CameraTransitionRoutine(cameraPositions[index]));
    }
    
    private System.Collections.IEnumerator CameraTransitionRoutine(Transform targetPosition)
    {
        Vector3 startPos = mainCamera.position;
        Quaternion startRot = mainCamera.rotation;
        Vector3 endPos = targetPosition.position;
        Quaternion endRot = targetPosition.rotation;

        float elapsed = 0f;

        while (elapsed < transitionDuration) 
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            mainCamera.position = Vector3.Lerp(startPos, endPos, t);
            mainCamera.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        mainCamera.position = endPos;
        mainCamera.rotation = endRot;
    }
}
