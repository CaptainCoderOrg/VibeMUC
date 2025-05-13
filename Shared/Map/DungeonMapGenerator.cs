using System;
using System.Collections.Generic;
using System.Linq;

namespace VibeMUC.Map
{
    public class DungeonMapGenerator
    {
        private readonly Random _random;
        private readonly int _width;
        private readonly int _height;
        private readonly List<Room> _rooms;
        private readonly List<Passage> _passages;
        private readonly CellData[] _cells;

        public DungeonMapGenerator(int width, int height, int? seed = null)
        {
            _width = width;
            _height = height;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _rooms = new List<Room>();
            _passages = new List<Passage>();
            _cells = new CellData[width * height];

            // Initialize all cells as empty
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new CellData { IsEmpty = true };
            }
        }

        public DungeonMapData Generate(int minRooms = 5, int maxRooms = 8)
        {
            // Step 1: Generate rooms
            GenerateRooms(minRooms, maxRooms);
            
            // Step 2: Connect rooms with passages
            ConnectRooms();
            
            // Step 3: Create the final map data
            return CreateMapData();
        }

        private void GenerateRooms(int minRooms, int maxRooms)
        {
            int numRooms = _random.Next(minRooms, maxRooms + 1);
            int maxAttempts = 100; // Prevent infinite loops
            int attempts = 0;

            while (_rooms.Count < numRooms && attempts < maxAttempts)
            {
                attempts++;
                
                // Randomly decide if this room should be rectangular or circular
                bool isCircular = _random.Next(2) == 0;
                
                Room newRoom;
                if (isCircular)
                {
                    newRoom = GenerateCircularRoom();
                }
                else
                {
                    newRoom = GenerateRectangularRoom();
                }

                // Check if room overlaps with existing rooms
                if (!DoesRoomOverlap(newRoom))
                {
                    _rooms.Add(newRoom);
                    PlaceRoom(newRoom);
                }
            }
        }

