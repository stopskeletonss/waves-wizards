using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkStartButtons : MonoBehaviour
{
    public string hostIPAddress = "192.168.2.13";
    public ushort port = 7777;
    private UnityTransport transport;

    private void Awake()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 40), "Start Host"))
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            NetworkManager.Singleton.StartHost();
        }

        if (GUI.Button(new Rect(10, 60, 150, 40), "Start Client"))
        {
            transport.SetConnectionData(hostIPAddress, port);
            NetworkManager.Singleton.StartClient();
        }

        if (GUI.Button(new Rect(10, 110, 150, 40), "Start Server"))
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            NetworkManager.Singleton.StartServer();
        }
    }
}
