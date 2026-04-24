using System;
using Godot;

public partial class NextRoundOverlay : CanvasLayer
{
	private const float DisplaySeconds = 2.5f;

	private Action _onContinue;
	private bool _ended;
	private Timer _timer;
	private ColorRect _dim;
	private Control _centerPanel;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_timer = GetNode<Timer>("%Timer");
		_dim = GetNode<ColorRect>("%Dim");
		_centerPanel = GetNodeOrNull<Control>("Center/Panel");
		_dim.GuiInput += OnDimGuiInput;
		PlayEntryAnimation();
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
		PlayExitAnimation(() =>
		{
			QueueFree();
			cb?.Invoke();
		});
	}

	private void PlayEntryAnimation()
	{
		if (_dim == null || _centerPanel == null)
			return;

		Color baseDim = _dim.Color;
		_dim.Color = new Color(baseDim.R, baseDim.G, baseDim.B, 0f);
		_centerPanel.Modulate = new Color(1f, 1f, 1f, 0f);
		Vector2 startPos = _centerPanel.Position + new Vector2(0f, 14f);
		_centerPanel.Position = startPos;

		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(_dim, "color", baseDim, 0.2f);
		tween.TweenProperty(_centerPanel, "modulate", Colors.White, 0.22f);
		tween.TweenProperty(_centerPanel, "position", startPos - new Vector2(0f, 14f), 0.22f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}

	private void PlayExitAnimation(Action onDone)
	{
		if (_dim == null || _centerPanel == null)
		{
			onDone?.Invoke();
			return;
		}

		Color baseDim = _dim.Color;
		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(_dim, "color", new Color(baseDim.R, baseDim.G, baseDim.B, 0f), 0.16f);
		tween.TweenProperty(_centerPanel, "modulate", new Color(1f, 1f, 1f, 0f), 0.16f);
		tween.TweenProperty(_centerPanel, "position", _centerPanel.Position + new Vector2(0f, 10f), 0.16f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);
		tween.Finished += () => onDone?.Invoke();
	}
}
