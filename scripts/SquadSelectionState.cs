using Godot;
using System.Collections.Generic;

public static class SquadSelectionState
{
	public const int SquadSize = 5;

	private static readonly string[] ClassResourcePaths =
	{
		"res://assets/classes/berserker.tres",
		"res://assets/classes/skirmisher.tres",
		"res://assets/classes/bulwark.tres"
	};

	private static List<CharacterClass> _availableClasses;
	private static readonly List<CharacterClass> _selectedSquad = new List<CharacterClass>();

	public static IReadOnlyList<CharacterClass> GetAvailableClasses()
	{
		if (_availableClasses != null)
			return _availableClasses;

		_availableClasses = new List<CharacterClass>();
		foreach (string path in ClassResourcePaths)
		{
			CharacterClass loaded = GD.Load<CharacterClass>(path);
			if (loaded != null)
				_availableClasses.Add(loaded);
		}

		return _availableClasses;
	}

	public static void SetSelectedSquad(IReadOnlyList<CharacterClass> squad)
	{
		_selectedSquad.Clear();
		if (squad == null) return;

		for (int i = 0; i < squad.Count && i < SquadSize; i++)
		{
			if (squad[i] != null)
				_selectedSquad.Add(squad[i]);
		}
	}

	public static List<CharacterClass> GetSelectedOrDefaultSquad()
	{
		List<CharacterClass> result = new List<CharacterClass>();
		IReadOnlyList<CharacterClass> available = GetAvailableClasses();

		if (_selectedSquad.Count > 0)
		{
			for (int i = 0; i < _selectedSquad.Count && result.Count < SquadSize; i++)
			{
				if (_selectedSquad[i] != null)
					result.Add(_selectedSquad[i]);
			}
		}

		if (available.Count == 0)
			return result;

		int fallbackIndex = 0;
		while (result.Count < SquadSize)
		{
			result.Add(available[fallbackIndex % available.Count]);
			fallbackIndex++;
		}

		return result;
	}
}
