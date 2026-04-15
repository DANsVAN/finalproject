using Godot;
using System;

public partial class EnemyFighter : GridEntity 
{	
	public EnemyFighter() : base(20,5,5,1,5)
	{
	}
	public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("Sprite2D");
    }
}
