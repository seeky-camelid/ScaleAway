using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
///
/// Adapted from Whisper.Utils.MicrophoneRecord
///
namespace Utils
{
    public class MicrophoneInitializer : MonoBehaviour
    {
        [Header("Microphone selection")] 
        [CanBeNull] public Dropdown microphoneDropdown;
        public string microphoneDefaultLabel = "Default microphone";

        public string SelectedMicDevice
        {
            get => GameManager.instance?.SelectedMic;
            set
            {
                if (value != null && !AvailableMicDevices.Contains(value))
                    throw new ArgumentException("Microphone device not found");
                if (GameManager.instance != null)
                {
                    GameManager.instance.SelectedMic = value;
                }
            }
        }

        public IEnumerable<string> AvailableMicDevices => Microphone.devices;

        private void Awake()
        {
            if(microphoneDropdown != null)
            {
                microphoneDropdown.options = AvailableMicDevices
                    .Prepend(microphoneDefaultLabel)
                    .Select(text => new Dropdown.OptionData(text))
                    .ToList();
                microphoneDropdown.value = microphoneDropdown.options
                    .FindIndex(op => op.text == microphoneDefaultLabel);
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneChanged);
            }
        }

        private void OnMicrophoneChanged(int ind)
        {
            if (microphoneDropdown == null) return;
            var opt = microphoneDropdown.options[ind];
            SelectedMicDevice = opt.text == microphoneDefaultLabel ? null : opt.text;
        }
    }
}
