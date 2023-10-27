using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

public class Grid
{
    public readonly int Width;
    public readonly int Height;
    private Cell[,] _cells;

    public Grid(string text)
    {
        string[] lines = text.Trim().Split("\n");

        var heightsLines = new List<string[]>();
        var velocitiesXLines = new List<string[]>();
        var velocitiesYLines = new List<string[]>();

        var index = 0;

        foreach (string line in lines)
        {
            string[] elements = line.Trim().Split(" ");
            if (elements[0] == "-")
            {
                index++;
                continue;
            }

            switch (index)
            {
                case 0:
                    heightsLines.Add(elements);
                    break;
                case 1:
                    velocitiesXLines.Add(elements);
                    break;
                case 2:
                    velocitiesYLines.Add(elements);
                    break;
            }
        }

        Height = heightsLines.Count;
        Width = heightsLines[0].Length;
        
        _cells = new Cell[Height, Width];

        if (velocitiesXLines.Count > 0) // check here so only one check
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    _cells[y, x] = new Cell(
                        x, y,
                        float.Parse(heightsLines[y][x]),
                        float.Parse(velocitiesXLines[y][x]),
                        float.Parse(velocitiesYLines[y][x])
                    );
                }
            }
        }
        else
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    _cells[y, x] = new Cell(
                        x, y,
                        float.Parse(heightsLines[y][x]),
                        0, 0
                    );
                }
            }
        }
    }

    public Cell GetCell(int x, int y)
    {
        return _cells[y, x];
    }
}