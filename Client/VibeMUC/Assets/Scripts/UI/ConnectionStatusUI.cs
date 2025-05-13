using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VibeMUC.Client;

namespace VibeMUC.UI
{
    public class ConnectionStatusUI : MonoBehaviour
    {
        [SerializeField] private DungeonClient _client;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Image _statusIcon;
        [SerializeField] private Button _reconnectButton;
        
        [Header("Colors")]
        [SerializeField] private Color _connectedColor = Color.green;
        [SerializeField] private Color _disconnectedColor = Color.red;
        [SerializeField] private Color _connectingColor = Color.yellow;

        private void OnEnable()
        {
            if (_client != null)
            {
                _client.OnConnected += HandleConnected;
                _client.OnDisconnected += HandleDisconnected;
                _client.OnConnectionStatusChanged += UpdateStatusText;
            }

            if (_reconnectButton != null)
            {
                _reconnectButton.onClick.AddListener(HandleReconnectClick);
            }
        }

        private void OnDisable()
        {
            if (_client != null)
            {
                _client.OnConnected -= HandleConnected;
                _client.OnDisconnected -= HandleDisconnected;
                _client.OnConnectionStatusChanged -= UpdateStatusText;
            }

            if (_reconnectButton != null)
            {
                _reconnectButton.onClick.RemoveListener(HandleReconnectClick);
            }
        }

        private void Start()
        {
            if (_client == null)
            {
                _client = FindObjectOfType<DungeonClient>();
            }

            if (_client != null)
            {
                UpdateUI(_client.IsConnected);
            }
            else
            {
                Debug.LogError("No DungeonClient found in scene!");
            }

            // Hide reconnect button initially
            if (_reconnectButton != null)
            {
                _reconnectButton.gameObject.SetActive(false);
            }
        }

        private void HandleConnected()
        {
            UpdateUI(true);
            if (_reconnectButton != null)
            {
                _reconnectButton.gameObject.SetActive(false);
            }
        }

        private void HandleDisconnected()
        {
            UpdateUI(false);
            if (_reconnectButton != null)
            {
                _reconnectButton.gameObject.SetActive(true);
            }
        }

        private void UpdateStatusText(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }

            // Update icon color based on status text
            if (_statusIcon != null)
            {
                if (status.Contains("Connected"))
                {
                    _statusIcon.color = _connectedColor;
                }
                else if (status.Contains("Connecting"))
                {
                    _statusIcon.color = _connectingColor;
                }
                else
                {
                    _statusIcon.color = _disconnectedColor;
                }
            }
        }

        private void UpdateUI(bool isConnected)
        {
            if (_statusIcon != null)
            {
                _statusIcon.color = isConnected ? _connectedColor : _disconnectedColor;
            }
        }

        private void HandleReconnectClick()
        {
            if (_client != null)
            {
                _client.ForceReconnect();
                if (_reconnectButton != null)
                {
                    _reconnectButton.gameObject.SetActive(false);
                }
            }
        }
    }
} 