using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VibeMUC.Map;
using VibeMUC.Network;
using Newtonsoft.Json;

namespace VibeMUC.Client
{
    public class DungeonClient : MonoBehaviour
    {
        [SerializeField] private string _serverAddress = "localhost";
        [SerializeField] private int _serverPort = NetworkConstants.DefaultPort;
        [SerializeField] private DungeonGrid _dungeonGrid;
        [SerializeField] private float _reconnectDelay = 5f;
        [SerializeField] private int _maxReconnectAttempts = 3;

        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;
        private int _reconnectAttempts;
        private bool _isReconnecting;
        private CancellationTokenSource _cts;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<Exception> OnError;
        public event Action<DungeonMapData> OnMapReceived;
        public event Action<string> OnConnectionStatusChanged;

        public bool IsConnected => _isConnected;

        private void Start()
        {
            _cts = new CancellationTokenSource();
            ConnectToServer();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            Disconnect();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            Disconnect();
        }

        private async void ConnectToServer()
        {
            if (_isReconnecting || _cts == null || _cts.Token.IsCancellationRequested) return;

            try
            {
                _client = new TcpClient();
                UpdateConnectionStatus($"Connecting to {_serverAddress}:{_serverPort}...");
                
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, timeoutCts.Token);
                
                var connectTask = _client.ConnectAsync(_serverAddress, _serverPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), linkedCts.Token);
                
                if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                {
                    throw new TimeoutException("Connection attempt timed out");
                }
                
                await connectTask; // Ensure any connection exceptions are thrown
                _stream = _client.GetStream();
                _isConnected = true;
                _reconnectAttempts = 0;
                
                Debug.Log($"Connected to server at {_serverAddress}:{_serverPort}");
                UpdateConnectionStatus("Connected to server");
                OnConnected?.Invoke();

                // Start receiving messages
                _ = ReceiveMessagesAsync(_cts.Token);

                // Request initial map data
                await RequestMapAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Connection attempt cancelled");
                HandleDisconnect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect: {ex.Message}");
                UpdateConnectionStatus($"Connection failed: {ex.Message}");
                OnError?.Invoke(ex);
                
