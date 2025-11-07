using System;
using UnityEngine;

namespace Unary.Recusant
{
    public class CompiledLevelData : ScriptableObject
    {
        public string LevelName = string.Empty;
        public string NextLevelName = string.Empty;
        public int AiTriangleStartIndex = -1;
        public Vector3[] AiTriangleVertices;
        public AiTriangleData[] AiTriangles;
        public AiBoundData[] AiBounds;
        public int AiMarkupSize = -1;
        [NonSerialized]
        public AiMarkup[] AiMarkups;
    }
}
