using Godot;

public partial class HowToPlay : CanvasLayer
{
	private const string MainMenuScenePath = "res://scenes/main_menu.tscn";
	private GameManager _gameManager;

	public override void _Ready()
	{
		_gameManager = GetParent() as GameManager;
	}

	public void _on_back_btn_pressed()
	{
		_gameManager?.ChangeChildScene(MainMenuScenePath);
	}
}
