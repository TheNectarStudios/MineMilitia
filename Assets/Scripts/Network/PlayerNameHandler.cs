using UnityEngine;
using TMPro;

public class PlayerNameInput : MonoBehaviour
{
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;

    void Start()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            nameInputPanel.SetActive(false);
        }
        else
        {
            nameInputPanel.SetActive(true);
        }
    }

    public void SubmitName()
    {
        string enteredName = nameInputField.text;
        if (!string.IsNullOrWhiteSpace(enteredName))
        {
            PlayerPrefs.SetString("PlayerName", enteredName);
            nameInputPanel.SetActive(false);
        }
    }
}
