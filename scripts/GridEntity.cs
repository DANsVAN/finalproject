using Godot;
using System;

// Base Class
public partial class GridEntity : Node2D 
{
   
    public Vector2I GridPosition;
	public int MovementRange;
	public int MaxHealth;
	public int CurrentHealth;
	public int Speed;
	public int AttackRange;
	public int BaseDamage;
	protected Sprite2D sprite;
	public int mapindex;
	public int spriteSize = 16;
	public Node2D Node2DEntity;


    // Constructor for the base class
    public GridEntity(int health, int speed, int movementRange, int attackRange, int baseDamage) 
    {
		MovementRange = movementRange;
        MaxHealth = health;
		CurrentHealth = health;
		Speed = speed;
		AttackRange = attackRange;
		BaseDamage = baseDamage;
        GD.Print("GridEntity constructor called!");
    }
    public virtual void TakeDamage(int amount) 
    {
        CurrentHealth -= amount;
        // Play hit animation, check for death, etc.
    }
}
