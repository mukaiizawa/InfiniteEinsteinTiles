using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System;

using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;

using static MathHex;
using static Tags;

[DefaultExecutionOrder(100)]
public class TilingSceneManager : MonoBehaviour
{

    enum Action
    {
        Put,
        Remove,
        Paint,
    }

    enum State
    {
        None,
        Menu,
        Setting,
        ConfirmRestart,
        ConfirmExit,
        Grabbing,
        Selecting,
        Selected,
        Blueprint,
        Paint,
        Pipette,
        Solved,
    }

    /*
     * GameObject
     */
    public GameObject OriginTile;
    public GameObject ActiveTiles;
    public GameObject PlacedTiles;
    public GameObject PrefabTile;
    public GameObject BackGround;
    public GameObject PuzzleFrame;    // enable only Puzzle mode.

    /*
     * UI
     */
    bool _isCursorInUI;
    public bool UICursorEnter;
    public bool UICursorExit;
    public GameObject Canvas;
    public TextMeshProUGUI TextTileCount;
    public TextMeshProUGUI TextMode;
    public GameObject UsageNone;
    public GameObject UsageGrabbing;
    public GameObject UsageSelected;
    public GameObject Hint;
    public GameObject MenuPanel;
    public GameObject ConfirmRestartPanel;
    public GameObject ConfirmExitPanel;
    public GameObject SettingPanel;
    public GameObject SolvedPanel;
    public GameObject ColorPalettePanel;
    public Button CameraButton;
    public Toggle RulerButton;
    public Button MenuOpenButton;
    public Button MenuCloseButton;
    public Button SettingOpenButton;
    public Button SettingCloseButton;
    public Button RestartButton;
    public Button RestartConfirmOKButton;
    public Button RestartConfirmCancelButton;
    public Button ExitWithoutSaveButton;
    public Button ExitConfirmOKButton;
    public Button ExitConfirmCancelButton;
    public Button SaveAndExitButton;
    public Button ContinueToMenuButton;    // solved.

    /*
     * Color Palette
     */
    Color _currentColorPaletteColor;
    Image _currentColorPaletteImage;
    Image[] _colorPaletteColorImages;
    public Button ColorPaletteOpenButton;
    public Button PipetteButton;
    public Toggle[] ColorPaletteColorButtons;
    public Image ColorPickerDisplay;
    public Slider[] ColorPickerSliders;    // R, G, B
    public TMP_InputField[] ColorPickerInputs;    // R, G, B

    /*
     * States
     */
    State _state;
    object _lock;

    /*
     * Managers
     */
    AudioManager _audioManager;
    AssetManager _assetManager;
    LoadingManager _loadingManager;
    NotificationManager _notificationManager;
    PersistentManager _persistentManager;
    SettingManager _settingManager;

    /*
     * Mouse & Keybord.
     */
    bool _isKeyModify1 = false;
    bool _isKeyModify2 = false;
    bool _isDrugging = false;
    int _mouseWheelRotationInputCount = 0;
    int _mouseWheelRotationThreshold = 2;
    float _clickStartTime = 0f;
    float _dragDistanceThreshold = 8f;
    float _dragTimeThreshold = 0.4f;
    Vector2 _mousePos;
    Vector2 _mouseScreenPos;
    Vector2 _clickedPos;
    Vector2 _clickedScreenPos;
    Vector2 _selectStartPos;
    Vector2 _selectEndPos;
    Rect _selectedArea;

    /*
     * Camera 
     */
    Camera _camera;    // Camera.main is slow due to internal use of Tag.
    float _cameraSpeed = 16f;
    float _cameraZoomSpeed = 400f;
    float _cameraMinZoom = 5f;
    float _cameraMaxZoom = 30f;
    Vector2 _cameraMoveDelta = Vector2.zero;
    Vector2 _cameraZoomDelta = Vector2.zero;

    /*
     * board
     */
    Board _answerBoard;
    ConcurrentStack<Tuple<Action, TileMemory[], Color>> _histories = new ConcurrentStack<Tuple<Action, TileMemory[], Color>>();
    ConcurrentStack<Tuple<Action, TileMemory[], Color>> _undoHistories = new ConcurrentStack<Tuple<Action, TileMemory[], Color>>();
    ConcurrentSet<PartialHex> _partialHexTable = new ConcurrentSet<PartialHex>();

    /*
     * tile API.
     */

    GameObject CursorObject()
    {
        return XGameObject.AtWorldPoint(_mousePos);
    }

