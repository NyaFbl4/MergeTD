using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public partial class LanePath
    {
        public float GetProgressToEnd(Vector3 position, int targetWaypointIndex)
        {
            if (WaypointCount <= 1)
                return 1f;

            var totalDistance = GetPathDistance(0, WaypointCount - 1);
            if (totalDistance <= Mathf.Epsilon)
                return 1f;

            var safeTargetIndex = Mathf.Clamp(targetWaypointIndex, 0, WaypointCount - 1);
            var completedDistance = safeTargetIndex <= 0 ? 0f : GetPathDistance(0, safeTargetIndex);

            var targetPosition = GetWaypointPosition(safeTargetIndex);
            var previousPosition = safeTargetIndex <= 0
                ? GetWaypointPosition(0)
                : GetWaypointPosition(safeTargetIndex - 1);
            var segmentDistance = Vector3.Distance(previousPosition, targetPosition);

            if (segmentDistance > Mathf.Epsilon)
            {
                var remainingSegmentDistance = Vector3.Distance(position, targetPosition);
                completedDistance -= Mathf.Clamp(remainingSegmentDistance, 0f, segmentDistance);
            }

            return Mathf.Clamp01(completedDistance / totalDistance);
        }

        private float GetPathDistance(int fromIndex, int toIndex)
        {
            var distance = 0f;
            var safeFromIndex = Mathf.Clamp(fromIndex, 0, WaypointCount - 1);
            var safeToIndex = Mathf.Clamp(toIndex, 0, WaypointCount - 1);

            for (var i = safeFromIndex + 1; i <= safeToIndex; i++)
                distance += Vector3.Distance(GetWaypointPosition(i - 1), GetWaypointPosition(i));

            return distance;
        }
    }
}
