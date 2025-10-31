using System;
using MortierFu;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    public bool isBombshellBreakable =true;
    public bool isStrikeBreakable =true;
    public void DestroyObject(int index)
    {
        if (isStrikeBreakable & index==1 || isBombshellBreakable & index ==0)
        {
            Destroy(gameObject);
        }
        
    }
}
