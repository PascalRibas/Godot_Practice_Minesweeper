using Godot;
using System;
using System.ComponentModel;
using System.Threading;

public partial class SweeperTileMap : TileMap
{
	const byte sizeX = 20;
	const byte sizeY = 20;
	private readonly byte[,] FieldMap = new byte[sizeX, sizeY];
	const byte LikelinessThingy = 1;
	const byte LikelinessDenominator = 7;


	public override void _Ready()
	{
		ClearLayer(0);
		ClearLayer(1);
		StartGame();
	}
	public byte GetFieldMap(byte x, byte y){
		return FieldMap[x, y];
	}

	private void StartButtonPressed()
	{
		GetNode<Button>("Button").Hide();
		StartGame();
	}

	public void StartGame ()
	{
		//GD.PrintErr("Rendering blank Field...");
		RenderField();
		//GD.PrintErr("Deploying Mines...");
		DeployMines();
		//GD.PrintErr("Generating Numbered Fields...");
		GenerateNumberedFields();
		//GD.PrintErr("Revealing First Field...");
		RevealFirstField(0);
	}

	private void DeployMines()
	{
		Random rnd = new();
		for(byte i = 0; i < sizeX; i++){
			for(byte j = 0; j < sizeY; j++){
				if(rnd.Next(LikelinessThingy-1, LikelinessDenominator) == 0)
				{
					FieldMap[i, j] = 9;
				}
			}
		}
	}

	private void GenerateNumberedFields(){
		for (byte i = 0; i < sizeX; i++)
		{
			for (byte j = 0; j < sizeY; j++)
			{
				FieldMap[i, j] = EnvironmentCheck(i, j);
			}
		}
	}


	private byte EnvironmentCheck(byte x, byte y)
	{
		byte counter = 0;
		byte exceptionCounter = 0;
		for (sbyte i = -1; i <= 1; i++)
		{
			for (sbyte j = -1; j <= 1; j++)
			{
				if(x+i >= 0 && x+i < sizeX && y+j >= 0 && y+j < sizeY)
				{
					if (FieldMap[x+i, y+j] == 9)
					{
						counter += 1;
					}
				}
				else
				{
					exceptionCounter += 1;
				}
			}
		}
		if (FieldMap[x, y] == 9)
		{
			//turn mines surrounded only by mines into 8-fields to be 100% fair to the player
			if (counter + exceptionCounter == 9)
			{
				return 8;
			}
			return 9;
		}
		return counter;
	}

	const byte LAYER = 0;
	const byte SOURCE_ID = 1;

	private void RenderField()
	{
		for (byte i = 0; i < sizeX; i++)
		{
			for (byte j = 0; j < sizeY; j++)
			{
				SetCell(LAYER, new Vector2I(i, j), SOURCE_ID, new Vector2I(2, 2));
			}
		}
	}

