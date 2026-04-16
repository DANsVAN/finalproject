using Godot;
using System;

public partial class PlayerFighter : GridEntity 
{	
	public PlayerFighter() : base(20,5,5,1,5,true)
	{
	}
	public override void _Ready()
    {
		base._Ready();
        sprite = GetNode<Sprite2D>("Sprite2D");
    }
}
