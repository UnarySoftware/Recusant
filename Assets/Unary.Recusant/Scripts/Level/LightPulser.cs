using System.Collections.Generic;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    // TODO Revamp this quick class
    public class LightPulser : MonoBehaviour
    {
        public float PulseSpeed = 1.0f;

        public AudioSource AudioSource;

        public List<Light> Lights;
        public List<float> OriginalIntensities;

        private bool shouldPlay = false;

        private void Update()
        {
            if(Lights.Count == 0 || OriginalIntensities.Count != Lights.Count)
            {
                return;
            }

            float sinValue = Mathf.Sin(Time.time * PulseSpeed);
            float adjustedSin = (sinValue + 1) / 2;

            if (adjustedSin > 0.5f)
            {
                shouldPlay = true;
            }

            if (AudioSource != null && shouldPlay && adjustedSin < 0.1f)
            {
                AudioSource.Play();
                shouldPlay = false;
            }

            for (int i = 0; i < Lights.Count; i++)
            {
                Light light = Lights[i];
                float originalIntensity = OriginalIntensities[i];

                float newIntensity = adjustedSin.Remap(0.0f, 1.0f, 0.0f, originalIntensity);

                light.intensity = newIntensity;
            }
        }
    }
}
