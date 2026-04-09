using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UITabNavigable : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public Selectable Next;
    bool _focused;

    public void OnSelect(BaseEventData _) => _focused = true;
    public void OnDeselect(BaseEventData _) => _focused = false;

    void Update()
    {
        if (_focused && Keyboard.current[Key.Tab].wasPressedThisFrame)
            Next.Select();
    }
}
