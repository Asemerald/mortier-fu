using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MortierFu
{
    public class PlatformSpawner : MonoBehaviour
    {
        [SerializeField] private List<GameObject> platform;
        [SerializeField] private Transform targetPoint;
        [SerializeField] private float2 spawnTimeDelay;

        [SerializeField] private float platformSpeed;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            StartCoroutine(SpawnPlatform());
        }

        IEnumerator SpawnPlatform()
        {
            GameObject newPlatform = Instantiate(platform[Random.Range(0, platform.Count)], transform.position,
                transform.rotation, gameObject.transform);
            newPlatform.GetComponent<Movable>()._target = targetPoint;
            newPlatform.GetComponent<Movable>()._speed = platformSpeed;
            yield return new WaitForSeconds(Random.Range(spawnTimeDelay.x, spawnTimeDelay.y));
            StartCoroutine(SpawnPlatform());
        }
    }
}