    GameObject ClickedObject()
    {
        return XGameObject.AtWorldPoint(_clickedPos);
    }
    GameObject[] CollectSelectedTiles()
    {
        return GameObject.FindGameObjectsWithTag(Tags.SelectedTile);
    }

    GameObject[] MakeTiles(TileMemory[] memories)
    {
        return memories.Select(memory => {
            GameObject tile = Instantiate(PrefabTile);
            tile.tag = Tags.Tile;
            tile.GetComponent<Tile>().InitAsset(_assetManager).ImportMemory(memory);
            return tile;
        }).ToArray();
    }

    GameObject[] CopyTiles(GameObject[] fromTiles)
    {
        return MakeTiles(fromTiles.Select(x => x.GetComponent<Tile>().ExportMemory()).ToArray());
    }

    GameObject[] BlueprintTiles(GameObject[] tiles)
    {
        foreach (var tile in tiles)
            tile.GetComponent<Tile>().ToBlueprint();
        return tiles;
    }

    void PutTiles(GameObject[] tiles)
    {
        _audioManager.PlaySE(_assetManager.SETilePut);
        foreach (GameObject tile in tiles)
        {
            tile.GetComponent<SpriteRenderer>().sortingOrder = 2;
            tile.transform.position = NearestObliqueWorldPoint(tile.transform.position);
            tile.transform.parent = PlacedTiles.transform;
        }
    }

    void RemoveTiles(GameObject[] tiles, bool isPlacedTiles)
    {
        _audioManager.PlaySE(_assetManager.SETileRemove);
        if (isPlacedTiles)
            UpdateBoardWithHistory(Action.Remove, tiles);
        foreach (GameObject tile in tiles)
            Destroy(tile);
    }

    void GrabTiles(GameObject[] tiles, bool isPlacedTiles)
    {
        _audioManager.PlaySE(_assetManager.SETileGrab);
        if (isPlacedTiles)
            UpdateBoardWithHistory(Action.Remove, tiles);
        var minX = tiles.Min(o => o.transform.position.x);
        var maxX = tiles.Max(o => o.transform.position.x);
        var minY = tiles.Min(o => o.transform.position.y);
        var maxY = tiles.Max(o => o.transform.position.y);
        ActiveTiles.transform.position = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
        foreach (GameObject tile in tiles)
        {
            tile.GetComponent<SpriteRenderer>().sortingOrder = 10;
            tile.transform.parent = ActiveTiles.transform;
        }
    }

    void RotateActiveTiles(int angle)
    {
        switch (_state)
        {
            case State.Grabbing:
            case State.Blueprint:
                {
                    _audioManager.PlaySE(_assetManager.SETileRotate);
                    var tiles = ActiveTiles.Children();
                    ActiveTiles.transform.position = _mousePos;
                    ActiveTiles.transform.Rotate(0, 0, angle);
                    ActiveTiles.transform.DetachChildren();
                    ActiveTiles.transform.Rotate(0, 0, -angle);
                    foreach (GameObject tile in tiles)
                        tile.transform.parent = ActiveTiles.transform;
                }
                break;
            default:
                break;
        }
    }

    void FlipActiveTiles()
    {
        _audioManager.PlaySE(_assetManager.SETileRotate);
        var tiles = ActiveTiles.Children();
        ActiveTiles.transform.position = _mousePos;
        Vector3 scale = ActiveTiles.transform.localScale;
        scale.x *= -1;
        ActiveTiles.transform.localScale = scale;
        ActiveTiles.transform.DetachChildren();
        scale.x *= -1;
        ActiveTiles.transform.localScale = scale;
        foreach (GameObject tile in tiles)
            tile.transform.parent = ActiveTiles.transform;
    }

    void SelectTiles(GameObject[] tiles)
    {
        foreach (GameObject tile in tiles)
        {
            tile.tag = Tags.SelectedTile;
            tile.GetComponent<Tile>().Highlight();
        }
    }

    GameObject[] UnselectTiles(GameObject[] tiles)
    {
        foreach (GameObject tile in tiles)
        {
            tile.tag = Tags.Tile;
            tile.GetComponent<Tile>().TurnOffHighlight();
        }
        return tiles;
    }

    /*
     * history api
     */

