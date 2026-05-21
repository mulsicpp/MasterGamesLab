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
    public NetworkList<int> numbers;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("list count: " + numbers.Count);

        for(int i = 0; i < 10; i++)
        {
            if(Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                numbers.Add(i);
            }
        }
    }
}