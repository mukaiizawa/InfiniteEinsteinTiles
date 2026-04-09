using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

public class AssetManager : MonoBehaviour
{

    class FrameInfo
    {
        public Vector2 Center;
    }

    public AssetData Data;

    /*
     * Passthrough properties — callers use these unchanged.
     * To add/remove assets, edit AssetData instead.
     */

    // Textures
    public Texture2D BrushTexture       => Data.BrushTexture;
    public Texture2D PipetteTexture     => Data.PipetteTexture;
    public Texture   TileTexture        => Data.TileTexture;
    public Texture   TileOutlineTexture => Data.TileOutlineTexture;

    // Materials
    public Material DefaultMaterial     => Data.DefaultMaterial;
    public Material HighlightMaterial   => Data.HighlightMaterial;
    public Material DissolveMaterial    => Data.DissolveMaterial;

    // BGM
    public AudioClip BGMTitle           => Data.BGMTitle;
    public AudioClip BGMMenu            => Data.BGMMenu;
    public AudioClip BGMPuzzleMenu      => Data.BGMPuzzleMenu;

    // SE
    public AudioClip SEOK               => Data.SEOK;
    public AudioClip SECancel           => Data.SECancel;
    public AudioClip SEOnHoverUI        => Data.SEOnHoverUI;
    public AudioClip SETileRotate       => Data.SETileRotate;
    public AudioClip SETilePut          => Data.SETilePut;
    public AudioClip SETileCannotPut    => Data.SETileCannotPut;
    public AudioClip SETileGrab         => Data.SETileGrab;
    public AudioClip SETileRemove       => Data.SETileRemove;
    public AudioClip SETileDissolve     => Data.SETileDissolve;
    public AudioClip SEPuzzleTimeOver   => Data.SEPuzzleTimeOver;
    public AudioClip SEPuzzleComplete   => Data.SEPuzzleComplete;

    /*
     * Sprite cache
     */
    Sprite[] _puzzleFrames;

    public AudioClip[] GetPlaylist(LoadingManager.Scene scene)
    {
        switch (scene)
        {
            case LoadingManager.Scene.Title:
                return new AudioClip[] { Data.BGMTitle };
            case LoadingManager.Scene.Menu:
                return new AudioClip[] { Data.BGMMenu };
            case LoadingManager.Scene.PuzzleMenu:
                return new AudioClip[] { Data.BGMPuzzleMenu };
            case LoadingManager.Scene.Tiling:
                return new AudioClip[] {
                    Data.BGMTiling0,
                    Data.BGMTiling1,
                    Data.BGMTiling2,
                    Data.BGMTiling3,
                    Data.BGMTiling4,
                    Data.BGMTiling5,
                    Data.BGMTiling6,
                    Data.BGMTiling7,
                    Data.BGMTiling8,
                    Data.BGMTiling9,
                    Data.BGMTiling10,
                    Data.BGMTiling11,
                    Data.BGMTiling12,
                };
            default:
                return Array.Empty<AudioClip>();
        }
    }

    void Awake()
    {
        _puzzleFrames = new Sprite[GlobalData.TotalLevel];
    }

    public Board LoadBoard(int level)
    {
        string file = Path.Combine(Application.streamingAssetsPath, "Puzzles", "Boards", $"board{level}.json");
        return JsonUtility.FromJson<Board>(File.ReadAllText(file));
    }

    string FrameURL(int level, Color bgColor)
    {
        var frameSuffix = "";
        if (bgColor != Color.white) frameSuffix = "-gray";
#if UNITY_STANDALONE_WIN
            var proto = "file:///";
#else
            var proto = "file://";
#endif
        var path = proto + Path.Combine(Application.streamingAssetsPath, "Puzzles", "Frames", $"frame{level}{frameSuffix}.png");
        Debug.Log($"AssetManager#FrameURL: load {path}");
        return path;
    }

    string FrameInfoURL(int level)
    {
#if UNITY_STANDALONE_WIN
            var proto = "file:///";
#else
            var proto = "file://";
#endif
        var path = proto + Path.Combine(Application.streamingAssetsPath, "Puzzles", "Frames", $"frame-info{level}.json");
        Debug.Log($"AssetManager#FrameURL: load {path}");
        return path;
    }

    public IEnumerator LoadPuzzleFrameAsync(int level, Color bgColor, Action<Sprite> onLoadTexture)
    {
        Debug.Log($"LoadPuzzleFrameAsync#level {level}");
        Sprite sprite;
        if ((sprite = _puzzleFrames[level - 1]) == null)
        {
            using (var req = UnityWebRequest.Get(FrameInfoURL(level)))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning($"AssetManager#LoadPuzzleFrameAsync: FrameInfo {req.error}");
                else
                {
                    FrameInfo info = JsonUtility.FromJson<FrameInfo>(DownloadHandlerBuffer.GetContent(req));
                    using (var req2 = UnityWebRequestTexture.GetTexture(FrameURL(level, bgColor)))
                    {
                        yield return req2.SendWebRequest();
                        if (req2.result != UnityWebRequest.Result.Success)
                            Debug.LogWarning($"AssetManager#LoadPuzzleFrameAsync: Frame {req2.error}");
                        else
                        {
                            var texture = DownloadHandlerTexture.GetContent(req2);
                            var (w, h) = (texture.width, texture.height);
                            sprite = Sprite.Create(texture, new Rect(0, 0, w, h), info.Center / new Vector2(w, h));
                            _puzzleFrames[level - 1] = sprite;
                        }
                    }
                }
            }
        }
        onLoadTexture(sprite);
    }

}
