using System;
using System.Collections.Generic;
using System.Linq;

namespace VibeMUC.Map
{
    /// <summary>
    /// Generates dungeons by placing rooms and connecting them with doors.
    /// </summary>
    public class RoomDungeonGenerator : DungeonGeneratorBase
    {
        public override string Name => "Room-based Generator";
        public override string Description => "Generates dungeons by placing rectangular rooms and connecting them with doors.";

        private const int MIN_ROOM_WIDTH = 2;
        private const int MIN_ROOM_HEIGHT = 3;
        private const int MAX_ROOM_SIZE = 6;
        private const int MAX_PLACEMENT_ATTEMPTS = 100;
        private const float ADDITIONAL_DOOR_CHANCE = 0.3f;

        protected override DungeonMapData GenerateInternal(int width, int height)
        {
            var map = CreateEmptyMap(width, height);
            var rooms = new List<Room>();

            // Try to place rooms
            int targetRoomCount = (width * height) / (MIN_ROOM_WIDTH * MIN_ROOM_HEIGHT * 3);  // Adjusted for smaller rooms
            int attempts = 0;

            while (rooms.Count < targetRoomCount && attempts < MAX_PLACEMENT_ATTEMPTS)
            {
                var room = GenerateRoom(width, height);
                bool canPlace = true;

                // Check if room overlaps with any existing rooms (including padding)
                foreach (var existingRoom in rooms)
                {
                    if (room.Overlaps(existingRoom, 1))  // 1 cell padding between rooms
                    {
                        canPlace = false;
                        break;
                    }
                }

                if (canPlace)
                {
                    rooms.Add(room);
                    PlaceRoom(map, room);
                }

                attempts++;
            }

            // Add doors to each room
            foreach (var room in rooms)
            {
                AddDoorsToRoom(room, rooms);
            }

            // Apply doors to the map
            foreach (var room in rooms)
            {
                foreach (var door in room.Doors)
                {
                    var cell = GetCell(map, door.x, door.y);
                    if (door.isHorizontal)
                    {
                        cell.HasNorthDoor = door.y > 0 && door.y < map.Height - 1 && door.y == room.Top;
                        cell.HasSouthDoor = door.y > 0 && door.y < map.Height - 1 && door.y == room.Y;
                    }
                    else
                    {
                        cell.HasWestDoor = door.x > 0 && door.x < map.Width - 1 && door.x == room.X;
                        cell.HasEastDoor = door.x > 0 && door.x < map.Width - 1 && door.x == room.Right;
                    }
                }
            }

            return map;
        }

        private Room GenerateRoom(int mapWidth, int mapHeight)
        {
            // Randomly decide if the room will be wider or taller
            bool isWider = Random.Next(2) == 0;
            
            int width, height;
            if (isWider)
            {
                // For wider rooms: minimum 3 width, minimum 2 height
                width = Random.Next(3, MAX_ROOM_SIZE + 1);
                height = Random.Next(MIN_ROOM_WIDTH, Math.Min(MAX_ROOM_SIZE, width) + 1);
            }
            else
            {
                // For taller rooms: minimum 3 height, minimum 2 width
                height = Random.Next(3, MAX_ROOM_SIZE + 1);
                width = Random.Next(MIN_ROOM_WIDTH, Math.Min(MAX_ROOM_SIZE, height) + 1);
            }

            int x = Random.Next(1, mapWidth - width);
            int y = Random.Next(1, mapHeight - height);

            return new Room
            {
                X = x,
                Y = y,
                Width = width,
                Height = height
            };
        }

        private void PlaceRoom(DungeonMapData map, Room room)
        {
            for (int y = room.Y; y <= room.Top; y++)
            {
                for (int x = room.X; x <= room.Right; x++)
                {
                    var cell = new CellData { IsEmpty = false, IsPassable = true };

                    // Add walls on room edges
                    bool isNorthEdge = y == room.Top;
                    bool isSouthEdge = y == room.Y;
                    bool isWestEdge = x == room.X;
                    bool isEastEdge = x == room.Right;

                    cell.HasNorthWall = isNorthEdge;
                    cell.HasSouthWall = isSouthEdge;
                    cell.HasWestWall = isWestEdge;
                    cell.HasEastWall = isEastEdge;

                    SetCell(map, x, y, cell);
                }
            }
        }

        private void AddDoorsToRoom(Room room, List<Room> allRooms)
        {
            var possibleDoors = room.GetPossibleDoorPositions();
            if (possibleDoors.Count == 0) return;

            // Always add at least one door
            var firstDoor = possibleDoors[Random.Next(possibleDoors.Count)];
            room.Doors.Add(firstDoor);

            // Potentially add more doors with decreasing probability
            while (room.Doors.Count < possibleDoors.Count && Random.NextDouble() < Math.Pow(ADDITIONAL_DOOR_CHANCE, room.Doors.Count))
            {
                var remainingDoors = possibleDoors.Where(d => !room.Doors.Contains(d)).ToList();
                if (remainingDoors.Count == 0) break;

                var nextDoor = remainingDoors[Random.Next(remainingDoors.Count)];
                room.Doors.Add(nextDoor);
            }
        }
    }
} 