using UnityEngine;

public class SpellSounds : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("Chybí AudioSource komponenta!");
        }
    }

    public void PlaySound(string soundName)
    {
        string path = "SoundsSpells/" + soundName; // bez přípony .wav
        AudioClip clip = ResourceCache.Load<AudioClip>(path);

        if (clip == null)
        {
            Debug.LogError($"Zvuk '{soundName}' nebyl nalezen v Resources/SoundsSpells/");
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}