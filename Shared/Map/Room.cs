using System;
using System.Collections.Generic;

namespace VibeMUC.Map
{
    /// <summary>
    /// Represents a rectangular room in the dungeon.
    /// </summary>
    public class Room
    {
        /// <summary>
        /// The X coordinate of the room's left edge.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// The Y coordinate of the room's bottom edge.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// The width of the room (including walls).
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the room (including walls).
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets the X coordinate of the room's right edge.
        /// </summary>
        public int Right => X + Width - 1;

        /// <summary>
        /// Gets the Y coordinate of the room's top edge.
        /// </summary>
        public int Top => Y + Height - 1;

        /// <summary>
        /// Gets the center X coordinate of the room.
        /// </summary>
        public int CenterX => X + Width / 2;

        /// <summary>
        /// Gets the center Y coordinate of the room.
        /// </summary>
        public int CenterY => Y + Height / 2;

        /// <summary>
        /// List of door positions in this room.
        /// </summary>
        public List<(int x, int y, bool isHorizontal)> Doors { get; } = new List<(int x, int y, bool isHorizontal)>();

        /// <summary>
        /// Checks if this room overlaps with another room (including walls).
        /// </summary>
        public bool Overlaps(Room other, int padding = 0)
        {
            return !(X - padding >= other.Right + padding + 1 ||
                    Right + padding + 1 <= other.X - padding ||
                    Y - padding >= other.Top + padding + 1 ||
                    Top + padding + 1 <= other.Y - padding);
        }

        /// <summary>
        /// Gets the Manhattan distance to another room's center.
        /// </summary>
        public int DistanceTo(Room other)
        {
            return Math.Abs(CenterX - other.CenterX) + Math.Abs(CenterY - other.CenterY);
        }

        /// <summary>
        /// Gets all possible door positions for this room.
        /// </summary>
        public List<(int x, int y, bool isHorizontal)> GetPossibleDoorPositions()
        {
            var positions = new List<(int x, int y, bool isHorizontal)>();

            // North wall (excluding corners)
            for (int x = X + 1; x < Right; x++)
                positions.Add((x, Top, true));

            // South wall (excluding corners)
            for (int x = X + 1; x < Right; x++)
                positions.Add((x, Y, true));

            // West wall (excluding corners)
            for (int y = Y + 1; y < Top; y++)
                positions.Add((X, y, false));

            // East wall (excluding corners)
            for (int y = Y + 1; y < Top; y++)
                positions.Add((Right, y, false));

            return positions;
        }
    }
} 