    void ReplayHistory(bool isUndo, Tuple<Action, TileMemory[], Color> record)
    {
        var (action, memories, color) = record;
        // paint.
        if (action == Action.Paint)
        {
            TileMemory memory = memories[0];    // support one tile.
            if (!isUndo) color = memory.Color;
            foreach (GameObject tile in PlacedTiles.Children())
            {
                var componentTile = tile.GetComponent<Tile>(); 
                if (componentTile.ExportMemory().Position == memory.Position)
                {
                    componentTile.ChangeColor(color);
                    break;
                }
            }
            return;
        }
        // put or remove.
        if (isUndo) action = (action == Action.Put)? Action.Remove: Action.Put;
        UpdateBoard(action, memories);
        if (action == Action.Put) PutTiles(MakeTiles(memories));
        else
        {
            HashSet<Vector2Int> targetPositions = memories.Select(x => x.Position).ToHashSet();
            foreach (GameObject tile in PlacedTiles.Children().Where(x => targetPositions.Contains(x.GetComponent<Tile>().ExportMemory().Position)))
                Destroy(tile);
        }
    }

    bool UpdateBoardWithHistory(Action action, GameObject[] tiles)
    {
        Debug.Log("TilingSceneManager#UpdateBoardWithHistory: " + action);
        var memories = tiles.Select(x => x.GetComponent<Tile>().ExportMemory()).ToArray();
        if (!UpdateBoard(action, memories))
        {
            _audioManager.PlaySE(_assetManager.SETileCannotPut);
            return false;
        }
        _histories.Push(Tuple.Create(action, memories, Color.white));
        _undoHistories.Clear();
        return true;
    }

    void UpdateTileCount()
    {
        int n = _partialHexTable.Count() / 8;
        switch (GlobalData.GameMode)
        {
            case GameMode.Creative:
                TextTileCount.text = $"{n}";
                break;
            case GameMode.Puzzle:
                var N = _answerBoard.PartialHexes.Count() / 8;
                _answerBoard.PartialHexes.Count();
                TextTileCount.text = $"{n} / {N}";
                TextTileCount.color = n == N? Colors.OK: n > N? Colors.NG: Color.white;
                break;
            default:
                break;
        }
    }

    bool UpdateBoard(Action action, TileMemory[] memories)
    {
        Debug.Log("TilingSceneManager#UpdateBoard: " + action);
        var partialHexes = memories.SelectMany(x => x.PartialHexes());
        switch (action)
        {
            case Action.Put:
                if (!_partialHexTable.TryAddAll(partialHexes)) return false;    // failed to put.
                break;
            case Action.Remove:
                _partialHexTable.RemoveAll(partialHexes);
                break;
        }
        UpdateTileCount();
        switch (GlobalData.GameMode)
        {
            case GameMode.Puzzle:
                if (_partialHexTable.Count() == _answerBoard.PartialHexes.Count()
                        && _partialHexTable.ContainsAll(_answerBoard.PartialHexes)
                        && (!_histories.IsEmpty || !_undoHistories.IsEmpty))    // Initial state of cleared saved data.
                {
                    _audioManager.PlaySE(_assetManager.SEPuzzleComplete);
                    PuzzleFrame.SetActive(false);
                    var tiles = ActiveTiles.Children();
                    PutTiles(CopyTiles(tiles));
                    RemoveTiles(tiles, false);
                    foreach (var tile in PlacedTiles.Children())
                        tile.AddComponent<RotatingProjectile>();
                    _persistentManager.SaveProgress(GlobalData.Slot, new Progress(Math.Max(GlobalData.Level, _persistentManager.LoadProgress(GlobalData.Slot).CurrentLevel)));
                    _persistentManager.SaveSolution(UpdatedSolution());
                    ChangeState(State.Solved);
                    return false;
                }
                break;
            default:
                break;
        }
        return true;
    }

