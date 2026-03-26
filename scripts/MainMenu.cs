using Godot;
using System;

public partial class MainMenu : CanvasLayer
{
	string secneFilePath;
	// GameManger gameManger;

	GameManager gameManger;





	public void _on_play_btn_pressed()
	{

		secneFilePath = "res://scenes/game_world.tscn";
		this.FollowViewportEnabled = true;
		gameManger.ChangeChildScene(secneFilePath);
	}
	public void _on_settings_btn_pressed()
	{
		secneFilePath = "res://scenes/settings.tscn";
		this.FollowViewportEnabled = true;
		gameManger.ChangeChildScene(secneFilePath);
	}

		public void _on_exit_btn_pressed()
	{
		gameManger.EndGame();
	}
	public override void _Ready()
	{
		// gameManger = (GameManager)GetTree().GetNodesInGroup("GameManager")[0];
		gameManger = GetParent() as GameManager;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
