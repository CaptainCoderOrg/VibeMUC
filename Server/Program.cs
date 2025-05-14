using System;
using System.Threading;
using System.Threading.Tasks;
using VibeMUC.Map;
using VibeMUC.Network;

namespace VibeMUC.Server
{
    public class Program
    {
        private static DungeonServer? _server;
        private static bool _isRunning = true;
        private static readonly CancellationTokenSource _cts = new();

        public static async Task Main(string[] args)
        {
            Console.WriteLine("VibeMUC Server");
            Console.WriteLine("==============");

            int port = NetworkConstants.DefaultPort;
            if (args.Length > 0 && int.TryParse(args[0], out int customPort))
            {
                port = customPort;
            }

            _server = new DungeonServer();
            SetupServerEvents();

            // Start server in background task
            var serverTask = RunServerAsync(port);

            // Start command processing
            await ProcessCommandsAsync();

            // Wait for server to stop
            await serverTask;
        }

        private static void SetupServerEvents()
        {
            if (_server == null) return;

            _server.OnClientConnected += (client) =>
            {
                Console.WriteLine($"Client connected: {client.Id}");
            };

            _server.OnClientDisconnected += (client) =>
            {
                Console.WriteLine($"Client disconnected: {client.Id}");
            };

            _server.OnError += (ex) =>
            {
                Console.WriteLine($"Server error: {ex.Message}");
            };
        }

