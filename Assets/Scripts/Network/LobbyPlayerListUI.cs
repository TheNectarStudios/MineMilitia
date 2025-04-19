using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LobbyPlayerListUI : MonoBehaviour
{
    public Transform playerListParent;
    public GameObject playerNamePrefab;
    public float refreshRate = 1f;

    private string currentLobbyId;
    private string localPlayerId;
    private Dictionary<string, GameObject> playerUIObjects = new Dictionary<string, GameObject>();

    // References to the already existing buttons in the scene
    public Button leaveButton;
    public Button readyButton;

    async void Start()
    {
        currentLobbyId = PlayerPrefs.GetString("CurrentLobbyId", string.Empty);
        localPlayerId = AuthenticationService.Instance.PlayerId;

        // Debug to check the lobby ID
        Debug.Log($"Current Lobby ID: {currentLobbyId}");

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning("No lobby ID stored. Can't fetch lobby.");
            return;
        }

        await InitializeAndFetchLobby();
        StartCoroutine(PlayerListUpdater());
    }

    // Fetch the current lobby details
    async Task InitializeAndFetchLobby()
    {
        try
        {
            // Debug log to check if lobby is being fetched correctly
            Debug.Log($"Attempting to fetch lobby with ID: {currentLobbyId}");

            var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
            if (lobby != null)
            {
                Debug.Log($"Lobby fetched: {lobby.Name}");
            }
            else
            {
                Debug.LogError("Failed to fetch lobby, lobby is null.");
                return;
            }

            if (lobby.HostId == localPlayerId)
            {
                Debug.Log("Starting heartbeat as host.");
                _ = HeartbeatLobby(currentLobbyId);  // Asynchronously start the heartbeat for the host
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fetching lobby: {e.Message}");
        }
    }

    // Heartbeat function to keep the lobby alive
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

            await Task.Delay(15000);  // Wait for 15 seconds before sending another heartbeat
        }
    }

    // Coroutine to keep refreshing the player list
    IEnumerator PlayerListUpdater()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(refreshRate);
            _ = UpdatePlayerList();
        }
    }

    // Function to update the player list UI
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
            if (currentLobby == null)
            {
                Debug.LogError("Current lobby is null.");
                return;
            }

            Debug.Log($"Fetched Lobby: {currentLobby.Name}");
            var players = currentLobby.Players;

            HashSet<string> currentPlayerIds = new HashSet<string>(players.Select(p => p.Id));

            // Remove players that left
            var playersToRemove = playerUIObjects.Keys.Except(currentPlayerIds).ToList();
            foreach (var playerId in playersToRemove)
            {
                Destroy(playerUIObjects[playerId]);
                playerUIObjects.Remove(playerId);
            }

            // Add or update players
            foreach (var player in players)
            {
                string playerId = player.Id;
                string name = "Unnamed";
                bool isReady = false;

                // Null checks for player data
                if (player.Data != null)
                {
                    if (player.Data.ContainsKey("name"))
                    {
                        name = player.Data["name"].Value;
                    }
                    else
                    {
                        Debug.LogWarning($"Player {playerId} does not have a 'name' in their data.");
                    }

                    if (player.Data.ContainsKey("ready"))
                    {
                        isReady = player.Data["ready"].Value == "true";
                    }
                    else
                    {
                        Debug.LogWarning($"Player {playerId} does not have a 'ready' status in their data.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Player {playerId} has no data.");
                }

                // If player is not in UI dictionary, add them
                if (!playerUIObjects.ContainsKey(playerId))
                {
                    // Instantiate new player UI
                    var go = Instantiate(playerNamePrefab, playerListParent);
                    var playerNameText = go.GetComponentInChildren<TMP_Text>();
                    if (playerNameText != null)
                    {
                        playerNameText.text = name;
                    }
                    else
                    {
                        Debug.LogError("Player Name Text is missing in the prefab.");
                    }

                    // Find buttons already in the scene
                    Button returnBtn = leaveButton;
                    Button readyBtn = readyButton;

                    if (returnBtn == null || readyBtn == null)
                    {
                        Debug.LogError("Buttons (LeaveButton/ReadyButton) are missing.");
                    }

                    // Handle button actions for the local player
                    if (playerId == localPlayerId)
                    {
                        returnBtn?.gameObject.SetActive(true); // Show return button for local player
                        readyBtn?.gameObject.SetActive(true);  // Show ready button for local player

                        returnBtn?.onClick.AddListener(() => ReturnToMainMenu());
                        readyBtn?.onClick.AddListener(() => ToggleReady(readyBtn));
                    }
                    else
                    {
                        returnBtn?.gameObject.SetActive(false); // Hide the return button for non-local players
                        readyBtn?.gameObject.SetActive(false);  // Hide the ready button for non-local players
                    }

                    // Set initial ready button state
                    if (readyBtn != null)
                    {
                        readyBtn.GetComponentInChildren<TMP_Text>().text = isReady ? "Ready ✔" : "Not Ready";
                    }

                    playerUIObjects[playerId] = go;
                }
                else
                {
                    // Update existing player UI
                    var go = playerUIObjects[playerId];
                    go.GetComponentInChildren<TMP_Text>().text = name;

                    var readyBtn = go.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "ReadyButton");
                    if (readyBtn != null)
                        readyBtn.GetComponentInChildren<TMP_Text>().text = isReady ? "Ready ✔" : "Not Ready";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating player list: {e.Message}");
        }
    }

    // Function to leave the lobby and go back to the main menu
    async void ReturnToMainMenu()
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobbyId, localPlayerId);
            Debug.Log("Left the lobby.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error leaving lobby: {e.Message}");
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // Toggle the ready status
    async void ToggleReady(Button readyButton)
    {
        try
        {
            var newStatus = readyButton.GetComponentInChildren<TMP_Text>().text != "Ready ✔";

            await LobbyService.Instance.UpdatePlayerAsync(currentLobbyId, localPlayerId, new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {
                        "ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, newStatus.ToString().ToLower())
                    }
                }
            });

            readyButton.GetComponentInChildren<TMP_Text>().text = newStatus ? "Ready ✔" : "Not Ready";
            Debug.Log($"Set ready status to: {newStatus}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to toggle ready: {e.Message}");
        }
    }
}
