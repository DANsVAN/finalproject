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
	[Export] double grassThreshold = 0.51;
	[Export] double waterThreshold = 0.65;
	[Export] double snowThreshold = 0.7;
	[Export] double iceThreshold = 0.75;
	[Export] double mountainThreshold = 0.8;
	[Export] float worldNoiseScale = 0.02f;
	[Export] int worldFractalOctaves = 4;
	[Export] float worldFractalLacunarity = 2.0f;
	[Export] float worldFractalGain = 0.5f;
	[Export] public PackedScene EntityPlayerFighter;
	[Export] public PackedScene EntityEnemyFighter;
	float[,] worldArray;
	TileMapLayer worldMap;
	TileMapLayer highlightLayer;
	int maxNumberOfTiles;
	Tile[] allTiles;
	GridEntity firstEntityInTheTimeline;
	List<GridEntity> allEntitys = new List<GridEntity> {};
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
		
		makeMap();
		generateNeighborsOfTiles();
		SpawnEntity(EntityPlayerFighter,149);
		SpawnEntity(EntityPlayerFighter,0);
		SpawnEntity(EntityPlayerFighter,100);
		SpawnEntity(EntityPlayerFighter,50);
		SpawnEntity(EntityPlayerFighter,49);
		SpawnEntity(EntityPlayerFighter,10);
		SpawnEntity(EntityEnemyFighter,600);
		SpawnEntity(EntityEnemyFighter,620);
		SpawnEntity(EntityEnemyFighter,115);
		SpawnEntity(EntityEnemyFighter,800);
		PositionAllEntintys();
		takeTurn();
		takeTurn();
		// takeTurn();
		// takeTurn();
		// takeTurn();
		// takeTurn();
		// takeTurn();
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
				if (noiseValueOfTile < grassThreshold)
				{
					allTiles[currentTileIndex].atlasCoords.X = 0;
					allTiles[currentTileIndex].atlasCoords.Y = 3;
					allTiles[currentTileIndex].tileName = "grass";
					allTiles[currentTileIndex].movementCost = 1;
				}
				else if (noiseValueOfTile < waterThreshold)
				{
					allTiles[currentTileIndex].atlasCoords.X = 3;
					allTiles[currentTileIndex].atlasCoords.Y = 0;
					allTiles[currentTileIndex].tileName = "water";
					allTiles[currentTileIndex].movementCost = 2;
				}
				else if (noiseValueOfTile < snowThreshold)
				{
					allTiles[currentTileIndex].atlasCoords.X = 1;
					allTiles[currentTileIndex].atlasCoords.Y = 0;
					allTiles[currentTileIndex].tileName = "snow";
					allTiles[currentTileIndex].movementCost = 3;
				}
					else if (noiseValueOfTile < iceThreshold)
				{
					allTiles[currentTileIndex].atlasCoords.X = 0;
					allTiles[currentTileIndex].atlasCoords.Y = 1;
					allTiles[currentTileIndex].tileName = "ice";
					allTiles[currentTileIndex].movementCost = 5;
				}
				else
				{
					//mountainThreshold
					allTiles[currentTileIndex].atlasCoords.X = 0;
					allTiles[currentTileIndex].atlasCoords.Y = 2;
					allTiles[currentTileIndex].tileName = "mountain";
					allTiles[currentTileIndex].movementCost = 6;
				}
				worldMap.SetCell(allTiles[currentTileIndex].tilePos,0,allTiles[currentTileIndex].atlasCoords);
				currentTileIndex ++;
			}
		}
	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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

public void SpawnEntity(PackedScene PackedEntityScene, int mapIndex)
{
    if (PackedEntityScene == null)
    {
        GD.PrintErr("EntityScene not assigned in Inspector!");
        return;
    }
	if (mapIndex < 0 || mapIndex >= allTiles.Length)
    {
        GD.PrintErr("Map index out of bounds!");
        return;
    }

    // 3. Create the instance
    var entity = PackedEntityScene.Instantiate<Node2D>();
    
    AddChild(entity);

    if (entity is GridEntity gridEntity)
    {
        allTiles[mapIndex].occupant = gridEntity;
    }
	allTiles[mapIndex].occupant.mapindex = mapIndex;
	// Vector2 pixlePosition = new Vector2((1 * allTiles[mapIndex].tilePos.X * allTiles[mapIndex].occupant.spriteSize) + (allTiles[mapIndex].occupant.spriteSize / 2) ,(1 * allTiles[mapIndex].tilePos.Y  * allTiles[mapIndex].occupant.spriteSize) + (allTiles[mapIndex].occupant.spriteSize / 2));
	allTiles[mapIndex].occupant.Node2DEntity = entity;
	// allTiles[mapIndex].occupant.Node2DEntity.Position = pixlePosition;
	allEntitys.Add(allTiles[mapIndex].occupant);
	// allEntitys[0].Node2DEntity.Position = pixlePosition;
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
            if (allTiles[next].tileName == "water" || allTiles[next].tileName == "mountain") 
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
public void takeTurn()
{
    updateTimeline();

    GridEntity activeUnit = firstEntityInTheTimeline;

    // 1. Calculate where they can go
    List<int> reachableTiles = GetReachableTiles(activeUnit.mapindex, activeUnit.MovementRange);

    // 2. Paint those tiles green!
    HighlightReachableTiles(reachableTiles);
}
}