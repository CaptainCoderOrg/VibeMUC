using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VibeMUC.Map;
using VibeMUC.Network;

namespace VibeMUC.Server
{
    public class DungeonServer
    {
        private TcpListener? _listener;
        private readonly List<ClientConnection> _clients = new();
        private readonly Dictionary<int, DungeonMapData> _maps = new();
        private bool _isRunning;
        private readonly object _clientLock = new();

        public event Action<ClientConnection>? OnClientConnected;
        public event Action<ClientConnection>? OnClientDisconnected;
        public event Action<Exception>? OnError;

        public async Task StartAsync(int port = NetworkConstants.DefaultPort)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

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
            Console.WriteLine($"Creating test map for client {client.Id}");
            var mapData = CreateTestMap();
            Console.WriteLine($"Created test map: {mapData.Width}x{mapData.Height}, {mapData.Cells.Length} cells");
            
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
            Console.WriteLine($"Map serialized to JSON (length: {jsonData.Length} bytes)");
            Console.WriteLine($"JSON content: {jsonData}");

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
            const string mapName = "Test Dungeon";
            const int width = 10;
            const int height = 10;
            
            var cells = new CellData[width * height];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = new CellData
                {
                    IsPassable = true,
                    HasNorthWall = i / width == height - 1,
                    HasSouthWall = i / width == 0,
                    HasEastWall = i % width == width - 1,
                    HasWestWall = i % width == 0
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

        public void AddMap(int id, DungeonMapData map)
        {
            _maps[id] = map;
        }

        public DungeonMapData? GetMap(int id)
        {
            return _maps.TryGetValue(id, out var map) ? map : null;
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
                
                Console.WriteLine($"Sending message type: {message.Type}, Payload length: {message.Payload.Length}");
                
                // Write message type
                await _stream.WriteAsync(new[] { (byte)message.Type }, 0, 1);
                
                // Write payload length
                var lengthBytes = BitConverter.GetBytes(message.Payload.Length);
                await _stream.WriteAsync(lengthBytes, 0, sizeof(int));
                
                // Write payload
                await _stream.WriteAsync(message.Payload, 0, message.Payload.Length);
                
                // Ensure data is sent immediately
                await _stream.FlushAsync();
                Console.WriteLine("Message sent and flushed to network stream");
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
                Console.WriteLine("Reading message type...");
                var bytesRead = await _stream.ReadAsync(typeBuffer, 0, 1);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected (0 bytes read for message type)");
                    return null;
                }
                Console.WriteLine($"Raw message type value: {typeBuffer[0]}");
                Console.WriteLine($"Message type enum value: {(MessageType)typeBuffer[0]}");

                // Read payload length
                var lengthBuffer = new byte[sizeof(int)];
                Console.WriteLine("Reading payload length...");
                bytesRead = await _stream.ReadAsync(lengthBuffer, 0, sizeof(int));
                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected (0 bytes read for payload length)");
                    return null;
                }

                var payloadLength = BitConverter.ToInt32(lengthBuffer, 0);
                Console.WriteLine($"Expected payload length: {payloadLength} bytes");
                
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
                    Console.WriteLine("Reading payload...");
                    bytesRead = await _stream.ReadAsync(payload, 0, payloadLength);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Client disconnected (0 bytes read for payload)");
                        return null;
                    }
                    Console.WriteLine($"Read {bytesRead} bytes of payload data");
                }
                else
                {
                    // For zero-length payloads, use an empty array
                    payload = Array.Empty<byte>();
                    Console.WriteLine("Message has no payload");
                }

                var message = new NetworkMessage
                {
                    Type = (MessageType)typeBuffer[0],
                    Payload = payload
                };

                Console.WriteLine($"Successfully received message - Type: {message.Type}, Payload: {message.Payload.Length} bytes");
                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}\nStack trace: {ex.StackTrace}");
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