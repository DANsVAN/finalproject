using Godot;
using System;

// Base Class
public partial class GridEntity : Node2D 
{
	private Control _healthBarRoot;
	private ProgressBar _healthBar;
	private Tween _healthBarTween;

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

	public override void _Ready()
	{
		CacheHealthBarNodes();
		RefreshHealthBar();
	}

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

	// Sets up the health bar nodes
	private void CacheHealthBarNodes()
	{
		_healthBarRoot = GetNodeOrNull<Control>("HealthBarRoot");
		_healthBar = GetNodeOrNull<ProgressBar>("HealthBarRoot/HealthBar");
		if (_healthBar != null)
		{
			_healthBar.MinValue = 0;
			_healthBar.MaxValue = MaxHealth;
			_healthBar.Value = CurrentHealth;
			_healthBar.Step = 1;
			_healthBar.ShowPercentage = false;
		}
	}

	// Refreshes the health bar to show the current health of the entity
	private void RefreshHealthBar()
	{
		if (_healthBar == null || _healthBarRoot == null)
			return;

		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = Math.Clamp(CurrentHealth, 0, MaxHealth);

		bool fullHp = CurrentHealth >= MaxHealth;
		_healthBarRoot.Visible = !fullHp;
	}

	// When the entity takes damage, the health bar pulses
	private void PulseHealthBarOnDamage()
	{
		if (_healthBarRoot == null)
			return;

		_healthBarTween?.Kill();
		_healthBarTween = CreateTween();
		_healthBarTween.SetParallel(true);

		Color neutral = Colors.White;
		Color flash = new Color(1.0f, 0.85f, 0.85f, 1.0f);
		_healthBarRoot.Modulate = neutral;

		_healthBarTween.TweenProperty(_healthBarRoot, "modulate", flash, 0.06f);
		_healthBarTween.Chain().TweenProperty(_healthBarRoot, "modulate", neutral, 0.12f);

		Vector2 baseScale = Vector2.One;
		_healthBarTween.TweenProperty(_healthBarRoot, "scale", baseScale * 1.12f, 0.06f);
		_healthBarTween.Chain().TweenProperty(_healthBarRoot, "scale", baseScale, 0.12f);
	}

    public virtual void TakeDamage(int amount) 
    {
        CurrentHealth -= amount;
		if (CurrentHealth < 0)
			CurrentHealth = 0;

		RefreshHealthBar();

		if (amount > 0 && CurrentHealth > 0 && CurrentHealth < MaxHealth)
			PulseHealthBarOnDamage();
    }
}
