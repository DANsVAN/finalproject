using Godot;
using System;

public static class CombatResolver
{
	// Gets the Manhattan distance between two tiles
	// Manhattan distance is the distance between two points in a grid, calculated by the sum of the absolute differences of the x and y coordinates
	private static int GetManhattanDistance(int indexA, int indexB, Func<int, Vector2I> getTilePos)
	{
		Vector2I a = getTilePos(indexA);
		Vector2I b = getTilePos(indexB);
		return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
	}

	// Checks if an attacker is in attack range of a target
	private static bool IsInAttackRange(int attackerIndex, int targetIndex, int attackRange, Func<int, Vector2I> getTilePos)
	{
		return GetManhattanDistance(attackerIndex, targetIndex, getTilePos) <= attackRange;
	}

	// Tries to perform a basic attack between an attacker and a target
	// If the attack is successful, the target takes damage and the onKilled action is called if the target is killed
	public static bool TryBasicAttack(
		GridEntity attacker,
		GridEntity target,
		Func<int, Vector2I> getTilePos,
		Action<GridEntity> onKilled)
	{
		return TryAttack(attacker, target, attacker.AttackRange, attacker.BaseDamage, getTilePos, onKilled);
	}

	public static bool TryAttack(
		GridEntity attacker,
		GridEntity target,
		int attackRange,
		int damage,
		Func<int, Vector2I> getTilePos,
		Action<GridEntity> onKilled)
	{
		if (attacker == null || target == null) return false;
		if (target.CurrentHealth <= 0) return false;
		if (!IsInAttackRange(attacker.mapindex, target.mapindex, attackRange, getTilePos)) return false;

		target.TakeDamage(damage);
		if (target.CurrentHealth <= 0)
			onKilled?.Invoke(target);

		return true;
	}
}
