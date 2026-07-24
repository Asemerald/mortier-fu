using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;


public class TEMP_Map2_Event : MonoBehaviour
{
    [SerializeField] private GameObject extRing;
    [SerializeField] private GameObject extParticules;
    [SerializeField] private GameObject intParticules;
    [SerializeField] private GameObject intRing;
    [SerializeField] private Transform targetPos;
    [SerializeField] private float timeBeforeSink;
    [SerializeField] private float2 eventTimeRange;
    [SerializeField] private float sinkSpeed;
    [SerializeField] private Material _newMaterial;
    private bool cansink = false;
    private GameObject targetObject;
    void Start()
    {
        StartCoroutine(Sink(extRing, extParticules));
    }

    private IEnumerator Sink(GameObject target,GameObject particules)
    {
        float time =  Random.Range(eventTimeRange.x, eventTimeRange.y);
        yield return new WaitForSeconds(time);
        particules.SetActive(true);
        target.GetComponent<Renderer>().material = _newMaterial;
        yield return new WaitForSeconds(timeBeforeSink);
        
        targetObject = target;
        cansink = true;
        
        if(targetObject == intRing) yield break;
        StartCoroutine(Sink(intRing, intParticules));
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (!cansink) return;
        targetObject.transform.position = Vector3.Lerp(targetObject.transform.position,targetPos.position,Time.deltaTime*sinkSpeed);
        
    }
}
