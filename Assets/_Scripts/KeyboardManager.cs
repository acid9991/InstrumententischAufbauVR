using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.Experimental.UI; 

public class KeyboardManager : MonoBehaviour
{
    public static KeyboardManager Instance { get; private set; }

    [Header("Keyboard Setup")]
    [Tooltip("Assign the NonNativeKeyboard prefab from your scene here.")]
    public NonNativeKeyboard keyboard;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (keyboard == null)
        {
            Debug.LogError("Keyboard reference is not set in KeyboardManager!");
            return;
        }

        keyboard.gameObject.SetActive(false);
    }

    public void ShowKeyboard(TMP_InputField target)
    {
        if (keyboard == null) return;

        keyboard.InputField = target;

        keyboard.PresentKeyboard(target.text);
    }

    public void CloseKeyboard()
    {
        if (keyboard == null) return;
        keyboard.gameObject.SetActive(false);
    }
}

