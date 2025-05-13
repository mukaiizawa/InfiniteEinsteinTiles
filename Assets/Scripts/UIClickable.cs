using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

    AudioManager _audioManager;
    AssetManager _assetManager;

    void Start()
    {
        var manager = GameObject.Find("/Manager");
        if (manager != null)
        {
            _audioManager = manager.GetComponent<AudioManager>();
            _assetManager = manager.GetComponent<AssetManager>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_audioManager != null && _assetManager != null)
            _audioManager.PlaySE(_assetManager.SEOK);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_audioManager != null && _assetManager != null)
            _audioManager.PlaySE(_assetManager.SEOnHoverUI);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }

}
