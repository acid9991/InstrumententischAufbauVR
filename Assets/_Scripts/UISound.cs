using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using TMPro; 

public class UISound : MonoBehaviour, ISelectHandler
{
    [Tooltip("The sound to play when this UI element is interacted with.")]
    public AudioClip interactionSound;

    void Start()
    {
        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(PlayInteractionSound);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (GetComponent<TMP_InputField>() != null)
        {
            PlayInteractionSound();
        }
    }

    void PlayInteractionSound()
    {
        if (SoundManager.Instance != null && interactionSound != null)
        {
            SoundManager.Instance.PlaySound(interactionSound);
        }
    }
}
