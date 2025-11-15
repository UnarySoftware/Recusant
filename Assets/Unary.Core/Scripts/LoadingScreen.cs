using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Unary.Core
{
    public class LoadingScreen : CoreSystem<LoadingScreen>
    {
        public LevelBackgroundEntry SelectedEntry { get; private set; }

        private bool _enabled = true;
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;

                if (!_enabled)
                {
                    Destroy(canvasGO);
                }
            }
        }

        public GameObject canvasGO;

        public override bool Initialize()
        {
            Dictionary<string, LevelBackgroundEntry> entries = new();

            List<string> definitions = ContentLoader.Instance.GetAssetPaths(typeof(LoadingBackgrounds));

            foreach (var definition in definitions)
            {
                LoadingBackgrounds levelBackgrounds = ContentLoader.Instance.LoadAsset<LoadingBackgrounds>(definition);

                foreach (var level in levelBackgrounds.Entries)
                {
                    entries[level.IdentifyingString] = level;
                }
            }

            List<LevelBackgroundEntry> finalEntries = entries.Values.ToList();

            SelectedEntry = finalEntries[Random.Range(0, finalEntries.Count)];

            // 1. Create Canvas GameObject
            canvasGO = new GameObject("FullscreenCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay on top of everything
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080); // Adjust as needed
            canvasGO.AddComponent<GraphicRaycaster>(); // Required for UI interaction, can be removed if not needed

            // 2. Create RawImage GameObject as child of Canvas
            GameObject rawImageGO = new GameObject("FullscreenRawImage");
            rawImageGO.transform.SetParent(canvasGO.transform, false); // Keep local position/scale

            RawImage rawImage = rawImageGO.AddComponent<RawImage>();
            rawImage.texture = SelectedEntry.Asset.Value; // Assign the Texture2D

            // 3. Stretch RawImage to fill Canvas (and thus, screen)
            RectTransform rawImageRect = rawImageGO.GetComponent<RectTransform>();
            rawImageRect.anchorMin = Vector2.zero; // Bottom-left anchor
            rawImageRect.anchorMax = Vector2.one;  // Top-right anchor
            rawImageRect.offsetMin = Vector2.zero; // No offset from anchors
            rawImageRect.offsetMax = Vector2.zero; // No offset from anchors


            return true;
        }
    }
}
