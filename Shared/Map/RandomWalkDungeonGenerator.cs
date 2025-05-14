using System;
using System.Collections.Generic;

namespace VibeMUC.Map
{
    public class RandomWalkDungeonGenerator : DungeonGeneratorBase
    {
        public override string Name => "Random Walk Generator";
        public override string Description => "Generates a room with four random walk passages";

        private const int ROOM_SIZE = 3;
        private const int MIN_WALK_STEPS = 2;
        private const int MAX_WALK_STEPS = 8;
        private const int MAX_POSSIBLE_STEPS = 16;
        private const double INITIAL_CONTINUE_CHANCE = 0.75;
        private const double TURN_CHANCE = 0.5; // 50% chance to turn vs continue straight
        private const int MIN_END_ROOM_SIZE = 2;
        private const int MAX_END_ROOM_SIZE = 4;
        private const int MIN_WALL_DISTANCE_FROM_EDGE = 3;
        private const double ADDITIONAL_DOOR_CHANCE = 0.5;
        private const double EDGE_DOOR_REDUCTION = 0.15; // Reduce door chance by 15% per step closer to edge
        private const int MIN_DOOR_SPACING = 2;

        private enum Direction
        {
            North,
            South,
            East,
            West
        }

        private enum TurnDirection
        {
            None,
            Left,
            Right
        }

        private record struct TurnPoint(int X, int Y, Direction OriginalDirection, Direction TurnedDirection);

        private record struct UnconnectedDoor(int X, int Y, Direction Direction);

        private HashSet<UnconnectedDoor> _unconnectedDoors;

