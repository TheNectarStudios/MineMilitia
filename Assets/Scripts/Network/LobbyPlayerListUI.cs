using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class LobbyPlayerListUI : MonoBehaviour
{
    public Transform playerListParent; // UI parent
    public GameObject playerNamePrefab; // UI prefab with TMP_Text
    public float refreshRate = 1f; // Faster updates to 1 second

    private string currentLobbyId;
    private Dictionary<string, GameObject> playerUIObjects = new Dictionary<string, GameObject>(); // key = playerId

    async void Start()
    {
        currentLobbyId = PlayerPrefs.GetString("CurrentLobbyId", string.Empty);

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning("No lobby ID stored. Can't fetch lobby.");
            return;
        }

        await InitializeAndFetchLobby();
        StartCoroutine(PlayerListUpdater());
    }

    async Task InitializeAndFetchLobby()
    {
        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
            Debug.Log($"Lobby fetched: {lobby.Name}");

            if (lobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                Debug.Log("Starting heartbeat as host.");
                _ = HeartbeatLobby(currentLobbyId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fetching lobby: {e.Message}");
        }
    }

    async Task HeartbeatLobby(string lobbyId)
    {
        while (true)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                Debug.Log("Heartbeat sent.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Heartbeat error: {e.Message}");
            }

            await Task.Delay(15000); // every 15 seconds
        }
    }

    IEnumerator PlayerListUpdater()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(refreshRate);
            _ = UpdatePlayerList(); // fire and forget
        }
    }

    async Task UpdatePlayerList()
    {
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning("currentLobbyId is empty or null.");
            return;
        }

        try
        {
            var currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
            var players = currentLobby.Players;

            HashSet<string> currentPlayerIds = new HashSet<string>(players.Select(p => p.Id));

            // Remove players that have left
            var playersToRemove = playerUIObjects.Keys.Except(currentPlayerIds).ToList();
            foreach (var playerId in playersToRemove)
            {
                Destroy(playerUIObjects[playerId]);
                playerUIObjects.Remove(playerId);
            }

            // Add new players or update existing ones
            foreach (var player in players)
            {
                string name = player.Data != null && player.Data.ContainsKey("name") ? player.Data["name"].Value : "Unnamed";

                if (!playerUIObjects.ContainsKey(player.Id))
                {
                    var go = Instantiate(playerNamePrefab, playerListParent);
                    go.GetComponentInChildren<TMP_Text>().text = name;
                    playerUIObjects[player.Id] = go;
                }
                else
                {
                    playerUIObjects[player.Id].GetComponentInChildren<TMP_Text>().text = name;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fetching lobby: {e.Message}");
        }
    }
}