    void ReloadScene()
    {
        GlobalData.IsRestart = true;
        StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.Tiling, 0.5f));
    }

    Solution UpdatedSolution()
    {
        Debug.Log($"TilingSceneManager#UpdatedSolution: {GlobalData.Solution}");
        GlobalData.Solution.Board = new Board(PlacedTiles.Children().Select(x => x.GetComponent<Tile>().ExportMemory()).ToArray(), _colorPaletteColorImages.Select(x => x.color).ToArray());
        GlobalData.Solution.UpdatedAt = DateTime.UnixTimeNow();
        return GlobalData.Solution;
    }

    void LoadPrevScene(bool withSave)
    {
        System.Action action = LoadingManager.OnLoadNone;
        switch (GlobalData.GameMode)
        {
            case GameMode.Creative:
                if (withSave) action = () => _persistentManager.SaveSolution(UpdatedSolution());
                StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.Menu, minLoadingTime: 1f, action: action));
                break;
            case GameMode.Puzzle:
                if (withSave) _persistentManager.SaveSolution(UpdatedSolution());
                StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.PuzzleMenu));
                break;
        }
    }

    static Vector2 _cursorLB = new Vector2(0, 31);

    void ChangeUsage(State to)
    {
        UsageGrabbing.SetActive(false);
        UsageSelected.SetActive(false);
        UsageNone.SetActive(false);
        switch (to)
        {
            case State.Blueprint:
            case State.Grabbing:
                UsageGrabbing.SetActive(true);
                break;
            case State.Selected:
                UsageSelected.SetActive(true);
                break;
            default:
                UsageNone.SetActive(true);
                break;
        }
    }

    void ChangeState(State to)
    {
#if UNITY_EDITOR
        TextMode.text = to.ToString();
#endif
        ChangeUsage(to);
        switch (to)
        {
            case State.None:
                OriginTile.SetActive(true);
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                ColorPalettePanel.SetActive(false);
                ConfirmRestartPanel.SetActive(false);
                ConfirmExitPanel.SetActive(false);
                MenuPanel.SetActive(false);
                break;
            case State.Menu:
                MenuPanel.SetActive(true);
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);    // Paint to Menu invoke from GUI Button.
                if (_state == State.Setting)
                {
                    SettingPanel.SetActive(false);
                    _persistentManager.SetBGMVolume(_settingManager.BGMSlider.value);
                    _persistentManager.SetSEVolume(_settingManager.SESlider.value);
                }
                break;
            case State.Setting:
                SettingPanel.SetActive(true);
                break;
            case State.ConfirmRestart:
                ConfirmRestartPanel.SetActive(true);
                break;
            case State.ConfirmExit:
                ConfirmExitPanel.SetActive(true);
                break;
            case State.Solved:
                SolvedPanel.SetActive(true);
                break;
            case State.Blueprint:
            case State.Grabbing:
            case State.Selecting:
            case State.Selected:
                break;
            case State.Paint:
                OriginTile.SetActive(false);
                ColorPalettePanel.SetActive(true);
                Cursor.SetCursor(_assetManager.BrushTexture, Vector2.zero, CursorMode.Auto);
                break;
            case State.Pipette:
                Cursor.SetCursor(_assetManager.PipetteTexture, _cursorLB, CursorMode.Auto);
                break;
            default:
                Debug.LogError("Unexpected _state" + to);
                break;
        }
        _state = to;
    }

    /*
     * Event handlers.
     */

    void Awake()
    {
        _lock = new object();
        _assetManager = this.gameObject.GetComponent<AssetManager>();
        _audioManager = this.gameObject.GetComponent<AudioManager>();
        _loadingManager = this.gameObject.GetComponent<LoadingManager>();
        _notificationManager = this.gameObject.GetComponent<NotificationManager>();
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
        _settingManager = this.gameObject.GetComponent<SettingManager>();
    }

    void Start()
    {
        _camera = Camera.main;
        _audioManager.SetPlaylist(_assetManager.GetPlaylist(LoadingManager.Scene.Tiling)).StartBGM();
        OriginTile.GetComponent<Button>().onClick.AddListener(OnOriginTileClick);
        MenuOpenButton.onClick.AddListener(() => ChangeState(State.Menu));
        MenuCloseButton.onClick.AddListener(() => ChangeState(State.None));
        SettingOpenButton.onClick.AddListener(() => ChangeState(State.Setting));
        SettingCloseButton.onClick.AddListener(() => ChangeState(State.Menu));
        CameraButton.onClick.AddListener(() => StartCoroutine(CaptureScreenshotCoroutine()));
        RulerButton.onValueChanged.AddListener(OnRulerToggle);
        ColorPaletteOpenButton.onClick.AddListener(OnColorPaletteOpenButtonClick);
        PipetteButton.onClick.AddListener(() => ChangeState(State.Pipette));
        RestartButton.onClick.AddListener(() => ChangeState(State.ConfirmRestart));
        RestartConfirmOKButton.onClick.AddListener(ReloadScene);
        RestartConfirmCancelButton.onClick.AddListener(() => ChangeState(State.None));
        ExitWithoutSaveButton.onClick.AddListener(() => ChangeState(State.ConfirmExit));
        SaveAndExitButton.onClick.AddListener(() => LoadPrevScene(true));
        ExitConfirmOKButton.onClick.AddListener(() => LoadPrevScene(false));
        ExitConfirmCancelButton.onClick.AddListener(() => ChangeState(State.None));
        ContinueToMenuButton.onClick.AddListener(() => LoadPrevScene(false));
        // color picker.
        {
            foreach (var slider in ColorPickerSliders)
                slider.onValueChanged.AddListener(OnColorPaletteSliderChange);
            foreach (var input in ColorPickerInputs)
                input.onEndEdit.AddListener(OnColorPaletteInputChange);
            var SavedPaletteColors = GlobalData.Solution.Board.ColorPalette;
            _colorPaletteColorImages = new Image[ColorPaletteColorButtons.Length];
            for (int i = 0; i < ColorPaletteColorButtons.Length; i++)
            {
                int j = i;    // for closure
                Toggle toggle = ColorPaletteColorButtons[i];
                _colorPaletteColorImages[i] = toggle.GetComponentInChildren<Image>();
                _colorPaletteColorImages[i].color = SavedPaletteColors[i];
                toggle.onValueChanged.AddListener((isOn) => OnColorPaletteColorToggle(j, isOn));
            }
            ColorPaletteColorButtons[0].isOn = false;
            ColorPaletteColorButtons[0].isOn = true;     // onValueChanged.Invoke is not working :(
        }
        switch (GlobalData.GameMode)
        {
            case GameMode.Creative:
                RestartButton.gameObject.SetActive(false);    // The restart button only available in puzzle mode.
                break;
            case GameMode.Puzzle:
                {
                    switch (GlobalData.Level)
                    {
                        case 1:
                        case 4:
                        case 8:
                            Hint.SetActive(true);
                            var tmp = Hint.GetComponentInChildren<TextMeshProUGUI>();
                            if (tmp != null)
                                tmp.text = LocalizationSettings.StringDatabase.GetTableEntry("default", $"hint_level_{GlobalData.Level}").Entry.Value;
                            var hintCloseButton = Hint.GetComponentInChildren<Button>();
                            if (hintCloseButton != null)
                                hintCloseButton.onClick.AddListener(() => Hint.SetActive(false));
                            break;
                        default:
                            Hint.SetActive(false);
                            break;
                    }
                    _answerBoard = _assetManager.LoadBoard(GlobalData.Level);
                    StartCoroutine(_assetManager.LoadPuzzleFrameAsync(GlobalData.Level, Color.gray, (sprite) => {
                        PuzzleFrame.SetActive(true);
                        var renderer = PuzzleFrame.GetComponent<SpriteRenderer>();
                        renderer.sprite = sprite;
                    }));
                }
                break;
            default:
                Debug.LogWarning($"TilingSceneManager#Start: Unexpected GameMode {GlobalData.GameMode}");
                // GlobalData.GameMode = GameMode.Creative;
                GlobalData.GameMode = GameMode.Puzzle;
                LoadPrevScene(false);
                return;
        }
        if (GlobalData.IsRestart) GlobalData.IsRestart = false;
        else if (GlobalData.Solution.Board.PlacedTiles != null)
        {
            var memories = GlobalData.Solution.Board.PlacedTiles;
            UpdateBoard(Action.Put, memories);
            PutTiles(MakeTiles(memories));
        }
        UpdateTileCount();
#if UNITY_EDITOR
#else
        var origin = GameObject.Find("/Origin");
        if (origin != null) origin.SetActive(false);
        TextMode.gameObject.SetActive(false);
#endif
        ChangeState(State.None);
    }

    void Update()
    {
        var dt = Time.deltaTime;
        if (_isKeyModify1) dt *= 2;
        _mouseScreenPos = Mouse.current.position.ReadValue();
        _mousePos = _camera.ScreenToWorldPoint(_mouseScreenPos);
        if (UICursorEnter)
        {
            _isCursorInUI = true;
            UICursorEnter = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        if (UICursorExit)
        {
            _isCursorInUI = false;
            UICursorExit = false;
            switch (_state)
            {
                case State.Paint:
                    Cursor.SetCursor(_assetManager.BrushTexture, Vector2.zero, CursorMode.Auto);
                    break;
                case State.Pipette:
                    Cursor.SetCursor(_assetManager.PipetteTexture, Vector2.zero, CursorMode.Auto);
                    break;
                default:
                    break;
            }
        }
        switch (_state)
        {
            case State.None:
            case State.Grabbing:
            case State.Selecting:
            case State.Selected:
            case State.Blueprint:
            case State.Paint:
            case State.Pipette:
                // camera
                if (_isDrugging && !_isCursorInUI)
                {
                    if (Time.time - _clickStartTime > 0.1f || Vector2.Distance(_clickedScreenPos, _mouseScreenPos) > _dragTimeThreshold)
                        _camera.transform.Translate(_clickedPos - _mousePos);
                }
                else
                {
                    _camera.transform.Translate(new Vector2(_cameraMoveDelta.x * _cameraSpeed * dt, _cameraMoveDelta.y * _cameraSpeed * dt));
                }
                // move back ground
                Vector3 screenCenterWorldPoint = _camera.ScreenToWorldPoint(new Vector2(Screen.width / 2, Screen.height / 2));
                BackGround.transform.position = LeftBottomObliqueWorldPoint(screenCenterWorldPoint);
                // zoom
                _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - _cameraZoomDelta.y * _cameraZoomSpeed * dt, _cameraMinZoom, _cameraMaxZoom);
                break;
            default:
                break;
        }
        switch (_state)
        {
            case State.Selecting:
                _selectEndPos = _mousePos;
                break;
            default:
                break;
        }
    }

    void OnGUI()
    {
        switch (_state)
        {
            case State.Selecting:
                var p = _camera.WorldToScreenPoint(_selectStartPos);
                var q = _camera.WorldToScreenPoint(_selectEndPos);
                _selectedArea = new Rect(p.x, Screen.height - p.y, q.x - p.x, -(q.y - p.y));
                GUI.Box(_selectedArea, "");
                break;
            default:
                break;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _cameraMoveDelta = context.ReadValue<Vector2>();
    }

    public void OnWheel(InputAction.CallbackContext context)
    {
        var val = context.ReadValue<Vector2>();
        if (_isKeyModify2) _cameraZoomDelta = val;
        else
        {
            if (context.performed) 
            {
                if ((_mouseWheelRotationInputCount = _mouseWheelRotationInputCount + 1) % _mouseWheelRotationThreshold == 0)
                    RotateActiveTiles(val.y > 0f? 60: -60);
            }
            _cameraZoomDelta = Vector2.zero;
        }
    }

    public void OnKeyModify1(InputAction.CallbackContext context)
    {
        if (context.performed) _isKeyModify1 = true;
        else if (context.canceled) _isKeyModify1 = false;
    }

    public void OnKeyModify2(InputAction.CallbackContext context)
    {
        if (context.performed) _isKeyModify2 = true;
        else if (context.canceled) _isKeyModify2 = false;
    }

    public void OnRotateRight(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            RotateActiveTiles(-60);
        }
    }

    public void OnRotateLeft(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            RotateActiveTiles(60);
        }
    }

    public void OnFlip(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            switch (_state)
            {
                case State.Grabbing:
                case State.Blueprint:
                    Debug.Log("TilingSceneManager#OnFlip");
                    FlipActiveTiles();
                    break;
                default:
                    break;
            }
        }
    }

    public void OnCopy(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            switch (_state)
            {
                case State.None:
                    var o = CursorObject();
                    if (Tags.match(o, Tags.Tile))
                    {
                        GrabTiles(CopyTiles(new GameObject[] { o }), false);
                        ChangeState(State.Grabbing);
                    }
                    break;
                case State.Selected:
                    Debug.Log("TilingSceneManager#OnCopy");
                    GrabTiles(BlueprintTiles(CopyTiles(UnselectTiles(CollectSelectedTiles()))), false);
                    ChangeState(State.Blueprint);
                    break;
                default:
                    break;
            }
        }
    }

    public void OnCut(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            switch (_state)
            {
                case State.Selected:
                    Debug.Log("TilingSceneManager#OnCut");
                    GrabTiles(BlueprintTiles(UnselectTiles(CollectSelectedTiles())), true);
                    ChangeState(State.Blueprint);
                    break;
                default:
                    break;
            }
        }
    }

    /*
     * delete event.
     */

    public void OnDelete(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            switch (_state)
            {
                case State.None:
                    var o = CursorObject();
                    if (Tags.match(o, Tags.Tile)) RemoveTiles(new GameObject[] { o }, true);
                    break;
                case State.Blueprint:
                case State.Grabbing:
                    RemoveTiles(ActiveTiles.Children(), false);
                    ChangeState(State.None);
                    break;
                case State.Selected:
                    RemoveTiles(CollectSelectedTiles(), true);
                    ChangeState(State.None);
                    break;
                default:
                    break;
            }
        }
    }

    /*
     * click event.
     */

    public void OnOriginTileClick()
    {
        switch (_state)
        {
            case State.None:
                if (_isKeyModify2) return;
                GrabTiles(MakeTiles(new TileMemory[] { new TileMemory(WorldPointToNearestOblique(_mousePos), _currentColorPaletteColor) }), false);
                ChangeState(State.Grabbing);
                break;
            default:
                break;
        }
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (context.performed)
            {
                Debug.Log("button down:"+Time.time);
                switch (_state)
                {
                    case State.None:
                        if (_isKeyModify2)
                        {
                            _selectStartPos = _mousePos;
                            ChangeState(State.Selecting);
                            return;
                        }
                        break;
                    case State.Selected:
                        if (_isKeyModify2)
                        {
                            UnselectTiles(CollectSelectedTiles());
                            _selectStartPos = _mousePos;
                            ChangeState(State.Selecting);
                            return;
                        }
                        break;
                    default:
                        break;
                }
                // start drug.
                _isDrugging = true;
                _clickStartTime = Time.time;
                _clickedPos = _mousePos;
                _clickedScreenPos = _mouseScreenPos;
                return;
            }
            if (context.canceled)
            {
                _isDrugging = false;
                bool isDrug = (Time.time - _clickStartTime > _dragTimeThreshold) || (Vector2.Distance(_clickedScreenPos, _mouseScreenPos) > _dragDistanceThreshold);
                switch (_state)
                {
                    case State.Selecting:
                        break;
                    default:
                        if (isDrug) return;
                        break;
                }
                switch (_state)
                {
                    case State.None:
                        {
                            var o = ClickedObject();
                            if (Tags.match(o, Tags.Tile))
                            {
                                GrabTiles(new GameObject[]{ o }, true);
                                ChangeState(State.Grabbing);
                                return;
                            }
                        }
                        break;
                    case State.Selected:
                        UnselectTiles(CollectSelectedTiles());
                        ChangeState(State.None);
                        break;
                    case State.Blueprint:
                        {
                            // put as much as possible.
                            var tiles = ActiveTiles.Children().Where(x => !_partialHexTable.ContainsAny(x.GetComponent<Tile>().ExportMemory().PartialHexes())).ToArray();
                            if (!UpdateBoardWithHistory(Action.Put, tiles))
                            {
                                Debug.LogWarning("TilingSceneManager#onClick#State.Blueprint: something wrong.");
                                return;
                            }
                            PutTiles(CopyTiles(tiles));
                        }
                        break;
                    case State.Grabbing:
                        {
                            var tiles = ActiveTiles.Children();
                            if (!UpdateBoardWithHistory(Action.Put, tiles)) return;    // can't put.
                            PutTiles(tiles);
                            ChangeState(State.None);
                        }
                        break;
                    case State.Paint:
                        {
                            var o = ClickedObject();
                            if (Tags.match(o, Tags.Tile))
                            {
                                var componentTile = o.GetComponent<Tile>();
                                var memory = componentTile.ExportMemory();
                                componentTile.ChangeColor(_currentColorPaletteColor);
                                _histories.Push(Tuple.Create(Action.Paint, new TileMemory[] { componentTile.ExportMemory() }, memory.Color));    // copy memory.
                                _undoHistories.Clear();
                            }
                        }
                        break;
                    case State.Pipette:
                        {
                            var o = ClickedObject();
                            if (Tags.match(o, Tags.Tile))
                            {
                                var color = o.GetComponent<Tile>().ExportMemory().Color;
                                ChangeColorPaletteColor(color);
                                ChangeState(State.Paint);
                            }
                        }
                        break;
                    case State.Selecting:
                        {
                            var minX = Mathf.Min(_selectStartPos.x, _selectEndPos.x);
                            var maxX = Mathf.Max(_selectStartPos.x, _selectEndPos.x);
                            var minY = Mathf.Min(_selectStartPos.y, _selectEndPos.y);
                            var maxY = Mathf.Max(_selectStartPos.y, _selectEndPos.y);
                            var selectedTiles = PlacedTiles.Children().Where(x => {
                                PolygonCollider2D collider = x.GetComponent<PolygonCollider2D>();
                                    return collider.points.Select(p => x.transform.TransformPoint(p)).Any(p => minX <= p.x && p.x <= maxX && minY <= p.y && p.y <= maxY);
                                }).ToArray();
                            if (selectedTiles.Length > 0) 
                            {
                                SelectTiles(selectedTiles);
                                ChangeState(State.Selected);
                                return;
                            }
                            ChangeState(State.None);
                        }
                        break;
                    default:
                        break;
                }
                return;
            }
        }
    }

    /*
     * undo/redo.
     */

    public void OnUndo(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            switch (_state)
            {
                case State.None:
                    if (_histories.TryPop(out Tuple<Action, TileMemory[], Color> historyRecord))
                    {
                        Debug.Log("TilingSceneManager#OnUndo");
                        ReplayHistory(true, historyRecord);
                        _undoHistories.Push(historyRecord);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    public void OnRedo(InputAction.CallbackContext context)
    {
        lock (_lock)
        {
            if (!context.performed) return;
            switch (_state)
            {
                case State.None:
                    if (_undoHistories.TryPop(out Tuple<Action, TileMemory[], Color> historyRecord))
                    {
                        Debug.Log("TilingSceneManager#OnRedo");
                        ReplayHistory(false, historyRecord);
                        _histories.Push(historyRecord);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    void OnRulerToggle(bool isOn)
    {
        BackGround.SetActive(!isOn);
    }

    void OnColorPaletteColorToggle(int i, bool isOn)
    {
        if (isOn)
        {
            _currentColorPaletteImage = _colorPaletteColorImages[i];
            ChangeColorPaletteColor(_currentColorPaletteImage.color);
        }
    }

    void OnColorPaletteOpenButtonClick()
    {
        switch (_state)
        {
            case State.None:
                ChangeState(State.Paint);
                break;
            case State.Paint:
                ChangeState(State.None);
                break;
            default:
                break;
        }
    }

    void ChangeColorPaletteColor(Color color)
    {
        _currentColorPaletteColor = color;
        ColorPickerSliders[0].value = color.r;    // [0, 1]
        ColorPickerSliders[1].value = color.g;
        ColorPickerSliders[2].value = color.b;
        ColorPickerInputs[0].text = Mathf.FloorToInt(color.r * 255).ToString();
        ColorPickerInputs[1].text = Mathf.FloorToInt(color.g * 255).ToString();
        ColorPickerInputs[2].text = Mathf.FloorToInt(color.b * 255).ToString();
        OriginTile.GetComponent<Image>().color = color;
        ColorPickerDisplay.color = color;
        _currentColorPaletteImage.color = color;
    }

    float ParseFloatOrDefault(string val, float defaultValue)
    {
        if (float.TryParse(val, out float result)) return result;
        return defaultValue;
    }

    void OnColorPaletteInputChange(string _)
    {
        var rbg = ColorPickerInputs.Select(x => ParseFloatOrDefault(x.text, 0f)).ToArray();
        ChangeColorPaletteColor(new Color(rbg[0] / 255f, rbg[1] / 255f, rbg[2] / 255f));
    }

    void OnColorPaletteSliderChange(float _)
    {
        var rgb = ColorPickerSliders.Select(x => x.value).ToArray();
        ChangeColorPaletteColor(new Color(rgb[0], rgb[1], rgb[2]));
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        switch (_state)
        {
            case State.None:
                ChangeState(State.Menu);
                break;
            case State.Menu:
                ChangeState(State.None);
                break;
            case State.Setting:
                ChangeState(State.Menu);
                break;
            case State.Selected:
                UnselectTiles(CollectSelectedTiles());
                ChangeState(State.None);
                break;
            case State.Blueprint:
            case State.Grabbing:
                RemoveTiles(ActiveTiles.Children(), false);
                ChangeState(State.None);
                break;
            case State.Paint:
                ChangeState(State.None);
                break;
            case State.Pipette:
                ChangeState(State.Paint);
                break;
            default:
                break;
        }
    }

    IEnumerator CaptureScreenshotCoroutine()
    {
        Canvas.SetActive(false);
        yield return new WaitForEndOfFrame();
        string path = Path.Combine(_persistentManager.ScreenshotDir, $"{System.DateTime.Now:yyyy-MM-ddTHH-mm-ss}.png");
        ScreenCapture.CaptureScreenshot(path);
        _notificationManager.Notify($"Screenshot saved: {path}");
        yield return null;
        Canvas.SetActive(true);
    }

    public void OnDebug(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Debug.Log("TilingSceneManager#OnDebug");
        Board board = new Board();
        board.GrabingTiles = ActiveTiles.Children().Select(x => x.GetComponent<Tile>().ExportMemory()).ToArray();
        board.PlacedTiles = PlacedTiles.Children().Select(x => x.GetComponent<Tile>().ExportMemory()).ToArray();
        board.PartialHexes = _partialHexTable.ToArray();
        board.ColorPalette = _colorPaletteColorImages.Select(x => x.color).ToArray();
        _persistentManager.Save(System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".log", board, true);
        Debug.Log("TilingSceneManager#OnDebug#histories");
        foreach (var o in _histories)
        {
            foreach (var p in o.Item2)
                Debug.Log(o.Item1 + "#" + p);
        }
        Debug.Log("TilingSceneManager#OnDebug#_undoHistories");
        foreach (var o in _undoHistories)
        {
            foreach (var p in o.Item2)
                Debug.Log(o.Item1 + "#" + p);
        }
    }

}
