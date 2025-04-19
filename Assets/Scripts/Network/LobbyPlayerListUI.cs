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

    public Button leaveButton;
    public Button readyButton;

    private string currentLobbyId;
    private string localPlayerId;
    private bool isLobbyActive = false; // NEW: Used to control heartbeat loop
    private Dictionary<string, GameObject> playerUIObjects = new Dictionary<string, GameObject>();

    async void Start()
    {
        currentLobbyId = PlayerPrefs.GetString("CurrentLobbyId", string.Empty);
        localPlayerId = AuthenticationService.Instance.PlayerId;

        Debug.Log($"Current Lobby ID: {currentLobbyId}");

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
                isLobbyActive = true;
                _ = HeartbeatLobby(currentLobbyId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fetching lobby: {e.Message}");
        }
    }

    // 💓 Safe heartbeat loop that respects lobby status
    async Task HeartbeatLobby(string lobbyId)
    {
        while (isLobbyActive)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                Debug.Log("Heartbeat sent.");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Heartbeat error: {e.Message}");

                if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                {
                    Debug.LogWarning("Lobby not found. Stopping heartbeat.");
                    isLobbyActive = false;
                    break;
                }
            }

            await Task.Delay(15000);
        }
    }

    IEnumerator PlayerListUpdater()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(refreshRate);
            _ = UpdatePlayerList();
        }
    }

    async Task UpdatePlayerList()
    {
        if (string.IsNullOrEmpty(currentLobbyId)) return;

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

            // Remove players who left
            var playersToRemove = playerUIObjects.Keys.Except(currentPlayerIds).ToList();
            foreach (var playerId in playersToRemove)
            {
                Destroy(playerUIObjects[playerId]);
                playerUIObjects.Remove(playerId);
            }

            foreach (var player in players)
            {
                string playerId = player.Id;
                string name = player.Data?.ContainsKey("name") == true ? player.Data["name"].Value : "Unnamed";
                bool isReady = player.Data?.ContainsKey("ready") == true && player.Data["ready"].Value == "true";

                if (!playerUIObjects.ContainsKey(playerId))
                {
                    var go = Instantiate(playerNamePrefab, playerListParent);
                    var playerNameText = go.GetComponentInChildren<TMP_Text>();
                    if (playerNameText != null)
                        playerNameText.text = name;

                    // These buttons are already in the scene, so we don’t instantiate them
                    if (playerId == localPlayerId)
                    {
                        leaveButton?.gameObject.SetActive(true);
                        readyButton?.gameObject.SetActive(true);

                        leaveButton.onClick.RemoveAllListeners();
                        leaveButton.onClick.AddListener(() => ReturnToMainMenu());

                        readyButton.onClick.RemoveAllListeners();
                        readyButton.onClick.AddListener(() => ToggleReady(readyButton));

                        // Initial text state
                        if (readyButton != null)
                            readyButton.GetComponentInChildren<TMP_Text>().text = isReady ? "Ready ✔" : "Not Ready";
                    }

                    playerUIObjects[playerId] = go;
                }
                else
                {
                    var go = playerUIObjects[playerId];
                    go.GetComponentInChildren<TMP_Text>().text = name;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating player list: {e.Message}");
        }
    }

    async void ReturnToMainMenu()
    {
        isLobbyActive = false; // 💥 Stop heartbeat before leaving

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

    async void ToggleReady(Button readyButton)
    {
        try
        {
            bool newStatus = readyButton.GetComponentInChildren<TMP_Text>().text != "Ready ✔";

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