                // Try to reconnect if we haven't exceeded the maximum attempts
                if (_reconnectAttempts < _maxReconnectAttempts && !_cts.Token.IsCancellationRequested)
                {
                    _reconnectAttempts++;
                    _isReconnecting = true;
                    UpdateConnectionStatus($"Reconnecting in {_reconnectDelay} seconds (Attempt {_reconnectAttempts}/{_maxReconnectAttempts})...");
                    
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(_reconnectDelay), _cts.Token);
                        _isReconnecting = false;
                        ConnectToServer();
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.Log("Reconnection cancelled");
                        _isReconnecting = false;
                    }
                }
                else
                {
                    UpdateConnectionStatus("Maximum reconnection attempts reached. Please restart the client.");
                }
            }
        }

        private void UpdateConnectionStatus(string status)
        {
            if (_cts?.Token.IsCancellationRequested ?? true) return;
            OnConnectionStatusChanged?.Invoke(status);
            Debug.Log($"Connection Status: {status}");
        }

        private async Task RequestMapAsync()
        {
            if (!_isConnected || _cts.Token.IsCancellationRequested) return;

            try
            {
                UpdateConnectionStatus("Requesting map data...");
                var message = new NetworkMessage
                {
                    Type = MessageType.RequestMap,
                    Payload = new byte[0] // No payload needed for request
                };

                Debug.Log("Sending map request to server...");
                await SendMessageAsync(message);
                Debug.Log("Map request sent successfully");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Map request cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to request map: {ex.Message}");
                OnError?.Invoke(ex);
                HandleDisconnect();
            }
        }

        private async Task SendMessageAsync(NetworkMessage message)
        {
            if (!_isConnected || _cts.Token.IsCancellationRequested) 
                throw new InvalidOperationException("Not connected to server");

            try
            {
                Debug.Log($"Sending message type: {message.Type}, Payload length: {message.Payload.Length}");
                
                // Write message type
                await _stream.WriteAsync(new[] { (byte)message.Type }, 0, 1, _cts.Token);
                
                // Write payload length
                var lengthBytes = BitConverter.GetBytes(message.Payload.Length);
                await _stream.WriteAsync(lengthBytes, 0, sizeof(int), _cts.Token);
                
                // Write payload
                if (message.Payload.Length > 0)
                {
                    await _stream.WriteAsync(message.Payload, 0, message.Payload.Length, _cts.Token);
                }
                
                await _stream.FlushAsync(_cts.Token);
                Debug.Log("Message sent and flushed to network stream");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Send operation cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send message: {ex.Message}");
                OnError?.Invoke(ex);
                HandleDisconnect();
                throw;
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            Debug.Log("Starting message receive loop");
            while (_isConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Debug.Log("Waiting for next message...");
                    var message = await ReceiveMessageAsync(cancellationToken);
                    if (message == null)
                    {
                        Debug.LogWarning("Received null message, disconnecting");
                        HandleDisconnect();
                        break;
                    }

                    Debug.Log($"Received message of type: {message.Type}, Payload length: {message.Payload.Length}");
                    await HandleMessageAsync(message);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Receive operation cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error receiving message: {ex.Message}\nStack trace: {ex.StackTrace}");
                    OnError?.Invoke(ex);
                    HandleDisconnect();
                    break;
                }
            }
            Debug.Log("Message receive loop ended");
        }

        private async Task<NetworkMessage> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Read message type
                var typeBuffer = new byte[1];
                Debug.Log("Reading message type...");
                var bytesRead = await _stream.ReadAsync(typeBuffer, 0, 1, cancellationToken);
                if (bytesRead == 0)
                {
                    Debug.LogWarning("Server closed connection (0 bytes read for message type)");
                    return null;
                }

                // Read payload length
                var lengthBuffer = new byte[sizeof(int)];
                Debug.Log("Reading payload length...");
                bytesRead = await _stream.ReadAsync(lengthBuffer, 0, sizeof(int), cancellationToken);
                if (bytesRead == 0)
                {
                    Debug.LogWarning("Server closed connection (0 bytes read for payload length)");
                    return null;
                }

                var payloadLength = BitConverter.ToInt32(lengthBuffer, 0);
                Debug.Log($"Expected payload length: {payloadLength} bytes");
                
                if (payloadLength > NetworkConstants.MaxMessageSize)
                {
                    Debug.LogError($"Message too large: {payloadLength} bytes");
                    throw new Exception($"Message too large: {payloadLength} bytes");
                }

                // Read payload
                var payload = new byte[payloadLength];
                Debug.Log("Reading payload...");
                bytesRead = await _stream.ReadAsync(payload, 0, payloadLength, cancellationToken);
                if (bytesRead == 0)
                {
                    Debug.LogWarning("Server closed connection (0 bytes read for payload)");
                    return null;
                }

                Debug.Log($"Successfully read message - Type: {(MessageType)typeBuffer[0]}, Payload: {bytesRead} bytes");
                return new NetworkMessage
                {
                    Type = (MessageType)typeBuffer[0],
                    Payload = payload
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error receiving message: {ex.Message}\nStack trace: {ex.StackTrace}");
                return null;
            }
        }

        private async Task HandleMessageAsync(NetworkMessage message)
        {
            if (_cts.Token.IsCancellationRequested) return;

            try
            {
                switch (message.Type)
                {
                    case MessageType.MapData:
                        await HandleMapDataAsync(message);
                        break;
                    default:
                        Debug.LogWarning($"Unhandled message type: {message.Type}");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Message handling cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error handling message: {ex.Message}");
                OnError?.Invoke(ex);
            }
        }

        private Task HandleMapDataAsync(NetworkMessage message)
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(message.Payload);
                Debug.Log($"Received map JSON data: {json}");

                var mapData = JsonConvert.DeserializeObject<DungeonMapData>(json);
                Debug.Log($"Parsed map data - Width: {mapData.Width}, Height: {mapData.Height}, Cells: {mapData.Cells?.Length ?? 0}");

                UpdateConnectionStatus("Map data received");
                OnMapReceived?.Invoke(mapData);

                // Update the dungeon grid if assigned
                if (_dungeonGrid != null)
                {
                    Debug.Log($"Applying map to grid - Grid size: {_dungeonGrid.GridSize.x}x{_dungeonGrid.GridSize.y}");
                    mapData.ApplyToDungeonGrid(_dungeonGrid);
                    Debug.Log("Map data applied to grid");
                }
                else
                {
                    Debug.LogError("DungeonGrid reference not set in DungeonClient");
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing map data: {ex.Message}\nStack trace: {ex.StackTrace}");
                OnError?.Invoke(ex);
                return Task.CompletedTask;
            }
        }

        private void HandleDisconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            UpdateConnectionStatus("Disconnected from server");
            
            try
            {
                _stream?.Dispose();
                _client?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during disconnect cleanup: {ex.Message}");
            }

            OnDisconnected?.Invoke();

            // Attempt to reconnect if not explicitly cancelled
            if (!(_cts?.Token.IsCancellationRequested ?? true) && _reconnectAttempts < _maxReconnectAttempts)
            {
                ConnectToServer();
            }
        }

        public void Disconnect()
        {
            HandleDisconnect();
        }

        public void ForceReconnect()
        {
            Disconnect();
            _reconnectAttempts = 0;
            ConnectToServer();
        }
    }
} 