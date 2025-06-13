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

public class SolutionManager : MonoBehaviour
{

    enum State
    {
        Default,
        Rename,
        DeleteConfirm,
    }

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
    List<Solution> _solutions;

    LoadingManager _loadingManager;
    PersistentManager _persistentManager;

    public SolutionManager Init()
    {
        _solutions = _persistentManager.LoadSolutions(GlobalData.GameMode, GlobalData.Slot, GlobalData.Level);
        foreach (var card in SolutionCards.Children())
            if (card != SolutionEmptyState) Destroy(card);
        foreach (var solution in _solutions)
            MakeSolutionCard(solution);
        SolutionEmptyState.SetActive(!_solutions.Any());
        return this;
    }

    void ChangeState(State to)
    {
        Debug.Log($"SolutionManager#ChangeState: change state from {_state} to {to}");
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
        ChangeState(State.Default);
    }

    /*
     * Solution API
     */

    string DefaultName()
    {
        return LocalizationSettings.StringDatabase.GetTableEntry("default", "untitled").Entry.Value;
    }

    string UniqueName(string name)
    {
        var names = _solutions.Select(x => x.Name);
        if (!names.Any(x => x == name)) return name;
        int n = 1;
        while (names.Any(x => x == $"{name} ({n})")) n++;
        return $"{name} ({n})";
    }

    Solution MakeSolution()
    {
        var solution = new Solution(GlobalData.GameMode, GlobalData.Slot, GlobalData.Level, UniqueName(DefaultName()));
        _persistentManager.SaveSolution(solution);
        _solutions.Add(solution);
        SolutionEmptyState.SetActive(false);
        return solution;
    }

    void DeleteSolution(Solution solution)
    {
        _persistentManager.DeleteSolution(solution);
        _solutions = _solutions.Where(x => x.PhysicalName != solution.PhysicalName).ToList();
        SolutionEmptyState.SetActive(!_solutions.Any());
    }

    Solution CopySolution(Solution solution)
    {
        var result = new Solution(GlobalData.GameMode, GlobalData.Slot, GlobalData.Level, UniqueName(solution.Name));
        result.Board = solution.Board;    // Safe as it is immutable.
        result.UpdatedAt = solution.UpdatedAt;
        _persistentManager.SaveSolution(result);
        _solutions.Add(result);
        return result;
    }

    public bool HasSolution()
    {
        return _solutions.Any();
    }

    public void OpenNewSolution()
    {
        OpenSolution(new Solution(GlobalData.GameMode, GlobalData.Slot, GlobalData.Level, UniqueName(DefaultName())));
    }

    void OpenSolution(Solution solution)
    {
        GlobalData.Solution = solution;
        StartCoroutine(_loadingManager.LoadAsync(LoadingManager.Scene.Tiling));
    }

    /*
     * UI
     */

    void UpdateSolutionCardText(SolutionCard card)
    {
        var solution = card.Solution;
        var tileCountLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "tile_count").Entry.Value;
        var createdAtLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "creation_date").Entry.Value;
        var updatedAtLabel = LocalizationSettings.StringDatabase.GetTableEntry("default", "last_modified_date").Entry.Value;
        card.GetComponentInChildren<TextMeshProUGUI>().text = string.Join("\n", new string[] {
            solution.Name
            , ""
            , $"{tileCountLabel}: {solution.Board.PlacedTileCount()}"
            , $"{createdAtLabel}: {DateTime.FromUnixTime(solution.CreatedAt)}"
            , $"{updatedAtLabel}: {DateTime.FromUnixTime(solution.UpdatedAt)}"
        });
    }

    void MakeSolutionCard(Solution solution)
    {
        GameObject o = Instantiate(PrefabSolutionCard, SolutionCards.transform);
        o.transform.SetAsFirstSibling();
        var card = o.GetComponent<SolutionCard>();
        card.Solution = solution;
        UpdateSolutionCardText(card);
        foreach (var button in o.GetComponentsInChildren<Button>())
        {
            switch (button.gameObject.name)
            {
                case "Open":
                    button.onClick.AddListener(() => OpenSolution(solution));
                    break;
                case "Copy":
                    button.onClick.AddListener(() => MakeSolutionCard(CopySolution(solution)));
                    break;
                case "Rename":
                    button.onClick.AddListener(() => {
                        _selectedSolutionCard = card;
                        SolutionRenameField.text = _selectedSolutionCard.Solution.Name;
                        ChangeState(State.Rename);
                    });
                    break;
                case "Delete":
                    button.onClick.AddListener(() => {
                        _selectedSolutionCard = card;
                        ChangeState(State.DeleteConfirm);
                    });
                    break;
                default:
                    Debug.LogAssertion(false);
                    break;
            }
        }
    }

    /*
     * Event handlers
     */

    void OnSolutionNewClick()
    {
        MakeSolutionCard(MakeSolution());
    }

    void OnSolutionRenameOKClick()
    {
        var solution = _selectedSolutionCard.Solution;
        solution.Name = SolutionRenameField.text.Trim();
        if (string.IsNullOrEmpty(solution.Name)) 
            solution.Name = LocalizationSettings.StringDatabase.GetTableEntry("default", "untitled").Entry.Value;
        UpdateSolutionCardText(_selectedSolutionCard);
        _persistentManager.SaveSolution(solution);
        ChangeState(State.Default);
    }

    void OnSolutionDeleteOKClick()
    {
        DeleteSolution(_selectedSolutionCard.Solution);
        Destroy(_selectedSolutionCard.gameObject);
        ChangeState(State.Default);
    }

    public void OnCancel()
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

    public void OnDebug(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
    }

}

