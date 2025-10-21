using UnityEngine;

namespace MortierFu
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Properties:
        public static T Instance { get; private set; }
        #endregion

        #region MonoBehaviour Callback Method(s):
        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this as T;
        }
        #endregion
    }
}