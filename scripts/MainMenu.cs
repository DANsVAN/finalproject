using Godot;
using System;
using System.Collections.Generic;

public partial class MainMenu : CanvasLayer
{
	private const string SquadBuilderScenePath = "res://scenes/squad_builder.tscn";
	private const string SettingsScenePath = "res://scenes/settings.tscn";
	private const string HowToPlayScenePath = "res://scenes/how_to_play.tscn";

	private string secneFilePath;
	private GameManager gameManger;
	private Control _centerPanel;
	private readonly Dictionary<Button, Tween> _buttonTweens = new Dictionary<Button, Tween>();

	public void _on_play_btn_pressed()
	{
		secneFilePath = SquadBuilderScenePath;
		this.FollowViewportEnabled = true;
		gameManger.ChangeChildScene(secneFilePath);
	}

	public void _on_settings_btn_pressed()
	{
		secneFilePath = SettingsScenePath;
		this.FollowViewportEnabled = true;
		gameManger.ChangeChildScene(secneFilePath);
	}

	public void _on_how_to_play_btn_pressed()
	{
		secneFilePath = HowToPlayScenePath;
		this.FollowViewportEnabled = true;
		gameManger.ChangeChildScene(secneFilePath);
	}

	public void _on_exit_btn_pressed()
	{
		gameManger.EndGame();
	}

	public override void _Ready()
	{
		gameManger = GetParent() as GameManager;
		_centerPanel = GetNode<Control>("CenterPanel");

		SetupButtonFeedback("CenterPanel/PanelMargin/VBoxContainer/PlayBTN");
		SetupButtonFeedback("CenterPanel/PanelMargin/VBoxContainer/HowToPlayBTN");
		SetupButtonFeedback("CenterPanel/PanelMargin/VBoxContainer/SettingsBTN");
		SetupButtonFeedback("CenterPanel/PanelMargin/VBoxContainer/ExitBTN");

		GetNode<Button>("CenterPanel/PanelMargin/VBoxContainer/PlayBTN").GrabFocus();
		PlayEntryAnimation();
	}

	private void PlayEntryAnimation()
	{
		_centerPanel.Modulate = new Color(1f, 1f, 1f, 0f);
		Vector2 startPos = _centerPanel.Position + new Vector2(0f, 20f);
		_centerPanel.Position = startPos;

		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(_centerPanel, "modulate", Colors.White, 0.25f);
		tween.TweenProperty(_centerPanel, "position", startPos - new Vector2(0f, 20f), 0.28f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
	}

	private void SetupButtonFeedback(string buttonPath)
	{
		Button button = GetNode<Button>(buttonPath);
		button.PivotOffset = button.Size * 0.5f;
		button.MouseEntered += () => AnimateButton(button, true);
		button.MouseExited += () => AnimateButton(button, false);
		button.FocusEntered += () => AnimateButton(button, true);
		button.FocusExited += () => AnimateButton(button, false);
	}

	private void AnimateButton(Button button, bool highlighted)
	{
		if (_buttonTweens.TryGetValue(button, out Tween activeTween))
			activeTween.Kill();

		Vector2 targetScale = highlighted ? new Vector2(1.03f, 1.03f) : Vector2.One;
		Color targetModulate = highlighted ? new Color(1f, 1f, 0.94f, 1f) : Colors.White;

		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(button, "scale", targetScale, 0.12f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(button, "modulate", targetModulate, 0.12f);
		_buttonTweens[button] = tween;
	}
}
