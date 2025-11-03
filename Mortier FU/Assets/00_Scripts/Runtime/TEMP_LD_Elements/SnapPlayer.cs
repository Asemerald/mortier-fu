using System;
using MortierFu;
using UnityEngine;

public class SnapPlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {   Debug.Log("1");
        if (other.TryGetComponent(out PlayerCharacter character) & gameObject.GetComponentInParent<Movable>().isPlatform)
        {
            Debug.Log("snaped");
            character.gameObject.transform.SetParent(gameObject.transform.parent.gameObject.transform);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        Debug.Log("2");
        if (other.TryGetComponent(out PlayerCharacter character) & gameObject.GetComponentInParent<Movable>().isPlatform)
        {
            Debug.Log("Unsnaped");
            character.gameObject.transform.SetParent(null);
        }
    }
}
