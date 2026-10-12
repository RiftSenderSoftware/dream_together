using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.PlayMode;
#endif

namespace Project.Network
{
    /// <summary>
    /// Minimal direct-IP connection for testing: Host / Client buttons,
    /// or an automatic start when running from the editor.
    /// </summary>
    public class ConnectionSetup : MonoBehaviour
    {
        // Accept connections from the LAN, not only from this machine.
        private const string HostListenAddress = "0.0.0.0";

        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private string _address = "127.0.0.1";
        [SerializeField] private ushort _port = 7777;
        [Tooltip("In the editor the main editor hosts and Multiplayer Play Mode virtual players join, without pressing buttons.")]
        [SerializeField] private bool _autoStartInEditor = true;

        private void OnValidate()
        {
            if (_networkManager == null)
                _networkManager = GetComponent<NetworkManager>();
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (!_autoStartInEditor)
                return;

            if (CurrentPlayer.IsMainEditor)
                StartHost();
            else
                StartClient();
#endif
        }

        private void StartHost()
        {
            GetTransport().SetConnectionData(_address, _port, HostListenAddress);
            _networkManager.StartHost();
        }

        // The transport keeps retrying, so a client may start before the host is up.
        private void StartClient()
        {
            GetTransport().SetConnectionData(_address, _port);
            _networkManager.StartClient();
        }

        private UnityTransport GetTransport()
        {
            return (UnityTransport)_networkManager.NetworkConfig.NetworkTransport;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 240f, 200f));

            if (!_networkManager.IsListening)
            {
                GUILayout.Label("Address");
                _address = GUILayout.TextField(_address);

                if (GUILayout.Button("Host"))
                    StartHost();

                if (GUILayout.Button("Client"))
                    StartClient();
            }
            else if (_networkManager.IsHost)
            {
                GUILayout.Label($"Host · players: {_networkManager.ConnectedClientsIds.Count}");
            }
            else
            {
                GUILayout.Label(_networkManager.IsConnectedClient ? "Client · connected" : $"Connecting to {_address}...");
            }

            GUILayout.EndArea();
        }
    }
}
