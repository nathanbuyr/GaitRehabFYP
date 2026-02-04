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
    public float lineSpacing = 0.4f;  // Distance between the two parallel lines
    [SerializeField]
    private float floorHeight = -0.3f;  // Height above floor (30cm below world origin)
    
    private Vector3 originalScale;
    private Renderer rend;
    private bool collected = false;
    private Transform playerCamera;
    private GameObject leftLineObject;
    private GameObject rightLineObject;
    private LineRenderer leftLine;
    private LineRenderer rightLine;
    
    // Called when player walks into waypoint
    public System.Action OnWaypointCollected;
    
    void Awake()
    {
        // Force floor height every time a waypoint spawns
        floorHeight = -0.3f;
    }

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

    void OnValidate()
    {
        // Keep consistent value if edited in Inspector
        floorHeight = -0.3f;
    }
    
    void SetupGuideLine()
    {
        // Create separate GameObjects for each line
        leftLineObject = new GameObject("LeftGuideLine");
        leftLineObject.transform.SetParent(transform);
        leftLine = leftLineObject.AddComponent<LineRenderer>();
        
        rightLineObject = new GameObject("RightGuideLine");
        rightLineObject.transform.SetParent(transform);
        rightLine = rightLineObject.AddComponent<LineRenderer>();
        
        // Configure both lines
        ConfigureLine(leftLine);
        ConfigureLine(rightLine);
    }
    
    void ConfigureLine(LineRenderer line)
    {
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = lineColor;
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }
    
    void Update()
    {
        if (collected || playerCamera == null)
            return;
        
        // Update guide lines on floor
        if (showGuideLine && leftLine != null && rightLine != null)
        {
            // Project both points to an absolute floor height in world space
            Vector3 playerPosFloor = new Vector3(playerCamera.position.x, floorHeight, playerCamera.position.z);
            Vector3 waypointPosFloor = new Vector3(transform.position.x, floorHeight, transform.position.z);
            
            // Direction on floor plane
            Vector3 toWaypoint = waypointPosFloor - playerPosFloor;
            toWaypoint.y = 0f;
            if (toWaypoint.sqrMagnitude < 0.0001f)
            {
                toWaypoint = Vector3.forward;
            }
            
            // Perpendicular direction for parallel lines
            Vector3 right = Vector3.Cross(toWaypoint.normalized, Vector3.up).normalized;
            float offset = lineSpacing * 0.5f;
            
            // Left line
            leftLine.SetPosition(0, playerPosFloor + right * offset);
            leftLine.SetPosition(1, waypointPosFloor + right * offset);
            
            // Right line
            rightLine.SetPosition(0, playerPosFloor - right * offset);
            rightLine.SetPosition(1, waypointPosFloor - right * offset);
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
