using UnityEngine;
using System.Collections.Generic;

public class WaypointManager : MonoBehaviour
{
    [Header("Waypoint Settings")]
public GameObject waypointPrefab;
public int totalWaypoints = 5;
public float minDistance = 2.0f;  
public float maxDistance = 4.0f;
public float waypointHeight = 1.5f;
    
    [Header("Stats")]
    public TextMesh statsDisplay;
    
    private Transform cameraTransform;
    private int waypointsReached = 0;
    private float sessionTime = 0f;
    private WaypointTarget currentWaypoint;
    private bool sessionActive = false;
    
    void Start()
    {
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
        }
        
        // Wait a moment for user to orient themselves
        Invoke("StartSession", 2f);
    }
    
    void StartSession()
    {
        sessionActive = true;
        SpawnNextWaypoint();
        Debug.Log("Waypoint session started!");
    }
    
    void Update()
    {
        if (sessionActive)
        {
            sessionTime += Time.deltaTime;
            UpdateStatsDisplay();
        }
    }
    
    void SpawnNextWaypoint()
    {
        if (waypointsReached >= totalWaypoints)
        {
            EndSession();
            return;
        }
        
        if (cameraTransform == null || waypointPrefab == null)
        {
            Debug.LogError("Missing camera or waypoint prefab!");
            return;
        }
        
        // Generate random position in front of user
        Vector3 spawnPosition = GenerateWaypointPosition();
        
        // Spawn waypoint
        GameObject waypointObj = Instantiate(waypointPrefab, spawnPosition, Quaternion.identity);
        currentWaypoint = waypointObj.GetComponent<WaypointTarget>();
        
        if (currentWaypoint != null)
        {
            currentWaypoint.OnWaypointReached += OnWaypointReached;
            Debug.Log("Waypoint " + (waypointsReached + 1) + " spawned at: " + spawnPosition);
        }
    }
    
    Vector3 GenerateWaypointPosition()
    {
        // Random distance
        float distance = Random.Range(minDistance, maxDistance);
        
        // Random angle (in front of user, ±60 degrees)
        float angle = Random.Range(-60f, 60f);
        
        // Calculate position
        Vector3 forward = cameraTransform.forward;
        forward.y = 0; // Keep on horizontal plane
        forward.Normalize();
        
        // Rotate by random angle
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        Vector3 direction = rotation * forward;
        
        // Position in world space
        Vector3 position = cameraTransform.position + (direction * distance);
        position.y = waypointHeight; // Set at eye level
        
        return position;
    }
    
    void OnWaypointReached()
    {
        waypointsReached++;
        Debug.Log("Waypoints reached: " + waypointsReached + "/" + totalWaypoints);
        
        // Spawn next waypoint after short delay
        Invoke("SpawnNextWaypoint", 1.5f);
    }
    
    void UpdateStatsDisplay()
    {
        if (statsDisplay == null) return;
        
        int minutes = (int)(sessionTime / 60);
        int seconds = (int)(sessionTime % 60);
        
        statsDisplay.text = string.Format(
            "Waypoints: {0}/{1}\nTime: {2:00}:{3:00}",
            waypointsReached, totalWaypoints, minutes, seconds
        );
    }
    
    void EndSession()
    {
        sessionActive = false;
        
        if (statsDisplay != null)
        {
            int minutes = (int)(sessionTime / 60);
            int seconds = (int)(sessionTime % 60);
            
            statsDisplay.text = string.Format(
                "SESSION COMPLETE!\n{0} waypoints\nTime: {1:00}:{2:00}",
                waypointsReached, minutes, seconds
            );
            statsDisplay.color = Color.green;
        }
        
        Debug.Log("Session complete! Time: " + sessionTime + "s");
    }
}