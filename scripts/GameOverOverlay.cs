using Godot;

public partial class GameOverOverlay : CanvasLayer
{
	private Label _scoreLabel;
	private Label _roundLabel;
	private GameManager _gameManager;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_scoreLabel = GetNode<Label>("%ScoreLabel");
		_roundLabel = GetNode<Label>("%RoundLabel");
		// GameManager -> GameWorld -> WorldMapPerlinNoise -> this overlay
		_gameManager = GetParent()?.GetParent()?.GetParent() as GameManager;
	}

	public void Setup(int finalScore, int roundReached)
	{
		if (_scoreLabel != null)
			_scoreLabel.Text = $"Score: {finalScore}";
		if (_roundLabel != null)
			_roundLabel.Text = $"Round reached: {roundReached}";
	}

	private void _on_home_pressed()
	{
		RunSession.ResetForNewRun();
		GetTree().Paused = false;
		_gameManager?.ChangeChildScene("res://scenes/main_menu.tscn");
	}

	private void _on_play_again_pressed()
	{
		RunSession.ResetForNewRun();
		GetTree().Paused = false;
		_gameManager?.ChangeChildScene("res://scenes/game_world.tscn");
	}
}
