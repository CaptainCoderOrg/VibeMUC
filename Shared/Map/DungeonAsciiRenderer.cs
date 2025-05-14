using System;
using System.Text;

namespace VibeMUC.Map
{
    /// <summary>
    /// Renders a DungeonMapData as ASCII art.
    /// Each cell is represented by a 2x2 grid of characters.
    /// </summary>
    public class DungeonAsciiRenderer
    {
        // ASCII characters for different cell elements
        private const char EMPTY = ' ';
        private const char FLOOR = '·';
        private const char WALL_VERTICAL = '│';
        private const char WALL_HORIZONTAL = '─';
        private const char CORNER_TOP_LEFT = '┌';
        private const char CORNER_TOP_RIGHT = '┐';
        private const char CORNER_BOTTOM_LEFT = '└';
        private const char CORNER_BOTTOM_RIGHT = '┘';
        private const char DOOR_VERTICAL = '║';  // Double vertical bar for east/west doors
        private const char DOOR_HORIZONTAL = '═';  // Double horizontal bar for north/south doors

        // ANSI color codes
        private const string RESET = "\u001b[0m";
        private const string BROWN = "\u001b[38;5;130m";  // Door color
        private const string DARK_GREY = "\u001b[38;5;240m";  // Wall color
        private const string WHITE = "\u001b[37m";  // Floor color

        private record ColoredChar(char Char, string? Color = null);

        /// <summary>
        /// Converts a DungeonMapData to an ASCII string representation.
        /// </summary>
        /// <param name="map">The map to convert</param>
        /// <returns>ASCII representation of the map</returns>
        public string Render(DungeonMapData map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (map.Width <= 0 || map.Height <= 0) throw new ArgumentException("Map dimensions must be positive");
            if (map.Cells == null || map.Cells.Length != map.Width * map.Height)
                throw new ArgumentException("Invalid cell data");

            // Calculate dimensions for the ASCII grid
            // Each cell is 3x2 characters (including walls)
            int gridWidth = map.Width * 3;
            int gridHeight = map.Height * 2;
            var grid = new ColoredChar[gridHeight, gridWidth];

            // Initialize grid with empty spaces
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    grid[y, x] = new ColoredChar(EMPTY);
                }
            }

            // Render each cell
            for (int mapY = 0; mapY < map.Height; mapY++)
            {
                for (int mapX = 0; mapX < map.Width; mapX++)
                {
                    var cell = map.GetCell(mapX, mapY);
                    if (cell != null && !cell.IsEmpty)
                    {
                        RenderCell(grid, mapX, mapY, cell);
                    }
                }
            }

            // Convert grid to string
            var sb = new StringBuilder();
            
            // Add map header
            sb.AppendLine($"Map: {map.MapName} ({map.Width}x{map.Height})");
            sb.AppendLine(new string('=', map.Width * 3));
            sb.AppendLine();

            // Convert grid to string, rendering from top to bottom
            for (int y = gridHeight - 1; y >= 0; y--)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var coloredChar = grid[y, x];
                    if (coloredChar.Color != null)
                    {
                        sb.Append(coloredChar.Color)
                          .Append(coloredChar.Char)
                          .Append(RESET);
                    }
                    else
                    {
                        sb.Append(coloredChar.Char);
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void RenderCell(ColoredChar[,] grid, int mapX, int mapY, CellData cell)
        {
            // Convert map coordinates to grid coordinates
            int baseX = mapX * 3;
            int baseY = mapY * 2;

            // Render top line
            if (cell.HasNorthWall)
            {
                // Left corner or wall
                grid[baseY + 1, baseX] = new ColoredChar(
                    cell.HasWestWall ? CORNER_TOP_LEFT : WALL_HORIZONTAL,
                    DARK_GREY
                );

                // Middle section
                grid[baseY + 1, baseX + 1] = new ColoredChar(
                    cell.HasNorthDoor ? DOOR_HORIZONTAL : WALL_HORIZONTAL,
                    cell.HasNorthDoor ? BROWN : DARK_GREY
                );

                // Right corner or wall
                grid[baseY + 1, baseX + 2] = new ColoredChar(
                    cell.HasEastWall ? CORNER_TOP_RIGHT : WALL_HORIZONTAL,
                    DARK_GREY
                );
            }
            else
            {
                // Vertical walls if no north wall
                if (cell.HasWestWall)
                {
                    grid[baseY + 1, baseX] = new ColoredChar(WALL_VERTICAL, DARK_GREY);
                }
                if (cell.HasEastWall)
                {
                    grid[baseY + 1, baseX + 2] = new ColoredChar(WALL_VERTICAL, DARK_GREY);
                }
            }

            // Render bottom line
            // Left wall or door
            if (cell.HasWestWall)
            {
                grid[baseY, baseX] = new ColoredChar(
                    cell.HasWestDoor ? DOOR_VERTICAL : WALL_VERTICAL,
                    cell.HasWestDoor ? BROWN : DARK_GREY
                );
            }

            // Floor
            grid[baseY, baseX + 1] = new ColoredChar(
                cell.IsPassable ? FLOOR : EMPTY,
                cell.IsPassable ? WHITE : null
            );

            // Right wall or door
            if (cell.HasEastWall)
            {
                grid[baseY, baseX + 2] = new ColoredChar(
                    cell.HasEastDoor ? DOOR_VERTICAL : WALL_VERTICAL,
                    cell.HasEastDoor ? BROWN : DARK_GREY
                );
            }

            // Render south wall if needed
            if (cell.HasSouthWall)
            {
                // Left corner or wall
                grid[baseY, baseX] = new ColoredChar(
                    cell.HasWestWall ? CORNER_BOTTOM_LEFT : WALL_HORIZONTAL,
                    DARK_GREY
                );

                // Middle section
                grid[baseY, baseX + 1] = new ColoredChar(
                    cell.HasSouthDoor ? DOOR_HORIZONTAL : WALL_HORIZONTAL,
                    cell.HasSouthDoor ? BROWN : DARK_GREY
                );

                // Right corner or wall
                grid[baseY, baseX + 2] = new ColoredChar(
                    cell.HasEastWall ? CORNER_BOTTOM_RIGHT : WALL_HORIZONTAL,
                    DARK_GREY
                );
            }
        }
    }
} 