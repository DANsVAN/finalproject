using System;
using Godot;

public partial class NextRoundOverlay : CanvasLayer
{
	private const float DisplaySeconds = 2.5f;

	private Action _onContinue;
	private bool _ended;
	private Timer _timer;
	private ColorRect _dim;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_timer = GetNode<Timer>("%Timer");
		_dim = GetNode<ColorRect>("%Dim");
		_dim.GuiInput += OnDimGuiInput;
	}

	public void Begin(Action onContinue, int roundCleared, int nextRoundNumber)
	{
		_onContinue = onContinue;
		Label title = GetNodeOrNull<Label>("%TitleLabel");
		Label subtitle = GetNodeOrNull<Label>("%SubtitleLabel");
		if (title != null)
			title.Text = $"Round {roundCleared} cleared!";
		if (subtitle != null)
			subtitle.Text = $"Starting round {nextRoundNumber}...";

		_timer.OneShot = true;
		_timer.WaitTime = DisplaySeconds;
		_timer.Timeout += OnFinish;
		_timer.Start();
	}

	private void OnDimGuiInput(InputEvent @event)
	{
		if (_ended)
			return;
		if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			OnFinish();
	}

	private void OnFinish()
	{
		if (_ended)
			return;
		_ended = true;
		if (_timer != null)
		{
			_timer.Timeout -= OnFinish;
			_timer.Stop();
		}

		Action cb = _onContinue;
		QueueFree();
		cb?.Invoke();
	}
}
