using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; // ✅ needed for LoadSceneMode

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;

    private void Start()
    {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += HandleSceneLoaded;
    }

    private void HandleSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneName != "GameScene") return;

        Debug.Log($"Scene loaded for client {clientId}, spawning player.");

        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
    }
}
