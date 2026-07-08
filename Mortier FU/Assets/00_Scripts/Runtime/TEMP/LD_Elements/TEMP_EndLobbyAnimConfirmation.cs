using MortierFu;
using UnityEngine;

public class TEMP_EndLobbyAnimConfirmation : MonoBehaviour
{

    public void AnimationHasEnded()
    {
        LobbyStartTarget parent = gameObject.transform.parent.parent.GetComponent<LobbyStartTarget>();
        if (parent == null)
            return;
        parent.ConfirmAnimationEnd();
    }
}