	private static Vector2I FindAssignment(byte x){
		return x switch
		{
			0 => new Vector2I(3, 1),
			1 => new Vector2I(0, 0),
			2 => new Vector2I(1, 0),
			3 => new Vector2I(2, 0),
			4 => new Vector2I(3, 0),
			5 => new Vector2I(4, 0),
			6 => new Vector2I(0, 1),
			7 => new Vector2I(1, 1),
			8 => new Vector2I(2, 1),
			9 => new Vector2I(1, 2),
			_ => new Vector2I(0, 2),
		};
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton maus)
		{
			Vector2I pos = (Vector2I)maus.Position;
			if (Input.IsActionPressed("RightClick"))
			{
				MarkField(pos);
			}

			if (Input.IsActionPressed("LeftClick"))
			{
				RevealFieldByMouseCoords(pos);
			}

			if (Input.IsActionPressed("MiddleClick"))
			{
				RevealSurroundingByMouseCoords(pos);
			}
		}
	}
	
	private void RevealFieldByMouseCoords(Vector2 position)
	{
		Vector2I realPos = LocalToMap(this.ToLocal(position));
		byte x = (byte)realPos.X;
		byte y = (byte)realPos.Y;
		RevealField(x, y);
	}

	private void RevealField(byte x, byte y)
	{
		if(x >= 0 && x < sizeX && y >= 0 && y < sizeY)
		{
			Vector2I realPos = new(x, y);
			if(GetCellAtlasCoords (LAYER, realPos, true).Equals(new Vector2I(2, 2)))
			{
				SetCell (LAYER, realPos, SOURCE_ID, FindAssignment(FieldMap[x, y]), 0);
				if(GetFieldMap(x, y) == 0)
				{
					RevealSurrounding(x, y);
				}
				if(GetFieldMap(x, y) == 9)
				{
					SetCell (LAYER, realPos, SOURCE_ID, new Vector2I(4, 1), 0);
					EndGame();
				}
			}
		}
	}

	private void RevealSurrounding(byte x, byte y)
	{
		for (sbyte i = -1; i <= 1; i++)
		{
			for (sbyte j = -1; j <= 1; j++)
			{
				byte newX = (byte)(x + i);
				byte newY = (byte)(y + j);
				if(x+i >= 0 && newX < sizeX && y+j >= 0 && newY < sizeY)
				{
					RevealField(newX, newY);
				}
			}
		}
	}

	private void MarkField(Vector2I position)
	{
		Vector2I realPos = LocalToMap(this.ToLocal(position));
		Vector2I atlasCoords = GetCellAtlasCoords (LAYER, realPos, true);
		Vector2I noFlag = new Vector2I(2, 2);
		Vector2I flag = new Vector2I(0, 2);
		if(atlasCoords.Equals(noFlag))
		{
			SetCell(LAYER, realPos, 1, flag, 0);
		}
		else if(atlasCoords.Equals(flag))
		{
			SetCell(LAYER, realPos, 1, noFlag, 0);
		}
	}

	private void RevealFirstField(byte value)
	{
		for (byte i = 0; i < sizeX; i++)
		{
			for (byte j = 0; j < sizeY; j++)
			{
				if(FieldMap[i, j] == value)
				{
					RevealField(i, j);
					return;
				}
			}
		}
		if(value < 2){
			RevealFirstField(++value);
		}
	}

	private void RevealAll()
	{
		for (byte i = 0; i < sizeX; i++)
		{
			for (byte j = 0; j < sizeY; j++)
			{
				SimplyRevealField(i, j);
			}
		}
	}

	private void SimplyRevealField(byte x, byte y)
	{
		if(x >= 0 && x < sizeX && y >= 0 && y < sizeY)
		{
			Vector2I realPos = new(x, y);
			if(GetCellAtlasCoords (LAYER, realPos, true).Equals(new Vector2I(2, 2)))
			{
				SetCell (LAYER, realPos, SOURCE_ID, FindAssignment(FieldMap[x, y]), 0);
			}
		}
	}

	private void RevealSurroundingByMouseCoords(Vector2I position)
	{
		Vector2I realPos = LocalToMap(this.ToLocal(position));
		byte x = (byte)realPos.X;
		byte y = (byte)realPos.Y;
		if(x >= 0 && x < sizeX && y >= 0 && y < sizeY)
		{
			Vector2I atlasCoords = GetCellAtlasCoords (LAYER, realPos, true);
			if(!(atlasCoords.Equals(new Vector2I(2, 2)) || atlasCoords.Equals(new Vector2I(0, 2))))
			{
				for (sbyte i = -1; i <= 1; i++)
				{
					for (sbyte j = -1; j <= 1; j++)
					{
						byte newX = (byte)(x + i);
						byte newY = (byte)(y + j);
						if(x+i >= 0 && newX < sizeX && y+j >= 0 && newY < sizeY)
						{
							RevealField(newX, newY);
						}
					}
				}
			}
		}
	}

	//the end of game function
	private void EndGame()
	{
		RevealAll();
		GetNode<Godot.Timer>("DoomTimer").Start();
	}
	
	//on Doom Timer timeout
	private void GimmeASec()
	{
		if (Input.IsActionPressed("LeftClick"))
		{
			ClearLayer(0);
			ClearLayer(1);
			StartGame();
		}
	}
}



