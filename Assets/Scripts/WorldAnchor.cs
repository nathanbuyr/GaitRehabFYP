using UnityEngine;
using UnityEngine.XR.WSA;

public class WorldAnchorSetup : MonoBehaviour
{
    void Start()
    {
        // Enable positional tracking
        WorldAnchor anchor = gameObject.AddComponent<WorldAnchor>();
        
        if (anchor.isLocated)
        {
            Debug.Log("World anchor located successfully");
        }
        else
        {
            Debug.LogWarning("World anchor not located - positional tracking may not work");
        }
        
        // Log tracking state
        Debug.Log("Positional tracking: " + (UnityEngine.XR.XRDevice.isPresent ? "Active" : "Inactive"));
    }
}