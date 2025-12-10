using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class GameController : MonoBehaviour
{
    [Header("Victory Effects")]
    public ParticleSystem victoryConfetti; 
    
    [System.Serializable]
    public struct ToolPlacementTarget
    {
        public string uniqueToolName;
        public Transform targetTransform;
        public Vector3 targetRotation;
    }

    [System.Serializable]
    public struct ToolPlacementInfo
    {
        public Vector3 targetLocalPosition;
        public Quaternion targetLocalRotation;
        public bool isPlacedCorrectly;
        public int lastAwardedBonus;
    }

    public ToolSelector toolSelector;
    public UIManager uiManager;
    public LeaderboardManager leaderboardManager;

    public TMP_Text timerText;
    public TMP_Text scoreText;

    private float elapsedTime;
    private bool isGameRunning = false;

    private int placementScore;
    private int speedBonusScore;
    private int completionBonus;
    private int totalScore;
    public int baseScorePerTool = 50;
    private Dictionary<string, float> toolGrabTimes = new Dictionary<string, float>();

    private float timeLimit = 900f;

    public List<ToolPlacementTarget> placementTargets;
    public float positionTolerance = 0.05f;
    public Transform tableSurface;

    private Dictionary<string, GameObject> ghostPrefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> activeGhosts = new Dictionary<string, GameObject>();
    private Dictionary<string, ToolPlacementInfo> toolExpectedPlacements = new Dictionary<string, ToolPlacementInfo>();

    public float ghostOffset = 0.01f;

    [Header("Ghost Visuals")]
    public Material ghostDefaultMaterial;
    public Material ghostValidMaterial;
    public float ghostProximityThreshold = 0.15f;
    private Dictionary<string, bool> isGhostInValidState = new Dictionary<string, bool>();

    [Header("Sound Effects")]
    public AudioClip correctPlacementSound;
    public AudioClip incorrectPlacementSound;
    public AudioClip finishGameSound; 
    private GameObject currentHeldTool = null;
    private bool allToolsArePlaced = false;

    

    void Start()
    {
        ResetGameState();
        InitializePlacementData();
        UpdateTotalScoreText();
    }

    private void InitializePlacementData()
    {
        toolExpectedPlacements.Clear();
        ghostPrefabs.Clear();
        activeGhosts.Clear();
        isGhostInValidState.Clear();

        foreach (var target in placementTargets)
        {
            ToolData toolData = toolSelector.toolDatabase.Find(t => target.uniqueToolName.StartsWith(t.toolName));

            if (toolData == null)
            {
                Debug.LogWarning($"No tool prefab found in ToolSelector for placement target: {target.uniqueToolName}. Skipping.");
                continue;
            }

            ToolPlacementInfo info = new ToolPlacementInfo
            {
                targetLocalPosition = target.targetTransform.localPosition,
                targetLocalRotation = Quaternion.Euler(target.targetRotation),
                isPlacedCorrectly = false,
                lastAwardedBonus = 0
            };
            toolExpectedPlacements.Add(target.uniqueToolName, info);

            if (!ghostPrefabs.ContainsKey(toolData.toolName))
            {
                GameObject ghostPrefab = GhostPrefabGenerator.GenerateGhostPrefab(toolData.prefab, ghostDefaultMaterial);
                ghostPrefabs.Add(toolData.toolName, ghostPrefab);
            }

            GameObject ghostInstance = Instantiate(ghostPrefabs[toolData.toolName], Vector3.zero, Quaternion.identity);
            ghostInstance.name = $"{target.uniqueToolName}_Ghost";
            ghostInstance.SetActive(false);
            activeGhosts.Add(target.uniqueToolName, ghostInstance);
            isGhostInValidState.Add(target.uniqueToolName, false);
        }
    }

    void Update()
    {
        if (currentHeldTool != null)
        {
            UpdateGhostState(currentHeldTool);
        }
    }

    public void OnToolGrabbed(SelectEnterEventArgs args)
    {
        GameObject tool = args.interactableObject.transform.gameObject;
        currentHeldTool = tool;
        string uniqueToolName = tool.name;

        Debug.Log($"Tool {uniqueToolName} was grabbed.");

        toolGrabTimes[uniqueToolName] = Time.time;

        Rigidbody rb = tool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = true;
        }

        foreach (Collider col in tool.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = false;
        }

        if (toolExpectedPlacements.ContainsKey(uniqueToolName))
        {
            ToolPlacementInfo info = toolExpectedPlacements[uniqueToolName];
            if (info.isPlacedCorrectly)
            {
                placementScore -= baseScorePerTool;
                speedBonusScore -= info.lastAwardedBonus;
                UpdateTotalScore();

                info.isPlacedCorrectly = false;
                info.lastAwardedBonus = 0;
                toolExpectedPlacements[uniqueToolName] = info;
            }
        }
    }

    public void OnToolReleased(SelectExitEventArgs args)
    {
        GameObject tool = args.interactableObject.transform.gameObject;
        string uniqueToolName = tool.name;

        Debug.Log($"Tool {uniqueToolName} was released.");

        CheckToolPlacement(tool);
        DeactivateAndResetGhost(uniqueToolName);
        currentHeldTool = null;
    }

    public void CheckToolPlacement(GameObject tool)
    {
        string uniqueToolName = tool.name;
        Rigidbody rb = tool.GetComponent<Rigidbody>();
        if (rb == null) rb = tool.AddComponent<Rigidbody>();

        Vector3 snappedWorldPosition;
        Quaternion snappedWorldRotation;

        SnapToolToTableSurface(tool, out snappedWorldPosition, out snappedWorldRotation);
        tool.transform.position = snappedWorldPosition;
        tool.transform.rotation = snappedWorldRotation;

        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        foreach (Collider col in tool.GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }

        if (toolExpectedPlacements.ContainsKey(uniqueToolName))
        {
            ToolPlacementInfo expectedInfo = toolExpectedPlacements[uniqueToolName];
            Vector3 placedLocalPosition = tableSurface.InverseTransformPoint(tool.transform.position);
            float distance = Vector3.Distance(placedLocalPosition, expectedInfo.targetLocalPosition);
            bool positionCorrect = distance <= positionTolerance;

            if (positionCorrect && !expectedInfo.isPlacedCorrectly)
            {
                placementScore += baseScorePerTool;

                int currentSpeedBonus = 0;
                if (toolGrabTimes.ContainsKey(uniqueToolName))
                {
                    float holdDuration = Time.time - toolGrabTimes[uniqueToolName];
                    if (holdDuration < 3f)
                    {
                        currentSpeedBonus = 50;
                    }
                    else if (holdDuration <= 5f)
                    {
                        currentSpeedBonus = 25;
                    }
                }
                speedBonusScore += currentSpeedBonus;

                expectedInfo.isPlacedCorrectly = true;
                expectedInfo.lastAwardedBonus = currentSpeedBonus;
                toolExpectedPlacements[uniqueToolName] = expectedInfo;

                UpdateTotalScore();
                Debug.Log($"Tool {uniqueToolName} placed correctly! Base: {baseScorePerTool}, Bonus: {currentSpeedBonus}");

                SoundManager.Instance.PlaySound(correctPlacementSound);
            }
            else if (!positionCorrect)
            {
                Debug.Log($"Tool {uniqueToolName} placed incorrectly.");
                SoundManager.Instance.PlaySound(incorrectPlacementSound);
            }
            CheckGameCompletion();
        }
    }

    public void UpdateGhostState(GameObject heldTool)
    {
        if (heldTool == null) return;
        string uniqueToolName = heldTool.name;
        if (activeGhosts.ContainsKey(uniqueToolName) && toolExpectedPlacements.ContainsKey(uniqueToolName))
        {
            GameObject ghost = activeGhosts[uniqueToolName];
            ToolPlacementInfo expectedInfo = toolExpectedPlacements[uniqueToolName];
            RaycastHit hit;
            if (Physics.Raycast(heldTool.transform.position, -tableSurface.up, out hit, 10f, LayerMask.GetMask("Table")))
            {
                if (!ghost.activeSelf) ghost.SetActive(true);
                ghost.transform.position = new Vector3(heldTool.transform.position.x, hit.point.y + ghostOffset, heldTool.transform.position.z);
                ghost.transform.rotation = tableSurface.rotation * expectedInfo.targetLocalRotation;
            }
            else
            {
                if (ghost.activeSelf) ghost.SetActive(false);
                return;
            }
            Vector3 targetWorldPosition = tableSurface.TransformPoint(expectedInfo.targetLocalPosition);
            float distance = Vector3.Distance(ghost.transform.position, targetWorldPosition);
            bool isCloseEnough = distance <= ghostProximityThreshold;
            if (isCloseEnough && !isGhostInValidState[uniqueToolName])
            {
                SetGhostMaterial(ghost, ghostValidMaterial);
                isGhostInValidState[uniqueToolName] = true;
            }
            else if (!isCloseEnough && isGhostInValidState[uniqueToolName])
            {
                SetGhostMaterial(ghost, ghostDefaultMaterial);
                isGhostInValidState[uniqueToolName] = false;
            }
        }
    }

    public void DeactivateAndResetGhost(string uniqueToolName)
    {
        if (activeGhosts.ContainsKey(uniqueToolName) && activeGhosts[uniqueToolName] != null)
        {
            activeGhosts[uniqueToolName].SetActive(false);
        }
    }

    private void SetGhostMaterial(GameObject ghostObject, Material mat)
    {
        if (ghostObject == null || mat == null) return;
        Renderer[] allRenderers = ghostObject.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in allRenderers)
        {
            Material[] mats = new Material[renderer.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            renderer.materials = mats;
        }
    }

    public void StartGame(string name)
    {
        isGameRunning = true;
        StartCoroutine(StartTimer());
        UpdateTotalScoreText();
    }

    private IEnumerator StartTimer()
    {
        elapsedTime = 0;
        while (isGameRunning && !allToolsArePlaced && elapsedTime < timeLimit)
        {
            elapsedTime += Time.deltaTime;
            timerText.text = $"Time: {elapsedTime:F2}s";
            yield return null;
        }
        if (isGameRunning && !allToolsArePlaced) EndGame();
    }

    public void EndGame()
    {
        isGameRunning = false;
        PlayVictoryEffects();
        ShowResults();
        DestroyAllGhostPrefabs();
    }

    private void DestroyAllGhostPrefabs()
    {
        foreach (var pair in activeGhosts)
        {
            if (pair.Value != null) Destroy(pair.Value);
        }
        activeGhosts.Clear();
    }

    private void PlayVictoryEffects()
    {
        // Play the victory sound
        SoundManager.Instance.PlaySound(finishGameSound);

        // Play the confetti particle system
        if (victoryConfetti != null)
        {
            victoryConfetti.Play();
            StartCoroutine(StopConfettiAfterDelay(2.0f)); // Stop after 2 seconds
        }
        else
        {
            Debug.LogWarning("Victory Confetti particle system is not assigned in the GameController.");
        }
    }

    private IEnumerator StopConfettiAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (victoryConfetti != null)
        {
            victoryConfetti.Stop();
        }
    }

    private void ShowResults()
    {
        completionBonus = 0;
        if (allToolsArePlaced)
        {
            if (elapsedTime <= 180f)
            {
                completionBonus = 500;
            }
            else if (elapsedTime <= 300f)
            {
                completionBonus = 250;
            }
        }
        UpdateTotalScore();

        if (leaderboardManager != null)
        {
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            leaderboardManager.AddScore(playerName, totalScore);
        }

        uiManager.ShowPanel("Results");

        UIManager.Panel resultsPanel = uiManager.panels.Find(panel => panel.panelName == "Results");
        if (resultsPanel != null && resultsPanel.panelObject != null)
        {
            TMP_Text resultText = resultsPanel.panelObject.GetComponentInChildren<TMP_Text>();
            if (resultText != null)
            {
                string playerName = PlayerPrefs.GetString("PlayerName", "Player");
                string header = $"<mark=#00000088><align=center>{playerName}</align></mark>\n\n";
                string placementLine = $"<size=80%><align=left>Placement Score</align><pos=65%><nobr>{placementScore}</nobr>\n";
                string speedBonusLine = $"<size=80%><align=left>Speed Bonus</align><pos=65%><nobr>{speedBonusScore}</nobr>\n";
                string completionBonusLine = $"<size=80%><align=left>Completion Bonus</align><pos=65%><nobr>{completionBonus}</nobr>\n\n";
                string totalLine = $"<size=120%><align=left>Total Score</align><pos=65%><nobr>{totalScore}</nobr>\n";
                string formattedTime = System.TimeSpan.FromSeconds(elapsedTime).ToString(@"mm\:ss");
                string totalTime = $"<size=90%><align=left>Total Time</align><pos=65%><nobr>{formattedTime}</nobr></size>\n\n";

                resultText.text = header + placementLine + speedBonusLine + completionBonusLine + totalLine + totalTime;
            }
        }
    }

    public void RestartGame()
    {
        isGameRunning = false;
        StopAllCoroutines();
        toolSelector.ResetTools();
        ResetGameState();
        DestroyAllGhostPrefabs();
        InitializePlacementData();
        isGameRunning = true;
        StartCoroutine(StartTimer());
        UpdateTotalScoreText();
    }

    private void ResetGameState()
    {
        elapsedTime = 0;
        if (timerText != null) timerText.text = "Time: 0.00s";

        placementScore = 0;
        speedBonusScore = 0;
        completionBonus = 0;
        totalScore = 0;
        toolGrabTimes.Clear();

        allToolsArePlaced = false;
        UpdateTotalScoreText();

        List<string> keys = new List<string>(toolExpectedPlacements.Keys);
        foreach (string key in keys)
        {
            var info = toolExpectedPlacements[key];
            info.isPlacedCorrectly = false;
            info.lastAwardedBonus = 0;
            toolExpectedPlacements[key] = info;
        }
    }

    private void UpdateTotalScore()
    {
        totalScore = placementScore + speedBonusScore + completionBonus;
        UpdateTotalScoreText();
    }

    private void UpdateTotalScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalScore}";
        }
    }

    private void SnapToolToTableSurface(GameObject tool, out Vector3 snappedPos, out Quaternion snappedRot)
    {
        string uniqueToolName = tool.name;
        if (tableSurface != null && toolExpectedPlacements.ContainsKey(uniqueToolName))
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(tool.transform.position.x, tableSurface.position.y + 1f, tool.transform.position.z), -tableSurface.up, out hit, 2f, LayerMask.GetMask("Table")))
            {
                snappedPos = hit.point;
                snappedRot = tableSurface.rotation * toolExpectedPlacements[uniqueToolName].targetLocalRotation;
            }
            else
            {
                snappedPos = tool.transform.position;
                snappedRot = tool.transform.rotation;
            }
        }
        else
        {
            snappedPos = tool.transform.position;
            snappedRot = tool.transform.rotation;
        }
    }

    private void CheckGameCompletion()
    {
        if (allToolsArePlaced) return;
        bool allPlaced = true;
        foreach (var entry in toolExpectedPlacements.Values)
        {
            if (!entry.isPlacedCorrectly)
            {
                allPlaced = false;
                break;
            }
        }

        if (allPlaced)
        {
            Debug.Log("All tools placed correctly! Timer stopped. Press Finish to see results.");
            allToolsArePlaced = true;
        }
    }


    public void FullReset()
    {
        isGameRunning = false;
        StopAllCoroutines();
        toolSelector.ResetTools();
        ResetGameState();
        DestroyAllGhostPrefabs();
        InitializePlacementData(); // Re-initialize ghosts and placement data
        UpdateTotalScoreText(); // Ensure score display is zero
    }
}