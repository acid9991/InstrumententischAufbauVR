using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ToolRigidbodyStateManager : MonoBehaviour
{
    private Rigidbody m_Rigidbody;
    private XRGrabInteractable m_GrabInteractable;


    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
        if (m_Rigidbody == null)
        {
            Debug.LogError("ToolRigidbodyStateManager: Rigidbody component not found on this GameObject. This script requires it.", this);
            enabled = false; 
            return;
        }
        if (m_GrabInteractable == null)
        {
            Debug.LogError("ToolRigidbodyStateManager: XRGrabInteractable component not found on this GameObject. This script requires it.", this);
            enabled = false; 
            return;
        }

        m_Rigidbody.isKinematic = false;
        m_Rigidbody.constraints = RigidbodyConstraints.None;
    }

    void OnEnable()
    {
        if (m_GrabInteractable != null)
        {
            m_GrabInteractable.selectEntered.AddListener(OnGrabbed);
            m_GrabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void OnDisable()
    {
        if (m_GrabInteractable != null)
        {
            m_GrabInteractable.selectEntered.RemoveListener(OnGrabbed);
            m_GrabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (m_Rigidbody != null)
        {
            // Set Rigidbody to non-kinematic and remove constraints, this allows the XRGrabInteractable to move it freely with the hand and allows physics to act on it if it's dropped.
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.constraints = RigidbodyConstraints.None;
            Debug.Log($"Tool {gameObject.name}: Grabbed! Rigidbody isKinematic=false, constraints=None.");
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        Debug.Log($"Tool {gameObject.name}: Released! Waiting for placement check...");
    }

    public void SetSnappedState()
    {
        if (m_Rigidbody != null)
        {
            // Crucial: Set Rigidbody to kinematic and freeze all movement, this locks the tool to the table surface at its snapped position.
            m_Rigidbody.isKinematic = true;
            m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            Debug.Log($"Tool {gameObject.name}: Snapped to table! Rigidbody isKinematic=true, constraints=FreezeAll.");
        }
    }

    public void ResetToGrabState()
    {
        if (m_Rigidbody != null)
        {
            m_Rigidbody.isKinematic = false;
            m_Rigidbody.constraints = RigidbodyConstraints.None;
            Debug.Log($"Tool {gameObject.name}: Reset to grab state.");
        }
    }
}