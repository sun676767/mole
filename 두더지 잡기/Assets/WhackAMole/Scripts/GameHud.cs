using UnityEngine;
using UnityEngine.UI;

namespace WhackAMole
{
    public sealed class GameHud : MonoBehaviour
    {
        public GameManager game;
        public Text label;
        private void OnEnable() { if (game) game.Changed += Refresh; Refresh(); }
        private void OnDisable() { if (game) game.Changed -= Refresh; }
        private void Refresh()
        {
            if (game && label) label.text = $"TIME {Mathf.CeilToInt(game.Remaining)}    SCORE {game.Score}    MISS {game.Misses}";
        }
    }
}
