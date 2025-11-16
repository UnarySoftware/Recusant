using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    public class AtmosphereProcessor : MonoBehaviour
    {
        public Material SkyboxMaterial;
        public Color DefaultFogColor = Color.white;

        private float _timer = 0.0f;
        public float Speed = 0.1f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            RenderSettings.fogColor = DefaultFogColor;
        }

        // Update is called once per frame
        void Update()
        {
            _timer += Speed * Time.deltaTime;

            float result = Mathf.Sin(_timer).Remap(-1.0f, 1.0f, 0.65f, 1.75f);

            SkyboxMaterial.SetFloat("_AtmosphereThickness", result);

            Color color = RenderSettings.fogColor;
            color.b = Mathf.Sin(_timer).Remap(-1.0f, 1.0f, 1.0f, 0.5f);
            RenderSettings.fogColor = color;
        }

        private void OnDestroy()
        {
            SkyboxMaterial.SetFloat("_AtmosphereThickness", 1.0f);
            RenderSettings.fogColor = DefaultFogColor;
        }
    }
}
