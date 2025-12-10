using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class ActivateTeleportationRay : MonoBehaviour
{

    public GameObject leftTeleportation;
    

    public InputActionProperty leftSelect;
   

    public InputActionProperty leftCancel;
    

    void Update()
    {
        leftTeleportation.SetActive(leftCancel.action.ReadValue<float>() == 0 && leftSelect.action.ReadValue<float>() > 0.1f);
        
    }
}
