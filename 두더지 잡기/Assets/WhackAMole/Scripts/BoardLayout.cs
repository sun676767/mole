using UnityEngine;

namespace WhackAMole
{
    [ExecuteAlways]
    public sealed class BoardLayout : MonoBehaviour
    {
        public BoardConfig config;
        public Mole molePrefab;
        [SerializeField] private Vector3[] localPositions;
        public Vector3[] LocalPositions => localPositions;
        public Mole[] Moles { get; private set; }

        private void Update()
        {
            // Also detects changes made directly on the shared config asset in edit mode.
            if (!Application.isPlaying) RefreshPositions();
        }

        public void RefreshPositions() => localPositions = config ? config.CreatePositions() : new Vector3[0];

        public bool Build()
        {
            if (!config || !molePrefab) return false;
            RefreshPositions();
            if (Moles != null)
                foreach (var mole in Moles) if (mole) Destroy(mole.gameObject);
            Moles = new Mole[localPositions.Length];
            for (int i = 0; i < Moles.Length; i++)
            {
                Moles[i] = Instantiate(molePrefab, transform);
                Moles[i].name = $"Mole_{i:00}";
                Moles[i].transform.localPosition = localPositions[i];
                Moles[i].Hide();
            }
            return true;
        }

        private void OnDrawGizmos()
        {
            if (!config) return;
            Gizmos.color = Color.yellow;
            foreach (var p in config.CreatePositions()) Gizmos.DrawWireSphere(transform.TransformPoint(p), .65f);
        }
    }
}
