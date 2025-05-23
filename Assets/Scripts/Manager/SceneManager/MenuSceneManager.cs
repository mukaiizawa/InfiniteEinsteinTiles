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
        Solutions,
        Credit,
        Language,
        Menu,
        Setting,
    }

    /* Puzzle Mode */
    public Button[] SlotButtons;

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
        "Ken'ichi Tokuoka",
        "Miki Yonemura",
        "Mituki Miharu aka Haru",
        "Nobuaki Akasaka",
        "Shinya Kato",
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
    public Button MenuOpenButton;
    public Button MenuCloseButton;

    public GameObject SettingPanel;
    public Button SettingOpenButton;
    public Button SettingCloseButton;

    public GameObject SolutionsPanel;
    public Button SolutionsOpenButton;
    public Button SolutionsCloseButton;
    public Button QuitButton;

    State _state;
    AssetManager _assetManager;
    AudioManager _audioManager;
    LoadingManager _loadingManager;
    PersistentManager _persistentManager;
    SettingManager _settingManager;
    SolutionsPanelManager _solutionsPanelManager;

    void ChangeState(State to)
    {
        Debug.Log($"MenuSceneManager#ChangeState: change state from {_state} to {to}");
        switch (to)
        {
            case State.None:
                SolutionsPanel.SetActive(false);
                MenuPanel.SetActive(false);
                LanguagePanel.SetActive(false);
                CreditPanel.SetActive(false);
                break;
            case State.Solutions:
                SolutionsPanel.SetActive(true);
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
        _solutionsPanelManager = this.gameObject.GetComponent<SolutionsPanelManager>();
    }

    void Start()
    {
        _audioManager.SetPlaylist(_assetManager.GetPlaylist(LoadingManager.Scene.Menu)).StartBGM();
        _solutionsPanelManager.Reload(GameMode.Creative);
        SolutionsOpenButton.onClick.AddListener(() => ChangeState(State.Solutions));
        SolutionsCloseButton.onClick.AddListener(() => ChangeState(State.None));
        for (int i = 0; i < SlotButtons.Length; i++)
        {
            int slot = i + 1;
            var slotButton = SlotButtons[i];
            slotButton.onClick.AddListener(() => OnClickSlot(slot));
            foreach (var tmp in slotButton.GetComponentsInChildren<TextMeshProUGUI>())
            {
                var progress = _persistentManager.LoadProgress(slot);
                if (tmp.gameObject.name == "Progress")
                    tmp.text = $"{progress.CurrentLevel * 100 / GlobalData.TotalLevel}%";
            }
        }
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
        LanguageOpenButton.onClick.AddListener(() => ChangeState(State.Language));
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
            case State.Solutions:
                _solutionsPanelManager.Cancel();
                if (!SolutionsPanel.activeSelf) ChangeState(State.None);
                break;
            case State.Menu:
            case State.Credit:
            case State.Language:
                ChangeState(State.None);
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
        var paper = LocalizationSettings.StringDatabase.GetTableEntry("default", "academic_paper").Entry.Value;
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
            wr.WriteLine($"# {paper}");
            wr.WriteLine("David Smith, Joseph Samuel Myers, Craig S. Kaplan, and Chaim Goodman-Strauss.");
            wr.WriteLine("Copyright The authors. Released under the CC BY license (International 4.0).");
            wr.WriteLine("https://escholarship.org/uc/item/3317z9z9");
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

    void OnClickSlot(int slot)
    {
        StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.PuzzleMenu, 0.5f, () => GlobalData.Slot = slot));
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
