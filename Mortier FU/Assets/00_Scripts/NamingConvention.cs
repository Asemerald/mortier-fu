using UnityEngine;

public class NamingConvention : MonoBehaviour
{
    private Vector2 _mousePos;
    [SerializeField] private Vector2 mousePos;
    public Vector2 MousePos { get { return _mousePos; } set { _mousePos = value; } }
    protected Vector2 mousePosProtected;
    private const float k_gravity = 9.81f;
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void MouseTest()
    {
        if (mousePosProtected.x != mousePos.x) 
            return;

        if (mousePosProtected.x != mousePos.x)
        {
            Debug.Log(mousePosProtected.x);
        }
    }
}
