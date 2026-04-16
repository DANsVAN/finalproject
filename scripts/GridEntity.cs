using Godot;
using System;

// Base Class
public partial class GridEntity : Node2D 
{
   
    public Vector2I GridPosition;
	public int MovementRange;
	public int MaxHealth;
	public int CurrentHealth;
	public int BaseSpeed;
	public int CurrentSpeed;
	public int AttackRange;
	public int BaseDamage;
	public Sprite2D sprite;
	public int mapindex;
	public int spriteSize = 16;
	public Node2D Node2DEntity;
	public bool IsPlayer;


    // Constructor for the base class
    public GridEntity(int health, int speed, int movementRange, int attackRange, int baseDamage, bool isPlayer) 
    {
		MovementRange = movementRange;
        MaxHealth = health;
		CurrentHealth = health;
		BaseSpeed = speed;
		CurrentSpeed = speed;
		AttackRange = attackRange;
		BaseDamage = baseDamage;
		IsPlayer = isPlayer;
        GD.Print("GridEntity constructor called!");
    }
    public virtual void TakeDamage(int amount) 
    {
        CurrentHealth -= amount;
        // Play hit animation, check for death, etc.
    }
}
