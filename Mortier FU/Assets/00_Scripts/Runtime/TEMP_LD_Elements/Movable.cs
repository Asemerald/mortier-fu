using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;

public class Movable : MonoBehaviour
{
    public bool isAutomatic = true;
    public Transform target;
    public float speed;
    private Vector3 startingPoint;
    private bool canMove = true;
    public float waitTime = 1.5f;

    private bool hasbeenActivated =false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startingPoint = transform.position;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null & isAutomatic & canMove || hasbeenActivated & target != null & !isAutomatic)
        {
            gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, target.position, speed/1000);
            if (transform.position == target.position)
            {
                (target.position,startingPoint) = (startingPoint,target.position);
                hasbeenActivated = false;
                StartCoroutine(Wait());
            }
        }
        else
        {
            Debug.Log("No target assign");
        }
    }

    public void InteratableMove()
    {
        hasbeenActivated = true;
    }

    private IEnumerator Wait()
    {
        canMove = !canMove;
        yield return new WaitForSeconds(waitTime);
    }
    
}
