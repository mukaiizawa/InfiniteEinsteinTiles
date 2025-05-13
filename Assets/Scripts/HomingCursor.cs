using UnityEngine;
using UnityEngine.InputSystem;

public class HomingCursor : MonoBehaviour
{

    void Update()
    {
        Vector3 wpos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        wpos.z = 0f;
        this.transform.position = wpos;
    }

}
