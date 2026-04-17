using Godot;

[GlobalClass]
public partial class CharacterClass : Resource
{
	[Export] public string ClassId = "";
	[Export] public string DisplayName = "";
	[Export] public Texture2D SpriteTexture;
	[Export] public int SpriteHFrames = 4;
	[Export] public int MaxHealth = 20;
	[Export] public int BaseSpeed = 5;
	[Export] public int MovementRange = 5;
	[Export] public int AttackRange = 1;
	[Export] public int BaseDamage = 5;
}
