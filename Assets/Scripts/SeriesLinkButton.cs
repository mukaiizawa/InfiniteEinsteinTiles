using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(Button))]
public class SeriesLinkButton : MonoBehaviour
{

    public enum Series
    {
        None,
        InfiniteEinsteinTiles,
        InfiniteEinsteinTiles2,
        InfiniteEinsteinTiles3,
        InfiniteEinsteinTilesTogether,
    }

    // Rewrite when this script is copied to another project in the series.
    const Series _self = Series.InfiniteEinsteinTiles;

    public Series Target;

    void Start()
    {
        if (Target == _self)
        {
            this.gameObject.SetActive(false);
            return;
        }
        this.gameObject.GetComponent<Button>().onClick.AddListener(OnClick);
    }

    uint AppId()
    {
        switch (Target)
        {
            case Series.InfiniteEinsteinTiles:
                return 3550470;
            case Series.InfiniteEinsteinTiles2:
                return 3893930;
            case Series.InfiniteEinsteinTiles3:
                return 4553450;
            case Series.InfiniteEinsteinTilesTogether:
                return 4812400;
            default:
                Debug.LogWarning($"SeriesLinkButton#AppId: unexpected target {Target}");
                return 0;
        }
    }

    void OnClick()
    {
        var appId = AppId();
        if (appId == 0) return;
        if (SteamManager._instance != null && SteamManager._instance.OpenStorePage(appId)) return;
        Application.OpenURL($"https://store.steampowered.com/app/{appId}/");
    }

}
