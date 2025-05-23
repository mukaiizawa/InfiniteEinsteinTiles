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

public class SolutionsPanelManager : MonoBehaviour
{

    enum State
    {
        Default,
        Rename,
        DeleteConfirm,
    }

    int _solutionCount = 0;
    SolutionCard _selectedSolutionCard;
    public GameObject SolutionPanel;
    public Button SolutionNewButton;
    public GameObject SolutionEmptyState;
    public GameObject SolutionCards;
    public GameObject PrefabSolutionCard;

    /* Rename */
    public GameObject SolutionRenamePanel;
    public TMP_InputField SolutionRenameField;
    public Button SolutionRenameOKButton;
    public Button SolutionRenameCancelButton;

    /* Delete confirm */
    public GameObject SolutionDeleteConfirmPanel;
    public Button SolutionDeleteOKButton;
    public Button SolutionDeleteCancelButton;

    State _state;
    GameMode _gameMode;
    int _level;
    int _slot;

    LoadingManager _loadingManager;
    PersistentManager _persistentManager;

    void ChangeState(State to)
    {
        Debug.Log($"SolutionsPanelManager#ChangeState: change state from {_state} to {to}");
        switch (to)
        {
            case State.Default:
                SolutionRenamePanel.SetActive(false);
                SolutionDeleteConfirmPanel.SetActive(false);
                break;
            case State.Rename:
                SolutionRenamePanel.SetActive(true);
                break;
            case State.DeleteConfirm:
                SolutionDeleteConfirmPanel.SetActive(true);
                break;
            default:
                Debug.LogError("Unexpected state" + to);
                break;
        }
        _state = to;
    }

    void Awake()
    {
        _loadingManager = this.gameObject.GetComponent<LoadingManager>();
        _persistentManager = this.gameObject.GetComponent<PersistentManager>();
    }

    void Start()
    {
        SolutionNewButton.onClick.AddListener(OnSolutionNewClick);
        SolutionRenameOKButton.onClick.AddListener(OnSolutionRenameOKClick);
        SolutionRenameCancelButton.onClick.AddListener(() => ChangeState(State.Default));
        SolutionDeleteOKButton.onClick.AddListener(OnSolutionDeleteOKClick);
        SolutionDeleteCancelButton.onClick.AddListener(() => ChangeState(State.Default));
    }

    public void Reload(GameMode gameMode)
    {
        if (gameMode != GameMode.Creative)
            Debug.LogError($"SolutionsPanelManager#Reload: invalid gameMode {gameMode}");
        Reload(gameMode, -1, -1);
    }

    public void Reload(GameMode gameMode, int slot, int level)
    {
        _gameMode = gameMode;
        _level = level;
        _slot = slot;
        SolutionEmptyState.transform.SetParent(null);
        SolutionCards.transform.DestroyAllChildren();
        SolutionEmptyState.transform.SetParent(SolutionCards.transform);
        SolutionEmptyState.SetActive(false);
        _solutionCount = 0;
        foreach (var solution in _persistentManager.LoadSolutions(_gameMode, _slot, _level))
            MakeSolutionCard(solution);
        if (gameMode == GameMode.Puzzle && _solutionCount == 0)
            OnSolutionNewClick();    // make default solution.
        ChangeState(State.Default);
    }

    public void Cancel()
    {
        switch (_state)
        {
            case State.Default:
                SolutionPanel.SetActive(false);
                break;
            case State.Rename:
            case State.DeleteConfirm:
                ChangeState(State.Default);
                break;
            default:
                break;
        }
    }

    void MakeSolutionCard(Solution solution)
    {
        _solutionCount++;
        SolutionEmptyState.SetActive(false);
        var tileCountLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "tile_count").Entry.Value;
        var createdAtLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "creation_date").Entry.Value;
        var updatedAtLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "last_modified_date").Entry.Value;
        GameObject card = Instantiate(PrefabSolutionCard, SolutionCards.transform);
        var cardComponent = card.GetComponent<SolutionCard>();
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
                        GlobalData.GameMode = _gameMode;
                        GlobalData.Solution = solution;
                        StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.Tiling));
                    });
                    break;
                case "Copy":
                    button.onClick.AddListener(() => {
                        var name = UniqueName(solution.Name, _persistentManager.LoadSolutions(_gameMode, _slot, _level).Select(x => x.Name).ToArray());
                        var _solution = new Solution(_gameMode, _slot, _level, name);
                        _solution.Board = solution.Board;    // Safe as it is immutable.
                        _solution.UpdatedAt = solution.UpdatedAt;
                        _persistentManager.SaveSolution(_solution);
                        MakeSolutionCard(_solution);
                    });
                    break;
                case "Rename":
                    button.onClick.AddListener(() => {
                        _selectedSolutionCard = cardComponent;
                        SolutionRenameField.text = _selectedSolutionCard.Solution.Name;
                        ChangeState(State.Rename);
                    });
                    break;
                case "Delete":
                    button.onClick.AddListener(() => {
                        _selectedSolutionCard = cardComponent;
                        ChangeState(State.DeleteConfirm);
                    });
                    break;
                default:
                    Debug.LogAssertion(false);
                    break;
            }
        }
    }

    void DeleteSolutionCard()
    {
        _solutionCount--;
        SolutionEmptyState.SetActive(_solutionCount == 0);
        Destroy(_selectedSolutionCard.gameObject);
    }

    string UniqueName(string name, string[] names)
    {
        var solutions = _persistentManager.LoadSolutions(_gameMode, _slot, _level);
        if (!names.Any(x => x == name)) return name;
        int n = 1;
        while (names.Any(x => x == $"{name} ({n})")) n++;
        return $"{name} ({n})";
    }

    void OnSolutionNewClick()
    {
        var defaultName = LocalizationSettings.StringDatabase.GetTableEntry("default", "untitled").Entry.Value;
        var name = UniqueName(defaultName, _persistentManager.LoadSolutions(_gameMode, _slot, _level).Select(x => x.Name).ToArray());
        var solution = new Solution(_gameMode, _slot, _level, name);
        _persistentManager.SaveSolution(solution);
        MakeSolutionCard(solution);
    }

    void OnSolutionRenameOKClick()
    {
        var solution = _selectedSolutionCard.Solution;
        solution.Name = SolutionRenameField.text.Trim();
        if (string.IsNullOrEmpty(solution.Name)) 
            solution.Name = LocalizationSettings.StringDatabase.GetTableEntry("default", "untitled").Entry.Value;
        _persistentManager.SaveSolution(solution);
        Reload(_gameMode, _slot, _level);
        ChangeState(State.Default);
    }

    void OnSolutionDeleteOKClick()
    {
        _persistentManager.DeleteSolution(_selectedSolutionCard.Solution);
        DeleteSolutionCard();
        ChangeState(State.Default);
    }

    public void OnDebug(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
    }

}

