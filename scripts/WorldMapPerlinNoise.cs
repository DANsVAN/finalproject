using Godot;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

public partial class WorldMapPerlinNoise : Node2D
{
	private enum PlayerTurnMode
	{
		Movement,
		Attack
	}

	private enum AttackPatternKind
	{
		Single,
		Line,
		Cone,
		AoeRadius
	}

	private readonly struct PlayerAttackDefinition
	{
		public readonly string DisplayName;
		public readonly AttackPatternKind Pattern;
		public readonly int MaxRange;
		public readonly int Damage;
		public readonly int LineLength;
		public readonly int ConeDepth;
		public readonly int AoeRadius;

		public PlayerAttackDefinition(string displayName, AttackPatternKind pattern, int maxRange, int damage, int lineLength = 0, int coneDepth = 0, int aoeRadius = 0)
		{
			DisplayName = displayName;
			Pattern = pattern;
			MaxRange = maxRange;
			Damage = damage;
			LineLength = lineLength;
			ConeDepth = coneDepth;
			AoeRadius = aoeRadius;
		}
	}

	// Change to adjust attack stats
	private static readonly PlayerAttackDefinition[] PlayerAttacks =
	{
		new PlayerAttackDefinition("Strike", AttackPatternKind.Single, maxRange: 1, damage: 8),
		new PlayerAttackDefinition("Line", AttackPatternKind.Line, maxRange: 4, damage: 5, lineLength: 4),
		new PlayerAttackDefinition("Blast", AttackPatternKind.AoeRadius, maxRange: 3, damage: 3, aoeRadius: 1),
	};

	[Export] int mapWidthInTiles = 50;
	[Export] int mapHightInTiles = 25;
	[Export] int tileSizeInpixels = 16;
	[Export] double noiseScale = 0.002;
	[Export] double grassThreshold = 0.5;
	[Export] double waterThreshold = 0.6;
	[Export] double snowThreshold = 0.85;
	[Export] double iceThreshold = 0.95;
	[Export] double mountainThreshold = 0.1;
	[Export] float worldNoiseScale = 0.02f;
	[Export] int worldFractalOctaves = 4;
	[Export] float worldFractalLacunarity = 2.0f;
	[Export] float worldFractalGain = 0.5f;
	[Export] public PackedScene EntityPlayerFighter;
	[Export] public PackedScene EntityEnemyFighter;
	private bool isMoving = false; // Prevents clicking while someone is walking
	float[,] worldArray;
	TileMapLayer worldMap;
	TileMapLayer highlightLayer;
	TileMapLayer pathLayer;
	TileMapLayer attackLayer;
	int maxNumberOfTiles;
	Tile[] allTiles;
	GridEntity firstEntityInTheTimeline;
	List<GridEntity> allEntitys = new List<GridEntity> {};
	TimelineOverlay timelineOverlay;
	PlayerAttackController playerAttackUi;

	private PlayerTurnMode _playerMode = PlayerTurnMode.Movement;
	private int _selectedAttackIndex = -1;
	private bool _playerHasMovedThisTurn;
	private bool _playerHasAttackedThisTurn;
	private int _lastAttackHoverIndex = -1;

	private static readonly Vector2I AttackReachAtlas = new Vector2I(1, 0); // snow tile in current tileset
	private static readonly Vector2I AttackPreviewAtlas = new Vector2I(0, 3); // grass tile placeholder
	public bool isplayer = true;
	public int mapStart = 0;
	public int mapMiddle;
	public int mapEnd;



	public struct Tile
	{
		public int index;
		public int[] neighbors;
		public int leftNeighbor;
		public int rightNeighbor;
		public int upNeighbor;
		public int downNeighbor;
		public int movementCost;
		public string tileName;
		public float noiseValue;
		public Vector2I tilePos;
		public Vector2I atlasCoords;
		public GridEntity occupant = null;
		public int parentIndex;

		// Constructor (Optional, but useful)
		public Tile()
		{
			int[] neighbors = new int[4];
		}
	}



