using UnityEngine;

namespace Blocks.Gameplay.Core
{
    public class TornadoMover : MonoBehaviour
    {
        [SerializeField] public float wanderRadius = 15f;
        [SerializeField] public float moveSpeed = 4f;
        [SerializeField] public float noiseScale = 0.3f;
        [SerializeField] public float noiseTimeScale = 0.5f;

        private Vector3 _spawnPoint;
        private float _noiseOffset;

        private void Awake()
        {
            _noiseOffset = Random.Range(0f, 100f);
            _spawnPoint = transform.position;
        }

        public void Initialize(Vector3 spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        private void Update()
        {
            float noiseTime = _noiseOffset + Time.time * noiseTimeScale;
            float nx = Mathf.PerlinNoise(noiseTime * noiseScale, 0f) * 2f - 1f;
            float nz = Mathf.PerlinNoise(0f, noiseTime * noiseScale) * 2f - 1f;
            Vector3 noiseDir = new Vector3(nx, 0f, nz).normalized;

            Vector3 toHome = _spawnPoint - transform.position;
            float dist = toHome.magnitude;
            float pullStrength = Mathf.Clamp01((dist - wanderRadius * 0.7f) / (wanderRadius * 0.3f));
            Vector3 finalDir = Vector3.Lerp(noiseDir, toHome.normalized, pullStrength);
            finalDir.y = 0f;

            transform.position += finalDir * moveSpeed * Time.deltaTime;
        }
    }
}
