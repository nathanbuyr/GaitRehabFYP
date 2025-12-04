using System.Collections;
using UnityEngine;

public class BeaconController : MonoBehaviour
{
    private AudioSource audioSource;
    
    public float currentBPM = 60f;
    public float minBPM = 30f;
    public float maxBPM = 120f;
    public float bpmChangeAmount = 10f;
    
    public float beepDuration = 0.1f;
    public float frequency = 800f;
    
    private bool isPlaying = false;
    
    void Start()
    {
        Debug.Log("BeaconController: Start");
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.spatialBlend = 1.0f;
        audioSource.loop = false;
        audioSource.volume = 1.0f;
        audioSource.playOnAwake = false;
        audioSource.minDistance = 0.5f;
        audioSource.maxDistance = 10f;
        
        audioSource.clip = CreateBeepClip();
        
        Debug.Log("Starting metronome at " + currentBPM + " BPM");
        StartMetronome();
    }
    
    public void StartMetronome()
    {
        if (!isPlaying)
        {
            isPlaying = true;
            StartCoroutine(MetronomeLoop());
            Debug.Log("Metronome started");
        }
    }
    
    public void StopMetronome()
    {
        isPlaying = false;
        StopAllCoroutines();
        Debug.Log("Metronome stopped");
    }
    
    public void IncreaseTempo()
    {
        currentBPM = Mathf.Min(currentBPM + bpmChangeAmount, maxBPM);
        Debug.Log("Tempo: " + currentBPM + " BPM");
    }
    
    public void DecreaseTempo()
    {
        currentBPM = Mathf.Max(currentBPM - bpmChangeAmount, minBPM);
        Debug.Log("Tempo: " + currentBPM + " BPM");
    }
    
    IEnumerator MetronomeLoop()
    {
        while (isPlaying)
        {
            audioSource.Play();
            float interval = 60f / currentBPM;
            yield return new WaitForSeconds(interval);
        }
    }
    
    AudioClip CreateBeepClip()
    {
        int sampleRate = 44100;
        int samples = (int)(sampleRate * beepDuration);
        AudioClip clip = AudioClip.Create("BeepClip", samples, 1, sampleRate, false);
        
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
    
    public float GetCurrentBPM()
    {
        return currentBPM;
    }
}