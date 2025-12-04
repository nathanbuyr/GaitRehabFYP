using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Linq;

public class GaitRehabManager : MonoBehaviour
{
    [Header("References")]
    public GameObject beaconObject;
    public TextMesh bpmDisplayText;
    
    private BeaconController beaconController;
    private KeywordRecognizer keywordRecognizer;
    
    void Start()
    {
        Debug.Log("GaitRehabManager: Initializing");
        
        if (beaconObject != null)
        {
            beaconController = beaconObject.GetComponent<BeaconController>();
        }
        
        // Setup voice commands the old-fashioned way
        string[] keywords = { "faster", "slower", "start", "stop" };
        keywordRecognizer = new KeywordRecognizer(keywords);
        keywordRecognizer.OnPhraseRecognized += OnKeywordRecognized;
        keywordRecognizer.Start();
        
        Debug.Log("Voice commands active");
        
        if (bpmDisplayText != null)
        {
            UpdateBPMDisplay();
        }
    }
    
    void OnKeywordRecognized(PhraseRecognizedEventArgs args)
    {
        string command = args.text.ToLower();
        Debug.Log("Voice command: " + command);
        
        switch (command)
        {
            case "faster":
                if (beaconController != null)
                {
                    beaconController.IncreaseTempo();
                    UpdateBPMDisplay();
                }
                break;
                
            case "slower":
                if (beaconController != null)
                {
                    beaconController.DecreaseTempo();
                    UpdateBPMDisplay();
                }
                break;
                
            case "start":
                if (beaconController != null)
                {
                    beaconController.StartMetronome();
                }
                break;
                
            case "stop":
                if (beaconController != null)
                {
                    beaconController.StopMetronome();
                }
                break;
        }
    }
    
    void UpdateBPMDisplay()
    {
        if (bpmDisplayText != null && beaconController != null)
        {
            float bpm = beaconController.GetCurrentBPM();
            bpmDisplayText.text = bpm.ToString("F0") + " BPM";
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