using UnityEngine;
using UnityEngine.EventSystems;

public class UICusorDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public TilingSceneManager eventManager;

    public void OnPointerEnter(PointerEventData eventData)
    {
        eventManager.UICursorEnter = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        eventManager.UICursorExit = true;
    }

}
