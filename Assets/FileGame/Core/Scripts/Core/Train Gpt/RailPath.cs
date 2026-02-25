using UnityEngine;

namespace Game.Train
{
    public class RailPath : MonoBehaviour
    {
        [Tooltip("Waypoints in order along the rail. Must be >= 2.")]
        public Transform[] points;

        private float[] segmentLengths;
        private float totalLength;

        public float TotalLength => totalLength;

        private void OnValidate() => Rebuild();

        public void Rebuild()
        {
            if (points == null || points.Length < 2)
            {
                segmentLengths = null;
                totalLength = 0f;
                return;
            }

            segmentLengths = new float[points.Length - 1];
            totalLength = 0f;

            for (int i = 0; i < points.Length - 1; i++)
            {
                float len = Vector3.Distance(points[i].position, points[i + 1].position);
                len = Mathf.Max(0.0001f, len);
                segmentLengths[i] = len;
                totalLength += len;
            }
        }

        public void Sample(float distance, out Vector3 position, out Vector3 forward)
        {
            if (segmentLengths == null || segmentLengths.Length == 0)
                Rebuild();

            if (segmentLengths == null || segmentLengths.Length == 0)
            {
                position = transform.position;
                forward = transform.forward;
                return;
            }

            float d = Mathf.Clamp(distance, 0f, totalLength);

            int seg = 0;
            while (seg < segmentLengths.Length - 1 && d > segmentLengths[seg])
            {
                d -= segmentLengths[seg];
                seg++;
            }

            Vector3 a = points[seg].position;
            Vector3 b = points[seg + 1].position;

            float t = d / segmentLengths[seg];
            position = Vector3.LerpUnclamped(a, b, t);

            forward = (b - a);
            if (forward.sqrMagnitude < 0.000001f) forward = transform.forward;
            else forward.Normalize();
        }
    }
}