	public override void _Ready()
	{
		
		maxNumberOfTiles = mapWidthInTiles * mapHightInTiles;
		allTiles = new Tile[maxNumberOfTiles];
		highlightLayer = GetNode<TileMapLayer>("%HighlightLayer");
		pathLayer = GetNode<TileMapLayer>("%PathLayer");
		attackLayer = GetNode<TileMapLayer>("%AttackLayer");
		timelineOverlay = GetNode<TimelineOverlay>("%TimelineOverlay");
		playerAttackUi = GetNode<PlayerAttackController>("%PlayerAttackUi");

		playerAttackUi.AttackSelected += OnPlayerAttackSelected;
		playerAttackUi.AttackDeselected += OnPlayerAttackDeselected;
		playerAttackUi.EndTurnPressed += OnPlayerEndTurnPressed;
		mapMiddle = (mapWidthInTiles * mapHightInTiles)/2;
		mapEnd = (mapWidthInTiles * mapHightInTiles) - 1;
		bool enemy = false;
		bool player = true;
		makeMap();
		generateNeighborsOfTiles();

		List<CharacterClass> selectedSquad = SquadSelectionState.GetSelectedOrDefaultSquad();
		for (int i = 0; i < selectedSquad.Count; i++)
		{
			SpawnEntity(EntityPlayerFighter, GD.RandRange(mapStart, mapMiddle), player, selectedSquad[i]);
		}

		SpawnEntity(EntityEnemyFighter,GD.RandRange(mapMiddle + 1, mapEnd),enemy);
		SpawnEntity(EntityEnemyFighter,GD.RandRange(mapMiddle + 1, mapEnd),enemy);
		SpawnEntity(EntityEnemyFighter,GD.RandRange(mapMiddle + 1, mapEnd),enemy);
		SpawnEntity(EntityEnemyFighter,GD.RandRange(mapMiddle + 1, mapEnd),enemy);
		PositionAllEntintys();
		takeTurn();
	}
	public FastNoiseLite generateRandNoise()
	{
		var noise = new FastNoiseLite();
		noise.Seed = (int)GD.Randi();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		noise.Frequency = worldNoiseScale;
		noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
		noise.FractalOctaves = worldFractalOctaves; // Higher = more detail/jagged edges
		noise.FractalLacunarity = worldFractalLacunarity; 
		noise.FractalGain = worldFractalGain;
		return(noise);
	}



public void generateMapFromNoise(FastNoiseLite noise)
	{
		int currentTileIndex = 0;
		worldMap.Clear();
		worldArray = new float[mapWidthInTiles , mapHightInTiles];
		for (int hight = 0; hight < mapHightInTiles; hight++)
		{
			for (int width = 0; width < mapWidthInTiles; width++)
			{
				
				float noiseValueOfTile = noise.GetNoise2D(width,hight);
				noiseValueOfTile = (noiseValueOfTile + 1) / 2; // noiseValueOfTile = (noiseValueOfTile + 1) / 2; this is makeing it gen a number from 0 to 1
				Vector2I tilePos = new(width,  hight );
				allTiles[currentTileIndex].index = currentTileIndex;
				allTiles[currentTileIndex].noiseValue = noiseValueOfTile;
				allTiles[currentTileIndex].tilePos = tilePos;
				if (noiseValueOfTile < waterThreshold) // 0.0 to 0.3
				{
					allTiles[currentTileIndex].atlasCoords = new Vector2I(3, 0);
					allTiles[currentTileIndex].tileName = "water";
					allTiles[currentTileIndex].movementCost = 2;
				}
				else if (noiseValueOfTile < grassThreshold) // 0.3 to 0.8 (Large Range = Lots of Grass)
				{
					allTiles[currentTileIndex].atlasCoords = new Vector2I(0, 3);
					allTiles[currentTileIndex].tileName = "grass";
					allTiles[currentTileIndex].movementCost = 1;
				}
				else if (noiseValueOfTile < snowThreshold) // 0.8 to 0.9
				{
					allTiles[currentTileIndex].atlasCoords = new Vector2I(1, 0);
					allTiles[currentTileIndex].tileName = "snow";
					allTiles[currentTileIndex].movementCost = 3;
				}
				else if (noiseValueOfTile < iceThreshold) // 0.9 to 0.95
				{
					allTiles[currentTileIndex].atlasCoords = new Vector2I(0, 1);
					allTiles[currentTileIndex].tileName = "ice";
					allTiles[currentTileIndex].movementCost = 4;
				}
				else // 0.95 to 1.0 (Smallest Range = Rare Mountains)
				{
					allTiles[currentTileIndex].atlasCoords = new Vector2I(0, 2);
					allTiles[currentTileIndex].tileName = "mountain";
					allTiles[currentTileIndex].movementCost = 999999;
				}
				worldMap.SetCell(allTiles[currentTileIndex].tilePos,0,allTiles[currentTileIndex].atlasCoords);
				currentTileIndex ++;
			}
		}
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
private List<int> currentReachableTiles = new List<int>();
private int lastHoveredIndex = -1;

public override void _Process(double delta)
{
	// Only allow movement preview and click input on player turns.
	if (isMoving || firstEntityInTheTimeline == null || !firstEntityInTheTimeline.IsPlayer)
	{
		pathLayer.Clear();
		attackLayer.Clear();
		lastHoveredIndex = -1;
		_lastAttackHoverIndex = -1;
		return;
	}

	// 1. Get Mouse Position relative to the TileMap
	Vector2 mousePos = worldMap.GetLocalMousePosition();
	Vector2I gridPos = worldMap.LocalToMap(mousePos);
	
	// Convert 2D Grid Pos to 1D Index
	int hoverIndex = gridPos.Y * mapWidthInTiles + gridPos.X;

	// Safety check: Is mouse inside map bounds?
	if (gridPos.X < 0 || gridPos.X >= mapWidthInTiles || gridPos.Y < 0 || gridPos.Y >= mapHightInTiles) 
		return;

	if (_playerMode == PlayerTurnMode.Movement)
	{
		// 2. Handle HOVER (purple Path)
		if (hoverIndex != lastHoveredIndex)
		{
			lastHoveredIndex = hoverIndex;
			if (!_playerHasMovedThisTurn && currentReachableTiles.Contains(hoverIndex))
			{
				var path = GetPathToTarget(hoverIndex, firstEntityInTheTimeline.mapindex);
				HighlightPath(path);
			}
			else
			{
				pathLayer.Clear();
			}
		}

		// 3. Handle CLICK (Move Entity)
		if (Input.IsActionJustPressed("left_mouse_click"))
		{
			if (!_playerHasMovedThisTurn && currentReachableTiles.Contains(hoverIndex))
				MoveEntity(firstEntityInTheTimeline, hoverIndex, OnPlayerMoveFinished);
		}
	}
	else if (_playerMode == PlayerTurnMode.Attack && _selectedAttackIndex >= 0 && _selectedAttackIndex < PlayerAttacks.Length)
	{
		PlayerAttackDefinition attack = PlayerAttacks[_selectedAttackIndex];
		List<int> attackReach = GetTilesWithinAttackRange(firstEntityInTheTimeline.mapindex, attack.MaxRange);

		if (hoverIndex != _lastAttackHoverIndex)
		{
			_lastAttackHoverIndex = hoverIndex;
			DrawAttackReachTiles(attackReach);

			if (attackReach.Contains(hoverIndex))
			{
				List<int> preview = BuildAttackPatternTiles(firstEntityInTheTimeline, hoverIndex, attack);
				DrawAttackPreviewTiles(preview);
			}
			else
			{
				DrawAttackReachTiles(attackReach);
			}
		}

		if (Input.IsActionJustPressed("left_mouse_click"))
		{
			if (!_playerHasAttackedThisTurn && attackReach.Contains(hoverIndex))
			{
				List<int> affected = BuildAttackPatternTiles(firstEntityInTheTimeline, hoverIndex, attack);
				if (ContainsEnemyInTiles(affected))
					TryExecutePlayerAttack(firstEntityInTheTimeline, attack, affected);
			}
		}
	}
}
public void HighlightPath(List<int> path)
{
	// 1. Clear the purple path from the last frame
	pathLayer.Clear();

	// 2. Use the same tile coordinates as your green highlights
	Vector2I highlightAtlasCoord = new Vector2I(0, 3); 

	// 3. Draw the purple tiles
	foreach (int index in path)
	{
		Vector2I gridPos = allTiles[index].tilePos;
		pathLayer.SetCell(gridPos, 0, highlightAtlasCoord);
	}
}
	private void CenterMap()
	{
		// 1. Get the current viewport/screen size in pixels
		Vector2 screenSize = GetViewportRect().Size;

		// 2. Calculate world size in pixels
		Vector2 worldPixelSize = new Vector2(
			mapWidthInTiles * tileSizeInpixels,
			mapHightInTiles * tileSizeInpixels
		);

		// 3. Calculate the centering offset
		// We cast to int and use Math.Floor to keep pixels perfectly aligned
		float offsetX = (float)Math.Floor((screenSize.X - worldPixelSize.X) / 2.0f);
		float offsetY = (float)Math.Floor((screenSize.Y - worldPixelSize.Y) / 2.0f);
		GlobalPosition = new Vector2(offsetX, offsetY);
	}
	public void makeMap()
	{
		CenterMap();
		worldMap = GetNode<TileMapLayer>("%WorldMapLayer");
		generateMapFromNoise(generateRandNoise());
	}
public void generateNeighborsOfTiles()
{   
	foreach (ref Tile tile in allTiles.AsSpan())
	{
		tile.neighbors = new int[4]; 

		int x = tile.index % mapWidthInTiles; // Column
		int y = tile.index / mapWidthInTiles; // Row

		// --- LEFT Neighbor ---
		// If x is 0, there is no one to the left.
		if (x > 0) 
			tile.neighbors[0] = tile.index - 1;
		else 
			tile.neighbors[0] = -1;

		// --- RIGHT Neighbor ---
		// If x is at mapWidth - 1, there is no one to the right.
		if (x < mapWidthInTiles - 1) 
			tile.neighbors[1] = tile.index + 1;
		else 
			tile.neighbors[1] = -1;

		// --- UP Neighbor ---
		// If y is 0, we are at the top row.
		if (y > 0) 
			tile.neighbors[2] = tile.index - mapWidthInTiles;
		else 
			tile.neighbors[2] = -1;

		// --- DOWN Neighbor ---
		// If y is at mapHeight - 1, we are at the bottom row.
		if (y < mapHightInTiles - 1) 
			tile.neighbors[3] = tile.index + mapWidthInTiles;
		else 
			tile.neighbors[3] = -1;
			
		// Sync the struct fields for backward compatibility with your code
		tile.leftNeighbor = tile.neighbors[0];
		tile.rightNeighbor = tile.neighbors[1];
		tile.upNeighbor = tile.neighbors[2];
		tile.downNeighbor = tile.neighbors[3];
	}
}

public void SpawnEntity(PackedScene PackedEntityScene, int mapIndex, bool player, CharacterClass playerClass = null)
{
	// 1. Safety Check: If index is out of bounds, wrap it
	int index = mapIndex % allTiles.Length;

	// 2. The "Bad Tile" Condition
	bool isMountain = allTiles[index].tileName == "mountain";
	bool isOccupied = allTiles[index].occupant != null;

	if (isMountain || isOccupied)
	{
			if (player)
			{
					SpawnEntity(EntityPlayerFighter, GD.RandRange(mapStart, mapMiddle), player, playerClass);
			}
			else
			{
					SpawnEntity(EntityEnemyFighter, GD.RandRange(mapMiddle + 1, mapEnd), player);
			}
		return; 
	}

	// 4. The "Good Tile" Base Case: Actually spawn the entity
	var entityNode = PackedEntityScene.Instantiate<Node2D>();
	AddChild(entityNode);

	if (entityNode is GridEntity gridEntity)
	{
		allTiles[index].occupant = gridEntity;
		gridEntity.mapindex = index;
		gridEntity.Node2DEntity = entityNode;
		allEntitys.Add(gridEntity);
		
		// Ensure sprite reference is set (since _Ready might not have fired yet)
		gridEntity.sprite = entityNode.GetNode<Sprite2D>("Sprite2D");
		if (player)
		{
			CharacterClass classDef = playerClass;
			if (classDef != null)
				gridEntity.ApplyCharacterClass(classDef, true);
			else
				gridEntity.IsPlayer = true;
		}
		else
		{
			gridEntity.IsPlayer = false;
		}
	}
}

public void PositionAllEntintys()
{
	foreach (GridEntity entity in allEntitys)
{
	Vector2 pixlePosition = new Vector2((1 * allTiles[entity.mapindex].tilePos.X * allTiles[entity.mapindex].occupant.spriteSize) + (allTiles[entity.mapindex].occupant.spriteSize / 2) ,(1 * allTiles[entity.mapindex].tilePos.Y  * allTiles[entity.mapindex].occupant.spriteSize) + (allTiles[entity.mapindex].occupant.spriteSize / 2));
	entity.Node2DEntity.Position = pixlePosition;
}
}
public void updateTimeline()
{
	if (allEntitys.Count == 0) return;

	// 1. Find the entity who CURRENTLY has the highest speed 
	// before we add this round's speed boost.
	firstEntityInTheTimeline = allEntitys[0];
	foreach (GridEntity entity in allEntitys)
	{
		if (entity.CurrentSpeed > firstEntityInTheTimeline.CurrentSpeed)
		{
			firstEntityInTheTimeline = entity;
		}
	}

	// 2. Process everyone based on whether they won or lost the timeline order
	foreach (GridEntity entity in allEntitys)
	{
		if (entity == firstEntityInTheTimeline)
		{
			entity.CurrentSpeed = entity.BaseSpeed; 
			entity.sprite.Modulate = Colors.Green;
		}
		else
		{
			entity.CurrentSpeed += entity.BaseSpeed;
			entity.sprite.Modulate = Colors.White;
		}
	}

	UpdateTimelineOverlay();
}

// Updates the timeline overlay to show the current timeline with enemies and players
private void UpdateTimelineOverlay()
{
	if (timelineOverlay == null || firstEntityInTheTimeline == null) return;
	List<GridEntity> preview = BuildTimelinePreview(4);
	timelineOverlay.SetQueue(preview);
}

// Builds the order in which entities will take a turn on the timeline, gets sent to the timeline overlay display
private List<GridEntity> BuildTimelinePreview(int count)
{
	// 1. Safety Check: If there are no entities or the first entity is null, return an empty list
	List<GridEntity> preview = new List<GridEntity>();
	if (count <= 0 || allEntitys.Count == 0 || firstEntityInTheTimeline == null) return preview;

	// 2. Add the first entity to the preview
	preview.Add(firstEntityInTheTimeline);
	if (count == 1) return preview;

	// Only one combatant: repeat them to fill timeline slots (overlay uses one icon per slot).
	if (allEntitys.Count == 1)
	{
		while (preview.Count < count)
			preview.Add(firstEntityInTheTimeline);
		return preview;
	}

	// 3. Create a list of all entities to simulate
	List<GridEntity> simulationUnits = new List<GridEntity>(allEntitys);
	Dictionary<GridEntity, int> simulatedSpeeds = new Dictionary<GridEntity, int>();
	foreach (GridEntity entity in simulationUnits)
		simulatedSpeeds[entity] = entity.CurrentSpeed;

	// Track who is already shown so the same unit does not occupy two preview slots
	// (the overlay historically used one node per entity id).
	HashSet<GridEntity> inPreview = new HashSet<GridEntity>();
	foreach (GridEntity e in preview)
		inPreview.Add(e);

	// 4. Loop through simulated turns until the preview is full. If the simulated next actor
	// is already in the preview, advance ATB without appending so another unit can appear.
	int safety = 0;
	const int maxSafety = 256;
	while (preview.Count < count && safety++ < maxSafety)
	{
		GridEntity next = simulationUnits[0];
		foreach (GridEntity entity in simulationUnits)
		{
			if (simulatedSpeeds[entity] > simulatedSpeeds[next])
				next = entity;
		}

		if (inPreview.Contains(next))
		{
			foreach (GridEntity entity in simulationUnits)
			{
				if (entity == next)
					simulatedSpeeds[entity] = entity.BaseSpeed;
				else
					simulatedSpeeds[entity] += entity.BaseSpeed;
			}
			continue;
		}

		preview.Add(next);
		inPreview.Add(next);

		foreach (GridEntity entity in simulationUnits)
		{
			if (entity == next)
				simulatedSpeeds[entity] = entity.BaseSpeed;
			else
				simulatedSpeeds[entity] += entity.BaseSpeed;
		}
	}

	return preview;
}
public List<int> GetReachableTiles(int startIndex, int movementRange)
{
	// Dictionary to track the lowest cost to reach each tile
	Dictionary<int, int> costSoFar = new Dictionary<int, int>();
	
	// Priority queue to explore cheapest paths first (TileIndex, TotalCost)
	PriorityQueue<int, int> frontier = new PriorityQueue<int, int>();

	// Add the starting tile
	frontier.Enqueue(startIndex, 0);
	costSoFar[startIndex] = 0;

	while (frontier.Count > 0)
	{
		int current = frontier.Dequeue();

		// Check all 4 neighbors
		foreach (int next in allTiles[current].neighbors)
		{
			// Skip invalid (out of bounds) neighbors
			if (next == -1) continue;

			// PREVENT WALKING THROUGH IMPASSABLE TERRAIN
			// You can adjust these string names based on your tile types
			if (allTiles[next].tileName == "mountain") 
				continue;

			// PREVENT WALKING THROUGH OTHER ENTITIES
			if (allTiles[next].occupant != null && next != startIndex) 
				continue;

			// Calculate cost to step on this tile. 
			// If movementCost isn't set, default to 1 to prevent freezing.
			int costToMove = allTiles[next].movementCost > 0 ? allTiles[next].movementCost : 1;
			
			int newCost = costSoFar[current] + costToMove;

			// If the total cost is within range, see if we should add it
			if (newCost <= movementRange)
			{
				// If we haven't visited this tile yet, OR we found a cheaper way to get here
				if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
				{
					costSoFar[next] = newCost;
					allTiles[next].parentIndex = current;
					frontier.Enqueue(next, newCost); // Add to queue to explore its neighbors later
				}
			}
		}
	}

	// Convert the visited dictionary keys into a list of reachable tile indexes
	return new List<int>(costSoFar.Keys);
}
public void HighlightReachableTiles(List<int> reachableIndices)
{
	// 1. Clear out any highlights from the previous turn
	highlightLayer.Clear();

	// 2. The Atlas Coordinates of the tile you want to use for highlighting.
	// Change this to the X,Y coordinates of a plain white tile in your TileSet!
	// For now, I'm using your grass tile coordinates (0, 3) as a placeholder.
	Vector2I highlightAtlasCoord = new Vector2I(0, 3); 

	// 3. Loop through every reachable tile and place a highlight block there
	foreach (int index in reachableIndices)
	{
		Vector2I gridPos = allTiles[index].tilePos;
		
		// Place the tile on the highlight layer (0 is the source ID of your tileset)
		highlightLayer.SetCell(gridPos, 0, highlightAtlasCoord);
	}
}

// When the player selects an attack, the attack is highlighted and the player can attack the target tile
private void OnPlayerAttackSelected(int attackIndex)
{
	if (!IsPlayerUnit(firstEntityInTheTimeline)) return;
	if (_playerHasAttackedThisTurn) return;

	_playerMode = PlayerTurnMode.Attack;
	_selectedAttackIndex = attackIndex;

	pathLayer.Clear();
	highlightLayer.Clear();
	lastHoveredIndex = -1;
	_lastAttackHoverIndex = -1;

	PlayerAttackDefinition selectedAttack = PlayerAttacks[attackIndex];
	List<int> attackReach = GetTilesWithinAttackRange(firstEntityInTheTimeline.mapindex, selectedAttack.MaxRange);
	DrawAttackReachTiles(attackReach);

	RefreshPlayerMovementHighlights();
}

// When the player deselects an attack, the attack is cleared and the player can move again
private void OnPlayerAttackDeselected()
{
	if (!IsPlayerUnit(firstEntityInTheTimeline)) return;

	_playerMode = PlayerTurnMode.Movement;
	_selectedAttackIndex = -1;

	attackLayer.Clear();
	pathLayer.Clear();
	lastHoveredIndex = -1;
	_lastAttackHoverIndex = -1;

	RefreshPlayerMovementHighlights();
}

// When the player ends their turn, the attack is cleared and the player can move again
private void OnPlayerEndTurnPressed()
{
	if (!IsPlayerUnit(firstEntityInTheTimeline)) return;
	if (isMoving) return;

	ExitAttackMode(clearSelection: true);
	highlightLayer.Clear();
	pathLayer.Clear();
	attackLayer.Clear();
	currentReachableTiles.Clear();
	takeTurn();
}

// When the player begins their turn, the attack is cleared and the player can move again
private void BeginPlayerTurn(GridEntity player)
{
	playerAttackUi?.SetTurnActive(true);
	playerAttackUi?.SetBusy(false);
	playerAttackUi?.ClearAttackSelection();
	playerAttackUi?.SetAttackSelectionLocked(false);

	_playerMode = PlayerTurnMode.Movement;
	_selectedAttackIndex = -1;
	_playerHasMovedThisTurn = false;
	_playerHasAttackedThisTurn = false;

	attackLayer.Clear();
	pathLayer.Clear();
	lastHoveredIndex = -1;
	_lastAttackHoverIndex = -1;

	currentReachableTiles = GetReachableTiles(player.mapindex, player.MovementRange);
	HighlightReachableTiles(currentReachableTiles);
}

// When the player finishes moving, the path is cleared and the player can attack again
private void OnPlayerMoveFinished()
{
	_playerHasMovedThisTurn = true;
	pathLayer.Clear();
	lastHoveredIndex = -1;

	if (IsPlayerUnit(firstEntityInTheTimeline))
	{
		playerAttackUi?.SetBusy(false);
		RefreshPlayerMovementHighlights();

		if (_playerMode == PlayerTurnMode.Attack && _selectedAttackIndex >= 0 && _selectedAttackIndex < PlayerAttacks.Length && !_playerHasAttackedThisTurn)
		{
			PlayerAttackDefinition attack = PlayerAttacks[_selectedAttackIndex];
			List<int> attackReach = GetTilesWithinAttackRange(firstEntityInTheTimeline.mapindex, attack.MaxRange);
			DrawAttackReachTiles(attackReach);

			if (_lastAttackHoverIndex >= 0 && attackReach.Contains(_lastAttackHoverIndex))
			{
				List<int> preview = BuildAttackPatternTiles(firstEntityInTheTimeline, _lastAttackHoverIndex, attack);
				DrawAttackPreviewTiles(preview);
			}
		}
	}
}

// Refreshes the player movement highlights
private void RefreshPlayerMovementHighlights()
{
	if (!IsPlayerUnit(firstEntityInTheTimeline)) return;

	if (_playerMode == PlayerTurnMode.Attack)
	{
		highlightLayer.Clear();
		currentReachableTiles.Clear();
		return;
	}

	if (_playerHasMovedThisTurn)
	{
		highlightLayer.Clear();
		currentReachableTiles.Clear();
		return;
	}

	currentReachableTiles = GetReachableTiles(firstEntityInTheTimeline.mapindex, firstEntityInTheTimeline.MovementRange);
	HighlightReachableTiles(currentReachableTiles);
}

// Exits the attack mode and clears the attack layer
private void ExitAttackMode(bool clearSelection)
{
	attackLayer.Clear();
	_playerMode = PlayerTurnMode.Movement;
	_selectedAttackIndex = -1;
	_lastAttackHoverIndex = -1;

	if (clearSelection)
		playerAttackUi?.ClearAttackSelection();

	playerAttackUi?.SetAttackSelectionLocked(_playerHasAttackedThisTurn);
	RefreshPlayerMovementHighlights();
}

// Gets the Manhattan distance between two tiles
// Manhattan distance is the distance between two points in a grid, calculated by the sum of the absolute differences of the x and y coordinates
private int GetManhattanTileDistance(int indexA, int indexB)
{
	Vector2I a = GetTilePos(indexA);
	Vector2I b = GetTilePos(indexB);
	return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
}

// Attack targeting range: each orthogonal step costs 1 (terrain movementCost is ignored).
// Mountains remain impassable for targeting, matching movement blocking.
private List<int> GetTilesWithinAttackRange(int startIndex, int maxSteps)
{
	Dictionary<int, int> costSoFar = new Dictionary<int, int>();
	PriorityQueue<int, int> frontier = new PriorityQueue<int, int>();

	frontier.Enqueue(startIndex, 0);
	costSoFar[startIndex] = 0;

	while (frontier.Count > 0)
	{
		int current = frontier.Dequeue();

		foreach (int next in allTiles[current].neighbors)
		{
			if (next == -1) continue;
			if (allTiles[next].tileName == "mountain")
				continue;

			const int stepCost = 1;
			int newCost = costSoFar[current] + stepCost;

			if (newCost <= maxSteps)
			{
				if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
				{
					costSoFar[next] = newCost;
					frontier.Enqueue(next, newCost);
				}
			}
		}
	}

	return new List<int>(costSoFar.Keys);
}

// Draws the attack reach tiles to the attack layer
private void DrawAttackReachTiles(List<int> reachableIndices)
{
	attackLayer.Clear();

	foreach (int index in reachableIndices)
	{
		Vector2I gridPos = allTiles[index].tilePos;
		attackLayer.SetCell(gridPos, 0, AttackReachAtlas);
	}
}

// Draws the attack preview tiles to the attack layer
private void DrawAttackPreviewTiles(List<int> previewIndices)
{
	foreach (int index in previewIndices)
	{
		Vector2I gridPos = allTiles[index].tilePos;
		attackLayer.SetCell(gridPos, 0, AttackPreviewAtlas);
	}
}

// Builds the attack pattern tiles for the attack layer
private List<int> BuildAttackPatternTiles(GridEntity attacker, int originIndex, PlayerAttackDefinition attack)
{
	List<int> tiles = new List<int>();
	if (attacker == null) return tiles;

	switch (attack.Pattern)
	{
		case AttackPatternKind.Single:
			tiles.Add(originIndex);
			break;

		case AttackPatternKind.Line:
			AddLinePatternTiles(attacker.mapindex, originIndex, attack.LineLength, attack.MaxRange, tiles);
			break;

		case AttackPatternKind.Cone:
			AddConePatternTiles(attacker.mapindex, originIndex, attack.ConeDepth, tiles);
			break;

		case AttackPatternKind.AoeRadius:
			AddAoeRadiusTiles(originIndex, attack.AoeRadius, tiles);
			break;
	}

	return tiles;
}

// Adds the line attack pattern tiles to the attack layer.
// Cardinal aim: steps along one axis. Diagonal aim: each step moves both axes (e.g. down-left each time).
// Stops before tiles outside maxRangeFromAttacker (same metric as GetTilesWithinAttackRange) so a
// diagonal beam cannot extend past where the attack is allowed to reach.
private void AddLinePatternTiles(int attackerIndex, int originIndex, int maxLength, int maxRangeFromAttacker, List<int> output)
{
	Vector2I from = GetTilePos(attackerIndex);
	Vector2I to = GetTilePos(originIndex);
	int dx = Math.Sign(to.X - from.X);
	int dy = Math.Sign(to.Y - from.Y);

	if (dx == 0 && dy == 0)
		return;

	HashSet<int> inAttackRange = new HashSet<int>(GetTilesWithinAttackRange(attackerIndex, maxRangeFromAttacker));

	int currentX = from.X;
	int currentY = from.Y;

	for (int step = 0; step < maxLength; step++)
	{
		currentX += dx;
		currentY += dy;

		if (currentX < 0 || currentX >= mapWidthInTiles || currentY < 0 || currentY >= mapHightInTiles)
			break;

		int idx = currentY * mapWidthInTiles + currentX;
		if (allTiles[idx].tileName == "mountain")
			break;

		if (!inAttackRange.Contains(idx))
			break;

		output.Add(idx);
	}
}

// Adds the cone attack pattern tiles to the attack layer
private void AddConePatternTiles(int attackerIndex, int originIndex, int depth, List<int> output)
{
	Vector2I a = GetTilePos(attackerIndex);
	Vector2I o = GetTilePos(originIndex);

	int dx = Math.Sign(o.X - a.X);
	int dy = Math.Sign(o.Y - a.Y);
	if (dx == 0 && dy == 0)
		return;

	if (dx != 0 && dy != 0)
	{
		AddConeCardinal(attackerIndex, new Vector2I(dx, 0), depth, output);
		AddConeCardinal(attackerIndex, new Vector2I(0, dy), depth, output);
		return;
	}

	Vector2I dir = new Vector2I(dx, dy);
	int perpX = -dir.Y;
	int perpY = dir.X;

	for (int d = 1; d <= depth; d++)
	{
		int baseX = a.X + dir.X * d;
		int baseY = a.Y + dir.Y * d;

		for (int s = -d; s <= d; s++)
		{
			int x = baseX + perpX * s;
			int y = baseY + perpY * s;

			if (x < 0 || x >= mapWidthInTiles || y < 0 || y >= mapHightInTiles)
				continue;

			int idx = y * mapWidthInTiles + x;
			if (allTiles[idx].tileName == "mountain")
				continue;

			output.Add(idx);
		}
	}
}

// Adds the cone cardinal attack pattern tiles to the attack layer
private void AddConeCardinal(int attackerIndex, Vector2I dir, int depth, List<int> output)
{
	Vector2I a = GetTilePos(attackerIndex);
	int perpX = -dir.Y;
	int perpY = dir.X;

	for (int d = 1; d <= depth; d++)
	{
		int baseX = a.X + dir.X * d;
		int baseY = a.Y + dir.Y * d;

		for (int s = -d; s <= d; s++)
		{
			int x = baseX + perpX * s;
			int y = baseY + perpY * s;

			if (x < 0 || x >= mapWidthInTiles || y < 0 || y >= mapHightInTiles)
				continue;

			int idx = y * mapWidthInTiles + x;
			if (allTiles[idx].tileName == "mountain")
				continue;

			output.Add(idx);
		}
	}
}

// Adds the AOE radius attack pattern tiles to the attack layer
private void AddAoeRadiusTiles(int centerIndex, int radius, List<int> output)
{
	Vector2I center = GetTilePos(centerIndex);

	for (int y = center.Y - radius; y <= center.Y + radius; y++)
	{
		for (int x = center.X - radius; x <= center.X + radius; x++)
		{
			if (x < 0 || x >= mapWidthInTiles || y < 0 || y >= mapHightInTiles)
				continue;

			if (GetManhattanTileDistance(centerIndex, y * mapWidthInTiles + x) > radius)
				continue;

			int idx = y * mapWidthInTiles + x;
			if (allTiles[idx].tileName == "mountain")
				continue;

			output.Add(idx);
		}
	}
}

// Checks if the list of tiles contains an enemy
private bool ContainsEnemyInTiles(List<int> tiles)
{
	foreach (int idx in tiles)
	{
		GridEntity occ = allTiles[idx].occupant;
		if (IsEnemy(occ))
			return true;
	}

	return false;
}

private void TryExecutePlayerAttack(GridEntity player, PlayerAttackDefinition attack, List<int> affectedTiles)
{
	if (player == null || _playerHasAttackedThisTurn) return;

	foreach (int idx in affectedTiles)
	{
		GridEntity target = allTiles[idx].occupant;
		if (!IsEnemy(target)) continue;

		CombatResolver.TryAttack(player, target, attack.MaxRange, attack.Damage, GetTilePos, RemoveEntity);
	}

	_playerHasAttackedThisTurn = true;
	attackLayer.Clear();
	pathLayer.Clear();
	lastHoveredIndex = -1;
	_lastAttackHoverIndex = -1;

	playerAttackUi?.SetAttackSelectionLocked(true);
	_playerMode = PlayerTurnMode.Movement;
	_selectedAttackIndex = -1;
	playerAttackUi?.ClearAttackSelection();

	RefreshPlayerMovementHighlights();
}

// Checks if an entity is an enemy
private bool IsEnemy(GridEntity entity)
{
	return entity != null && !entity.IsPlayer;
}

// Checks if an entity is a player
private bool IsPlayerUnit(GridEntity entity)
{
	return entity != null && entity.IsPlayer;
}

// Get the position of a tile within the grid
private Vector2I GetTilePos(int index)
{
	return allTiles[index].tilePos;
}

// Gets the movement cost of a tile based on the tile's type
private int GetMovementCost(int index)
{
	return allTiles[index].movementCost;
}

// Removes an entity from the grid
private void RemoveEntity(GridEntity entity)
{
	if (entity == null) return;

	if (entity.mapindex >= 0 && entity.mapindex < allTiles.Length && allTiles[entity.mapindex].occupant == entity)
		allTiles[entity.mapindex].occupant = null;

	allEntitys.Remove(entity);
	entity.Node2DEntity?.QueueFree();
}

// Checks if either all players or all enemies are dead
private bool IsCombatOver()
{
	bool hasPlayers = false;
	bool hasEnemies = false;

	foreach (GridEntity entity in allEntitys)
	{
		if (IsPlayerUnit(entity)) hasPlayers = true;
		if (IsEnemy(entity)) hasEnemies = true;
	}

	return !hasPlayers || !hasEnemies;
}

// Executes an enemy's turn
private void ExecuteEnemyTurn(GridEntity enemy)
{
	// 1. Get the reachable tiles for the enemy
	List<int> reachableTiles = GetReachableTiles(enemy.mapindex, enemy.MovementRange);

	// 2. Choose an action for the enemy based on the best utility score
	EnemyAi.Decision decision = EnemyAi.ChooseAction(
		enemy,
		allEntitys,
		reachableTiles,
		GetTilePos,
		GetMovementCost);

	// 3. If the enemy is ending their turn or doesn't have a target, end their turn
	if (decision.Kind == EnemyAi.ActionKind.EndTurn || decision.Target == null)
	{
		takeTurn();
		return;
	}

	// 4. If the enemy is attacking, perform the attack and end their turn
	if (decision.Kind == EnemyAi.ActionKind.AttackNow)
	{
		CombatResolver.TryBasicAttack(enemy, decision.Target, GetTilePos, RemoveEntity);
		takeTurn();
		return;
	}

	// 5. If the enemy is moving, move them to the target tile and end their turn
	MoveEntity(enemy, decision.MoveToIndex, () =>
	{
		if (IsCombatOver())
		{
			highlightLayer.Clear();
			pathLayer.Clear();
			currentReachableTiles.Clear();
			return;
		}

		// 6. If the enemy is moving, choose an action for the enemy based on the best utility score
		EnemyAi.Decision postMoveDecision = EnemyAi.ChooseAction(
			enemy,
			allEntitys,
			Array.Empty<int>(),
			GetTilePos,
			GetMovementCost);

		// 7. If the enemy is attacking, perform the attack and end their turn
		if (postMoveDecision.Kind == EnemyAi.ActionKind.AttackNow && postMoveDecision.Target != null)
			CombatResolver.TryBasicAttack(enemy, postMoveDecision.Target, GetTilePos, RemoveEntity);

		// 8. End the enemy's turn
		takeTurn();
	});
}

public async void takeTurn() // Mark as async to allow a small delay for "thinking"
{
	// Checks if combat is over and clears the highlight and path layers
	if (IsCombatOver())
	{
		highlightLayer.Clear();
		pathLayer.Clear();
		attackLayer.Clear();
		currentReachableTiles.Clear();
		playerAttackUi?.SetTurnActive(false);
		return;
	}

	updateTimeline();
	GridEntity activeUnit = firstEntityInTheTimeline;
	if (activeUnit == null) return;

	if (IsPlayerUnit(activeUnit))
	{
		BeginPlayerTurn(activeUnit);
	}
	else
	{
		// Enemy's turn: AI Logic
		currentReachableTiles.Clear();
		highlightLayer.Clear();
		pathLayer.Clear();
		attackLayer.Clear();
		playerAttackUi?.SetTurnActive(false);
		
		// Small delay so the player can see who is moving
		await ToSignal(GetTree().CreateTimer(0.5), "timeout");

		ExecuteEnemyTurn(activeUnit);
	}
}

public List<int> GetPathToTarget(int targetIndex, int startIndex)
{
	List<int> path = new List<int>();
	int current = targetIndex;

	while (current != startIndex)
	{
		path.Add(current);
		current = allTiles[current].parentIndex;
	}
	path.Reverse(); // Make it go from Start -> Target
	return path;
}

public void MoveEntity(GridEntity entity, int newIndex, Action onMoveComplete = null)
{
	if (isMoving) return;

	if (IsPlayerUnit(entity))
		playerAttackUi?.SetBusy(true);
	
	List<int> path = GetPathToTarget(newIndex, entity.mapindex);
	if (path.Count == 0)
	{
		highlightLayer.Clear();
		pathLayer.Clear();
		attackLayer.Clear();
		currentReachableTiles.Clear();
		if (onMoveComplete != null)
			onMoveComplete();
		else if (!IsPlayerUnit(entity))
			takeTurn();
		else
			OnPlayerMoveFinished();
		return;
	}

	isMoving = true;

	Path2D pathNode = new Path2D();
	PathFollow2D follower = new PathFollow2D();
	pathNode.TopLevel = true;
	follower.Loop = false;
	follower.Rotates = false; 

	AddChild(pathNode);
	pathNode.AddChild(follower);

	Curve2D curve = new Curve2D();
	
	// --- FIX STARTS HERE ---
	// Don't use GlobalPosition directly. 
	// Use the TileIndexToWorldPos for the STARTING tile too.
	curve.AddPoint(TileIndexToWorldPos(entity.mapindex));

	foreach (int index in path)
	{
		curve.AddPoint(TileIndexToWorldPos(index));
	}
	// --- FIX ENDS HERE ---

	pathNode.Curve = curve;

	RemoteTransform2D remote = new RemoteTransform2D();
	remote.RemotePath = entity.Node2DEntity.GetPath();
	remote.UpdateRotation = false; 
	follower.AddChild(remote);

	float duration = path.Count * 0.15f; 
	Tween tween = GetTree().CreateTween();
	
	tween.TweenProperty(follower, "progress_ratio", 1.0f, duration)
 		.SetTrans(Tween.TransitionType.Quad)
 		.SetEase(Tween.EaseType.InOut); 

	tween.Finished += () => {
		allTiles[entity.mapindex].occupant = null;
		allTiles[newIndex].occupant = entity;
		entity.mapindex = newIndex;

		// Ensure we snap to the same calculated position at the end
		entity.Node2DEntity.GlobalPosition = TileIndexToWorldPos(newIndex);
		
		pathNode.QueueFree();
		isMoving = false;
		
		highlightLayer.Clear();
		pathLayer.Clear();
		attackLayer.Clear();
		currentReachableTiles.Clear();

		if (onMoveComplete != null)
			onMoveComplete();
		else if (!IsPlayerUnit(entity))
			takeTurn();
		else
			OnPlayerMoveFinished();
	};
}

private Vector2 TileIndexToWorldPos(int index)
{
	Vector2I tilePos = allTiles[index].tilePos;
	// Calculate local position + offset to center of tile
	float x = (tilePos.X * tileSizeInpixels) + (tileSizeInpixels / 2.0f);
	float y = (tilePos.Y * tileSizeInpixels) + (tileSizeInpixels / 2.0f);
	
	// Return absolute world position (adding the WorldMap's own position)
	return GlobalPosition + new Vector2(x, y);
}
}
