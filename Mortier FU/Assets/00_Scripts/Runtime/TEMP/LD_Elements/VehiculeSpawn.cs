using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehiculeSpawn : MonoBehaviour
{
    [SerializeField] private Transform startingPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private GameObject vehicule;
    [SerializeField] private GameObject redLight;
    [SerializeField] private float vehiculeSpeed;
    [SerializeField] private Vector2 vehiculteRate;
    [SerializeField] private Vector2 redLightTime;
    private bool canMove = true;

    [SerializeField] private Material redLightMat;
    [SerializeField] private Material greenLightMat;
    
    private List<GameObject> vehiculList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vehiculList = new List<GameObject>();
        StartCoroutine(ActivateCarMovement());
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVehiculePosition();
    }

    private void UpdateVehiculePosition()
    {
        for (int i = 0; i < vehiculList.Count; i++)
        {
            vehiculList[i].transform.position = Vector3.MoveTowards(vehiculList[i].transform.position,
                endPoint.position, vehiculeSpeed / 1000);
            if (vehiculList[i].transform.position == endPoint.position)
            {
                Destroy(vehiculList[i]);
                vehiculList.RemoveAt(i);
            }
        }
    }

    private IEnumerator CreateCar()
    {
        vehiculList.Add(Instantiate(vehicule, startingPoint.position,
            Quaternion.LookRotation((endPoint.position - startingPoint.position).normalized)));
        yield return new WaitForSeconds(Random.Range(vehiculteRate.x, vehiculteRate.y));
        StartCoroutine(CreateCar());
    }

    private IEnumerator ActivateCarMovement()
    {
        if (canMove)
        {
            redLight.GetComponent<MeshRenderer>().material = greenLightMat;
            Debug.Log("Startcar");
            StartCoroutine(CreateCar());
        }
        yield return new WaitForSeconds(Random.Range(redLightTime.x,redLightTime.y));
        redLight.GetComponent<MeshRenderer>().material = redLightMat;
        Debug.Log("Stopcar");
        StopAllCoroutines();
        canMove = !canMove;
        StartCoroutine(ActivateCarMovement());
    }
}
        