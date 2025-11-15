using Unary.Core;
using Steamworks;
using UnityEngine;
using System.Collections.Generic;

namespace Unary.Recusant
{
    public class SteamScreenshotManager : System<SteamScreenshotManager>
    {
        private Callback<ScreenshotReady_t> ScreenshotCallback;

        public override void Initialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }

            ScreenshotCallback = Callback<ScreenshotReady_t>.Create(OnScreenshot);
        }

        private void OnScreenshot(ScreenshotReady_t data)
        {
            if (data.m_eResult != EResult.k_EResultOK)
            {
                return;
            }

            if (UiManager.Instance.State.GetType() != typeof(GameplayState))
            {
                return;
            }

            LevelDefinition levelDefinition = LevelManager.Instance.LevelDefinition;

            if (levelDefinition == null)
            {
                return;
            }

            SteamScreenshots.SetLocation(data.m_hLocal, levelDefinition.FullName);

            List<CSteamID> taggedUsers = new();

            Camera camera = CameraManager.Instance.CurrentCamera;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

            var localPlayer = PlayerManager.Instance.LocalPlayer;
            var players = PlayerManager.Instance.Players;

            Bounds bounds;
            List<Renderer> renderers = new();

            foreach (var player in players)
            {
                // Dont include ourselves into a user screenshot tag
                if (player.Key == localPlayer)
                {
                    continue;
                }

                Component[] components = player.Value.GameObject.GetComponentsInChildren(typeof(Renderer), true);

                renderers.Clear();

                foreach (var component in components)
                {
                    renderers.Add((Renderer)component);
                }

                if (renderers.Count == 0)
                {
                    continue;
                }

                bounds = renderers[0].bounds;

                for (int i = 1; i < renderers.Count; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                if (GeometryUtility.TestPlanesAABB(planes, bounds))
                {
                    CSteamID targetSteamId = new(player.Value.GameObject.GetComponent<PlayerIdentity>().SteamId);
                    if (Steam.Instance.SteamId != targetSteamId)
                    {
                        taggedUsers.Add(targetSteamId);
                    }
                }
            }

            while (taggedUsers.Count > Constants.k_nScreenshotMaxTaggedUsers)
            {
                int index = Random.Range(0, taggedUsers.Count);
                taggedUsers.RemoveAt(index);
            }

            foreach (var user in taggedUsers)
            {
                SteamScreenshots.TagUser(data.m_hLocal, user);
            }

            var visualMods = SteamUGCManager.Instance.GetFileIdsForVisualTag();

            foreach (var mod in visualMods)
            {
                SteamScreenshots.TagPublishedFile(data.m_hLocal, mod);
            }
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
