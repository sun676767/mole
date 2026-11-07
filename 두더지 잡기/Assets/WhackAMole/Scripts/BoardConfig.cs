using UnityEngine;

namespace WhackAMole
{
    [CreateAssetMenu(menuName = "Whack A Mole/Board Config")]
    public sealed class BoardConfig : ScriptableObject
    {
        [Min(1)] public int rows = 3;
        [Min(1)] public int columns = 3;
        public Vector2 spacing = new Vector2(2.5f, 2f);
        public Vector2 center = Vector2.zero;
        [Min(1)] public float gameSeconds = 30f;
        [Min(0.05f)] public float upSeconds = 0.8f;
        [Min(0.05f)] public float transitionSeconds = 0.15f;
        [Min(0)] public float intervalSeconds = 0.25f;
        [Min(1)] public int pointsPerHit = 100;
        public int rankA = 1000;
        public int rankS = 2000;

        public Vector3[] CreatePositions()
        {
            int r = Mathf.Clamp(rows, 1, 20), c = Mathf.Clamp(columns, 1, 20);
            var positions = new Vector3[r * c];
            for (int y = 0; y < r; y++)
                for (int x = 0; x < c; x++)
                    positions[y * c + x] = new Vector3(center.x + (x - (c - 1) * .5f) * spacing.x,
                        center.y + ((r - 1) * .5f - y) * spacing.y, 0);
            return positions;
        }

        private void OnValidate()
        {
            rows = Mathf.Clamp(rows, 1, 20); columns = Mathf.Clamp(columns, 1, 20);
            gameSeconds = Mathf.Max(1, gameSeconds); upSeconds = Mathf.Max(.05f, upSeconds);
            transitionSeconds = Mathf.Max(.05f, transitionSeconds); intervalSeconds = Mathf.Max(0, intervalSeconds);
            pointsPerHit = Mathf.Max(1, pointsPerHit); rankA = Mathf.Max(0, rankA); rankS = Mathf.Max(rankA, rankS);
        }
    }
}
