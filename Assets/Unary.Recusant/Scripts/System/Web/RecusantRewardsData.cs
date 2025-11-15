using System;
using System.Collections.Generic;
using Unary.Core;
using UnityEngine;

namespace Unary.Recusant
{
    [Serializable]
    public enum RecusantRewardsType
    {
        Unspecified
    };

    [Serializable]
    public class RecusantRewardsDatEntryItem
    {
        [SerializeField]
        public AssetRef<ScriptableObject> Item;
        [SerializeField]
        public float Count;
    }

    [Serializable]
    public class RecusantRewardsDataEntry
    {
        [SerializeField]
        public UnixTime Time;
        [SerializeField]
        public ulong SteamId;
        [SerializeField]
        public RecusantRewardsType Type;
        [SerializeField]
        public string Comment;
        [SerializeField]
        public RecusantRewardsDatEntryItem[] Items;
    }
}
