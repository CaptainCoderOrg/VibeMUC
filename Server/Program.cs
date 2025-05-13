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
            Console.WriteLine("  exit     - Stop server and exit");
            Console.WriteLine();

            while (_isRunning)
            {
                Console.Write("> ");
                string? command = Console.ReadLine()?.ToLower().Trim();

                switch (command)
                {
                    case "help":
                        Console.WriteLine("\nAvailable commands:");
                        Console.WriteLine("  help     - Show this help message");
                        Console.WriteLine("  clients  - List connected clients");
                        Console.WriteLine("  maps     - List loaded maps");
                        Console.WriteLine("  newmap   - Create a new test map");
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

                    case "":
                        break;

                    default:
                        if (!string.IsNullOrEmpty(command))
                        {
                            Console.WriteLine($"Unknown command: {command}");
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
                int x = i % width;
                int y = i / width;
                
                bool isEdge = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                bool isRoom1 = x >= 3 && x <= 7 && y >= 3 && y <= 7;
                bool isRoom2 = x >= 9 && x <= 13 && y >= 3 && y <= 7;
                bool isCorridor = x >= 7 && x <= 9 && y == 5;

                cells[i] = new CellData
                {
                    IsPassable = !isEdge && (isRoom1 || isRoom2 || isCorridor),
                    HasNorthWall = y < height - 1 && (isEdge || (y == 7 && (isRoom1 || isRoom2))),
                    HasSouthWall = y > 0 && (isEdge || (y == 3 && (isRoom1 || isRoom2))),
                    HasEastWall = x < width - 1 && (isEdge || (x == 7 && !isCorridor) || (x == 13 && isRoom2)),
                    HasWestWall = x > 0 && (isEdge || (x == 3 && isRoom1) || (x == 9 && !isCorridor))
                };
            }

            var map = new DungeonMapData
            {
                Width = width,
                Height = height,
                MapName = mapName,
                FloorLevel = 1,
                Cells = cells
            };

            return map;
        }
    }
} 