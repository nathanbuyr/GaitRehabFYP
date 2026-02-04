using UnityEngine;
using UnityEngine.XR.WSA;

public class Waypoint : MonoBehaviour
{
    [Header("Detection")]
    public float collectDistance = 0.5f;  // How close you need to be (meters)
    
    [Header("Visual")]
    public float pulseSpeed = 2f;
    public float pulseScale = 1.2f;
    
    [Header("Floor Guide Line")]
    public bool showGuideLine = true;
    public Color lineColor = Color.green;
    public float lineWidth = 0.08f;
    public float lineSpacing = 0.3f;  // Distance between the two parallel lines
    
    private Vector3 originalScale;
    private Renderer rend;
    private bool collected = false;
    private Transform playerCamera;
    private LineRenderer guideLineLeft;
    private LineRenderer guideLineRight;
    
    // Called when player walks into waypoint
    public System.Action OnWaypointCollected;
    
    void Start()
    {
        // Find player camera (MRTK's Main Camera)
        playerCamera = Camera.main?.transform;
        if (playerCamera == null)
            Debug.LogError("Waypoint: No Main Camera found!");
        
        // Setup visuals
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.cyan;
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", Color.cyan * 2f);
        }
        
        originalScale = transform.localScale;
        
        // Setup guide line on floor
        if (showGuideLine)
        {
            SetupGuideLine();
        }
        
        // World-anchor this waypoint so it stays in place
        WorldAnchor anchor = gameObject.AddComponent<WorldAnchor>();
        Debug.Log("Waypoint spawned at " + transform.position + " (anchored: " + anchor.isLocated + ")");
    }
    
    void SetupGuideLine()
    {
        // Create left guide line
        guideLineLeft = gameObject.AddComponent<LineRenderer>();
        guideLineLeft.startWidth = lineWidth;
        guideLineLeft.endWidth = lineWidth;
        guideLineLeft.material = new Material(Shader.Find("Sprites/Default"));
        guideLineLeft.startColor = lineColor;
        guideLineLeft.endColor = lineColor;
        guideLineLeft.positionCount = 2;
        guideLineLeft.useWorldSpace = true;
        
        // Create right guide line
        guideLineRight = gameObject.AddComponent<LineRenderer>();
        guideLineRight.startWidth = lineWidth;
        guideLineRight.endWidth = lineWidth;
        guideLineRight.material = new Material(Shader.Find("Sprites/Default"));
        guideLineRight.startColor = lineColor;
        guideLineRight.endColor = lineColor;
        guideLineRight.positionCount = 2;
        guideLineRight.useWorldSpace = true;
    }
    
    void Update()
    {
        if (collected || playerCamera == null)
            return;
        
        // Update guide lines to create walking path on floor
        if (showGuideLine && guideLineLeft != null && guideLineRight != null)
        {
            // Get direction from player to waypoint on ground plane
            Vector3 playerPos = playerCamera.position;
            playerPos.y = 0;
            
            Vector3 waypointPos = transform.position;
            waypointPos.y = 0;
            
            Vector3 direction = (waypointPos - playerPos).normalized;
            
            // Get perpendicular vector for spacing the lines
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            
            // Calculate positions for parallel lines (like railroad tracks)
            float halfSpacing = lineSpacing / 2f;
            
            // Left line
            Vector3 leftStart = playerPos + perpendicular * halfSpacing;
            leftStart.y = 0.005f;  // Just above floor
            Vector3 leftEnd = waypointPos + perpendicular * halfSpacing;
            leftEnd.y = 0.005f;
            
            guideLineLeft.SetPosition(0, leftStart);
            guideLineLeft.SetPosition(1, leftEnd);
            
            // Right line
            Vector3 rightStart = playerPos - perpendicular * halfSpacing;
            rightStart.y = 0.005f;
            Vector3 rightEnd = waypointPos - perpendicular * halfSpacing;
            rightEnd.y = 0.005f;
            
            guideLineRight.SetPosition(0, rightStart);
            guideLineRight.SetPosition(1, rightEnd);
        }
        
        // Get distance from player's head position to this waypoint
        float distance = Vector3.Distance(playerCamera.position, transform.position);
        
        // Pulse effect based on distance
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        float scale = Mathf.Lerp(1f, pulseScale, pulse);
        transform.localScale = originalScale * scale;
        
        // Visual feedback: change color based on proximity
        if (rend != null)
        {
            Color col = Color.Lerp(Color.green, Color.cyan, Mathf.Clamp01(distance / 2f));
            rend.material.color = col;
            rend.material.SetColor("_EmissionColor", col * 2f);
        }
        
        // Check if player is close enough to collect
        if (distance <= collectDistance)
        {
            Collect();
        }
    }
    
    void Collect()
    {
        if (collected) return;
        collected = true;
        
        Debug.Log("Waypoint collected!");
        
        // Flash green
        if (rend != null)
        {
            rend.material.color = Color.green;
            rend.material.SetColor("_EmissionColor", Color.green * 3f);
        }
        
        // Notify manager
        OnWaypointCollected?.Invoke();
        
        // Destroy after brief delay
        Destroy(gameObject, 0.3f);
    }
}
