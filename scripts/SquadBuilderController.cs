using Godot;
using System;
using System.Collections.Generic;

public partial class SquadBuilderController : CanvasLayer
{
	private const string GameWorldScenePath = "res://scenes/game_world.tscn";
	private const string MainMenuScenePath = "res://scenes/main_menu.tscn";

	private readonly List<OptionButton> _slotSelectors = new List<OptionButton>();
	private readonly List<TextureRect> _cardSprites = new List<TextureRect>();
	private readonly List<Label> _cardStats = new List<Label>();
	private readonly List<Label> _cardDescs = new List<Label>();

	private readonly List<CharacterClass> _availableClasses = new List<CharacterClass>();
	private GameManager _gameManager;

	public override void _Ready()
	{
		_gameManager = GetParent() as GameManager;

		_slotSelectors.Clear();
		_cardSprites.Clear();
		_cardStats.Clear();
		_cardDescs.Clear();

		for (int slot = 0; slot < SquadSelectionState.SquadSize; slot++)
		{
			int n = slot + 1;
			OptionButton selector = GetNode<OptionButton>($"Root/MainVBox/ColumnsRow/Column{n}/ClassSelector");
			int capturedSlot = slot;
			selector.ItemSelected += _ => OnSlotSelectionChanged(capturedSlot);
			_slotSelectors.Add(selector);

			TextureRect cardSprite = GetNode<TextureRect>($"Root/MainVBox/ColumnsRow/Column{n}/CardPanel/CardPad/CardVBox/CardSprite");
			cardSprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
			_cardSprites.Add(cardSprite);
			_cardStats.Add(GetNode<Label>($"Root/MainVBox/ColumnsRow/Column{n}/CardPanel/CardPad/CardVBox/CardStats"));
			_cardDescs.Add(GetNode<Label>($"Root/MainVBox/ColumnsRow/Column{n}/CardPanel/CardPad/CardVBox/CardDesc"));
		}

		IReadOnlyList<CharacterClass> classes = SquadSelectionState.GetAvailableClasses();
		_availableClasses.Clear();
		for (int i = 0; i < classes.Count; i++)
			_availableClasses.Add(classes[i]);

		PopulateSelectors();
		ApplyCurrentOrDefaultSelection();
		for (int i = 0; i < SquadSelectionState.SquadSize; i++)
			RefreshCard(i);
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

	private void OnSlotSelectionChanged(int slotIndex)
	{
		RefreshCard(Mathf.Clamp(slotIndex, 0, _slotSelectors.Count - 1));
	}

	private void RefreshCard(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= _slotSelectors.Count)
			return;

		if (_availableClasses.Count == 0)
		{
			RenderCard(slotIndex, null);
			return;
		}

		int selectedIndex = _slotSelectors[slotIndex].Selected;
		if (selectedIndex < 0 || selectedIndex >= _availableClasses.Count)
			selectedIndex = 0;

		RenderCard(slotIndex, _availableClasses[selectedIndex]);
	}

	private void RenderCard(int slotIndex, CharacterClass classDef)
	{
		TextureRect spriteRect = _cardSprites[slotIndex];
		Label statsLabel = _cardStats[slotIndex];
		Label descLabel = _cardDescs[slotIndex];

		if (classDef == null)
		{
			spriteRect.Texture = null;
			statsLabel.Text = "";
			descLabel.Text = "";
			return;
		}

		spriteRect.Texture = BuildFrame0Atlas(classDef);
		statsLabel.Text =
			$"HP {classDef.MaxHealth}\n" +
			$"Spd {classDef.BaseSpeed}\n" +
			$"Mv {classDef.MovementRange}\n" +
			$"Rng {classDef.AttackRange}\n" +
			$"Dmg {classDef.BaseDamage}";
		descLabel.Text = string.IsNullOrWhiteSpace(classDef.Description) ? "" : classDef.Description;
	}

	private static Texture2D BuildFrame0Atlas(CharacterClass classDef)
	{
		if (classDef?.SpriteTexture == null)
			return null;

		Vector2 size = classDef.SpriteTexture.GetSize();
		int hframes = Math.Max(1, classDef.SpriteHFrames);
		float frameW = size.X / hframes;

		AtlasTexture atlas = new AtlasTexture();
		atlas.Atlas = classDef.SpriteTexture;
		atlas.Region = new Rect2(0, 0, frameW, size.Y);
		return atlas;
	}

	public void _on_start_battle_pressed()
	{
		if (_availableClasses.Count == 0)
			return;

		List<CharacterClass> squad = new List<CharacterClass>();
		for (int i = 0; i < _slotSelectors.Count; i++)
		{
			int selectedIndex = _slotSelectors[i].Selected;
			if (selectedIndex < 0 || selectedIndex >= _availableClasses.Count)
				return;

			squad.Add(_availableClasses[selectedIndex]);
		}

		SquadSelectionState.SetSelectedSquad(squad);
		RunSession.ResetForNewRun();
		_gameManager?.ChangeChildScene(GameWorldScenePath);
	}

	public void _on_back_pressed()
	{
		_gameManager?.ChangeChildScene(MainMenuScenePath);
	}
}
