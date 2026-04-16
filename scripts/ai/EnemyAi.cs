using Godot;
using System;
using System.Collections.Generic;

public static class EnemyAi
{
	public enum ActionKind
	{
		AttackNow,
		MoveThenAttack,
		MoveOnly,
		EndTurn
	}

	public readonly struct Decision
	{
		public readonly ActionKind Kind;
		public readonly int MoveToIndex;
		public readonly GridEntity Target;

		public Decision(ActionKind kind, int moveToIndex, GridEntity target)
		{
			Kind = kind;
			MoveToIndex = moveToIndex;
			Target = target;
		}
	}

	// Adjust these to change the AI's behavior
	public sealed class Weights
	{
		public float KillBonus = 100.0f;
		public float DamageWeight = 4.0f;
		public float RetaliationPenaltyWeight = 1.5f;
		public float HpPriorityWeight = 0.4f;
		public float DistanceTieBreakerWeight = 0.25f;
		public float MovementCostPenaltyWeight = 0.1f;
	}

	// Chooses the best action for the enemy based on the weights and the reachable tiles
	public static Decision ChooseAction(
		GridEntity enemy,
		IReadOnlyList<GridEntity> entities,
		IReadOnlyList<int> reachableTiles,
		Func<int, Vector2I> getTilePos,
		Func<int, int> getMovementCost,
		Weights weights = null)
	{
		weights ??= new Weights();

		GridEntity bestTargetHere = SelectBestTargetForEnemy(enemy, enemy.mapindex, entities, getTilePos, weights);
		if (bestTargetHere == null)
			return new Decision(ActionKind.EndTurn, enemy.mapindex, null);

		if (IsInAttackRange(enemy.mapindex, bestTargetHere.mapindex, enemy.AttackRange, getTilePos))
			return new Decision(ActionKind.AttackNow, enemy.mapindex, bestTargetHere);

		int bestTile = enemy.mapindex;
		GridEntity bestTargetAtBestTile = bestTargetHere;
		float bestScore = EvaluateMoveUtility(enemy, bestTargetHere, enemy.mapindex, entities, getTilePos, getMovementCost, weights);

		foreach (int tileIndex in reachableTiles)
		{
			GridEntity targetAtTile = SelectBestTargetForEnemy(enemy, tileIndex, entities, getTilePos, weights);
			if (targetAtTile == null) continue;

			float score = EvaluateMoveUtility(enemy, targetAtTile, tileIndex, entities, getTilePos, getMovementCost, weights);
			if (score > bestScore)
			{
				bestScore = score;
				bestTile = tileIndex;
				bestTargetAtBestTile = targetAtTile;
			}
		}

		if (bestTile == enemy.mapindex)
			return new Decision(ActionKind.EndTurn, enemy.mapindex, bestTargetHere);

		bool willBeInRange = bestTargetAtBestTile != null &&
			IsInAttackRange(bestTile, bestTargetAtBestTile.mapindex, enemy.AttackRange, getTilePos);

		return new Decision(
			willBeInRange ? ActionKind.MoveThenAttack : ActionKind.MoveOnly,
			bestTile,
			bestTargetAtBestTile);
	}

	// Checks if the entity is a player
	private static bool IsPlayer(GridEntity entity) => entity != null && entity.IsPlayer;

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

	// Estimates the risk of retaliation from a player in range of the AI
	private static float EstimateRetaliationRisk(int tileIndex, IReadOnlyList<GridEntity> entities, Func<int, Vector2I> getTilePos)
	{
		float risk = 0.0f;
		foreach (GridEntity entity in entities)
		{
			if (!IsPlayer(entity) || entity.CurrentHealth <= 0) continue;
			if (IsInAttackRange(entity.mapindex, tileIndex, entity.AttackRange, getTilePos))
				risk += entity.BaseDamage;
		}
		return risk;
	}

	// Evaluates the total utility of attacking a target
	private static float EvaluateAttackUtility(
		GridEntity enemy,
		GridEntity target,
		int fromTileIndex,
		IReadOnlyList<GridEntity> entities,
		Func<int, Vector2I> getTilePos,
		Weights weights)
	{
		if (enemy == null || target == null) return float.NegativeInfinity;
		if (!IsInAttackRange(fromTileIndex, target.mapindex, enemy.AttackRange, getTilePos))
			return float.NegativeInfinity;

		int expectedDamage = Math.Min(enemy.BaseDamage, target.CurrentHealth);
		float score = expectedDamage * weights.DamageWeight;
		if (enemy.BaseDamage >= target.CurrentHealth)
			score += weights.KillBonus;

		score -= EstimateRetaliationRisk(fromTileIndex, entities, getTilePos) * weights.RetaliationPenaltyWeight;
		return score;
	}

	// Evaluates the total utility of moving to a target tile
	private static float EvaluateMoveUtility(
		GridEntity enemy,
		GridEntity target,
		int candidateTileIndex,
		IReadOnlyList<GridEntity> entities,
		Func<int, Vector2I> getTilePos,
		Func<int, int> getMovementCost,
		Weights weights)
	{
		if (enemy == null || target == null) return float.NegativeInfinity;

		float score = EvaluateAttackUtility(enemy, target, candidateTileIndex, entities, getTilePos, weights);
		if (float.IsNegativeInfinity(score))
			score = 0.0f;

		int distance = GetManhattanDistance(candidateTileIndex, target.mapindex, getTilePos);
		score -= distance * weights.DistanceTieBreakerWeight;
		score -= getMovementCost(candidateTileIndex) * weights.MovementCostPenaltyWeight;
		return score;
	}

	// Selects the best target for the enemy based on the weights and the reachable tiles
	// The best target is the target that has the highest utility score
	private static GridEntity SelectBestTargetForEnemy(
		GridEntity enemy,
		int fromTileIndex,
		IReadOnlyList<GridEntity> entities,
		Func<int, Vector2I> getTilePos,
		Weights weights)
	{
		GridEntity bestTarget = null;
		float bestScore = float.NegativeInfinity;

		foreach (GridEntity candidate in entities)
		{
			if (!IsPlayer(candidate) || candidate.CurrentHealth <= 0) continue;

			float score = EvaluateMoveUtility(enemy, candidate, fromTileIndex, entities, getTilePos, _ => 0, weights);
			score += (100 - candidate.CurrentHealth) * weights.HpPriorityWeight;

			if (score > bestScore)
			{
				bestScore = score;
				bestTarget = candidate;
			}
		}

		return bestTarget;
	}
}