        private Room GenerateRectangularRoom()
        {
            int minSize = 3;
            int maxWidth = Math.Min(8, _width / 3);
            int maxHeight = Math.Min(8, _height / 3);

            int width = _random.Next(minSize, maxWidth + 1);
            int height = _random.Next(minSize, maxHeight + 1);
            int x = _random.Next(1, _width - width - 1);
            int y = _random.Next(1, _height - height - 1);

            return new Room
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                IsCircular = false
            };
        }

        private Room GenerateCircularRoom()
        {
            int minRadius = 2;
            int maxRadius = Math.Min(4, Math.Min(_width, _height) / 6);
            int radius = _random.Next(minRadius, maxRadius + 1);
            
            int x = _random.Next(radius + 1, _width - radius - 1);
            int y = _random.Next(radius + 1, _height - radius - 1);

            return new Room
            {
                X = x,
                Y = y,
                Width = radius * 2 + 1,
                Height = radius * 2 + 1,
                IsCircular = true,
                Radius = radius
            };
        }

        private bool DoesRoomOverlap(Room newRoom)
        {
            foreach (var existingRoom in _rooms)
            {
                // Add buffer space between rooms
                const int buffer = 2;
                
                int newLeft = newRoom.X - buffer;
                int newRight = newRoom.X + newRoom.Width + buffer;
                int newTop = newRoom.Y + newRoom.Height + buffer;
                int newBottom = newRoom.Y - buffer;

                int existingLeft = existingRoom.X - buffer;
                int existingRight = existingRoom.X + existingRoom.Width + buffer;
                int existingTop = existingRoom.Y + existingRoom.Height + buffer;
                int existingBottom = existingRoom.Y - buffer;

                if (!(newLeft > existingRight ||
                    newRight < existingLeft ||
                    newBottom > existingTop ||
                    newTop < existingBottom))
                {
                    return true;
                }
            }
            return false;
        }

        private void PlaceRoom(Room room)
        {
            if (room.IsCircular)
            {
                PlaceCircularRoom(room);
            }
            else
            {
                PlaceRectangularRoom(room);
            }
        }

        private void PlaceRectangularRoom(Room room)
        {
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    int index = y * _width + x;
                    _cells[index].IsEmpty = false;
                    _cells[index].IsPassable = true;

                    // Add walls
                    bool isEdge = x == room.X || x == room.X + room.Width - 1 ||
                                y == room.Y || y == room.Y + room.Height - 1;
                    
                    if (isEdge)
                    {
                        _cells[index].HasNorthWall = y == room.Y + room.Height - 1;
                        _cells[index].HasSouthWall = y == room.Y;
                        _cells[index].HasEastWall = x == room.X + room.Width - 1;
                        _cells[index].HasWestWall = x == room.X;
                    }
                }
            }
        }

        private void PlaceCircularRoom(Room room)
        {
            int centerX = room.X;
            int centerY = room.Y;
            int radius = room.Radius;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    double distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    if (distance <= radius)
                    {
                        int index = y * _width + x;
                        _cells[index].IsEmpty = false;
                        _cells[index].IsPassable = true;

                        // Add walls for cells at the edge of the circle
                        if (distance >= radius - 1)
                        {
                            bool hasNorthNeighbor = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y + 1 - centerY, 2)) <= radius;
                            bool hasSouthNeighbor = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - 1 - centerY, 2)) <= radius;
                            bool hasEastNeighbor = Math.Sqrt(Math.Pow(x + 1 - centerX, 2) + Math.Pow(y - centerY, 2)) <= radius;
                            bool hasWestNeighbor = Math.Sqrt(Math.Pow(x - 1 - centerX, 2) + Math.Pow(y - centerY, 2)) <= radius;

                            _cells[index].HasNorthWall = !hasNorthNeighbor;
                            _cells[index].HasSouthWall = !hasSouthNeighbor;
                            _cells[index].HasEastWall = !hasEastNeighbor;
                            _cells[index].HasWestWall = !hasWestNeighbor;
                        }
                    }
                }
            }
        }

        private void ConnectRooms()
        {
            // Create a list of rooms that still need to be connected
            var unconnectedRooms = new List<Room>(_rooms);
            var connectedRooms = new List<Room>();

            // Start with a random room
            var firstRoom = unconnectedRooms[_random.Next(unconnectedRooms.Count)];
            unconnectedRooms.Remove(firstRoom);
            connectedRooms.Add(firstRoom);

            // Connect all rooms
            while (unconnectedRooms.Count > 0)
            {
                // Find the closest pair of unconnected rooms
                Room roomToConnect = unconnectedRooms[_random.Next(unconnectedRooms.Count)];
                Room closestRoom = FindClosestRoom(roomToConnect, connectedRooms);

                // Create a passage between them
                CreatePassage(roomToConnect, closestRoom);

                // Update room lists
                unconnectedRooms.Remove(roomToConnect);
                connectedRooms.Add(roomToConnect);
            }

            // Add some extra connections for variety (creating loops)
            int extraConnections = _random.Next(1, Math.Max(2, _rooms.Count / 2));
            for (int i = 0; i < extraConnections; i++)
            {
                Room room1 = _rooms[_random.Next(_rooms.Count)];
                Room room2 = FindClosestUnconnectedRoom(room1, _rooms);
                if (room2 != null)
                {
                    CreatePassage(room1, room2);
                }
            }
        }

        private Room FindClosestRoom(Room room, List<Room> otherRooms)
        {
            Room closest = otherRooms[0];
            double minDistance = double.MaxValue;

            foreach (var otherRoom in otherRooms)
            {
                if (otherRoom == room) continue;

                double distance = Math.Sqrt(
                    Math.Pow(room.X - otherRoom.X, 2) +
                    Math.Pow(room.Y - otherRoom.Y, 2)
                );

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = otherRoom;
                }
            }

            return closest;
        }

        private Room FindClosestUnconnectedRoom(Room room, List<Room> allRooms)
        {
            var connectedRooms = _passages
                .Where(p => p.Room1 == room || p.Room2 == room)
                .SelectMany(p => new[] { p.Room1, p.Room2 })
                .Distinct()
                .ToList();

            var unconnectedRooms = allRooms
                .Where(r => r != room && !connectedRooms.Contains(r))
                .ToList();

            return unconnectedRooms.Count > 0 ? 
                FindClosestRoom(room, unconnectedRooms) : null;
        }

        private void CreatePassage(Room room1, Room room2)
        {
            // Determine passage type
            PassageType type = (PassageType)_random.Next(3);

            // Create passage based on type
            var passage = new Passage
            {
                Room1 = room1,
                Room2 = room2,
                Type = type
            };

            switch (type)
            {
                case PassageType.Straight:
                    CreateStraightPassage(room1, room2);
                    break;
                case PassageType.TShaped:
                    CreateTShapedPassage(room1, room2);
                    break;
                case PassageType.XShaped:
                    CreateXShapedPassage(room1, room2);
                    break;
            }

            _passages.Add(passage);
        }

        private void CreateStraightPassage(Room room1, Room room2)
        {
            // Calculate center points
            int x1 = room1.X + room1.Width / 2;
            int y1 = room1.Y + room1.Height / 2;
            int x2 = room2.X + room2.Width / 2;
            int y2 = room2.Y + room2.Height / 2;

            // Create horizontal then vertical passage
            for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            {
                PlacePassageCell(x, y1);
            }
            for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            {
                PlacePassageCell(x2, y);
            }

            // Add doors where passages meet rooms
            AddDoorToRoom(room1, x1, y1);
            AddDoorToRoom(room2, x2, y2);
        }

        private void CreateTShapedPassage(Room room1, Room room2)
        {
            // Similar to straight passage but with an extra perpendicular section
            CreateStraightPassage(room1, room2);

            // Add a perpendicular section in the middle
            int midX = (room1.X + room2.X) / 2;
            int midY = (room1.Y + room2.Y) / 2;
            int length = _random.Next(3, 6);

            bool isHorizontal = _random.Next(2) == 0;
            if (isHorizontal)
            {
                for (int x = midX - length; x <= midX + length; x++)
                {
                    PlacePassageCell(x, midY);
                }
            }
            else
            {
                for (int y = midY - length; y <= midY + length; y++)
                {
                    PlacePassageCell(midX, y);
                }
            }
        }

        private void CreateXShapedPassage(Room room1, Room room2)
        {
            // Create the main passage
            CreateStraightPassage(room1, room2);

            // Add two perpendicular sections
            int midX = (room1.X + room2.X) / 2;
            int midY = (room1.Y + room2.Y) / 2;
            int length = _random.Next(3, 6);

            // Add horizontal section
            for (int x = midX - length; x <= midX + length; x++)
            {
                PlacePassageCell(x, midY);
            }

            // Add vertical section
            for (int y = midY - length; y <= midY + length; y++)
            {
                PlacePassageCell(midX, y);
            }
        }

        private void PlacePassageCell(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            int index = y * _width + x;
            _cells[index].IsEmpty = false;
            _cells[index].IsPassable = true;

            // Add walls to the sides of the passage
            bool hasNorthCell = y < _height - 1 && !_cells[(y + 1) * _width + x].IsEmpty;
            bool hasSouthCell = y > 0 && !_cells[(y - 1) * _width + x].IsEmpty;
            bool hasEastCell = x < _width - 1 && !_cells[y * _width + (x + 1)].IsEmpty;
            bool hasWestCell = x > 0 && !_cells[y * _width + (x - 1)].IsEmpty;

            _cells[index].HasNorthWall = !hasNorthCell;
            _cells[index].HasSouthWall = !hasSouthCell;
            _cells[index].HasEastWall = !hasEastCell;
            _cells[index].HasWestWall = !hasWestCell;
        }

        private void AddDoorToRoom(Room room, int x, int y)
        {
            int index = y * _width + x;
            
            // Determine which wall the door should be on
            if (y == room.Y && _cells[index].HasSouthWall)
            {
                _cells[index].HasSouthWall = true;
                _cells[index].HasSouthDoor = true;
            }
            else if (y == room.Y + room.Height - 1 && _cells[index].HasNorthWall)
            {
                _cells[index].HasNorthWall = true;
                _cells[index].HasNorthDoor = true;
            }
            else if (x == room.X && _cells[index].HasWestWall)
            {
                _cells[index].HasWestWall = true;
                _cells[index].HasWestDoor = true;
            }
            else if (x == room.X + room.Width - 1 && _cells[index].HasEastWall)
            {
                _cells[index].HasEastWall = true;
                _cells[index].HasEastDoor = true;
            }
        }

        private DungeonMapData CreateMapData()
        {
            var map = new DungeonMapData
            {
                Width = _width,
                Height = _height,
                MapName = $"Generated Map {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                FloorLevel = 1,
                Cells = _cells
            };

            return map;
        }

        public void LoadFromMapData(DungeonMapData mapData)
        {
            if (mapData.Width != _width || mapData.Height != _height)
            {
                throw new ArgumentException("Map dimensions do not match generator dimensions");
            }

            // Copy cells
            for (int i = 0; i < mapData.Cells.Length; i++)
            {
                _cells[i] = mapData.Cells[i];
            }
        }

        private class Room
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsCircular { get; set; }
            public int Radius { get; set; }
        }

        private class Passage
        {
            public Room Room1 { get; set; }
            public Room Room2 { get; set; }
            public PassageType Type { get; set; }
        }

        private enum PassageType
        {
            Straight,
            TShaped,
            XShaped
        }
    }
} 