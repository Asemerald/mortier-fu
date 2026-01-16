using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MortierFu;
using Unity.Cinemachine;
using UnityEngine;

public class TrailerCamera : MonoBehaviour
{
    
    private Bombshell bombshell;

    [TextArea] public string utilisation;
    
    [SerializeField] private Transform playerTracked;
    
    [Space] [SerializeField] private CinemachineCamera animationCamera, baseCamera;

    private bool animationStarted;

    void Update()
    {
        if (playerTracked == null) return;
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            ReferenceTarget();
        }
        
        if (bombshell == null)
        {
            FindBombshell();
        }
    }

    private void FindBombshell()
    {
        var bomb = FindAnyObjectByType<Bombshell>();

        if (bomb != null)
        {
            bombshell = bomb;
            StartCoroutine(Sequence());
            
            animationStarted = true;
        }
    }

    private IEnumerator Sequence()
    {
        animationCamera.Target.TrackingTarget = bombshell.transform;
        yield return new WaitForSeconds(3f);
        baseCamera.Priority.Value = 10;
        yield return new WaitForSeconds(2f);
        animationCamera.Target.TrackingTarget = playerTracked;
        baseCamera.Priority.Value = -1;
        bombshell = null;
        
        animationStarted = false;
    }

    private void ReferenceTarget()
    {
        animationCamera.Target.TrackingTarget = playerTracked;
        baseCamera.Target.TrackingTarget = playerTracked;
    }
}
