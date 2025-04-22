using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class PlayerNameSync : NetworkBehaviour
{
    private TextMeshPro nameText;

    private NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes(""), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        // Auto-find the nameText child
        nameText = GetComponentInChildren<TextMeshPro>();

        if (IsOwner)
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "Player");
            SubmitNameServerRpc(new FixedString64Bytes(savedName));
        }

        playerName.OnValueChanged += (oldVal, newVal) =>
        {
            if (nameText != null)
                nameText.text = newVal.ToString();
        };

        if (nameText != null)
            nameText.text = playerName.Value.ToString();
    }

    [ServerRpc]
    void SubmitNameServerRpc(FixedString64Bytes name)
    {
        playerName.Value = name;
    }

    void Update()
    {
        if (nameText != null && Camera.main != null)
        {
            nameText.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
