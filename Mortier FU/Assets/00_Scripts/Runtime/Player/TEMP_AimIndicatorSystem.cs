using MortierFu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TEMP_AimIndicatorSystem : MonoBehaviour
{
    public Image tempAimIndicator;
    private Vector2 direction;
    private InputAction AimAction;
    private Transform pivotPoint;
    private Camera mainCamera; 
    private RectTransform aimIndicatorRect; 
    private RectTransform canvasRect;
    public bool isTargeting = false;
    void Awake()
    {
        tempAimIndicator.gameObject.SetActive(false);
    }

    public void Initialize()
    {
        pivotPoint = gameObject.transform;
        gameObject.GetComponent<PlayerCharacter>().FindInputAction("Aim", out AimAction);
        
        mainCamera = Camera.main; 
        
        aimIndicatorRect = tempAimIndicator.GetComponent<RectTransform>();
        canvasRect = tempAimIndicator.transform.parent.GetComponentInParent<RectTransform>(); 
        
        AimAction.performed += UpdatAimIndicator;
        AimAction.canceled += UpdatAimIndicator;
    }

    private void UpdatAimIndicator(InputAction.CallbackContext ctx)
    {
        if (isTargeting)
        {
            tempAimIndicator.gameObject.SetActive(false);
            return;
        }
        
        Vector2 aimInput = AimAction.ReadValue<Vector2>();
        
        if (aimInput.sqrMagnitude < 0.1f) 
        {
            tempAimIndicator.gameObject.SetActive(false);
            return;
        }
        
        tempAimIndicator.gameObject.SetActive(true);

        Vector3 worldDirection = new Vector3(aimInput.x, 0f, aimInput.y);
        
        float indicatorDistance = 1.5f;
        Vector3 targetPositionWorld = pivotPoint.position + (worldDirection.normalized * indicatorDistance);
        
        if (mainCamera != null && canvasRect != null)
        {
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPositionWorld);
            Vector2 localPosition;
            
            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                mainCamera,
                out localPosition
            );

            if (success)
            {
                aimIndicatorRect.anchoredPosition = localPosition;
            }
        }
        else
        {
            tempAimIndicator.transform.position = targetPositionWorld;
        }
        
        Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
        Quaternion fixRotation = Quaternion.Euler(90f, 0f, 0f); 
        
        tempAimIndicator.transform.rotation = targetRotation * fixRotation; 
    }
    
    
}
