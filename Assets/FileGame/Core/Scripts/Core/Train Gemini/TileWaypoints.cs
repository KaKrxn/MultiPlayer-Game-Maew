using UnityEngine;

/// <summary>
/// Holds an ordered array of waypoint transforms for a single track tile.
/// The train controller reads these to follow the path.
/// </summary>
public class TileWaypoints : MonoBehaviour
{
    [Tooltip("Drag waypoint transforms in order (1, 2, 3...)")]
    public Transform[] orderedPoints;
}
