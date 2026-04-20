using System.Collections.Generic;
using Godot;

/// <summary>Static run state for a single playthrough from squad select through combat rounds.</summary>
public static class RunSession
{
	public const int PointsPerKill = 50;
	public const int RoundBonusMultiplier = 100;

	public static int CurrentRound { get; private set; } = 1;
	public static int Score { get; private set; }
	public static int Kills { get; private set; }

	private static List<SquadMemberCarryover> _pendingPlayerCarryovers;

	public static bool HasPendingPlayerCarryovers =>
		_pendingPlayerCarryovers != null && _pendingPlayerCarryovers.Count > 0;

	public static void SetPendingPlayerCarryovers(List<SquadMemberCarryover> survivors)
	{
		_pendingPlayerCarryovers = survivors;
	}

	public static List<SquadMemberCarryover> ConsumePendingPlayerCarryovers()
	{
		List<SquadMemberCarryover> copy = _pendingPlayerCarryovers;
		_pendingPlayerCarryovers = null;
		return copy ?? new List<SquadMemberCarryover>();
	}

	public static void ClearPendingCarryovers()
	{
		_pendingPlayerCarryovers = null;
	}

	public static void ResetForNewRun()
	{
		CurrentRound = 1;
		Score = 0;
		Kills = 0;
		ClearPendingCarryovers();
	}

	public static void RegisterEnemyKill()
	{
		Kills++;
		Score += PointsPerKill;
	}

	/// <summary>Award bonus for clearing the current round, then advance to the next round number.</summary>
	public static void RegisterRoundWon()
	{
		Score += RoundBonusMultiplier * CurrentRound;
		CurrentRound++;
	}
}

/// <summary>Living player state carried into the next combat round.</summary>
public struct SquadMemberCarryover
{
	public CharacterClass Class;
	public int CurrentHealth;
	public int MaxHealth;
}
