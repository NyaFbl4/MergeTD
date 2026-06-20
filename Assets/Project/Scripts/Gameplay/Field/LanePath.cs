using UnityEngine;

namespace Project.Scripts.Gameplay.Field
{
    public partial class LanePath : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints;

        public int WaypointCount => _waypoints?.Length ?? 0;

        public Vector3 GetSpawnPosition()
        {
            if (WaypointCount == 0)
                return transform.position;

            return _waypoints[0].position;
        }

        public Vector3 GetWaypointPosition(int index)
        {
            if (WaypointCount == 0)
                return transform.position;

            var safeIndex = Mathf.Clamp(index, 0, WaypointCount - 1);
            return _waypoints[safeIndex].position;
        }

        private void OnDrawGizmosSelected()
        {
            if (_waypoints == null || _waypoints.Length < 2)
                return;

            Gizmos.color = Color.yellow;
            for (var i = 1; i < _waypoints.Length; i++)
            {
                if (_waypoints[i - 1] == null || _waypoints[i] == null)
                    continue;

                Gizmos.DrawLine(_waypoints[i - 1].position, _waypoints[i].position);
            }
        }
    }
}
