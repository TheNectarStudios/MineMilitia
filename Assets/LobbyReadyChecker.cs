using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;  // Added for networked scene loading
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LobbyReadyChecker : MonoBehaviour
{
    public float checkInterval = 2f; // How often to check readiness

    private string currentLobbyId;
    private string localPlayerId;
    private bool isHost;

    void Start()
    {
        currentLobbyId = PlayerPrefs.GetString("CurrentLobbyId", string.Empty);
        localPlayerId = AuthenticationService.Instance.PlayerId;

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning("No Lobby ID found.");
            return;
        }

        // Start the readiness check loop
        StartCoroutine(CheckReadinessLoop());
    }

    // Coroutine to periodically check the readiness of players
    IEnumerator CheckReadinessLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(checkInterval);
            _ = CheckAllPlayersReady();
        }
    }

    // Main logic to check if all players are ready
    async Task CheckAllPlayersReady()
    {
        try
        {
            var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobbyId);
            if (lobby == null)
            {
                Debug.LogError("Lobby is null.");
                return;
            }

            isHost = lobby.HostId == localPlayerId;

            // Check if all players have marked themselves as ready
            bool allReady = lobby.Players.All(player =>
                player.Data != null &&
                player.Data.ContainsKey("ready") &&
                player.Data["ready"].Value == "true"
            );

            Debug.Log($"All players ready? {allReady}");

            // If all players are ready and this player is the host
            if (allReady && isHost)
            {
                await StartGameWithRelay();
            }

            // Check if the host has triggered the game start
            if (lobby.Data != null && 
                lobby.Data.ContainsKey("start") && 
                lobby.Data["start"].Value == "true")
            {
                string joinCode = lobby.Data.ContainsKey("joinCode") ? lobby.Data["joinCode"].Value : "";

                if (!string.IsNullOrEmpty(joinCode))
                {
                    // Save join code so the GameScene can join
                    PlayerPrefs.SetString("JoinCode", joinCode);
                    PlayerPrefs.SetInt("IsHost", 0);

                    Debug.Log("Game start detected. Join code stored. Loading GameScene...");
                    
                    // Load the scene with networked scene management
                    NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
                }
                else
                {
                    Debug.LogError("Game start detected but join code is missing.");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking readiness: {e.Message}");
        }
    }

    // Create a relay and start the game if the host is ready
    async Task StartGameWithRelay()
    {
        try
        {
            // Create a Relay allocation with max players allowed
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(10);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Ensure we have a valid join code
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("Failed to create a join code.");
                return;
            }

            // Update the lobby data with the join code and game start trigger
            await LobbyService.Instance.UpdateLobbyAsync(currentLobbyId, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "start", new DataObject(DataObject.VisibilityOptions.Public, "true") },
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            });

            // Store join code and mark as host in PlayerPrefs
            PlayerPrefs.SetString("JoinCode", joinCode);
            PlayerPrefs.SetInt("IsHost", 1);
            PlayerPrefs.Save();

            Debug.Log("All players ready. Relay created and starting game...");

            // Load the scene with networked scene management
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error starting game with relay: {e.Message}");
        }
    }
}
    