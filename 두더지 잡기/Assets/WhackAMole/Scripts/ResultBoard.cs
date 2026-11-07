using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WhackAMole
{
    public sealed class ResultBoard : MonoBehaviour
    {
        public GameManager game;
        public CanvasGroup group;
        public RectTransform panel;
        public Text resultText;
        public Button restartButton;
        [Min(.1f)] public float duration = .65f;
        private Coroutine animationRoutine;
        private void OnEnable()
        {
            if (game) { game.Finished += Show; game.Changed += HandleChange; }
            if (restartButton) restartButton.onClick.AddListener(Restart);
            Hide();
        }
        private void OnDisable()
        {
            if (game) { game.Finished -= Show; game.Changed -= HandleChange; }
            if (restartButton) restartButton.onClick.RemoveListener(Restart);
            if (animationRoutine != null) StopCoroutine(animationRoutine);
        }
        private void HandleChange() { if (game.IsPlaying && group && group.alpha > 0) Hide(); }
        private void Hide()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            if (!group) return;
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        }
        private void Show()
        {
            if (!group || !panel || !resultText) { Debug.LogError("Result Board 필드를 연결하세요.", this); return; }
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            animationRoutine = StartCoroutine(Animate());
        }
        private IEnumerator Animate()
        {
            group.blocksRaycasts = true;
            group.interactable = false;
            float elapsed = 0;
            do
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.1f, duration));
                float ease = 1 - Mathf.Pow(1 - t, 3);
                group.alpha = t;
                panel.anchoredPosition = Vector2.Lerp(new Vector2(0, -180), Vector2.zero, ease);
                panel.localScale = Vector3.one * Mathf.Lerp(.7f, 1, ease);
                resultText.text = $"RESULT\nHITS  {game.Hits}\nMISSES  {game.Misses}\nSCORE  {Mathf.RoundToInt(game.Score * ease)}\nBEST  {game.Best}\nRANK  {(t >= 1 ? game.Rank : "-")}";
                yield return null;
            } while (elapsed < Mathf.Max(.1f, duration));
            group.interactable = true;
            animationRoutine = null;
        }
        public void Restart() { Hide(); game.StartGame(); }
    }
}
