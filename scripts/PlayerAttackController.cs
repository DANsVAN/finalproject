using Godot;
using System;

/// Bottom-left attack UI: three attacks + End Turn. Toggle same attack to deselect.
public partial class PlayerAttackController : CanvasLayer
{
	[Signal] public delegate void AttackSelectedEventHandler(int attackIndex);
	[Signal] public delegate void AttackDeselectedEventHandler();
	[Signal] public delegate void EndTurnPressedEventHandler();

	private Button _attackButton0;
	private Button _attackButton1;
	private Button _attackButton2;
	private Button _endTurnButton;

	private int _selectedIndex = -1;

	public override void _Ready()
	{
		_attackButton0 = GetNode<Button>("Root/VBox/AttackRow/Attack0");
		_attackButton1 = GetNode<Button>("Root/VBox/AttackRow/Attack1");
		_attackButton2 = GetNode<Button>("Root/VBox/AttackRow/Attack2");
		_endTurnButton = GetNode<Button>("Root/VBox/EndTurn");

		_attackButton0.Pressed += () => OnAttackPressed(0);
		_attackButton1.Pressed += () => OnAttackPressed(1);
		_attackButton2.Pressed += () => OnAttackPressed(2);
		_endTurnButton.Pressed += OnEndTurnPressed;

		SetTurnActive(false);
	}

	public void SetTurnActive(bool active)
	{
		Visible = active;
		_attackButton0.Disabled = !active;
		_attackButton1.Disabled = !active;
		_attackButton2.Disabled = !active;
		_endTurnButton.Disabled = !active;
		if (!active)
			ClearSelectionVisual();
	}

	/// After the player has used their one attack, allow deselect only; block switching attacks.
	public void SetAttackSelectionLocked(bool locked)
	{
		if (locked)
		{
			_attackButton0.Disabled = true;
			_attackButton1.Disabled = true;
			_attackButton2.Disabled = true;
		}
		else
		{
			bool active = Visible;
			_attackButton0.Disabled = !active;
			_attackButton1.Disabled = !active;
			_attackButton2.Disabled = !active;
		}
	}

	public void SetBusy(bool busy)
	{
		_endTurnButton.Disabled = busy || !Visible;
	}

	public void ClearAttackSelection()
	{
		_selectedIndex = -1;
		ClearSelectionVisual();
	}

	private void OnAttackPressed(int index)
	{
		if (_selectedIndex == index)
		{
			_selectedIndex = -1;
			ClearSelectionVisual();
			EmitSignal(SignalName.AttackDeselected);
			return;
		}

		_selectedIndex = index;
		UpdateSelectionVisual();
		EmitSignal(SignalName.AttackSelected, index);
	}

	private void OnEndTurnPressed()
	{
		EmitSignal(SignalName.EndTurnPressed);
	}

	private void UpdateSelectionVisual()
	{
		SetButtonSelected(_attackButton0, _selectedIndex == 0);
		SetButtonSelected(_attackButton1, _selectedIndex == 1);
		SetButtonSelected(_attackButton2, _selectedIndex == 2);
	}

	private void ClearSelectionVisual()
	{
		SetButtonSelected(_attackButton0, false);
		SetButtonSelected(_attackButton1, false);
		SetButtonSelected(_attackButton2, false);
	}

	private static void SetButtonSelected(Button button, bool selected)
	{
		if (button == null) return;
		button.Modulate = selected ? new Color(1.0f, 1.0f, 0.75f, 1.0f) : Colors.White;
	}
}
