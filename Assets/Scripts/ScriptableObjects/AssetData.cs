using UnityEngine;

[CreateAssetMenu(fileName = "AssetData", menuName = "Game/Asset Data")]
public class AssetData : ScriptableObject
{

    [Header("Textures")]
    public Texture2D BrushTexture;
    public Texture2D PipetteTexture;
    public Texture TileTexture;
    public Texture TileOutlineTexture;

    [Header("Materials")]
    public Material DefaultMaterial;
    public Material HighlightMaterial;
    public Material DissolveMaterial;

    [Header("BGM")]
    public AudioClip BGMTitle;
    public AudioClip BGMMenu;
    public AudioClip BGMPuzzleMenu;
    public AudioClip BGMTiling0;
    public AudioClip BGMTiling1;
    public AudioClip BGMTiling2;
    public AudioClip BGMTiling3;
    public AudioClip BGMTiling4;
    public AudioClip BGMTiling5;
    public AudioClip BGMTiling6;
    public AudioClip BGMTiling7;
    public AudioClip BGMTiling8;
    public AudioClip BGMTiling9;
    public AudioClip BGMTiling10;
    public AudioClip BGMTiling11;
    public AudioClip BGMTiling12;

    [Header("SE")]
    public AudioClip SEOK;
    public AudioClip SECancel;
    public AudioClip SEOnHoverUI;
    public AudioClip SETileRotate;
    public AudioClip SETilePut;
    public AudioClip SETileCannotPut;
    public AudioClip SETileGrab;
    public AudioClip SETileRemove;
    public AudioClip SETileDissolve;
    public AudioClip SEPuzzleTimeOver;
    public AudioClip SEPuzzleComplete;

}
