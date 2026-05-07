using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;

public class Test : NetworkBehaviour
{
    int number = 0;
    NetworkObject netObj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        netObj = GetComponent<NetworkObject>();

        Debug.Log(NetworkInterface.GetAllNetworkInterfaces()
        .Where(i => i.OperationalStatus == OperationalStatus.Up) // Interface must be active
        .SelectMany(i => i.GetIPProperties().UnicastAddresses)
        .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
        ?.Address.ToString());
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnStartClient(InputAction.CallbackContext context)
    {
        if(!IsClient && !IsServer)
        {
            netObj.NetworkManager.StartClient();
        }
    }

    public void OnStartHost(InputAction.CallbackContext context)
    {
        if (!IsClient && !IsServer)
        {
            netObj.NetworkManager.StartHost();
        }
    }

    public void OnIncNumber(InputAction.CallbackContext context)
    {
        if (IsClient && context.started)
        {
            IncrementNumberServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void IncrementNumberServerRpc()
    {
        number++;
        LogNumberClientRpc(number);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    public void LogNumberClientRpc(int newNumber)
    {
        Debug.Log("Number: " + newNumber);
    }
}