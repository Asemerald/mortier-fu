using System.Collections.Generic;
using UnityEngine;

public class TEMP_Onboarding : MonoBehaviour
{
    [SerializeField] List<Animator> animator;
    private Vector3[] basePos;
    private bool[] ready;
    private int actualID = -1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ready = new bool[animator.Count];
        basePos = new Vector3[animator.Count];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReadyPlayer(int playerID)
    {
        ready[playerID] = !ready[playerID];
        
        animator[playerID].SetTrigger("ReadyTrigger");
        animator[playerID].SetBool("bIsReady", ready[playerID]);
        if (ready[playerID])
        {
            basePos[playerID] = animator[playerID].gameObject.transform.position;
            animator[playerID].gameObject.transform.position = new Vector3(-23.5f,0, basePos[playerID].z);
        }
        else
        {
            animator[playerID].gameObject.transform.position = basePos[playerID];
        }
    }
}
