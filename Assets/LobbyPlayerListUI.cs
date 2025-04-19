using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LobbyPlayerListUI : MonoBehaviour
{
    public Transform playerListParent; // UI parent
    public GameObject playerNamePrefab; // UI prefab with TMP_Text
    public float refreshRate = 5f;

    private string currentLobbyId; // Store the current lobby ID

    async void Start()
    {
        // Retrieve the current lobby ID from PlayerPrefs
        currentLobbyId = PlayerPrefs.GetString("CurrentLobbyId", string.Empty);

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning("No lobby ID stored. Can't fetch lobby.");
            return;
        }

        await InitializeAndFetchLobby();
        InvokeRepeating(nameof(UpdatePlayerList), 0f, refreshRate);  // Refresh the list periodically
    }

    async Task InitializeAndFetchLobby()
    {
        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
            Debug.Log($"Lobby fetched: {lobby.Name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fetching lobby: {e.Message}");
        }
    }

    async void UpdatePlayerList()
    {
        Debug.Log("UpdatePlayerList() called.");
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning("currentLobbyId is empty or null.");
            return;
        }
        else
        {
            Debug.Log($"Fetching Lobby with ID: {currentLobbyId}");
        }

        try
        {
            var currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
            Debug.Log($"Fetched Lobby: {currentLobby.Name} with {currentLobby.Players.Count} players.");

            // Clear existing UI elements
            foreach (Transform child in playerListParent)
                Destroy(child.gameObject);

            // Populate the player list UI with player names
            foreach (var player in currentLobby.Players)
            {
                if (player.Data != null && player.Data.ContainsKey("name"))
                {
                    var go = Instantiate(playerNamePrefab, playerListParent);
                    var text = go.GetComponentInChildren<TMP_Text>();
                    text.text = player.Data["name"].Value;
                    Debug.Log($"Player found: {player.Data["name"].Value}");
                }
                else
                {
                    Debug.LogWarning($"Player data doesn't contain 'name'.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error fetching lobby: {e.Message}");
        }
    }
}
