using UnityEngine;
using UnityEngine.Windows.Speech;

public class MetronomeVoiceControl : MonoBehaviour
{
    public WaypointSystemManager waypointManager;
    
    private KeywordRecognizer keywordRecognizer;
    
    void Start()
    {
        if (waypointManager == null)
        {
            waypointManager = FindObjectOfType<WaypointSystemManager>();
        }
        
        if (waypointManager == null)
        {
            Debug.LogError("MetronomeVoiceControl: No WaypointSystemManager found!");
            return;
        }
        
        SetupVoiceCommands();
    }
    
    void SetupVoiceCommands()
    {
        string[] keywords = { 
            "metronome on",
            "metronome off",
            "enable metronome",
            "disable metronome",
            "start metronome",
            "stop metronome"
        };
        
        keywordRecognizer = new KeywordRecognizer(keywords);
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
        
        Debug.Log("Metronome voice control active");
    }
    
    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string command = args.text.ToLower();
        Debug.Log("Metronome command: " + command);
        
        switch (command)
        {
            case "metronome on":
            case "enable metronome":
            case "start metronome":
                waypointManager.ToggleMetronome(true);
                Debug.Log("Metronome enabled");
                break;
                
            case "metronome off":
            case "disable metronome":
            case "stop metronome":
                waypointManager.ToggleMetronome(false);
                Debug.Log("Metronome disabled");
                break;
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
