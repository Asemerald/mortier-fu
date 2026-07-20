using UnityEngine;

public class TEMP_IceVariantSystem : MonoBehaviour
{
    [SerializeField] private GameObject iceVariant;
    [SerializeField][Range(0,1)] private float _iceProbability;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable()
    {
        if (iceVariant == null)
            return;
        iceVariant.SetActive(false);
        if (Random.value <= _iceProbability)
        {
            iceVariant.SetActive(true);
        }
    }

    private void OnDisable()
    {
        iceVariant.SetActive(false);
    }
}
