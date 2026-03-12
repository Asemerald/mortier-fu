using System;
using MortierFu;
using Unity.Cinemachine;
using UnityEngine;

public class TEMP_CamOnboarding : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;

    
    private void OnTriggerEnter(Collider other)
    {
        cam.gameObject.SetActive(true);
        cam.Lens.FieldOfView = 20;
    }

    private void OnTriggerExit(Collider other)
    {
        cam.gameObject.SetActive(false);
    }
}
