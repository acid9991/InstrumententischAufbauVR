using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class ShowKeyboard : MonoBehaviour
{
    private TMP_InputField inputField;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(OpenKeyboard);
    }

    public void OpenKeyboard(string currentText)
    {
        if (NonNativeKeyboard.Instance != null)
        {
            NonNativeKeyboard.Instance.InputField = inputField;

            NonNativeKeyboard.Instance.PresentKeyboard(inputField.text);
        }
        else
        {
            Debug.LogError("NonNativeKeyboard.Instance is not found in the scene!");
        }
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSelect.RemoveListener(OpenKeyboard);
        }
    }
}

