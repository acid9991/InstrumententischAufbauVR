using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class ToolData
{
    // The internal name used for matching (e.g., "Scalpel Short").
    public string toolName;

    // The name displayed on the button (e.g., "Skalpell (kurz)").
    public string displayName;

    public GameObject prefab;
    public int totalCount = 1;
    public Button uiButton;

    [HideInInspector]
    public int remainingCount;
}

public class ToolSelector : MonoBehaviour
{
    public GameController gameController;
    public Transform handTransform;
    public XRDirectInteractor directInteractor;

    public List<ToolData> toolDatabase = new List<ToolData>();

    private Color32 selectedColor = new Color32(139, 0, 0, 255);
    private Color defaultColor = new Color32(69, 73, 77, 255);

    void Start()
    {
        InitializeTools();
    }

    private void InitializeTools()
    {
        foreach (var tool in toolDatabase)
        {
            tool.remainingCount = tool.totalCount;
            UpdateButtonUI(tool);
        }
    }

    public void SelectTool(int toolIndex)
    {
        if (toolIndex < 0 || toolIndex >= toolDatabase.Count)
        {
            Debug.LogError("Invalid tool index provided.");
            return;
        }

        ToolData selectedToolData = toolDatabase[toolIndex];

        if (selectedToolData.remainingCount <= 0)
        {
            Debug.Log($"No more instances of {selectedToolData.toolName} left to spawn.");
            return;
        }

        int instanceNumber = selectedToolData.totalCount - selectedToolData.remainingCount + 1;
        string uniqueToolName = $"{selectedToolData.toolName}_{instanceNumber}";

        GameObject currentTool = Instantiate(selectedToolData.prefab, handTransform.position, handTransform.rotation);
        currentTool.tag = "Tool";
        currentTool.name = uniqueToolName;

        selectedToolData.remainingCount--;
        Debug.Log($"{selectedToolData.toolName} remaining: {selectedToolData.remainingCount}");
        UpdateButtonUI(selectedToolData);

        XRGrabInteractable grabInteractable = currentTool.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null) grabInteractable = currentTool.AddComponent<XRGrabInteractable>();

        Rigidbody rb = currentTool.GetComponent<Rigidbody>();
        if (rb == null) rb = currentTool.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        grabInteractable.selectEntered.AddListener(gameController.OnToolGrabbed);
        grabInteractable.selectExited.AddListener(gameController.OnToolReleased);

        if (directInteractor != null && grabInteractable != null && directInteractor.interactionManager != null)
        {
            directInteractor.interactionManager.SelectEnter((IXRSelectInteractor)directInteractor, (IXRSelectInteractable)grabInteractable);
        }
    }

    private void UpdateButtonUI(ToolData tool)
    {
        if (tool.uiButton == null) return;

        TMP_Text buttonText = tool.uiButton.GetComponentInChildren<TMP_Text>();
        if (buttonText == null) return;

        string nameToShow = string.IsNullOrEmpty(tool.displayName) ? tool.toolName : tool.displayName;

        if (tool.remainingCount > 0)
        {
            // Button is available
            buttonText.text = tool.totalCount > 1 ? $"{nameToShow} x{tool.remainingCount}" : nameToShow;
            tool.uiButton.GetComponent<Image>().color = defaultColor; 
            tool.uiButton.interactable = true;
        }
        else
        {
            // Button is depleted
            buttonText.text = nameToShow;
            tool.uiButton.GetComponent<Image>().color = selectedColor; 
            tool.uiButton.interactable = false;
        }
    }

    public void ResetTools()
    {
        GameObject[] tools = GameObject.FindGameObjectsWithTag("Tool");
        foreach (var tool in tools)
        {
            Destroy(tool);
        }

        InitializeTools();
        Debug.Log("All tools reset.");
    }
}