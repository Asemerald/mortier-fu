using UnityEngine;

public class MainMenuCameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform[] cameraPositions;
    [SerializeField] private float transitionDuration = 2f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private int currentPositionIndex = 0;
    private float transitionTimer = 0f;
    private bool isTransitioning = false;

    private void Start()
    {
        if (cameraPositions.Length > 0)
        {
            mainCamera.transform.position = cameraPositions[0].position;
            mainCamera.transform.rotation = cameraPositions[0].rotation;
        }
    }

    private void Update()
    {
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            t = transitionCurve.Evaluate(t);

            Transform targetPosition = cameraPositions[currentPositionIndex];
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition.position, t);
            mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetPosition.rotation, t);

            if (t >= 1f)
            {
                isTransitioning = false;
            }
        }
    }

    [ContextMenu("Move To Next Position")]
    public void MoveToNextPosition()
    {
        if (cameraPositions.Length == 0) return;

        currentPositionIndex = (currentPositionIndex + 1) % cameraPositions.Length;
        transitionTimer = 0f;
        isTransitioning = true;
    }
}
