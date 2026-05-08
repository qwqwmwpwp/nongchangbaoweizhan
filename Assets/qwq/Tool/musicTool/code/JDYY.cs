using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JDYY : MonoBehaviour
{
   public  AudioClip clip1;
   public AudioClip clip2;
    private float a = 2f;
    bool b = true;
    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(clip1);
        }
    }

    void Update()
    {

    }
}
