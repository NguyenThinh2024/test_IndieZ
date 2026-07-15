using Nexzap.Base;
using UnityEngine;

public class PlaySound : MonoBehaviour
{
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float volume = 0.2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioController.Instance.PlaySound(audioClip, volume);
    }
}
