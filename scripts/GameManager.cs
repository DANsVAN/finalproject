using Godot;
using System;

public partial class GameManager : Node2D
{
[Export] public NodePath ContainerPath;
    private Node _container;
    private Node _currentScene;
	int curentScore;



	public void UnloadCurrent()
	{
		if (_currentScene != null)
		{
			_container.RemoveChild(_currentScene);
			_currentScene.QueueFree();
			_currentScene = null;
		}
	}

	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		_container = GetNode<Node>(ContainerPath);
        // Track the initial child if one exists
        if (_container.GetChildCount() > 0)
            _currentScene = _container.GetChild(0);
			

	}
	public void ChangeChildScene(string scenePath)
    {
        // 1. Remove the old scene safely
        if (_currentScene != null)
        {
            _currentScene.QueueFree(); 
        }

        // 2. Load and Instance the new scene
        var nextScene = GD.Load<PackedScene>(scenePath);
        _currentScene = nextScene.Instantiate();

        // 3. Add to the tree
        _container.AddChild(_currentScene);
    }
	public void EndGame()
	{
		GetTree().Quit();
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
}
