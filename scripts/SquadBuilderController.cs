using Godot;
using System.Collections.Generic;

public partial class SquadBuilderController : CanvasLayer
{
	private const string GameWorldScenePath = "res://scenes/game_world.tscn";
	private const string MainMenuScenePath = "res://scenes/main_menu.tscn";

	private readonly List<OptionButton> _slotSelectors = new List<OptionButton>();
	private readonly List<CharacterClass> _availableClasses = new List<CharacterClass>();
	private Label _statusLabel;
	private GameManager _gameManager;

	public override void _Ready()
	{
		_gameManager = GetParent() as GameManager;
		_statusLabel = GetNode<Label>("Root/VBox/StatusLabel");

		_slotSelectors.Clear();
		for (int slot = 0; slot < SquadSelectionState.SquadSize; slot++)
		{
			OptionButton selector = GetNode<OptionButton>($"Root/VBox/Slots/Slot{slot + 1}/ClassSelector");
			_slotSelectors.Add(selector);
		}

		IReadOnlyList<CharacterClass> classes = SquadSelectionState.GetAvailableClasses();
		_availableClasses.Clear();
		for (int i = 0; i < classes.Count; i++)
			_availableClasses.Add(classes[i]);

		PopulateSelectors();
		ApplyCurrentOrDefaultSelection();
		UpdateStatus("Select your 5-unit squad, then start battle.");
	}

	private void PopulateSelectors()
	{
		for (int i = 0; i < _slotSelectors.Count; i++)
		{
			OptionButton selector = _slotSelectors[i];
			selector.Clear();

			for (int classIndex = 0; classIndex < _availableClasses.Count; classIndex++)
			{
				CharacterClass classDef = _availableClasses[classIndex];
				string label = classDef?.DisplayName ?? $"Class {classIndex + 1}";
				selector.AddItem(label, classIndex);
			}
		}
	}

	private void ApplyCurrentOrDefaultSelection()
	{
		List<CharacterClass> selected = SquadSelectionState.GetSelectedOrDefaultSquad();
		for (int i = 0; i < _slotSelectors.Count; i++)
		{
			if (_availableClasses.Count == 0)
				return;

			CharacterClass target = selected.Count > i ? selected[i] : _availableClasses[0];
			int index = _availableClasses.IndexOf(target);
			if (index < 0) index = 0;
			_slotSelectors[i].Select(index);
		}
	}

	public void _on_start_battle_pressed()
	{
		if (_availableClasses.Count == 0)
		{
			UpdateStatus("No classes found. Add class resources first.");
			return;
		}

		List<CharacterClass> squad = new List<CharacterClass>();
		for (int i = 0; i < _slotSelectors.Count; i++)
		{
			int selectedIndex = _slotSelectors[i].Selected;
			if (selectedIndex < 0 || selectedIndex >= _availableClasses.Count)
			{
				UpdateStatus("Each slot must have a valid class selected.");
				return;
			}

			squad.Add(_availableClasses[selectedIndex]);
		}

		SquadSelectionState.SetSelectedSquad(squad);
		_gameManager?.ChangeChildScene(GameWorldScenePath);
	}

	public void _on_back_pressed()
	{
		_gameManager?.ChangeChildScene(MainMenuScenePath);
	}

	private void UpdateStatus(string message)
	{
		if (_statusLabel != null)
			_statusLabel.Text = message;
	}
}
