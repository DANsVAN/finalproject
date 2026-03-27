using Godot;
using System;

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
	float[,] worldArray;
	TileMapLayer worldMap;
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
		worldMap.Clear();
		worldArray = new float[mapWidthInTiles , mapHightInTiles];
		for (int hight = 0; hight < mapHightInTiles; hight++)
		{
			for (int width = 0; width < mapWidthInTiles; width++)
			{
				
				float noiseValueOfTile = noise.GetNoise2D(width,hight);
				worldArray[width,hight] = noiseValueOfTile;
				noiseValueOfTile = (noiseValueOfTile + 1) / 2; // noiseValueOfTile = (noiseValueOfTile + 1) / 2; this is makeing it gen a number from 0 to 1
				worldArray[width,hight] = noiseValueOfTile;
				Vector2I tilePos = new Vector2I(width,  hight );
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
		makeMap();
		printWorldArray();
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



	public void printWorldArray()
	{
	for (int y = 0; y < mapHightInTiles; y++)
{
	GD.Print("row " + y);
    string rowOutput = "";
    for (int x = 0; x < mapWidthInTiles; x++)
    {
        // Add each value to a string, rounded to 2 decimal places for readability
        rowOutput += worldArray[x, y].ToString("0.00") + " ";
    }
    GD.Print(rowOutput); // Prints one full row at a time
}
	}
}
