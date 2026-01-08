using TMPro;
using UnityEngine;

public class DebugCanvas : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;

    void Start()
    {
        versionText.text = Application.version; 
    }
}
