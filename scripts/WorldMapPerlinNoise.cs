using Godot;
using System;

public partial class WorldMapPerlinNoise : Node2D
{
	[Export] int mapWidth = 25;
	[Export] int mapHight = 25;
	[Export] double noiseScale = 0.1;
	[Export] double grassThreshold = 0.1;
	[Export] double waterThreshold = 0.2;
	[Export] double snowThreshold = 0.3;
	[Export] double iceThreshold = 0.4;
	[Export] double mountainThreshold = 0.5;
	[Export] float worldNoiseScale = 0.02f;
	float[,] worldArray;

	TileMapLayer worldMap;
	public FastNoiseLite generateRandNoise()
	{
		var noise = new FastNoiseLite();
		noise.Seed = (int)GD.Randi();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		noise.Frequency = worldNoiseScale;
		return(noise);
	}
	public void generateMapFromNoise(FastNoiseLite noise)
	{
		worldMap.Clear();
		worldArray = new float[mapHight, mapWidth];
		for (int hight = 0; hight < mapHight; hight++)
		{
			for (int width = 0; width < mapWidth; width++)
			{
				float noiseValueOfTile = noise.GetNoise2D(hight,width);
				noiseValueOfTile = (noiseValueOfTile + 1) / 2;
				// noiseValueOfTile = (noiseValueOfTile + 1) / 2; this is makeing it gen a number from 0 to 1
				worldArray[hight,width] = noiseValueOfTile;
				Vector2I tilePos = new Vector2I(hight,width);
				Vector2I atlasCoords = new Vector2I(0,0);

				// pick the tile
				if (noiseValueOfTile < grassThreshold)
				{
					atlasCoords.X = 0;
					atlasCoords.Y = 3;
				}
				else if (noiseValueOfTile < waterThreshold)
				{
					atlasCoords.X = 3;
					atlasCoords.Y = 0;
				}
				else if (noiseValueOfTile < snowThreshold)
				{
					atlasCoords.X = 1;
					atlasCoords.Y = 0;
				}
					else if (noiseValueOfTile < iceThreshold)
				{
					atlasCoords.X = 0;
					atlasCoords.Y = 1;
				}
				else
				{
					//mountainThreshold
					atlasCoords.X = 0;
					atlasCoords.Y = 2;
				}
				worldMap.SetCell(tilePos,0,atlasCoords);
			}
		}
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("make map");
		worldMap = GetNode<TileMapLayer>("%WorldMapLayer");
		generateMapFromNoise(generateRandNoise());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
