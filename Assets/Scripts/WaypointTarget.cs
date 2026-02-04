using UnityEngine;

public class WaypointTarget : MonoBehaviour
{
    [Header("Audio Settings")]
    public float beepInterval = 1.0f;
    public float beepDuration = 0.1f;
    public float frequency = 600f;
    public float audioVolume = 0.8f;
    
    [Header("Visual Settings")]
    public bool pulseVisual = true;
    public float pulseScale = 1.3f;
    public Color targetColor = Color.cyan;
    
    [Header("Detection")]
    public float reachDistance = 1.0f; // How close to "reach" it
    
    private AudioSource audioSource;
    private Renderer targetRenderer;
    private Vector3 originalScale;
    private Transform cameraTransform;
    private bool isActive = true;
    
    // Event to notify when reached
    public System.Action OnWaypointReached;
    
    void Start()
    {
        SetupAudio();
        SetupVisual();
        
        // Find camera
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
        }
        
        originalScale = transform.localScale;
        
        StartCoroutine(BeaconLoop());
        
        Debug.Log("Waypoint created at: " + transform.position);
    }
    
    void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; // Full 3D
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = audioVolume;
        audioSource.minDistance = 1.0f;
        audioSource.maxDistance = 20.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0;
        
        // Create beep sound
        audioSource.clip = CreateBeepClip();
    }
    
    void SetupVisual()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetRenderer.material.color = targetColor;
            
            // Make it emissive so it glows
            targetRenderer.material.EnableKeyword("_EMISSION");
            targetRenderer.material.SetColor("_EmissionColor", targetColor * 0.5f);
        }
    }
    
    System.Collections.IEnumerator BeaconLoop()
    {
        while (isActive)
        {
            audioSource.Play();
            
            if (pulseVisual)
            {
                StartCoroutine(PulseEffect());
            }
            
            yield return new WaitForSeconds(beepInterval);
        }
    }
    
    System.Collections.IEnumerator PulseEffect()
    {
        float elapsed = 0f;
        float pulseDuration = 0.2f;
        
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            float scale = Mathf.Lerp(pulseScale, 1.0f, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    AudioClip CreateBeepClip()
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * beepDuration);
        AudioClip clip = AudioClip.Create("WaypointBeep", samples, 1, sampleRate, false);
        
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float fade = 1f - (t / beepDuration);
            data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * fade * 0.5f;
        }
        
        clip.SetData(data, 0);
        return clip;
    }
    
    void Update()
{
    if (!isActive || cameraTransform == null) return;
    
    // Get current distance
    float distance = Vector3.Distance(transform.position, cameraTransform.position);
    
    // DEBUG: Show distance in real-time
    if (Time.frameCount % 30 == 0) // Every half second
    {
        Debug.Log(string.Format("Distance to waypoint: {0:F2}m (need < {1:F2}m)", 
            distance, reachDistance));
    }
    
    // Visual feedback - gets brighter as you get closer
    if (targetRenderer != null)
    {
        float normalizedDistance = Mathf.Clamp01(distance / 10f);
        float intensity = Mathf.Lerp(3.0f, 0.5f, normalizedDistance);
        targetRenderer.material.SetColor("_EmissionColor", targetColor * intensity);
        
        // Change color based on distance
        if (distance < reachDistance * 1.5f)
        {
            // Close - turn green
            targetRenderer.material.color = Color.Lerp(Color.green, targetColor, (distance / (reachDistance * 1.5f)));
        }
        else
        {
            targetRenderer.material.color = targetColor;
        }
    }
    
    // Check if reached
    if (distance <= reachDistance)
    {
        Debug.Log(">>> WAYPOINT REACHED! <<<");
        ReachWaypoint();
    }
    
    // Make it face the player
    transform.LookAt(cameraTransform);
    transform.Rotate(0, 180, 0);
}
    void ReachWaypoint()
    {
        Debug.Log("Waypoint reached!");
        isActive = false;
        
        // Play success sound
        audioSource.pitch = 1.5f;
        audioSource.PlayOneShot(audioSource.clip);
        
        // Visual feedback
        StartCoroutine(SuccessEffect());
        
        // Notify manager
        OnWaypointReached?.Invoke();
    }
    
    System.Collections.IEnumerator SuccessEffect()
    {
        // Flash bright green
        Color originalColor = targetRenderer.material.color;
        targetRenderer.material.color = Color.green;
        targetRenderer.material.SetColor("_EmissionColor", Color.green * 2f);
        
        yield return new WaitForSeconds(0.5f);
        
        // Destroy waypoint
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}