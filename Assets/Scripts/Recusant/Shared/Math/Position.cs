using System;
using UnityEngine;

namespace Recusant
{
    public class Position
    {
        private static Vector3 magnitudeVector = new();
        private static float distance = float.MaxValue;
        private static float tempDistance = 0.0f;
        private static int closestEntry = -1;

        public static void CalculateCloserEntry(int entryIndex, Vector3 entryPosition, Vector3 targetPosition)
        {
            magnitudeVector.x = targetPosition.x - entryPosition.x;
            magnitudeVector.y = targetPosition.y - entryPosition.y;
            magnitudeVector.z = targetPosition.z - entryPosition.z;

            tempDistance = magnitudeVector.sqrMagnitude;

            if (tempDistance < distance)
            {
                closestEntry = entryIndex;
                distance = tempDistance;
            }
        }

        public static int GetClosestEntry()
        {
            int resultEntry = closestEntry;
            distance = float.MaxValue;
            closestEntry = -1;
            return resultEntry;
        }
    }
}
