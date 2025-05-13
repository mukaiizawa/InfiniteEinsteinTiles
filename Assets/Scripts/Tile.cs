using UnityEngine;

using static MathHex;
using static Colors;

public class Tile : MonoBehaviour
{

    TileMemory _memory;
    SpriteRenderer _render;
    Material _defaultMaterial;
    Material _highlightMaterial;

    void Awake()
    {
        _render = this.gameObject.GetComponent<SpriteRenderer>();
        _memory = new TileMemory();
    }

    public Tile InitAsset(AssetManager asset)
    {
        _defaultMaterial = asset.DefaultMaterial;
        _highlightMaterial = new Material(asset.HighlightMaterial);
        _highlightMaterial.mainTexture = asset.TileTexture;
        _highlightMaterial.SetTexture(Shader.PropertyToID("_OutlineTex"), asset.TileOutlineTexture);
        _highlightMaterial.SetColor(Shader.PropertyToID("_OutlineColor"), Colors.SelectedTile);
        return this;
    }

    public void Highlight()
    {
        _highlightMaterial.SetColor(Shader.PropertyToID("_TileColor"), _render.color);
        _render.color = Colors.Tile;
        _render.material = _highlightMaterial;
    }

    public void TurnOffHighlight()
    {
        _render.color = _memory.Color;
        _render.material = _defaultMaterial;
    }

    TileMemory readTransformFromMemory()
    {
        this.transform.position = ObliqueToWorldPoint(_memory.Position);
        this.transform.Rotate(0, 0, _memory.Rotation * 60);
        if (_memory.IsFlip)
        {
            Vector3 scale = this.transform.localScale;
            scale.x *= -1;
            this.transform.localScale = scale;
        }
        return _memory;
    }

    TileMemory writeTransformToMemory()
    {
        _memory.Position = WorldPointToNearestOblique(transform.position);
        _memory.Rotation = ((360 + (int)transform.eulerAngles.z + 1) / 60) % 6;
        _memory.IsFlip = transform.lossyScale.x < 0;
        return _memory;
    }

    public void ChangeColor(Color color)
    {
        _render.color = color;
        _memory.Color = color;
    }

    public void ToBlueprint()
    {
        _render.color = ChangeAlpha(ChangeSaturation(_memory.Color, 1.5f), 0.5f);
    }

    public void RestoreColor()
    {
        ChangeColor(_memory.Color);
    }

    public PartialHex[] PartialHexes()
    {
        return writeTransformToMemory().PartialHexes();
    }

    public TileMemory ExportMemory()
    {
        return new TileMemory().CopyFrom(writeTransformToMemory());
    }

    public void ImportMemory(TileMemory memory)
    {
        _memory.CopyFrom(memory);
        readTransformFromMemory();
        RestoreColor();
    }

}
