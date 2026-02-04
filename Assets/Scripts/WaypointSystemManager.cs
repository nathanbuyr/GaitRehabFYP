using UnityEngine;

public class WaypointSystemManager : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public GameObject waypointPrefab;        // Assign a sphere prefab here
    public int totalWaypoints = 10;
    
    [Header("Spawn Distance")]
    public float minDistance = 1.0f;         // Min distance in front of player
    public float maxDistance = 3.0f;         // Max distance in front of player
    public float waypointHeight = 1.5f;      // Eye level (adjust if needed)
    
    [Header("Stats Display (Optional)")]
    public TextMesh statsText;
    public bool autoCreateStatsText = true;
    public float statsDistanceFromCamera = 2.0f;
    public float statsHeightOffset = -0.15f;
    public float statsTextScale = 0.012f;
    public bool hideStatsText = false;
    
    [Header("Visual Metronome")]
    public bool enableMetronome = false;
    public GameObject metronomePrefab;  // Assign a small sphere/cube
    public float metronomeBPM = 40f;    // Beats per minute (slower default - adjust to your preference)
    public Color metronomeColor = Color.yellow;
    
    private Transform playerCamera;
    private int waypointsCollected = 0;
    private float sessionStartTime;
    private bool sessionActive = false;
    private float totalDistanceTraveled = 0f;
    private Vector3 lastCameraPosition;
    private Waypoint currentWaypoint;
    private GameObject metronomeObject;
    private Renderer metronomeRenderer;
    private float metronomeBeatTime;
    
    void Start()
    {
        // Get the player camera (MRTK's Main Camera)
        playerCamera = Camera.main?.transform;
        if (playerCamera == null)
        {
            Debug.LogError("WaypointSystemManager: Main Camera not found!");
            return;
        }
        
        if (waypointPrefab == null)
        {
            Debug.LogError("WaypointSystemManager: Waypoint prefab not assigned!");
            return;
        }

        if (statsText == null && autoCreateStatsText)
        {
            CreateStatsText();
        }

        if (statsText != null)
        {
            statsText.gameObject.SetActive(!hideStatsText);
        }
        
        // Start the session after a moment (let user orient)
        Invoke("StartSession", 1f);
    }
    
    void StartSession()
    {
        sessionActive = true;
        sessionStartTime = Time.time;
        lastCameraPosition = playerCamera.position;
        
        // Setup metronome if enabled
        if (enableMetronome)
        {
            SetupMetronome();
        }
        
        SpawnNextWaypoint();
        Debug.Log("Waypoint session started! Collect " + totalWaypoints + " waypoints.");
    }

    void CreateStatsText()
    {
        GameObject statsObj = new GameObject("StatsText");
        statsText = statsObj.AddComponent<TextMesh>();
        statsText.fontSize = 48;
        statsText.color = Color.white;
        statsText.alignment = TextAlignment.Center;
        statsText.anchor = TextAnchor.MiddleCenter;
        statsObj.transform.localScale = Vector3.one * statsTextScale;

        if (playerCamera != null)
        {
            statsObj.transform.SetParent(playerCamera, false);
            statsObj.transform.localPosition = new Vector3(0f, statsHeightOffset, statsDistanceFromCamera);
            statsObj.transform.localRotation = Quaternion.identity;
        }
    }
    
    void SpawnNextWaypoint()
    {
        if (waypointsCollected >= totalWaypoints)
        {
            EndSession();
            return;
        }
        
        // Generate random position in front of player
        Vector3 spawnPos = GenerateRandomPosition();
        
        // Instantiate waypoint
        GameObject waypointObj = Instantiate(waypointPrefab, spawnPos, Quaternion.identity);
        waypointObj.name = "Waypoint_" + (waypointsCollected + 1);
        
        // Get waypoint script
        Waypoint waypoint = waypointObj.GetComponent<Waypoint>();
        if (waypoint != null)
        {
            currentWaypoint = waypoint;
            waypoint.OnWaypointCollected += OnWaypointCollected;
        }
        else
        {
            Debug.LogWarning("Waypoint prefab missing Waypoint script component!");
        }
        
        Debug.Log("Spawned waypoint " + (waypointsCollected + 1) + " at " + spawnPos);
    }
    
    Vector3 GenerateRandomPosition()
    {
        // Random distance in front
        float distance = Random.Range(minDistance, maxDistance);
        
        // Random angle (±45 degrees from center)
        float angle = Random.Range(-45f, 45f);
        
        // Get forward direction from camera
        Vector3 forward = playerCamera.forward;
        forward.y = 0;  // Keep horizontal
        forward.Normalize();
        
        // Rotate by angle
        Quaternion rot = Quaternion.Euler(0, angle, 0);
        Vector3 direction = rot * forward;
        
        // Calculate world position
        Vector3 position = playerCamera.position + (direction * distance);
        position.y = playerCamera.position.y;  // Match current eye level
        
        return position;
    }
    
    void OnWaypointCollected()
    {
        waypointsCollected++;
        currentWaypoint = null;
        Debug.Log("Waypoints collected: " + waypointsCollected + " / " + totalWaypoints);
        
        UpdateStats();
        
        // Spawn next waypoint after short delay
        Invoke("SpawnNextWaypoint", 0.5f);
    }
    
    void Update()
    {
        if (!sessionActive || playerCamera == null)
            return;
        
        // Track distance traveled
        float frameDistance = Vector3.Distance(playerCamera.position, lastCameraPosition);
        totalDistanceTraveled += frameDistance;
        lastCameraPosition = playerCamera.position;
        
        // Update metronome
        if (enableMetronome && metronomeObject != null)
        {
            UpdateMetronome();
        }
        
        UpdateStats();
    }
    
    void UpdateStats()
    {
        if (statsText == null || !sessionActive)
            return;
        
        float elapsed = Time.time - sessionStartTime;
        int minutes = (int)(elapsed / 60);
        int seconds = (int)(elapsed % 60);
        
        statsText.text = string.Format(
            "Waypoints: {0}/{1}\nDistance: {2:F1}m\nOff-course: {3:F0}%\nTime: {4:00}:{5:00}",
            waypointsCollected,
            totalWaypoints,
            totalDistanceTraveled,
            currentWaypoint != null ? currentWaypoint.CurrentOffCoursePercent : 0f,
            minutes,
            seconds
        );
    }

    public string GetStatsText()
    {
        if (!sessionActive)
        {
            return "Session not started";
        }

        float elapsed = Time.time - sessionStartTime;
        int minutes = (int)(elapsed / 60);
        int seconds = (int)(elapsed % 60);

        return string.Format(
            "Waypoints: {0}/{1}\nDistance: {2:F1}m\nOff-course: {3:F0}%\nTime: {4:00}:{5:00}",
            waypointsCollected,
            totalWaypoints,
            totalDistanceTraveled,
            currentWaypoint != null ? currentWaypoint.CurrentOffCoursePercent : 0f,
            minutes,
            seconds
        );
    }
    
    void EndSession()
    {
        sessionActive = false;
        
        float elapsed = Time.time - sessionStartTime;
        int minutes = (int)(elapsed / 60);
        int seconds = (int)(elapsed % 60);
        
        if (statsText != null)
        {
            statsText.text = string.Format(
                "COMPLETE!\n{0} waypoints\nDistance: {1:F1}m\nTime: {2:00}:{3:00}",
                totalWaypoints, totalDistanceTraveled, minutes, seconds
            );
            statsText.color = Color.green;
        }
        
        Debug.Log("Session complete! Collected " + waypointsCollected + " in " + elapsed + "s. Distance: " + totalDistanceTraveled + "m");
    }
    
    void SetupMetronome()
    {
        // Create metronome visual in front of player at floor level
        if (metronomePrefab != null)
        {
            metronomeObject = Instantiate(metronomePrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            // Create default sphere
            metronomeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            metronomeObject.transform.localScale = Vector3.one * 0.1f;  // Small 10cm sphere
            Destroy(metronomeObject.GetComponent<Collider>());
        }
        
        metronomeObject.name = "Metronome";
        metronomeRenderer = metronomeObject.GetComponent<Renderer>();
        
        if (metronomeRenderer != null)
        {
            metronomeRenderer.material.color = metronomeColor;
            metronomeRenderer.material.EnableKeyword("_EMISSION");
        }
        
        metronomeBeatTime = 60f / metronomeBPM;
        Debug.Log("Visual metronome enabled at " + metronomeBPM + " BPM");
    }
    
    void UpdateMetronome()
    {
        // Get center position in front of player
        Vector3 forward = playerCamera.forward;
        forward.y = 0;
        forward.Normalize();
        
        Vector3 centerPos = playerCamera.position + forward * 1.5f;
        centerPos.y = playerCamera.position.y;  // Match eye level
        
        // Calculate pendulum swing on semicircular arc (slower swing)
        float beatProgress = (Time.time % metronomeBeatTime) / metronomeBeatTime;
        float angle = Mathf.Sin(beatProgress * Mathf.PI * 2f) * 45f;  // ±45 degrees swing (reduced from 60)
        
        // Convert angle to radians
        float angleRad = angle * Mathf.Deg2Rad;
        
        // Arc radius (smaller for eye level)
        float arcRadius = 0.3f;
        
        // Get right vector for side-to-side movement
        Vector3 right = playerCamera.right;
        right.y = 0;
        right.Normalize();
        
        // Calculate position on arc (x = sin, y = -cos for downward arc)
        float xOffset = Mathf.Sin(angleRad) * arcRadius;
        float yOffset = -Mathf.Cos(angleRad) * arcRadius + arcRadius;  // Offset so arc bottom is at floor
        
        // Apply arc position
        Vector3 metronomePos = centerPos + (right * xOffset);
        metronomePos.y = centerPos.y + yOffset;
        metronomeObject.transform.position = metronomePos;
        
        if (metronomeRenderer != null)
        {
            // Brighten at the extremes of the swing
            float intensity = Mathf.Abs(angle) > 35f ? 3f : 1.5f;
            metronomeRenderer.material.SetColor("_EmissionColor", metronomeColor * intensity);
        }
    }
    
    public void ToggleMetronome(bool enabled)
    {
        enableMetronome = enabled;
        
        if (metronomeObject != null)
        {
            metronomeObject.SetActive(enabled);
        }
        else if (enabled && sessionActive)
        {
            SetupMetronome();
        }
    }
}
