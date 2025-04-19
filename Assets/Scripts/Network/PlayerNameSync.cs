using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerNameSync : NetworkBehaviour
{
    private TextMeshPro nameText;
    private NetworkVariable<string> playerName = new NetworkVariable<string>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Auto-find the nameText child
        nameText = GetComponentInChildren<TextMeshPro>();

        if (IsOwner)
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "Player");
            SubmitNameServerRpc(savedName);
        }

        playerName.OnValueChanged += (oldVal, newVal) =>
        {
            if (nameText != null)
                nameText.text = newVal;
        };

        if (nameText != null)
            nameText.text = playerName.Value;
    }

    [ServerRpc]
    void SubmitNameServerRpc(string name)
    {
        playerName.Value = name;
    }

    void Update()
    {
        if (nameText != null)
        {
            nameText.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