        private Direction TurnLeft(Direction current)
        {
            return current switch
            {
                Direction.North => Direction.West,
                Direction.South => Direction.East,
                Direction.East => Direction.North,
                Direction.West => Direction.South,
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        private Direction TurnRight(Direction current)
        {
            return current switch
            {
                Direction.North => Direction.East,
                Direction.South => Direction.West,
                Direction.East => Direction.South,
                Direction.West => Direction.North,
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        private Direction GetOppositeDirection(Direction dir)
        {
            return dir switch
            {
                Direction.North => Direction.South,
                Direction.South => Direction.North,
                Direction.East => Direction.West,
                Direction.West => Direction.East,
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        protected override DungeonMapData GenerateInternal(int width, int height)
        {
            var map = CreateEmptyMap(width, height);
            _unconnectedDoors = new HashSet<UnconnectedDoor>();
            
            // Place initial 3x3 room in the center
            int centerX = width / 2;
            int centerY = height / 2;
            PlaceRoom(map, centerX - 1, centerY - 1);

            // Generate initial walks from each door
            GenerateWalk(map, centerX, centerY + 1, Direction.North);
            GenerateWalk(map, centerX, centerY - 1, Direction.South);
            GenerateWalk(map, centerX + 1, centerY, Direction.East);
            GenerateWalk(map, centerX - 1, centerY, Direction.West);

            // Process any unconnected doors until there are none left
            ProcessUnconnectedDoors(map);

            // Add walls between placed tiles and empty spaces
            AddBorderWalls(map);

            return map;
        }

        private void PlaceRoom(DungeonMapData map, int x, int y)
        {
            // Create 3x3 room
            for (int roomY = y; roomY < y + ROOM_SIZE; roomY++)
            {
                for (int roomX = x; roomX < x + ROOM_SIZE; roomX++)
                {
                    var cell = GetCell(map, roomX, roomY);
                    if (cell != null)
                    {
                        cell.IsEmpty = false;
                        cell.IsPassable = true;
                        
                        // Add walls on room edges
                        bool isNorthEdge = roomY == y + ROOM_SIZE - 1;
                        bool isSouthEdge = roomY == y;
                        bool isWestEdge = roomX == x;
                        bool isEastEdge = roomX == x + ROOM_SIZE - 1;

                        cell.HasNorthWall = isNorthEdge;
                        cell.HasSouthWall = isSouthEdge;
                        cell.HasWestWall = isWestEdge;
                        cell.HasEastWall = isEastEdge;

                        // Add doors in the middle of each wall
                        bool isCenterX = roomX == x + 1;
                        bool isCenterY = roomY == y + 1;

                        if (isCenterX && isNorthEdge) cell.HasNorthDoor = true;
                        if (isCenterX && isSouthEdge) cell.HasSouthDoor = true;
                        if (isCenterY && isEastEdge) cell.HasEastDoor = true;
                        if (isCenterY && isWestEdge) cell.HasWestDoor = true;
                    }
                }
            }
        }

        private bool HasNearbyDoor(DungeonMapData map, int x, int y, Direction wallDirection)
        {
            // Check MIN_DOOR_SPACING cells in both directions
            // This means checking up to MIN_DOOR_SPACING * 2 + 1 cells total
            // to ensure MIN_DOOR_SPACING empty walls between doors
            for (int offset = -MIN_DOOR_SPACING * 2; offset <= MIN_DOOR_SPACING * 2; offset++)
            {
                if (offset == 0) continue; // Skip the current cell

                CellData cellToCheck = null;
                switch (wallDirection)
                {
                    case Direction.North:
                    case Direction.South:
                        // Check horizontally along wall
                        cellToCheck = GetCell(map, x + offset, y);
                        break;
                    case Direction.East:
                    case Direction.West:
                        // Check vertically along wall
                        cellToCheck = GetCell(map, x, y + offset);
                        break;
                }

                if (cellToCheck != null)
                {
                    // Check if this cell has a door in the same direction
                    bool hasDoor = wallDirection switch
                    {
                        Direction.North => cellToCheck.HasNorthDoor,
                        Direction.South => cellToCheck.HasSouthDoor,
                        Direction.East => cellToCheck.HasEastDoor,
                        Direction.West => cellToCheck.HasWestDoor,
                        _ => false
                    };

                    if (hasDoor) return true;
                }
            }
            return false;
        }

        private double GetDoorChanceAtPosition(DungeonMapData map, int x, int y)
        {
            // Calculate distance from each edge
            int distanceFromLeft = x;
            int distanceFromRight = map.Width - 1 - x;
            int distanceFromBottom = y;
            int distanceFromTop = map.Height - 1 - y;

            // Get the minimum distance to any edge
            int minDistance = Math.Min(Math.Min(distanceFromLeft, distanceFromRight),
                                     Math.Min(distanceFromBottom, distanceFromTop));

            // Calculate reduction based on distance
            // Full chance at MIN_WALL_DISTANCE_FROM_EDGE or more steps from edge
            // Reduced chance closer to edge
            if (minDistance >= MIN_WALL_DISTANCE_FROM_EDGE)
            {
                return ADDITIONAL_DOOR_CHANCE;
            }
            else
            {
                double reduction = (MIN_WALL_DISTANCE_FROM_EDGE - minDistance) * EDGE_DOOR_REDUCTION;
                return Math.Max(0.0, ADDITIONAL_DOOR_CHANCE - reduction);
            }
        }

        private void AddUnconnectedDoor(int x, int y, Direction direction)
        {
            _unconnectedDoors.Add(new UnconnectedDoor(x, y, direction));
        }

        private void RemoveUnconnectedDoor(int x, int y, Direction direction)
        {
            _unconnectedDoors.Remove(new UnconnectedDoor(x, y, direction));
        }

        private void ProcessUnconnectedDoors(DungeonMapData map)
        {
            while (_unconnectedDoors.Count > 0)
            {
                // Process each unconnected door
                foreach (var door in _unconnectedDoors.ToList()) // Create a copy to iterate over
                {
                    // Calculate the starting position for the walk based on door direction
                    int startX = door.X;
                    int startY = door.Y;
                    
                    switch (door.Direction)
                    {
                        case Direction.North:
                            startY++;
                            break;
                        case Direction.South:
                            startY--;
                            break;
                        case Direction.East:
                            startX++;
                            break;
                        case Direction.West:
                            startX--;
                            break;
                    }

                    // Remove this door from unconnected set before generating the walk
                    RemoveUnconnectedDoor(door.X, door.Y, door.Direction);

                    // Generate a new walk from this door
                    GenerateWalk(map, startX, startY, door.Direction);
                }
            }
        }

        private void AddRandomDoorsToRoom(DungeonMapData map, int roomX, int roomY, int width, int height, Direction entryDirection)
        {
            // Check each wall of the room
            // North wall
            if (roomY + height - 1 < map.Height - MIN_WALL_DISTANCE_FROM_EDGE)
            {
                // Skip the wall we entered from
                if (entryDirection != Direction.North)
                {
                    // Check each position along the wall
                    for (int x = roomX; x < roomX + width; x++)
                    {
                        // Skip corners
                        if (x == roomX || x == roomX + width - 1) continue;
                        
                        double doorChance = GetDoorChanceAtPosition(map, x, roomY + height - 1);
                        if (Random.NextDouble() < doorChance && 
                            !HasNearbyDoor(map, x, roomY + height - 1, Direction.North))
                        {
                            var cell = GetCell(map, x, roomY + height - 1);
                            if (cell != null)
                            {
                                cell.HasNorthDoor = true;
                                AddUnconnectedDoor(x, roomY + height - 1, Direction.North);
                            }
                        }
                    }
                }
            }

            // South wall
            if (roomY >= MIN_WALL_DISTANCE_FROM_EDGE)
            {
                if (entryDirection != Direction.South)
                {
                    for (int x = roomX; x < roomX + width; x++)
                    {
                        if (x == roomX || x == roomX + width - 1) continue;
                        
                        double doorChance = GetDoorChanceAtPosition(map, x, roomY);
                        if (Random.NextDouble() < doorChance && 
                            !HasNearbyDoor(map, x, roomY, Direction.South))
                        {
                            var cell = GetCell(map, x, roomY);
                            if (cell != null)
                            {
                                cell.HasSouthDoor = true;
                                AddUnconnectedDoor(x, roomY, Direction.South);
                            }
                        }
                    }
                }
            }

            // East wall
            if (roomX + width - 1 < map.Width - MIN_WALL_DISTANCE_FROM_EDGE)
            {
                if (entryDirection != Direction.East)
                {
                    for (int y = roomY; y < roomY + height; y++)
                    {
                        if (y == roomY || y == roomY + height - 1) continue;
                        
                        double doorChance = GetDoorChanceAtPosition(map, roomX + width - 1, y);
                        if (Random.NextDouble() < doorChance && 
                            !HasNearbyDoor(map, roomX + width - 1, y, Direction.East))
                        {
                            var cell = GetCell(map, roomX + width - 1, y);
                            if (cell != null)
                            {
                                cell.HasEastDoor = true;
                                AddUnconnectedDoor(roomX + width - 1, y, Direction.East);
                            }
                        }
                    }
                }
            }

            // West wall
            if (roomX >= MIN_WALL_DISTANCE_FROM_EDGE)
            {
                if (entryDirection != Direction.West)
                {
                    for (int y = roomY; y < roomY + height; y++)
                    {
                        if (y == roomY || y == roomY + height - 1) continue;
                        
                        double doorChance = GetDoorChanceAtPosition(map, roomX, y);
                        if (Random.NextDouble() < doorChance && 
                            !HasNearbyDoor(map, roomX, y, Direction.West))
                        {
                            var cell = GetCell(map, roomX, y);
                            if (cell != null)
                            {
                                cell.HasWestDoor = true;
                                AddUnconnectedDoor(roomX, y, Direction.West);
                            }
                        }
                    }
                }
            }
        }

        private bool PlaceEndRoom(DungeonMapData map, int doorX, int doorY, Direction direction)
        {
            // Initial room size
            int width = Random.Next(MIN_END_ROOM_SIZE, MAX_END_ROOM_SIZE + 1);
            int height = Random.Next(MIN_END_ROOM_SIZE, MAX_END_ROOM_SIZE + 1);
            
            // Keep track of original dimensions for centering calculations
            int originalWidth = width;
            int originalHeight = height;
            
            // Calculate initial room position
            int roomX = doorX;
            int roomY = doorY;
            
            switch (direction)
            {
                case Direction.North:
                    roomY += 1; // Room extends northward
                    roomX -= originalWidth / 2; // Center horizontally on door
                    break;
                case Direction.South:
                    roomY -= originalHeight; // Room extends southward
                    roomX -= originalWidth / 2; // Center horizontally on door
                    break;
                case Direction.East:
                    roomX += 1; // Room extends eastward
                    roomY -= originalHeight / 2; // Center vertically on door
                    break;
                case Direction.West:
                    roomX -= originalWidth; // Room extends westward
                    roomY -= originalHeight / 2; // Center vertically on door
                    break;
            }

            // Adjust room size and position until it fits
            bool roomFits = false;
            while (!roomFits && width >= MIN_END_ROOM_SIZE && height >= MIN_END_ROOM_SIZE)
            {
                // Check if room would go out of bounds
                if (roomX <= 0 || roomX + width >= map.Width - 1 ||
                    roomY <= 0 || roomY + height >= map.Height - 1)
                {
                    // Shrink room and recenter
                    if (direction == Direction.North || direction == Direction.South)
                    {
                        width--;
                        roomX = doorX - width / 2;
                    }
                    else
                    {
                        height--;
                        roomY = doorY - height / 2;
                    }
                    continue;
                }

                // Check for overlaps with existing tiles
                bool hasOverlap = false;
                for (int y = roomY; y < roomY + height && !hasOverlap; y++)
                {
                    for (int x = roomX; x < roomX + width && !hasOverlap; x++)
                    {
                        var cell = GetCell(map, x, y);
                        if (cell != null && !cell.IsEmpty)
                        {
                            hasOverlap = true;
                        }
                    }
                }

                if (!hasOverlap)
                {
                    roomFits = true;
                }
                else
                {
                    // Shrink room based on direction
                    if (direction == Direction.North || direction == Direction.South)
                    {
                        width--;
                        roomX = doorX - width / 2;
                    }
                    else
                    {
                        height--;
                        roomY = doorY - height / 2;
                    }
                }
            }

            // If room is too small, don't place it
            if (!roomFits || width < MIN_END_ROOM_SIZE || height < MIN_END_ROOM_SIZE)
            {
                return false;
            }

            // Place the room
            for (int y = roomY; y < roomY + height; y++)
            {
                for (int x = roomX; x < roomX + width; x++)
                {
                    var cell = GetCell(map, x, y);
                    if (cell != null)
                    {
                        cell.IsEmpty = false;
                        cell.IsPassable = true;

                        // Add walls on room edges
                        bool isNorthEdge = y == roomY + height - 1;
                        bool isSouthEdge = y == roomY;
                        bool isWestEdge = x == roomX;
                        bool isEastEdge = x == roomX + width - 1;

                        cell.HasNorthWall = isNorthEdge;
                        cell.HasSouthWall = isSouthEdge;
                        cell.HasWestWall = isWestEdge;
                        cell.HasEastWall = isEastEdge;
                    }
                }
            }

            // Add random doors to walls that are far enough from map edges
            AddRandomDoorsToRoom(map, roomX, roomY, width, height, direction);

            return true;
        }

        private void GenerateWalk(DungeonMapData map, int startX, int startY, Direction direction)
        {
            int currentX = startX;
            int currentY = startY;
            int steps = Random.Next(MIN_WALK_STEPS, MAX_WALK_STEPS + 1);
            int totalSteps = 0;
            TurnDirection lastTurn = TurnDirection.None;
            
            // Track turn points for potential branching
            var turnPoints = new List<TurnPoint>();
            Direction currentDirection = direction;

            // Create initial passable tile at the starting position
            var startCell = GetCell(map, startX, startY);
            if (startCell != null)
            {
                startCell.IsEmpty = false;
                startCell.IsPassable = true;
                startCell.HasNorthWall = false;
                startCell.HasSouthWall = false;
                startCell.HasEastWall = false;
                startCell.HasWestWall = false;
            }

            while (totalSteps < MAX_POSSIBLE_STEPS)
            {
                // Calculate next position based on direction
                int nextX = currentX;
                int nextY = currentY;
                switch (currentDirection)
                {
                    case Direction.North:
                        nextY++;
                        break;
                    case Direction.South:
                        nextY--;
                        break;
                    case Direction.East:
                        nextX++;
                        break;
                    case Direction.West:
                        nextX--;
                        break;
                }

                // Check if we're still within map bounds
                if (nextX <= 0 || nextX >= map.Width - 1 || 
                    nextY <= 0 || nextY >= map.Height - 1)
                    break;

                // Check if we've hit a room wall
                var nextCell = GetCell(map, nextX, nextY);
                if (nextCell != null && !nextCell.IsEmpty)
                {
                    // Check if this is a wall cell (has any walls)
                    bool hasWall = false;
                    switch (currentDirection)
                    {
                        case Direction.North:
                            hasWall = nextCell.HasSouthWall;
                            break;
                        case Direction.South:
                            hasWall = nextCell.HasNorthWall;
                            break;
                        case Direction.East:
                            hasWall = nextCell.HasWestWall;
                            break;
                        case Direction.West:
                            hasWall = nextCell.HasEastWall;
                            break;
                    }

                    if (hasWall)
                    {
                        // Check if there's a door nearby before placing one
                        var currentCell = GetCell(map, currentX, currentY);
                        if (currentCell != null && !HasNearbyDoor(map, currentX, currentY, currentDirection))
                        {
                            switch (currentDirection)
                            {
                                case Direction.North:
                                    currentCell.HasNorthWall = true;
                                    currentCell.HasNorthDoor = true;
                                    nextCell.HasSouthDoor = true;
                                    RemoveUnconnectedDoor(currentX, currentY, Direction.North);
                                    break;
                                case Direction.South:
                                    currentCell.HasSouthWall = true;
                                    currentCell.HasSouthDoor = true;
                                    nextCell.HasNorthDoor = true;
                                    RemoveUnconnectedDoor(currentX, currentY, Direction.South);
                                    break;
                                case Direction.East:
                                    currentCell.HasEastWall = true;
                                    currentCell.HasEastDoor = true;
                                    nextCell.HasWestDoor = true;
                                    RemoveUnconnectedDoor(currentX, currentY, Direction.East);
                                    break;
                                case Direction.West:
                                    currentCell.HasWestWall = true;
                                    currentCell.HasWestDoor = true;
                                    nextCell.HasEastDoor = true;
                                    RemoveUnconnectedDoor(currentX, currentY, Direction.West);
                                    break;
                            }
                        }
                        // Process turn points before returning
                        ProcessTurnPoints(map, turnPoints);
                        return;
                    }
                }

                // Move to next position
                currentX = nextX;
                currentY = nextY;

                // Create passable tile with no walls
                var cell = GetCell(map, currentX, currentY);
                if (cell != null)
                {
                    cell.IsEmpty = false;
                    cell.IsPassable = true;
                    cell.HasNorthWall = false;
                    cell.HasSouthWall = false;
                    cell.HasEastWall = false;
                    cell.HasWestWall = false;
                }

                totalSteps++;

                // After minimum steps, check if we should continue
                if (totalSteps >= MIN_WALK_STEPS)
                {
                    // Calculate continuation chance
                    double continueChance = INITIAL_CONTINUE_CHANCE * (1 - ((double)(totalSteps - MIN_WALK_STEPS) / (MAX_POSSIBLE_STEPS - MIN_WALK_STEPS)));
                    
                    if (Random.NextDouble() > continueChance)
                    {
                        break;
                    }

                    // Decide if we should turn
                    if (Random.NextDouble() < TURN_CHANCE)
                    {
                        // Determine turn direction based on last turn
                        TurnDirection newTurn = lastTurn switch
                        {
                            TurnDirection.None => Random.NextDouble() < 0.5 ? TurnDirection.Left : TurnDirection.Right,
                            TurnDirection.Left => TurnDirection.Right,
                            TurnDirection.Right => TurnDirection.Left,
                            _ => throw new ArgumentException("Invalid turn direction")
                        };

                        // Store the turn point before turning
                        turnPoints.Add(new TurnPoint(currentX, currentY, currentDirection, 
                            newTurn == TurnDirection.Left ? TurnLeft(currentDirection) : TurnRight(currentDirection)));

                        // Apply the turn
                        currentDirection = newTurn == TurnDirection.Left ? TurnLeft(currentDirection) : TurnRight(currentDirection);
                        lastTurn = newTurn;
                    }
                }
            }

            // If we didn't hit a room wall, place a door at the end of the walk
            var finalCell = GetCell(map, currentX, currentY);
            if (finalCell != null)
            {
                // Try to place the end room first
                bool roomPlaced = PlaceEndRoom(map, currentX, currentY, currentDirection);

                // Only add the door if the room was successfully placed and there's no nearby door
                if (roomPlaced && !HasNearbyDoor(map, currentX, currentY, currentDirection))
                {
                    switch (currentDirection)
                    {
                        case Direction.North:
                            finalCell.HasNorthWall = true;
                            finalCell.HasNorthDoor = true;
                            break;
                        case Direction.South:
                            finalCell.HasSouthWall = true;
                            finalCell.HasSouthDoor = true;
                            break;
                        case Direction.East:
                            finalCell.HasEastWall = true;
                            finalCell.HasEastDoor = true;
                            break;
                        case Direction.West:
                            finalCell.HasWestWall = true;
                            finalCell.HasWestDoor = true;
                            break;
                    }
                }
                else if (!roomPlaced)
                {
                    // If room couldn't be placed, just add a wall for the dead end
                    switch (currentDirection)
                    {
                        case Direction.North:
                            finalCell.HasNorthWall = true;
                            break;
                        case Direction.South:
                            finalCell.HasSouthWall = true;
                            break;
                        case Direction.East:
                            finalCell.HasEastWall = true;
                            break;
                        case Direction.West:
                            finalCell.HasWestWall = true;
                            break;
                    }
                }

                // Process turn points after placing the room
                ProcessTurnPoints(map, turnPoints);
            }
        }

        private void ProcessTurnPoints(DungeonMapData map, List<TurnPoint> turnPoints)
        {
            foreach (var turnPoint in turnPoints)
            {
                // 50% chance to create a new walk
                if (Random.NextDouble() < 0.5)
                {
                    // Get the direction we didn't take at this turn
                    Direction oppositeDirection = GetOppositeDirection(turnPoint.TurnedDirection);
                    
                    // Start a new walk in the opposite direction
                    GenerateWalk(map, turnPoint.X, turnPoint.Y, oppositeDirection);
                }
            }
        }

        private void AddBorderWalls(DungeonMapData map)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var currentCell = GetCell(map, x, y);
                    if (currentCell == null || currentCell.IsEmpty)
                    {
                        continue;
                    }

                    // Check north neighbor
                    var northCell = GetCell(map, x, y + 1);
                    if (northCell == null || northCell.IsEmpty)
                    {
                        currentCell.HasNorthWall = true;
                    }

                    // Check south neighbor
                    var southCell = GetCell(map, x, y - 1);
                    if (southCell == null || southCell.IsEmpty)
                    {
                        currentCell.HasSouthWall = true;
                    }

                    // Check east neighbor
                    var eastCell = GetCell(map, x + 1, y);
                    if (eastCell == null || eastCell.IsEmpty)
                    {
                        currentCell.HasEastWall = true;
                    }

                    // Check west neighbor
                    var westCell = GetCell(map, x - 1, y);
                    if (westCell == null || westCell.IsEmpty)
                    {
                        currentCell.HasWestWall = true;
                    }
                }
            }
        }
    }
} 