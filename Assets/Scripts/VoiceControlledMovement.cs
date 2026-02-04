using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Linq;

public class VoiceControlledMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float stepDistance = 0.5f;        // Each step = 0.5 meters
    public float walkSpeed = 1.5f;           // Continuous walk speed
    public bool continuousWalk = false;
    
    private Transform cameraTransform;
    private KeywordRecognizer keywordRecognizer;
    private bool isWalking = false;
    
    void Start()
    {
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            cameraTransform = cam.transform;
            Debug.Log("VoiceControlledMovement: Camera found at " + cameraTransform.position);
        }
        
        SetupVoiceCommands();
    }
    
    void SetupVoiceCommands()
    {
        string[] keywords = { 
            "walk forward", 
            "step forward",
            "walk",
            "stop walking",
            "stop"
        };
        
        keywordRecognizer = new KeywordRecognizer(keywords);
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
        
        Debug.Log("Voice movement active - step distance: " + stepDistance + "m");
    }
    
    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string command = args.text.ToLower();
        Debug.Log("Movement command: " + command);
        
        switch (command)
        {
            case "walk forward":
            case "walk":
            case "step forward":
                TakeStep();
                break;
                
            case "stop walking":
            case "stop":
                StopContinuousWalking();
                break;
        }
    }
    
    void TakeStep()
    {
        if (cameraTransform == null) return;
        
        Vector3 oldPosition = cameraTransform.position;
        
        // Get forward direction (horizontal plane only)
        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        forward.Normalize();
        
        // Move camera forward
        Vector3 newPosition = cameraTransform.position + (forward * stepDistance);
        cameraTransform.position = newPosition;
        
        // Calculate actual movement
        float actualDistance = Vector3.Distance(oldPosition, newPosition);
        
        Debug.Log(string.Format("STEP: Moved {0:F2}m | From {1} to {2}", 
            actualDistance, oldPosition, newPosition));
    }
    
    void StartContinuousWalking()
    {
        isWalking = true;
        Debug.Log("Continuous walking started");
    }
    
    void StopContinuousWalking()
    {
        isWalking = false;
        Debug.Log("Continuous walking stopped");
    }
    
    void Update()
    {
        if (isWalking && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            forward.Normalize();
            
            cameraTransform.position += forward * walkSpeed * Time.deltaTime;
        }
    }
    
    void OnDestroy()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }
    }
}