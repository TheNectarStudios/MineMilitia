using UnityEngine;
using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;

public class AutoNetcodeStarter : MonoBehaviour
{
    private async void Start()
    {
        bool isHost = PlayerPrefs.GetInt("IsHost", 0) == 1;
        string joinCode = PlayerPrefs.GetString("JoinCode", "");

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join code is missing! Can't start networking.");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (isHost)
        {
            var allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(10);
            string newJoinCode = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Relay Join Code (Host): {newJoinCode}");

            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
        }
        else
        {
            var joinAllocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);

            transport.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
    }
}
 