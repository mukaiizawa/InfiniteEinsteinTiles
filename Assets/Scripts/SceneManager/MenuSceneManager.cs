using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System;

using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class MenuSceneManager : MonoBehaviour
{

    enum State
    {
        None,
        Creative,
        CreativeRename,
        CreativeDeleteConfirm,
        Credit,
        Language,
        Menu,
        Setting,
    }

    /* Puzzle Mode */
    public Button[] Slots;

    /* Creative Mode */
    int _solutionCount = 0;
    CreativeCard _selectedCreativeCard;
    public GameObject CreativePanel;
    public Button CreativeOpenButton;
    public Button CreativeCloseButton;
    public Button CreativeNewButton;
    public GameObject CreativeEmptyState;
    public GameObject CreativeCards;
    public GameObject PrefabCreativeCard;

    /* Creative Mode (Rename) */
    public GameObject CreativeRenamePanel;
    public TMP_InputField CreativeRenameField;
    public Button CreativeRenameOKButton;
    public Button CreativeRenameCancelButton;

    /* Creative Mode (Delete confirm) */
    public GameObject CreativeDeleteConfirmPanel;
    public Button CreativeDeleteOKButton;
    public Button CreativeDeleteCancelButton;

    /* Manual */
    static string _manualPath = Path.Combine(Application.streamingAssetsPath, "Manual", "An_aperiodic_monotile.pdf");
    public Button ManualButton;

    /* Credit */
    public GameObject CreditPanel;
    public Button CreditOpenButton;
    public Button CreditCloseButton;
    static string[] _creditCreatedBy = new string[] {
        "Shintaro Mukai",
        "Sayuri Mukai",
    };
    static string[] _creditMusicAndSound = new string[] {
        "MaouDamashii: https://maou.audio/",
    };
    static string[] _creditIcon = new string[] {
        "UXWing: https://uxwing.com/",
    };
    static string[] _creditGameEngine = new string[] {
        "Unity: https://unity.com/",
    };
    static string[] _creditSpecialThanksTo = new string[] {
        "Kai Kimura",
        "Kenichi Tokuoka",
        "Miki Yonemura",
        "Mituki Miharu aka Haru",
    };

    /* Languages */
    public GameObject LanguagePanel;
    public Button LanguageOpenButton;
    public Toggle[] Languages;
    string[] _langCodes = new string[] {
        "en",    // English
        "it",    // Italian
        "fr",    // French
        "de",    // German
        "ru",    // Russian
        "pl",    // Polish
        "pt",    // Portuguese
        "es",    // Spanish
        "ja",    // Japanese
        "zh-Hans",    // Chinese Simplified
        "zh-Hant",    // Chinese Traditional
        "ko",    // Korean
    };

    /* Menu */
    public GameObject MenuPanel;
    public GameObject SettingPanel;
    public Button MenuOpenButton;
    public Button MenuCloseButton;
    public Button SettingOpenButton;
    public Button SettingCloseButton;
    public Button QuitButton;

    State _state;
    AssetManager _assetManager;
    AudioManager _audioManager;
    LoadingManager _loadingManager;
    PersistentManager _persistentManager;
    SettingManager _settingManager;

    void ChangeState(State to)
    {
        Debug.Log($"MenuSceneManager#ChangeState: change state from {_state} to {to}");
        switch (to)
        {
            case State.None:
                CreativePanel.SetActive(false);
                MenuPanel.SetActive(false);
                LanguagePanel.SetActive(false);
                CreditPanel.SetActive(false);
                break;
            case State.Creative:
                CreativePanel.SetActive(true);
                CreativeRenamePanel.SetActive(false);
                CreativeDeleteConfirmPanel.SetActive(false);
                break;
            case State.CreativeRename:
                CreativeRenamePanel.SetActive(true);
                break;
            case State.CreativeDeleteConfirm:
                CreativeDeleteConfirmPanel.SetActive(true);
                break;
            case State.Credit:
                CreditPanel.SetActive(true);
                break;
            case State.Language:
                LanguagePanel.SetActive(true);
                break;
            case State.Menu:
                MenuPanel.SetActive(true);
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
            default:
                Debug.LogError("Unexpected _state" + to);
                break;
        }
        _state = to;
    }

    void Awake()
    {
        _audioManager = this.gameObject.GetComponent<AudioManager>();
        _assetManager = this.gameObject.GetComponent<AssetManager>();
        _loadingManager = this.gameObject.GetComponent<LoadingManager>();
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
        _settingManager = this.gameObject.GetComponent<SettingManager>();
    }

    void Start()
    {
        _audioManager.SetPlaylist(_assetManager.GetPlaylist(LoadingManager.Scene.Menu)).StartBGM();
        CreativeOpenButton.onClick.AddListener(() => ChangeState(State.Creative));
        CreativeCloseButton.onClick.AddListener(() => ChangeState(State.None));
        var solutions = _persistentManager.LoadCreativeSolutions();
        _solutionCount = solutions.Count;
        foreach (var o in solutions) MakeCreativeCard(o);
        CreativeEmptyState.SetActive(solutions.Count == 0);
        CreativeNewButton.onClick.AddListener(OnCreativeNewClick);
        CreativeRenameOKButton.onClick.AddListener(OnCreativeRenameOKClick);
        CreativeRenameCancelButton.onClick.AddListener(() => ChangeState(State.Creative));
        CreativeDeleteOKButton.onClick.AddListener(OnCreativeDeleteOKClick);
        CreativeDeleteCancelButton.onClick.AddListener(() => ChangeState(State.Creative));
        for (int i = 0; i < Slots.Length; i++)
        {
            int slotNo = i + 1;
            var slot = Slots[i];
            slot.onClick.AddListener(() => OnClickSlot(slotNo));
            foreach (var component in slot.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (component.gameObject.name == "Progress")
                    component.text = $"{_persistentManager.GetCurrentLevel(slotNo) * 100 / GlobalData.TotalLevel}%";
            }
        }
        LanguageOpenButton.onClick.AddListener(() => ChangeState(State.Language));
        {
            var currLang = _persistentManager.GetLocale();
            for (int i = 0; i < Languages.Length; i++)
            {
                int j = i;    // for closure
                Toggle toggle = Languages[i];
                toggle.onValueChanged.AddListener((isOn) => OnLanguageToggle(j, isOn));
                if (_langCodes[i] == currLang) toggle.isOn = true;
            }
        }
        ManualButton.onClick.AddListener(OnManualButtonClick);
        CreditOpenButton.onClick.AddListener(OnCreditOpenButtonClick);
        CreditCloseButton.onClick.AddListener(() => ChangeState(State.None));
        MenuOpenButton.onClick.AddListener(() => ChangeState(State.Menu));
        MenuCloseButton.onClick.AddListener(() => ChangeState(State.None));
        SettingOpenButton.onClick.AddListener(() => ChangeState(State.Setting));
        SettingCloseButton.onClick.AddListener(() => ChangeState(State.Menu));
        QuitButton.onClick.AddListener(OnPowerOff);
        ChangeState(State.None);
    }

    void OnPowerOff()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        _audioManager.PlaySE(_assetManager.SECancel);
        switch (_state)
        {
            case State.None:
                ChangeState(State.Menu);
                break;
            case State.Menu:
            case State.Credit:
            case State.Language:
            case State.Creative:
                ChangeState(State.None);
                break;
            case State.CreativeRename:
            case State.CreativeDeleteConfirm:
                ChangeState(State.Creative);
                break;
            case State.Setting:
                ChangeState(State.Menu);
                break;
            default:
                break;
        }
    }

    string MarkdownList(string[] vals)
    {
        return vals.Select(x => $"- {x}").Aggregate((x, y) => x + "\n" + y);
    }

    void OnManualButtonClick()
    {
        try {
#if UNITY_STANDALONE_WIN
            var proto = "file:///";
#else
            var proto = "file://";
#endif
            Application.OpenURL(proto + _manualPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"MenuSceneManager#OnManualButtonClick: manual open failed {e.Message}");
        }
    }

    void OnCreditOpenButtonClick()
    {
        var tmp = CreditPanel.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            Debug.LogWarning("MenuSceneManager#OnCreditOpenButtonClick: missing TextMeshProUGUI.");
            return;
        }
        var credit = LocalizationSettings.StringDatabase.GetTableEntry("default", "credit").Entry.Value;
        var creditMessage = LocalizationSettings.StringDatabase.GetTableEntry("default", "credit_content").Entry.Value;
        var createdBy = LocalizationSettings.StringDatabase.GetTableEntry("default", "created_by").Entry.Value;
        var musicAndSound = LocalizationSettings.StringDatabase.GetTableEntry("default", "music_and_sound").Entry.Value;
        var icon = LocalizationSettings.StringDatabase.GetTableEntry("default", "icon").Entry.Value;
        var gameEngine = LocalizationSettings.StringDatabase.GetTableEntry("default", "game_engine").Entry.Value;
        var specialThanksTo = LocalizationSettings.StringDatabase.GetTableEntry("default", "special_thanks_to").Entry.Value;
        using (StringWriter wr = new StringWriter())
        {
            wr.WriteLine($"# {credit}");
            wr.WriteLine(creditMessage);
            wr.WriteLine();
            wr.WriteLine($"# {createdBy}");
            wr.WriteLine(MarkdownList(_creditCreatedBy));
            wr.WriteLine();
            wr.WriteLine($"# {icon}");
            wr.WriteLine(MarkdownList(_creditIcon));
            wr.WriteLine();
            wr.WriteLine($"# {musicAndSound}");
            wr.WriteLine(MarkdownList(_creditMusicAndSound));
            wr.WriteLine();
            wr.WriteLine($"# {gameEngine}");
            wr.WriteLine(MarkdownList(_creditGameEngine));
            wr.WriteLine();
            wr.WriteLine($"# {specialThanksTo}");
            wr.WriteLine(MarkdownList(_creditSpecialThanksTo));
            wr.WriteLine();
            tmp.text = wr.ToString();
        }
        ChangeState(State.Credit);
    }

    void MakeCreativeCard(Solution solution)
    {
        var tileCountLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "tile_count").Entry.Value;
        var createdAtLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "creation_date").Entry.Value;
        var updatedAtLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "last_modified_date").Entry.Value;
        GameObject card = Instantiate(PrefabCreativeCard, CreativeCards.transform);
        var cardComponent = card.GetComponent<CreativeCard>();
        cardComponent.Solution = solution;
        card.transform.SetAsFirstSibling();
        card.GetComponentInChildren<TextMeshProUGUI>().text = string.Join("\n", new string[] {
            solution.Name
            , ""
            , $"{tileCountLabel}: {solution.Board.PlacedTileCount()}"
            , $"{createdAtLabel}: {DateTime.FromUnixTime(solution.CreatedAt)}"
            , $"{updatedAtLabel}: {DateTime.FromUnixTime(solution.UpdatedAt)}"
        });
        foreach (var button in card.GetComponentsInChildren<Button>())
        {
            switch (button.gameObject.name)
            {
                case "Open":
                    button.onClick.AddListener(() => {
                        if (_state != State.Creative) return;
                        GlobalData.GameMode = GameMode.Creative;
                        GlobalData.Solution = solution;
                        StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.Tiling));
                    });
                    break;
                case "Copy":
                    button.onClick.AddListener(() => {
                        if (_state != State.Creative) return;
                        _solutionCount++;
                        var _solution = new Solution(UniqueName(solution.Name, _persistentManager.LoadCreativeSolutions().Select(x => x.Name).ToArray()));
                        _solution.Board = solution.Board;    // Safe as it is immutable.
                        _solution.UpdatedAt = solution.UpdatedAt;
                        _persistentManager.SaveCreativeSolution(_solution);
                        MakeCreativeCard(_solution);
                    });
                    break;
                case "Rename":
                    button.onClick.AddListener(() => {
                        if (_state != State.Creative) return;
                        _selectedCreativeCard = cardComponent;
                        CreativeRenameField.text = _selectedCreativeCard.Solution.Name;
                        ChangeState(State.CreativeRename);
                    });
                    break;
                case "Delete":
                    button.onClick.AddListener(() => {
                        if (_state != State.Creative) return;
                        _selectedCreativeCard = cardComponent;
                        ChangeState(State.CreativeDeleteConfirm);
                    });
                    break;
                default:
                    Debug.LogAssertion(false);
                    break;
            }
        }
    }

    string UniqueName(string name, string[] names)
    {
        var solutions = _persistentManager.LoadCreativeSolutions();
        if (!names.Any(x => x == name)) return name;
        int n = 1;
        while (names.Any(x => x == $"{name} ({n})")) n++;
        return $"{name} ({n})";
    }

    void OnCreativeNewClick()
    {
        _solutionCount++;
        CreativeEmptyState.SetActive(false);
        var name = LocalizationSettings.StringDatabase.GetTableEntry("default", "untitled").Entry.Value;
        var solution = new Solution(UniqueName(name, _persistentManager.LoadCreativeSolutions().Select(x => x.Name).ToArray()));
        _persistentManager.SaveCreativeSolution(solution);
        MakeCreativeCard(solution);
    }

    void OnCreativeRenameOKClick()
    {
        var solution = _selectedCreativeCard.Solution;
        solution.Name = CreativeRenameField.text.Trim();
        if (string.IsNullOrEmpty(solution.Name)) 
            solution.Name = LocalizationSettings.StringDatabase.GetTableEntry("default", "untitled").Entry.Value;
        _persistentManager.SaveCreativeSolution(solution);
        CreativeEmptyState.transform.SetParent(null);
        CreativeCards.transform.DestroyAllChildren();
        CreativeEmptyState.transform.SetParent(CreativeCards.transform);
        var solutions = _persistentManager.LoadCreativeSolutions();
        foreach (var o in solutions) MakeCreativeCard(o);
        ChangeState(State.Creative);
    }

    void OnCreativeDeleteOKClick()
    {
        _solutionCount--;
        CreativeEmptyState.SetActive(_solutionCount == 0);
        _persistentManager.DeleteCreativeSolution(_selectedCreativeCard.Solution);
        Destroy(_selectedCreativeCard.gameObject);
        ChangeState(State.Creative);
    }

    void OnClickSlot(int slot)
    {
        StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.PuzzleMenu, 0.5f, () => _persistentManager.SetActiveSlot(slot)));
    }

    IEnumerator ChangeLocale(string localeCd)
    {
        if (_persistentManager.GetLocale() == localeCd) yield break;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var selectedLocale = locales.Find(locale => locale.Identifier.Code.Equals(localeCd));
        if (selectedLocale == null)
        {
            Debug.LogWarning("invalid locale" + localeCd);
            yield break;
        }
        LocalizationSettings.SelectedLocale = selectedLocale;
        _persistentManager.SetLocale(localeCd);
        yield return null;
    }

    void OnLanguageToggle(int i, bool isOn)
    {
        if (isOn)
        {
            StartCoroutine(ChangeLocale(_langCodes[i]));
        }
        LanguagePanel.SetActive(false);
    }

    public void OnDebug(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
    }

}
