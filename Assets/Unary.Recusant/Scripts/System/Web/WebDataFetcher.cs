using Newtonsoft.Json;
using System;
using System.Collections;
using Unary.Core;
using UnityEngine.Networking;

namespace Unary.Recusant
{
    public class WebDataFetcher : System<WebDataFetcher>
    {
        public const string RepoName = "unarysoftware.github.io";
        public const string RepoFolder = "../" + RepoName;
        public const string UrlBase = "https://" + RepoName + "/";

        IEnumerator GetJsonData<T>(string path, Action<object, string> dispatcher)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(UrlBase + path + ".json");

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Logger.Instance.Error("Error: " + webRequest.error);
            }
            else
            {
                dispatcher(JsonConvert.DeserializeObject(webRequest.downloadHandler.text, typeof(T)), webRequest.downloadHandler.text);
            }
        }

        public struct RecusantRewardsData
        {
            public string OriginalText;
            public RecusantRewardsDataEntry[] Entries;
        }

        public EventFunc<RecusantRewardsData> OnRewardsData = new();
        public RecusantRewardsData RewardsData { get; private set; }

        public override void Initialize()
        {
            if (!Steam.Initialized)
            {
                return;
            }

            StartCoroutine(GetJsonData<RecusantRewardsDataEntry[]>("recusant_rewards",
                (result, text) =>
                {
                    if (result is RecusantRewardsDataEntry[] entries)
                    {
                        RecusantRewardsData newData = new()
                        {
                            Entries = entries,
                            OriginalText = text
                        };

                        RewardsData = newData;
                        OnRewardsData.Publish(RewardsData);
                    }
                }));
        }

        public override void Deinitialize()
        {

        }
    }
}
