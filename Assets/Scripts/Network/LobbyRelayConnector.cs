using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityEngine.SceneManagement; // This is important for scene management
using System.Collections.Generic;
using System.Threading.Tasks;

public class LobbyRelayConnector : MonoBehaviour
{
    public int maxPlayers = 4;
    private string playerName;

    public async void Init()
    {
        await InitializeServicesAsync();
        await TryJoinOrCreateLobby();
    }

    async Task InitializeServicesAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        playerName = PlayerPrefs.GetString("PlayerName", "Player");
        Debug.Log($"Signed in as {playerName}");
    }

    async Task TryJoinOrCreateLobby()
    {
        try
        {
            var queryOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Count = 1
            };

            var lobbies = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            if (lobbies.Results.Count > 0)
            {
                var lobby = lobbies.Results[0];

                var playerData = new Dictionary<string, PlayerDataObject>
                {
                    { "name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                };

                var joinOptions = new JoinLobbyByIdOptions
                {
                    Player = new Player(AuthenticationService.Instance.PlayerId, data: playerData)
                };

                var joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, joinOptions);

                // Store the lobby ID in PlayerPrefs
                PlayerPrefs.SetString("CurrentLobbyId", joinedLobby.Id);
                PlayerPrefs.Save();

                // Check if the lobby has a valid join code in its data
                if (joinedLobby.Data.ContainsKey("joinCode"))
                {
                    var joinCode = joinedLobby.Data["joinCode"].Value;
                    Debug.Log($"Join code retrieved: {joinCode}");

                    if (!string.IsNullOrEmpty(joinCode))
                    {
                        await JoinRelay(joinCode);
                        Debug.Log("Successfully joined the relay.");
                    }
                    else
                    {
                        Debug.LogError("Join code is empty or invalid.");
                    }
                }
                else
                {
                    Debug.LogError("No join code found in lobby data.");
                }
            }
            else
            {
                await CreateLobbyAndHost();
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Lobby error: {e.Message}");
        }
    }

    async Task CreateLobbyAndHost()
    {
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var playerData = new Dictionary<string, PlayerDataObject>
        {
            { "name", new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Public, value: playerName) }
        };

        var lobbyData = new Dictionary<string, DataObject>
        {
            { "joinCode", new DataObject(visibility: DataObject.VisibilityOptions.Public, value: joinCode) }
        };

        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = new Player(id: AuthenticationService.Instance.PlayerId, data: playerData),
            Data = lobbyData
        };

        // Create the lobby
        var createdLobby = await LobbyService.Instance.CreateLobbyAsync("MyLobby", maxPlayers, options);

        // Store the lobby ID in PlayerPrefs
        PlayerPrefs.SetString("CurrentLobbyId", createdLobby.Id);
        PlayerPrefs.Save();

        // Set up relay server data for hosting
        var relayServerData = new RelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        // Start hosting the game
        NetworkManager.Singleton.StartHost();

        // Now use SceneManager to load the scene
        SceneManager.LoadScene("GameLobby");

        Debug.Log($"Lobby created and hosting. Join code: {joinCode}");
    }

    async Task JoinRelay(string joinCode)
    {
        try
        {
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var relayServerData = new RelayServerData(joinAllocation, "dtls");

            // Set relay server data for the client
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            // Start the client to join the game
            NetworkManager.Singleton.StartClient();

            Debug.Log("Connected as client.");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay join error: {e.Message}");
        }
    }
}
