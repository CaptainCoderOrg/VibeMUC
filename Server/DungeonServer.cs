using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VibeMUC.Map;
using VibeMUC.Network;
using System.Linq;

namespace VibeMUC.Server
{
    public class DungeonServer
    {
        private TcpListener? _listener;
        private readonly List<ClientConnection> _clients = new();
        private readonly Dictionary<int, DungeonMapData> _maps = new();
        private int _currentMapId = -1;
        private bool _isRunning;
        private readonly object _clientLock = new();

        public event Action<ClientConnection>? OnClientConnected;
        public event Action<ClientConnection>? OnClientDisconnected;
        public event Action<Exception>? OnError;

        public int CurrentMapId
        {
            get => _currentMapId;
            set
            {
                if (_maps.ContainsKey(value))
                {
                    _currentMapId = value;
                    // Broadcast the map update to all clients
                    if (_maps.TryGetValue(value, out var map))
                    {
                        BroadcastMapUpdateAsync(map).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new ArgumentException($"Map with ID {value} does not exist.");
                }
            }
        }

        public DungeonMapData? CurrentMap => _currentMapId >= 0 ? GetMap(_currentMapId) : null;

        public async Task StartAsync(int port = NetworkConstants.DefaultPort)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            // Create initial test map
            if (_maps.Count == 0)
            {
                var initialMap = CreateTestMap();
                int mapId = GetNextMapId();
                AddMap(mapId, initialMap);
                Console.WriteLine($"Created initial test map with ID: {mapId}");
            }

            Console.WriteLine($"Server started on port {port}");

            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex) when (_isRunning)
                {
                    OnError?.Invoke(ex);
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();

            lock (_clientLock)
            {
                foreach (var client in _clients)
                {
                    client.Disconnect();
                }
                _clients.Clear();
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var client = new ClientConnection(tcpClient);
            
            lock (_clientLock)
            {
                _clients.Add(client);
            }
            
            OnClientConnected?.Invoke(client);
            Console.WriteLine($"Client {client.Id} connected and added to active clients list");

            try
            {
                while (client.IsConnected)
                {
                    Console.WriteLine($"Waiting for message from client {client.Id}...");
                    var message = await client.ReceiveMessageAsync();
                    if (message == null)
                    {
                        Console.WriteLine($"Received null message from client {client.Id}, breaking connection loop");
                        break;
                    }

                    try
                    {
                        Console.WriteLine($"Received message type {message.Type} from client {client.Id}, handling...");
                        await HandleMessageAsync(client, message);
                        Console.WriteLine($"Successfully handled message type {message.Type} from client {client.Id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling message from client {client.Id}: {ex.Message}\nStack trace: {ex.StackTrace}");
                        OnError?.Invoke(ex);
                        // Don't break the loop for message handling errors
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in client {client.Id} connection loop: {ex.Message}\nStack trace: {ex.StackTrace}");
                OnError?.Invoke(ex);
            }
            finally
            {
                lock (_clientLock)
                {
                    _clients.Remove(client);
                    Console.WriteLine($"Client {client.Id} removed from active clients list");
                }
                client.Disconnect();
                OnClientDisconnected?.Invoke(client);
                Console.WriteLine($"Client {client.Id} disconnected");
            }
        }

        private async Task HandleMessageAsync(ClientConnection client, NetworkMessage message)
        {
            Console.WriteLine($"Processing message type {message.Type} from client {client.Id}");
            switch (message.Type)
            {
                case MessageType.RequestMap:
                    Console.WriteLine($"Handling map request from client {client.Id}");
                    await HandleMapRequestAsync(client, message);
                    break;
                default:
                    Console.WriteLine($"Unknown message type {message.Type} from client {client.Id}");
                    break;
            }
        }

        private async Task HandleMapRequestAsync(ClientConnection client, NetworkMessage message)
        {
            Console.WriteLine($"Handling map request from client {client.Id}");
            
            // Send the current map if one exists, otherwise create a new one
            DungeonMapData mapData;
            if (CurrentMap != null)
            {
                mapData = CurrentMap;
                Console.WriteLine($"Sending existing map (ID: {_currentMapId}) to client {client.Id}");
            }
            else
            {
                Console.WriteLine($"No current map exists, creating new map for client {client.Id}");
                mapData = CreateTestMap();
                int mapId = GetNextMapId();
                AddMap(mapId, mapData);
                CurrentMapId = mapId;
                Console.WriteLine($"Created new map with ID: {mapId}");
            }
            
            try 
            {
                Console.WriteLine($"Sending map to client {client.Id}");
                await SendMapToClientAsync(client, mapData);
                Console.WriteLine($"Map successfully sent to client {client.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending map to client {client.Id}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task SendMapToClientAsync(ClientConnection client, DungeonMapData mapData)
        {
            Console.WriteLine("Serializing map data...");
            var jsonData = mapData.ToJson();
            Console.WriteLine($"Map serialized (length: {jsonData.Length} bytes)");

            var message = new NetworkMessage
            {
                Type = MessageType.MapData,
                Payload = System.Text.Encoding.UTF8.GetBytes(jsonData)
            };

            Console.WriteLine($"Sending map data message (payload: {message.Payload.Length} bytes)");
            await client.SendMessageAsync(message);
        }

        private DungeonMapData CreateTestMap()
        {
            const int width = 10;
            const int height = 10;
            const int roomSize = 5;
            
            // Calculate room position (centered)
            int roomX = (width - roomSize) / 2;  // Should be 2
            int roomY = (height - roomSize) / 2; // Should be 2

            var cells = new CellData[width * height];

            // Initialize all cells as empty (non-existent)
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new CellData { IsEmpty = true, IsPassable = false };
            }

            // Create the room
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    bool isInRoom = x >= roomX && x < roomX + roomSize &&
                                  y >= roomY && y < roomY + roomSize;

                    if (isInRoom)
                    {
                        cells[index].IsEmpty = false;
                        cells[index].IsPassable = true;

                        // Add walls on room edges
                        bool isNorthEdge = y == roomY + roomSize - 1;  // North is at higher Y
                        bool isSouthEdge = y == roomY;                 // South is at lower Y
                        bool isWestEdge = x == roomX;
                        bool isEastEdge = x == roomX + roomSize - 1;

                        cells[index].HasNorthWall = isNorthEdge;
                        cells[index].HasSouthWall = isSouthEdge;
                        cells[index].HasWestWall = isWestEdge;
                        cells[index].HasEastWall = isEastEdge;

                        // Add north door in the center of the north wall
                        if (isNorthEdge && x == roomX + roomSize / 2)
                        {
                            cells[index].HasNorthDoor = true;
                        }

                        // Add south door in the center of the south wall
                        if (isSouthEdge && x == roomX + roomSize / 2)
                        {
                            cells[index].HasSouthDoor = true;
                        }

                        // Add east door in the center of the east wall
                        if (isEastEdge && y == roomY + roomSize / 2)
                        {
                            cells[index].HasEastDoor = true;
                        }

                        // Add west door in the center of the west wall
                        if (isWestEdge && y == roomY + roomSize / 2)
                        {
                            cells[index].HasWestDoor = true;
                        }
                    }
                }
            }

            var map = new DungeonMapData
            {
                Width = width,
                Height = height,
                MapName = "Test Room",
                FloorLevel = 1,
                Cells = cells
            };

            return map;
        }

        public int GetNextMapId()
        {
            if (_maps.Count == 0) return 1;
            return _maps.Keys.Max() + 1;
        }

        public void AddMap(int id, DungeonMapData map)
        {
            _maps[id] = map;
            // If this is the first map added, make it the current map
            if (_currentMapId < 0)
            {
                _currentMapId = id;
            }
            // Broadcast the map update to all clients
            BroadcastMapUpdateAsync(map).ConfigureAwait(false);
        }

        public DungeonMapData? GetMap(int id)
        {
            return _maps.TryGetValue(id, out var map) ? map : null;
        }

        private async Task BroadcastMapUpdateAsync(DungeonMapData mapData)
        {
            Console.WriteLine("Broadcasting map update to all clients...");
            List<ClientConnection> clientsToRemove = new();

            lock (_clientLock)
            {
                foreach (var client in _clients)
                {
                    try
                    {
                        if (client.IsConnected)
                        {
                            _ = SendMapToClientAsync(client, mapData);
                        }
                        else
                        {
                            clientsToRemove.Add(client);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending map to client {client.Id}: {ex.Message}");
                        clientsToRemove.Add(client);
                    }
                }

                // Remove any disconnected clients
                foreach (var client in clientsToRemove)
                {
                    _clients.Remove(client);
                    OnClientDisconnected?.Invoke(client);
                }
            }
        }
    }

    public class ClientConnection
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public bool IsConnected => _client?.Connected ?? false;
        public string Id { get; }

        public ClientConnection(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            Id = Guid.NewGuid().ToString();
        }

        public async Task SendMessageAsync(NetworkMessage message)
        {
            try
            {
                await _sendLock.WaitAsync();
                
                // Write message type
                await _stream.WriteAsync(new[] { (byte)message.Type }, 0, 1);
                
                // Write payload length
                var lengthBytes = BitConverter.GetBytes(message.Payload.Length);
                await _stream.WriteAsync(lengthBytes, 0, sizeof(int));
                
                // Write payload
                await _stream.WriteAsync(message.Payload, 0, message.Payload.Length);
                
                // Ensure data is sent immediately
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                throw;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task<NetworkMessage?> ReceiveMessageAsync()
        {
            try
            {
                // Read message type
                var typeBuffer = new byte[1];
                var bytesRead = await _stream.ReadAsync(typeBuffer, 0, 1);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected (0 bytes read for message type)");
                    return null;
                }

                // Read payload length
                var lengthBuffer = new byte[sizeof(int)];
                bytesRead = await _stream.ReadAsync(lengthBuffer, 0, sizeof(int));
                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected (0 bytes read for payload length)");
                    return null;
                }

                var payloadLength = BitConverter.ToInt32(lengthBuffer, 0);
                
                if (payloadLength > NetworkConstants.MaxMessageSize)
                {
                    Console.WriteLine($"Message too large: {payloadLength} bytes");
                    throw new Exception("Message too large");
                }

                byte[] payload;
                if (payloadLength > 0)
                {
                    // Read payload only if there are bytes to read
                    payload = new byte[payloadLength];
                    bytesRead = await _stream.ReadAsync(payload, 0, payloadLength);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected (0 bytes read for payload)");
                        return null;
                    }
                }
                else
                {
                    // For zero-length payloads, use an empty array
                    payload = Array.Empty<byte>();
                }

                return new NetworkMessage
                {
                    Type = (MessageType)typeBuffer[0],
                    Payload = payload
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                return null;
            }
        }

        public void Disconnect()
        {
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
} 