        private static async Task RunServerAsync(int port)
        {
            try
            {
                if (_server != null)
                {
                    await _server.StartAsync(port);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal server error: {ex.Message}");
                _isRunning = false;
            }
        }

        private static async Task ProcessCommandsAsync()
        {
            Console.WriteLine("\nAvailable commands:");
            Console.WriteLine("  help     - Show this help message");
            Console.WriteLine("  clients  - List connected clients");
            Console.WriteLine("  maps     - List loaded maps");
            Console.WriteLine("  newmap   - Create a new test map");
            Console.WriteLine("  genmap   - Generate a new map with parameters:");
            Console.WriteLine("             genmap [type] [width] [height] [minRooms] [maxRooms] [seed]");
            Console.WriteLine("             type: 'room', 'passage', or 'walk' (default: passage)");
            Console.WriteLine("  showmap  - Display the current map in ASCII format");
            Console.WriteLine("  exit     - Stop server and exit");
            Console.WriteLine();

            while (_isRunning)
            {
                Console.Write("> ");
                string? command = Console.ReadLine()?.ToLower().Trim();
                string[] args = command?.Split(' ') ?? Array.Empty<string>();

                switch (args[0])
                {
                    case "help":
                        Console.WriteLine("\nAvailable commands:");
                        Console.WriteLine("  help     - Show this help message");
                        Console.WriteLine("  clients  - List connected clients");
                        Console.WriteLine("  maps     - List loaded maps");
                        Console.WriteLine("  newmap   - Create a new test map");
                        Console.WriteLine("  genmap   - Generate a new map with parameters:");
                        Console.WriteLine("             genmap [type] [width] [height] [minRooms] [maxRooms] [seed]");
                        Console.WriteLine("             type: 'room', 'passage', or 'walk' (default: passage)");
                        Console.WriteLine("  showmap  - Display the current map in ASCII format");
                        Console.WriteLine("  exit     - Stop server and exit");
                        break;

                    case "exit":
                        await ShutdownAsync();
                        break;

                    case "clients":
                        // This would need a new property/method in DungeonServer to expose client count
                        Console.WriteLine("Client list not implemented yet");
                        break;

                    case "maps":
                        // This would need a new property/method in DungeonServer to expose maps
                        Console.WriteLine("Map list not implemented yet");
                        break;

                    case "newmap":
                        // Create and store a new test map
                        if (_server != null)
                        {
                            var mapData = CreateNewMap();
                            _server.AddMap(1, mapData); // Using ID 1 for test
                            Console.WriteLine("Created new test map with ID: 1");
                        }
                        break;

                    case "genmap":
                        if (_server != null)
                        {
                            try
                            {
                                // Parse generator type (default to passage)
                                string generatorType = args.Length > 1 ? args[1].ToLower() : "passage";
                                
                                // Shift other parameters based on type parameter
                                int width = args.Length > 2 && int.TryParse(args[2], out int w) ? w : 30;
                                int height = args.Length > 3 && int.TryParse(args[3], out int h) ? h : 30;
                                int minRooms = args.Length > 4 && int.TryParse(args[4], out int min) ? min : 5;
                                int maxRooms = args.Length > 5 && int.TryParse(args[5], out int max) ? max : 8;
                                int? seed = args.Length > 6 && int.TryParse(args[6], out int s) ? s : null;

                                // Validate parameters
                                if (width < 10 || height < 10)
                                {
                                    Console.WriteLine("Width and height must be at least 10");
                                    break;
                                }
                                if (minRooms < 1 || maxRooms < minRooms)
                                {
                                    Console.WriteLine("Invalid room count parameters");
                                    break;
                                }

                                // Create appropriate generator
                                IDungeonGenerator generator;
                                DungeonMapData mapData;
                                switch (generatorType)
                                {
                                    case "room":
                                        generator = new RoomDungeonGenerator();
                                        mapData = generator.Generate(width, height, seed);
                                        break;
                                    case "walk":
                                        generator = new RandomWalkDungeonGenerator();
                                        mapData = generator.Generate(width, height, seed);
                                        break;
                                    case "passage":
                                        // DungeonMapGenerator doesn't implement IDungeonGenerator
                                        var passageGen = new DungeonMapGenerator(width, height, seed);
                                        mapData = passageGen.Generate(minRooms, maxRooms);
                                        generator = null; // For name display
                                        break;
                                    default:
                                        Console.WriteLine("Invalid generator type. Use 'room', 'passage', or 'walk'.");
                                        return;
                                }
                                
                                // Find next available map ID
                                int mapId = _server.GetNextMapId();
                                _server.AddMap(mapId, mapData);
                                _server.CurrentMapId = mapId;  // Set as current map
                                
                                Console.WriteLine($"Generated new map with ID: {mapId} (Current Map)");
                                Console.WriteLine($"Generator: {generator?.Name ?? "Passage-based Generator"}");
                                Console.WriteLine($"Parameters: {width}x{height}, Rooms: {minRooms}-{maxRooms}" + 
                                                (seed.HasValue ? $", Seed: {seed}" : ""));
                                Console.WriteLine();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error generating map: {ex.Message}");
                            }
                        }
                        break;

                    case "showmap":
                        if (_server?.CurrentMap != null)
                        {
                            var renderer = new DungeonAsciiRenderer();
                            string ascii = renderer.Render(_server.CurrentMap);
                            Console.WriteLine(ascii);
                        }
                        else
                        {
                            Console.WriteLine("No current map available.");
                        }
                        break;

                    case "":
                        break;

                    default:
                        if (!string.IsNullOrEmpty(args[0]))
                        {
                            Console.WriteLine($"Unknown command: {args[0]}");
                            Console.WriteLine("Type 'help' for available commands");
                        }
                        break;
                }
            }
        }

        private static async Task ShutdownAsync()
        {
            Console.WriteLine("Shutting down server...");
            _isRunning = false;
            _server?.Stop();
            _cts.Cancel();
            await Task.Delay(1000); // Give time for cleanup
            Console.WriteLine("Server stopped");
        }

        private static DungeonMapData CreateNewMap()
        {
            const int width = 15;
            const int height = 15;
            string mapName = $"Test Map {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            
            var cells = new CellData[width * height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new CellData 
                { 
                    IsEmpty = true,
                    IsPassable = false,
                    HasNorthWall = false,
                    HasSouthWall = false,
                    HasEastWall = false,
                    HasWestWall = false
                };
            }

            // Create a single room
            int roomX = 5;
            int roomY = 5;
            int roomWidth = 5;
            int roomHeight = 5;

            // Fill in room cells
            for (int y = roomY; y < roomY + roomHeight; y++)
            {
                for (int x = roomX; x < roomX + roomWidth; x++)
                {
                    int index = y * width + x;
                    cells[index] = new CellData
                    {
                        IsEmpty = false,
                        IsPassable = true,
                        HasNorthWall = y == roomY + roomHeight - 1,
                        HasSouthWall = y == roomY,
                        HasEastWall = x == roomX + roomWidth - 1,
                        HasWestWall = x == roomX,
                        HasNorthDoor = y == roomY + roomHeight - 1 && x == roomX + roomWidth / 2,
                        HasSouthDoor = y == roomY && x == roomX + roomWidth / 2,
                        HasEastDoor = x == roomX + roomWidth - 1 && y == roomY + roomHeight / 2,
                        HasWestDoor = x == roomX && y == roomY + roomHeight / 2
                    };
                }
            }

            return new DungeonMapData
            {
                Width = width,
                Height = height,
                MapName = mapName,
                FloorLevel = 1,
                Cells = cells
            };
        }
    }
} 