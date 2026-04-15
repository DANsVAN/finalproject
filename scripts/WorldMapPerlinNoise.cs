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
	[Export] double grassThreshold = 0.4;
	[Export] double waterThreshold = 0.5;
	[Export] double snowThreshold = 0.6;
	[Export] double iceThreshold = 0.7;
	[Export] double mountainThreshold = 0.8;
	[Export] float worldNoiseScale = 0.02f;
	[Export] int worldFractalOctaves = 4;
	[Export] float worldFractalLacunarity = 2.0f;
	[Export] float worldFractalGain = 0.5f;
	[Export] public PackedScene EntityPlayerFighter;
	[Export] public PackedScene EntityEnemyFighter;
	float[,] worldArray;
	TileMapLayer worldMap;
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
		updateTimeline();
		PositionAllEntintys();
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
				}
				else if (noiseValueOfTile < waterThreshold)
				{
					allTiles[currentTileIndex].atlasCoords.X = 3;
					allTiles[currentTileIndex].atlasCoords.Y = 0;
					allTiles[currentTileIndex].tileName = "water";
				}
				else if (noiseValueOfTile < snowThreshold)
				{
					allTiles[currentTileIndex].atlasCoords.X = 1;
					allTiles[currentTileIndex].atlasCoords.Y = 0;
					allTiles[currentTileIndex].tileName = "snow";
				}
					else if (noiseValueOfTile < iceThreshold)
				{
					allTiles[currentTileIndex].atlasCoords.X = 0;
					allTiles[currentTileIndex].atlasCoords.Y = 1;
					allTiles[currentTileIndex].tileName = "ice";
				}
				else
				{
					//mountainThreshold
					allTiles[currentTileIndex].atlasCoords.X = 0;
					allTiles[currentTileIndex].atlasCoords.Y = 2;
					allTiles[currentTileIndex].tileName = "mountain";
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
        // 1. Initialize the array BEFORE trying to use it!
        tile.neighbors = new int[4]; 

        tile.leftNeighbor = tile.index - 1;
        tile.rightNeighbor = tile.index + 1;
        tile.upNeighbor = tile.index - mapWidthInTiles;
        tile.downNeighbor = tile.index + mapWidthInTiles;
        
        // 2. Corrected Logic: If it's OUT of bounds (< 0 or >= maxNumberOfTiles), it is -1.
        // Otherwise, it is a valid neighbor.
        
        // Left Neighbor
        if(tile.leftNeighbor < 0 || tile.leftNeighbor >= maxNumberOfTiles)
        {
            tile.neighbors[0] = -1;
        }
        else
        {
            tile.neighbors[0] = tile.leftNeighbor;
        }

        // Right Neighbor
        if(tile.rightNeighbor < 0 || tile.rightNeighbor >= maxNumberOfTiles)
        {
            tile.neighbors[1] = -1;
        }
        else
        {
            tile.neighbors[1] = tile.rightNeighbor;
        }

        // Up Neighbor
        if(tile.upNeighbor < 0 || tile.upNeighbor >= maxNumberOfTiles)
        {
            tile.neighbors[2] = -1;
        }
        else
        {
            tile.neighbors[2] = tile.upNeighbor;
        }

        // Down Neighbor
        if(tile.downNeighbor < 0 || tile.downNeighbor >= maxNumberOfTiles)
        {
            tile.neighbors[3] = -1;
        }
        else
        {
            tile.neighbors[3] = tile.downNeighbor;
        }
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
		GridEntity firstEntityInTheTimeline = allEntitys[0];
		foreach (GridEntity entity in allEntitys)
		{
			if(entity.CurrentSpeed > firstEntityInTheTimeline.CurrentSpeed)
			{
				firstEntityInTheTimeline = entity;
			}
			else
			{
				entity.CurrentSpeed += entity.BaseSpeed;
				entity.sprite.Modulate = Colors.White;
			}
		}
		firstEntityInTheTimeline.CurrentSpeed = firstEntityInTheTimeline.BaseSpeed;
		firstEntityInTheTimeline.sprite.Modulate = Colors.Green;
}
}
