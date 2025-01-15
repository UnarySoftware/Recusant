using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TempTools
{
    public class CustomComparer : IComparer<Transform>
    {
        public int Compare(Transform x, Transform y)
        {
            var regex = new Regex("(\\d+)");

            var xRegexResult = regex.Match(x.name);
            var yRegexResult = regex.Match(y.name);

            if (xRegexResult.Success && yRegexResult.Success)
            {
                return int.Parse(xRegexResult.Groups[1].Value).CompareTo(int.Parse(yRegexResult.Groups[1].Value));
            }

            return x.name.CompareTo(y.name);
        }
    }

    [MenuItem("Recusant/SortChildrenByName")]
    public static void SortChildrenByName()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            List<Transform> children = new();
            for (int i = obj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = obj.transform.GetChild(i);
                children.Add(child);
                child.parent = null;
            }

            var myComparer = new CustomComparer();
            children.Sort(myComparer);
            foreach (Transform child in children)
            {
                child.parent = obj.transform;
            }
        }
    }
}
