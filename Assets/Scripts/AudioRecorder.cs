using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Audio;

public class AudioRecorder : MonoBehaviour
{
    public AudioSource audioSource;
    public int duration = 1;
    public AudioMixerGroup mixerGroupMicrophone;

    void Start()
    {
        print("AudioRecorder enabled!!!!!");
        //print(Microphone.devices.Length);
        Assert.IsNotNull(GameManager.instance);
        GameManager.instance.StartGameEvent += OnStartGame;
        GameManager.instance.EndGameEvent += OnEndGame;
    }

    void OnStartGame()
    {
        print("Start recording with: " + GameManager.instance.SelectedMic);
        /** Notes:
         * string:   Device name
         * loop:     If we want to continuously record, then must set to true
         * duration: Doesn't matter for continuous recording
         */
        audioSource.clip = Microphone.Start(GameManager.instance.SelectedMic, true, duration, AudioSettings.outputSampleRate);
        audioSource.outputAudioMixerGroup = mixerGroupMicrophone;
        audioSource.Play();
    }

    void OnEndGame()
    {
        print("End recording");
        audioSource.Stop();
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.StartGameEvent -= OnStartGame;
            GameManager.instance.EndGameEvent -= OnEndGame;
        }
        //print("AudioRecorder destroyed!!!!!");
    }

}
