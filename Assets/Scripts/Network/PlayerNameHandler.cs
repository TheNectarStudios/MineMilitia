using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerNameInput : MonoBehaviour
{
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public LobbyRelayConnector lobbyConnector; // Reference to the lobby script

    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            nameInputPanel.SetActive(false);
            StartCoroutine(EnableLobbyConnectorNextFrame());
        }
        else
        {
            nameInputPanel.SetActive(true);
            lobbyConnector.enabled = false;
        }
    }

    public void SubmitName()
    {
        string enteredName = nameInputField.text;
        if (!string.IsNullOrWhiteSpace(enteredName))
        {
            PlayerPrefs.SetString("PlayerName", enteredName);
            nameInputPanel.SetActive(false);
            StartCoroutine(EnableLobbyConnectorNextFrame());
        }
    }

    IEnumerator EnableLobbyConnectorNextFrame()
    {
        yield return null; // Wait 1 frame to let UI close properly
        lobbyConnector.enabled = true;
    }
}
