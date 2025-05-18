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

    /*
     * Texture
     */
    Sprite[] _puzzleFrames;
    public Texture2D BrushTexture;
    public Texture2D PipetteTexture;
    public Texture TileTexture;
    public Texture TileOutlineTexture;
    public Material DefaultMaterial;
    public Material HighlightMaterial;
    public Material DissolveMaterial;

    /*
     * Audio
     * unity default asset does not support array :(
     */
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
    public AudioClip SEOK;
    public AudioClip SECancel;
    public AudioClip SEOnHoverUI;
    public AudioClip SETileRotate;
    public AudioClip SETilePut;
    public AudioClip SETileCannotPut;
    public AudioClip SETileGrab;
    public AudioClip SETileRemove;
    public AudioClip SETileDissolve;
    public AudioClip SEPuzzleComplete;

    public AudioClip[] GetPlaylist(LoadingManager.Scene scene)
    {
        switch (scene)
        {
            case LoadingManager.Scene.Title:
                return new AudioClip[] { BGMTitle };
            case LoadingManager.Scene.Menu:
                return new AudioClip[] { BGMMenu };
            case LoadingManager.Scene.PuzzleMenu:
                return new AudioClip[] { BGMPuzzleMenu };
            case LoadingManager.Scene.Tiling:
                return new AudioClip[] {
                    BGMTiling0,
                    BGMTiling1,
                    BGMTiling2,
                    BGMTiling3,
                    BGMTiling4,
                    BGMTiling5,
                    BGMTiling6,
                    BGMTiling7,
                    BGMTiling8,
                    BGMTiling9,
                    BGMTiling10,
                    BGMTiling11,
                    BGMTiling12,
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
        return Path.Combine(Application.streamingAssetsPath, "Puzzles", "Frames", $"frame{level}{frameSuffix}.png");
    }

    string FrameInfoURL(int level)
    {
        return Path.Combine(Application.streamingAssetsPath, "Puzzles", "Frames", $"frame-info{level}.json");
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
