using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;
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

        StartCoroutine(CheckReadinessLoop());
    }

    IEnumerator CheckReadinessLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(checkInterval);
            _ = CheckAllPlayersReady();
        }
    }

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

            bool allReady = lobby.Players.All(player =>
                player.Data != null &&
                player.Data.ContainsKey("ready") &&
                player.Data["ready"].Value == "true"
            );

            Debug.Log($"All players ready? {allReady}");

            if (allReady && isHost)
            {
                // Only host triggers game start
                await LobbyService.Instance.UpdateLobbyAsync(currentLobbyId, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "start", new DataObject(DataObject.VisibilityOptions.Public, "true") }
                    }
                });

                Debug.Log("All ready. Starting game...");
            }

            // Check if host has triggered the game start
            if (lobby.Data != null && lobby.Data.ContainsKey("start") && lobby.Data["start"].Value == "true")
            {
                Debug.Log("Game start detected. Loading GameScene...");
                SceneManager.LoadScene("GameScene");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking readiness: {e.Message}");
        }
    }
}
