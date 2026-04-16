using Godot;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

public partial class WorldMapPerlinNoise : Node2D
{
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
	int maxNumberOfTiles;
	Tile[] allTiles;
	GridEntity firstEntityInTheTimeline;
	List<GridEntity> allEntitys = new List<GridEntity> {};
	TimelineOverlay timelineOverlay;
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
		timelineOverlay = GetNode<TimelineOverlay>("%TimelineOverlay");
		mapMiddle = (mapWidthInTiles * mapHightInTiles)/2;
		mapEnd = (mapWidthInTiles * mapHightInTiles) - 1;
		bool enemy = false;
		bool player = true;
		makeMap();
		generateNeighborsOfTiles();
		SpawnEntity(EntityPlayerFighter,GD.RandRange(mapStart, mapMiddle),player);
		SpawnEntity(EntityPlayerFighter,GD.RandRange(mapStart, mapMiddle),player);
		SpawnEntity(EntityPlayerFighter,GD.RandRange(mapStart, mapMiddle),player);
		SpawnEntity(EntityPlayerFighter,GD.RandRange(mapStart, mapMiddle),player);
		SpawnEntity(EntityPlayerFighter,GD.RandRange(mapStart, mapMiddle),player);
		SpawnEntity(EntityPlayerFighter,GD.RandRange(mapStart, mapMiddle),player);
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
		lastHoveredIndex = -1;
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

	// 2. Handle HOVER (purple Path)
	if (hoverIndex != lastHoveredIndex)
	{
		lastHoveredIndex = hoverIndex;
		if (currentReachableTiles.Contains(hoverIndex))
		{
			// Calculate and show the purple path
			var path = GetPathToTarget(hoverIndex, firstEntityInTheTimeline.mapindex);
			HighlightPath(path); // You'll create a second purple layer for this
		}
		else
		{
		// Mouse is not on a reachable tile, so clear the purple path
		pathLayer.Clear();
		}
	}

	// 3. Handle CLICK (Move Entity)
	if (Input.IsActionJustPressed("left_mouse_click")) // Ensure this is defined in Input Map
	{
		if (currentReachableTiles.Contains(hoverIndex))
		{
			MoveEntity(firstEntityInTheTimeline, hoverIndex);
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

public void SpawnEntity(PackedScene PackedEntityScene, int mapIndex,bool player)
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
					SpawnEntity(EntityPlayerFighter,GD.RandRange(mapStart, mapMiddle),player);
			}
			else
			{
					SpawnEntity(EntityEnemyFighter,GD.RandRange(mapMiddle + 1, mapEnd),player);
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

	// 3. Create a list of all entities to simulate
	List<GridEntity> simulationUnits = new List<GridEntity>(allEntitys);
	Dictionary<GridEntity, int> simulatedSpeeds = new Dictionary<GridEntity, int>();
	foreach (GridEntity entity in simulationUnits)
		simulatedSpeeds[entity] = entity.CurrentSpeed;

	// 4. Loop through the entities and add them to the preview in the order of their speed
	for (int i = 1; i < count; i++)
	{
		GridEntity next = simulationUnits[0];
		foreach (GridEntity entity in simulationUnits)
		{
			if (simulatedSpeeds[entity] > simulatedSpeeds[next])
				next = entity;
		}

		preview.Add(next);
		
		// 5. Loop through the entities and update their speed in the simulation
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
// public void takeTurn()
// {
//     updateTimeline();

//     GridEntity activeUnit = firstEntityInTheTimeline;

//     // FIX: Assign the result to the class-level variable
//     currentReachableTiles = GetReachableTiles(activeUnit.mapindex, activeUnit.MovementRange);

//     HighlightReachableTiles(currentReachableTiles);
// }

public async void takeTurn() // Mark as async to allow a small delay for "thinking"
{
	updateTimeline();
	GridEntity activeUnit = firstEntityInTheTimeline;

	if (activeUnit.IsPlayer)
	{
		// 1. Calculate player reachable tiles for UI and input.
		currentReachableTiles = GetReachableTiles(activeUnit.mapindex, activeUnit.MovementRange);

		// Player's turn: Just highlight and wait for input in _Process
		HighlightReachableTiles(currentReachableTiles);
	}
	else
	{
		// Enemy's turn: AI Logic
		currentReachableTiles.Clear();
		highlightLayer.Clear();
		pathLayer.Clear();
		List<int> enemyReachableTiles = GetReachableTiles(activeUnit.mapindex, activeUnit.MovementRange);
		
		// Small delay so the player can see who is moving
		await ToSignal(GetTree().CreateTimer(0.5), "timeout");

		PerformEnemyAI(activeUnit, enemyReachableTiles);
	}
}
public void PerformEnemyAI(GridEntity enemy, List<int> enemyReachableTiles)
{
	int targetPlayerIndex = GetClosestPlayerIndex(enemy.mapindex);
	if (targetPlayerIndex == -1) return; // No players left!

	Vector2I playerPos = allTiles[targetPlayerIndex].tilePos;
	int bestTileIndex = enemy.mapindex;
	float closestDistToPlayer = float.MaxValue;

	// Loop through all tiles the enemy CAN reach this turn
	foreach (int tileIndex in enemyReachableTiles)
	{
		// We want the tile that is closest to the player's coordinate
		float dist = allTiles[tileIndex].tilePos.DistanceTo(playerPos);
		
		// Optional: If you want them to stop 1 tile away (attack range),
		// you can add logic here to prefer a distance of 1.0.


		if (Mathf.Abs(dist - enemy.AttackRange) < 0.1f) 
		{
			bestTileIndex = tileIndex;
			break; // Found an attack spot!
		}

		if (dist < closestDistToPlayer)
		{
			closestDistToPlayer = dist;
			bestTileIndex = tileIndex;
		}
	}

	// Move to the best calculated tile
	MoveEntity(enemy, bestTileIndex);
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

public void MoveEntity(GridEntity entity, int newIndex)
{
	if (isMoving) return;
	
	List<int> path = GetPathToTarget(newIndex, entity.mapindex);
	if (path.Count == 0) return;

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
		currentReachableTiles.Clear();
		
		takeTurn();
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
public int GetClosestPlayerIndex(int enemyMapIndex)
{
	int closestIndex = -1;
	float minDistance = float.MaxValue;

	foreach (GridEntity entity in allEntitys)
	{
		// Only target entities marked as players
		if (entity.IsPlayer) 
		{
			float dist = allTiles[enemyMapIndex].tilePos.DistanceTo(allTiles[entity.mapindex].tilePos);
			if (dist < minDistance)
			{
				minDistance = dist;
				closestIndex = entity.mapindex;
			}
		}
	}
	return closestIndex;
}
}
