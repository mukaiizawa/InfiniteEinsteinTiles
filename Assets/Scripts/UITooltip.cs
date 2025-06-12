using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    public GameObject Tooltip;

    void Start()
    {
        if (Tooltip != null) Tooltip.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Tooltip != null) Tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (Tooltip != null) Tooltip.SetActive(false);
    }

}
