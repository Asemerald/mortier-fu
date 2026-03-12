using System;
using MortierFu;
using Unity.Cinemachine;
using UnityEngine;

public class TEMP_CamOnboarding : MonoBehaviour
{
    [SerializeField] private CinemachineCamera cam;
    [SerializeField] private LevelReporter levelReporter;
    private void OnTriggerEnter(Collider other)
    {
        cam.gameObject.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        cam.gameObject.SetActive(false);
    }
}
