using Netick;
using System.Collections.Generic;
using Unary.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unary.Recusant
{
    public class GameplayPlayerList : UiUnit
    {
        private VisualElement _root;

        public AssetRef<VisualTreeAsset> PlayerListEntry;

        public AssetRef<Texture2D> DefaultAvatar;

        public override void Initialize()
        {
            DefaultAvatar.Precache();

            var Document = GetComponent<UIDocument>();
            _root = Document.rootVisualElement.Q<VisualElement>("PlayerList");

            _root.style.display = DisplayStyle.None;

            PlayerManager.Instance.OnPlayerAdded.Subscribe(OnPlayerAdded, this);
            PlayerManager.Instance.OnPlayerRemoved.Subscribe(OnPlayerRemoved, this);

            Updater.Instance.SubscribeUpdate(UpdateValues, 0.2f);

            if (Steam.Initialized)
            {
                Steam.Instance.OnIdentityUpdate.Subscribe(OnIdentityUpdated, this);
            }
        }

        public override void Deinitialize()
        {
            PlayerManager.Instance.OnPlayerAdded.Unsubscribe(this);
            PlayerManager.Instance.OnPlayerRemoved.Unsubscribe(this);
            Updater.Instance.UnsubscribeUpdate(UpdateValues);

            if (Steam.Initialized)
            {
                Steam.Instance.OnIdentityUpdate.Unsubscribe(this);
            }
        }

        private struct Entry
        {
            public PlayerNetworkInfo Info;
            public PlayerIdentity Identity;
            public VisualElement Root;
            public VisualElement Picture;
            public Label Name;
            public Label Fps;
            public Label Ping;
            public Label BandwithIn;
            public Label BandwithOut;
            public Label LossIn;
            public Label LossOut;
        };

        private readonly Dictionary<NetworkPlayerId, Entry> _entries = new();

        private bool OnIdentityUpdated(ref Steam.PersonaStateChangeData data)
        {
            foreach (var entry in _entries)
            {
                if (entry.Value.Identity.InputSourcePlayerId == data.PlayerId)
                {
                    if (data.OnlineName != null)
                    {
                        entry.Value.Name.text = data.OnlineName;
                    }

                    if (data.Avatar != null)
                    {
                        entry.Value.Picture.style.backgroundImage = data.Avatar;
                    }

                    break;
                }
            }

            return true;
        }

        private bool OnPlayerAdded(ref PlayerManager.PlayerChangedData data)
        {
            var NewEntry = PlayerListEntry.Value.Instantiate();

            Entry newEntry = new()
            {
                Info = data.GameObject.GetComponent<PlayerNetworkInfo>(),
                Identity = data.GameObject.GetComponent<PlayerIdentity>(),
                Root = NewEntry,
                Picture = NewEntry.Q<VisualElement>("Picture"),
                Name = NewEntry.Q<Label>("Name"),
                Fps = NewEntry.Q<Label>("Fps"),
                Ping = NewEntry.Q<Label>("Ping"),
                BandwithIn = NewEntry.Q<Label>("BandwithIn"),
                BandwithOut = NewEntry.Q<Label>("BandwithOut"),
                LossIn = NewEntry.Q<Label>("LossIn"),
                LossOut = NewEntry.Q<Label>("LossOut"),
            };

            newEntry.Picture.style.backgroundImage = DefaultAvatar.Value;

            if (!Steam.Initialized)
            {
                newEntry.Name.text = newEntry.Identity.OfflineName;
            }

            _entries[data.Id] = newEntry;
            _root.Add(NewEntry);

            return true;
        }

        private bool OnPlayerRemoved(ref PlayerManager.PlayerChangedData info)
        {
            if (_entries.TryGetValue(info.Id, out var entry))
            {
                _root.Remove(entry.Root);
                _entries.Remove(info.Id);
            }
            return true;
        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }

        private void Update()
        {
            if (!IsOpen())
            {
                return;
            }

            if (Input.GetKey(KeyCode.Tab))
            {
                if (_root.style.display == DisplayStyle.None)
                {
                    _root.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                if (_root.style.display == DisplayStyle.Flex)
                {
                    _root.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateValues()
        {
            if (!IsOpen())
            {
                return;
            }

            foreach (var entry in _entries)
            {
                PlayerNetworkInfo info = entry.Value.Info;

                entry.Value.Fps.text = info.Fps.ToString();

                if (info.Ping == 0)
                {
                    entry.Value.Ping.text = "HOST";
                }
                else
                {
                    entry.Value.Ping.text = info.Ping.ToString();
                }

                entry.Value.BandwithIn.text = info.BandwithIn.ToString("0.0") + "<br>kb/s";
                entry.Value.BandwithOut.text = info.BandwithOut.ToString("0.0") + "<br>kb/s";
                entry.Value.LossIn.text = (info.PacketLossIn * 100.0f).ToString("0.0") + "%";
                entry.Value.LossOut.text = (info.PacketLossOut * 100.0f).ToString("0.0") + "%";
            }
        }
    }
}
