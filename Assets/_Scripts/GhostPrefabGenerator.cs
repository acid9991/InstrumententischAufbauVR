using UnityEngine;

public static class GhostPrefabGenerator
{
    public static GameObject GenerateGhostPrefab(GameObject originalPrefab, Material ghostMaterial)
    {
        if (originalPrefab == null)
        {
            Debug.LogError("Original prefab is null.");
            return null;
        }

        if (ghostMaterial == null)
        {
            Debug.LogError("The provided ghost material is null.");
            return null;
        }

        GameObject ghostClone = GameObject.Instantiate(originalPrefab);
        ghostClone.name = originalPrefab.name + " Ghost";

        Renderer[] allRenderers = ghostClone.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in allRenderers)
        {
            Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = new Material(ghostMaterial);
            }
            renderer.materials = newMaterials;
        }

        ghostClone.layer = LayerMask.NameToLayer("Ignore Raycast");
        foreach (Transform child in ghostClone.transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        ghostClone.SetActive(false);

        return ghostClone;
    }
}