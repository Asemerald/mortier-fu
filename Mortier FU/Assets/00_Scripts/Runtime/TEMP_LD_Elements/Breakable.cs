using System;
using MortierFu;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private bool isBombshellBreakable =true;
    [SerializeField] private bool isStrikeBreakable =true;
    public bool canprotect = false;
    [SerializeField] private int life = 1;
    [SerializeField] private Material mat;
    public void DestroyObject(int index)
    {
        if (isStrikeBreakable & index==1 || isBombshellBreakable & index ==0)
        {
            life--;
            if (life <= 0)
            {
                Destroy(gameObject);
                return;
            }
            gameObject.GetComponent<MeshRenderer>().material = mat;
            
        }
    }
}
