using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VibeMUC.Map
{
    /// <summary>
    /// Serializable data structure representing a dungeon map.
    /// This can be used for saving/loading and network transmission.
    /// </summary>
    [Serializable]
    public class DungeonMapData
    {
        // Map dimensions
        [JsonProperty]
        public int Width { get; set; }
        
        [JsonProperty]
        public int Height { get; set; }
        
        // Cells stored as a single array for more efficient serialization
        [JsonProperty(Required = Required.Always)]
        public CellData[] Cells { get; set; }

        // Optional metadata
        [JsonProperty(Required = Required.Always)]
        public string MapName { get; set; }
        
        [JsonProperty]
        public int FloorLevel { get; set; }
        
        [JsonProperty]
        public Dictionary<string, string> Metadata { get; set; }

        public DungeonMapData()
        {
            Metadata = new Dictionary<string, string>();
        }

        public DungeonMapData(int width, int height, string mapName) : this()
        {
            Width = width;
            Height = height;
            MapName = mapName;
            Cells = new CellData[width * height];
            for (int i = 0; i < Cells.Length; i++)
            {
                Cells[i] = new CellData();
            }
        }

        // Helper method to get cell index from coordinates
        public int GetCellIndex(int x, int y)
        {
            return y * Width + x;
        }

        // Helper method to get cell data at coordinates
        public CellData GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return Cells[GetCellIndex(x, y)];
        }

        // Helper method to set cell data at coordinates
        public void SetCell(int x, int y, CellData cell)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            Cells[GetCellIndex(x, y)] = cell;
        }

        // Create map data from a DungeonGrid
        public static DungeonMapData FromDungeonGrid(DungeonGrid grid)
        {
            Vector2Int size = grid.GridSize;
            var mapData = new DungeonMapData(size.x, size.y, $"Grid Map {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Cell cell = grid.GetCell(x, y);
                    if (cell != null)
                    {
                        mapData.SetCell(x, y, new CellData
                        {
                            IsPassable = cell.IsPassable,
                            HasNorthWall = cell.HasNorthWall,
                            HasEastWall = cell.HasEastWall,
                            HasSouthWall = cell.HasSouthWall,
                            HasWestWall = cell.HasWestWall
                        });
                    }
                }
            }

            return mapData;
        }

        // Apply map data to a DungeonGrid
        public void ApplyToDungeonGrid(DungeonGrid grid)
        {
            if (grid.GridSize.x != Width || grid.GridSize.y != Height)
            {
                Debug.LogError($"Grid size mismatch. Map is {Width}x{Height} but grid is {grid.GridSize.x}x{grid.GridSize.y}");
                return;
            }

            Debug.Log($"Applying map data: {Width}x{Height}, {Cells.Length} cells");

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    CellData cellData = GetCell(x, y);
                    if (cellData != null)
                    {
                        Cell cell = grid.GetCell(x, y);
                        if (cell != null)
                        {
                            cell.IsPassable = cellData.IsPassable;
                            cell.SetWalls(
                                cellData.HasNorthWall,
                                cellData.HasEastWall,
                                cellData.HasSouthWall,
                                cellData.HasWestWall
                            );
                            Debug.Log($"Updated cell {x},{y}: passable={cellData.IsPassable}, walls={cellData.HasNorthWall},{cellData.HasEastWall},{cellData.HasSouthWall},{cellData.HasWestWall}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Serializable data structure representing a single cell in the dungeon.
    /// </summary>
    [Serializable]
    public class CellData
    {
        // Basic properties
        [JsonProperty]
        public bool IsPassable { get; set; } = true;

        // Wall configuration
        [JsonProperty]
        public bool HasNorthWall { get; set; }
        
        [JsonProperty]
        public bool HasEastWall { get; set; }
        
        [JsonProperty]
        public bool HasSouthWall { get; set; }
        
        [JsonProperty]
        public bool HasWestWall { get; set; }

        // Optional properties that might be needed for different cell types
        [JsonProperty]
        public string CellType { get; set; } = "Default";
        
        [JsonProperty]
        public Dictionary<string, string> Properties { get; set; }

        public CellData()
        {
            Properties = new Dictionary<string, string>();
        }

        // Helper to quickly set all walls
        public void SetWalls(bool north, bool east, bool south, bool west)
        {
            HasNorthWall = north;
            HasEastWall = east;
            HasSouthWall = south;
            HasWestWall = west;
        }
    }
} 