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

        private string ColoredWall(char c) => $"{DARK_GREY}{c}{RESET}";
        private string ColoredDoor(char c) => $"{BROWN}{c}{RESET}";
        private string ColoredFloor(char c) => $"{WHITE}{c}{RESET}";

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

            var sb = new StringBuilder();

            // Add map header
            sb.AppendLine($"Map: {map.MapName} ({map.Width}x{map.Height})");
            sb.AppendLine(new string('=', map.Width * 2));
            sb.AppendLine();

            // Render each row from bottom to top to match game coordinates
            for (int y = map.Height - 1; y >= 0; y--)
            {
                RenderRow(map, y, sb);
                
                // Only add vertical spacing if this isn't the bottom of a room section
                if (y > 0 && !HasSouthWallInRow(map, y))
                {
                    var spacerLine = new StringBuilder();
                    for (int x = 0; x < map.Width; x++)
                    {
                        var cell = map.GetCell(x, y);
                        var leftCell = x > 0 ? map.GetCell(x - 1, y) : null;
                        var rightCell = x < map.Width - 1 ? map.GetCell(x + 1, y) : null;

                        if (cell != null && !cell.IsEmpty)
                        {
                            bool hasWestWall = cell.HasWestWall || (leftCell == null || leftCell.IsEmpty);
                            if (hasWestWall)
                            {
                                spacerLine.Append(ColoredWall(WALL_VERTICAL));
                            }
                            else
                            {
                                spacerLine.Append(EMPTY);
                            }

                            spacerLine.Append(EMPTY);
                        }
                        else
                        {
                            spacerLine.Append("  ");
                        }

                        // Add east wall at the end of room sections
                        if (x == map.Width - 1 || (cell != null && !cell.IsEmpty && (rightCell == null || rightCell.IsEmpty)))
                        {
                            if (cell != null && !cell.IsEmpty)
                            {
                                bool hasEastWall = cell.HasEastWall || (rightCell == null || rightCell.IsEmpty);
                                if (hasEastWall)
                                {
                                    spacerLine.Append(ColoredWall(WALL_VERTICAL));
                                }
                            }
                        }
                    }
                    sb.AppendLine(spacerLine.ToString());
                }
            }

            return sb.ToString();
        }

        private void RenderRow(DungeonMapData map, int y, StringBuilder sb)
        {
            var line1 = new StringBuilder();
            var line2 = new StringBuilder();

            // Process each cell in the row
            for (int x = 0; x < map.Width; x++)
            {
                var cell = map.GetCell(x, y);
                var leftCell = x > 0 ? map.GetCell(x - 1, y) : null;
                var rightCell = x < map.Width - 1 ? map.GetCell(x + 1, y) : null;
                var topCell = y < map.Height - 1 ? map.GetCell(x, y + 1) : null;
                var bottomCell = y > 0 ? map.GetCell(x, y - 1) : null;

                // Handle top line (walls and doors)
                if (cell != null && !cell.IsEmpty)
                {
                    // Check if this is part of a wall
                    bool isWallStart = leftCell == null || leftCell.IsEmpty;
                    bool isWallEnd = rightCell == null || rightCell.IsEmpty;
                    bool hasNorthWall = cell.HasNorthWall || (topCell == null || topCell.IsEmpty);

                    if (hasNorthWall && isWallStart)
                    {
                        line1.Append(ColoredWall(CORNER_TOP_LEFT));
                    }
                    else if (hasNorthWall)
                    {
                        line1.Append(ColoredWall(WALL_HORIZONTAL));
                    }
                    else
                    {
                        line1.Append(EMPTY);
                    }

                    if (cell.HasNorthDoor)
                    {
                        line1.Append(ColoredDoor(DOOR_HORIZONTAL));
                    }
                    else if (hasNorthWall)
                    {
                        line1.Append(ColoredWall(WALL_HORIZONTAL));
                    }
                    else
                    {
                        line1.Append(EMPTY);
                    }

                    // Add top-right corner if this is the end of a room section
                    if (hasNorthWall && isWallEnd)
                    {
                        line1.Append(ColoredWall(CORNER_TOP_RIGHT));
                    }
                }
                else
                {
                    line1.Append("  ");
                }

                // Handle bottom line (floor and vertical walls/doors)
                if (cell != null && !cell.IsEmpty)
                {
                    bool hasWestWall = cell.HasWestWall || (leftCell == null || leftCell.IsEmpty);
                    if (hasWestWall)
                    {
                        line2.Append(cell.HasWestDoor ? ColoredDoor(DOOR_VERTICAL) : ColoredWall(WALL_VERTICAL));
                    }
                    else
                    {
                        line2.Append(EMPTY);
                    }

                    line2.Append(cell.IsPassable ? ColoredFloor(FLOOR) : EMPTY);
                }
                else
                {
                    line2.Append("  ");
                }

                // Add east wall at the end of room sections
                if (x == map.Width - 1 || (cell != null && !cell.IsEmpty && (rightCell == null || rightCell.IsEmpty)))
                {
                    if (cell != null && !cell.IsEmpty)
                    {
                        bool hasEastWall = cell.HasEastWall || (rightCell == null || rightCell.IsEmpty);
                        if (cell.HasEastDoor)
                        {
                            line2.Append(ColoredDoor(DOOR_VERTICAL));
                        }
                        else if (hasEastWall)
                        {
                            line2.Append(ColoredWall(WALL_VERTICAL));
                        }
                    }
                }
            }

            // Only append line1 if it's not empty or if it's the top row
            if (y == map.Height - 1 || line1.ToString().Trim().Length > 0)
            {
                sb.AppendLine(line1.ToString());
            }
            sb.AppendLine(line2.ToString());

            // If this is the bottom row or we're at the bottom of a room section, add the south wall
            if (y == 0 || HasSouthWallInRow(map, y))
            {
                var bottomLine = new StringBuilder();
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = map.GetCell(x, y);
                    var leftCell = x > 0 ? map.GetCell(x - 1, y) : null;
                    var rightCell = x < map.Width - 1 ? map.GetCell(x + 1, y) : null;

                    if (cell != null && !cell.IsEmpty && (cell.HasSouthWall || y == 0))
                    {
                        bool isWallStart = leftCell == null || leftCell.IsEmpty;
                        if (isWallStart)
                        {
                            bottomLine.Append(ColoredWall(CORNER_BOTTOM_LEFT));
                        }
                        else
                        {
                            bottomLine.Append(ColoredWall(WALL_HORIZONTAL));
                        }

                        if (cell.HasSouthDoor)
                        {
                            bottomLine.Append(ColoredDoor(DOOR_HORIZONTAL));
                        }
                        else
                        {
                            bottomLine.Append(ColoredWall(WALL_HORIZONTAL));
                        }

                        bool isWallEnd = rightCell == null || rightCell.IsEmpty;
                        if (isWallEnd)
                        {
                            bottomLine.Append(ColoredWall(CORNER_BOTTOM_RIGHT));
                        }
                    }
                    else
                    {
                        bottomLine.Append("  ");
                    }
                }
                if (bottomLine.Length > 0)
                {
                    sb.AppendLine(bottomLine.ToString());
                }
            }
        }

        private bool HasSouthWallInRow(DungeonMapData map, int y)
        {
            if (y <= 0) return false;
            for (int x = 0; x < map.Width; x++)
            {
                var cell = map.GetCell(x, y);
                var bottomCell = map.GetCell(x, y - 1);
                if (cell != null && !cell.IsEmpty && (bottomCell == null || bottomCell.IsEmpty))
                {
                    return true;
                }
            }
            return false;
        }
    }
} 