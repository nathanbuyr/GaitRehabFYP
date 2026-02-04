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
    public bool useCameraRelativeFloor = true;
    public float assumedEyeHeight = 1.6f;  // meters (used if camera-relative floor is enabled)
    public float floorHeight = 0f;  // Absolute floor height if not camera-relative
    public float floorOffset = 0.02f;  // Raise lines slightly above floor to avoid clipping
    
    private Vector3 originalScale;
    private Renderer rend;
    private bool collected = false;
    private Transform playerCamera;
    private GameObject leftLineObject;
    private GameObject rightLineObject;
    private LineRenderer leftLine;
    private LineRenderer rightLine;
    private Vector3 guideStart;
    private Vector3 guideEnd;
    private bool guideInitialized = false;

    public float CurrentOffCoursePercent { get; private set; }
    
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
            InitializeGuideLinePath();
        }
        
        // World-anchor this waypoint so it stays in place
        WorldAnchor anchor = gameObject.AddComponent<WorldAnchor>();
        Debug.Log("Waypoint spawned at " + transform.position + " (anchored: " + anchor.isLocated + ")");
    }

    void OnValidate()
    {
        assumedEyeHeight = Mathf.Max(0.5f, assumedEyeHeight);
        floorOffset = Mathf.Clamp(floorOffset, 0f, 0.2f);
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
        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Unlit/Color");
        }
        if (lineShader != null)
        {
            line.material = new Material(lineShader);
            line.material.color = lineColor;
        }
        line.startColor = lineColor;
        line.endColor = lineColor;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }

    void InitializeGuideLinePath()
    {
        if (leftLine == null || rightLine == null || playerCamera == null)
            return;

        // Static path from player spawn position to waypoint position
        float floorY = GetFloorY();
        guideStart = new Vector3(playerCamera.position.x, floorY, playerCamera.position.z);
        guideEnd = new Vector3(transform.position.x, floorY, transform.position.z);

        Vector3 toWaypoint = guideEnd - guideStart;
        toWaypoint.y = 0f;
        if (toWaypoint.sqrMagnitude < 0.0001f)
        {
            toWaypoint = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(toWaypoint.normalized, Vector3.up).normalized;
        float offset = lineSpacing * 0.5f;

        leftLine.SetPosition(0, guideStart + right * offset);
        leftLine.SetPosition(1, guideEnd + right * offset);

        rightLine.SetPosition(0, guideStart - right * offset);
        rightLine.SetPosition(1, guideEnd - right * offset);

        guideInitialized = true;
    }
    
    void Update()
    {
        if (collected || playerCamera == null)
            return;
        
        // Update off-course percentage
        UpdateOffCoursePercent();
        
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

    void UpdateOffCoursePercent()
    {
        if (!guideInitialized || playerCamera == null)
        {
            CurrentOffCoursePercent = 0f;
            return;
        }

        Vector3 playerPosFloor = new Vector3(playerCamera.position.x, guideStart.y, playerCamera.position.z);

        float distanceToPath = DistancePointToSegmentXZ(guideStart, guideEnd, playerPosFloor);
        float halfSpacing = Mathf.Max(0.001f, lineSpacing * 0.5f);

        CurrentOffCoursePercent = Mathf.Clamp01(distanceToPath / halfSpacing) * 100f;
    }

    float GetFloorY()
    {
        if (useCameraRelativeFloor && playerCamera != null)
        {
            return (playerCamera.position.y - assumedEyeHeight) + floorOffset;
        }

        return floorHeight + floorOffset;
    }

    float DistancePointToSegmentXZ(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector2 a2 = new Vector2(a.x, a.z);
        Vector2 b2 = new Vector2(b.x, b.z);
        Vector2 p2 = new Vector2(p.x, p.z);

        Vector2 ab = b2 - a2;
        float abLenSq = ab.sqrMagnitude;
        if (abLenSq < 0.0001f)
        {
            return Vector2.Distance(p2, a2);
        }

        float t = Vector2.Dot(p2 - a2, ab) / abLenSq;
        t = Mathf.Clamp01(t);
        Vector2 closest = a2 + ab * t;
        return Vector2.Distance(p2, closest);
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
