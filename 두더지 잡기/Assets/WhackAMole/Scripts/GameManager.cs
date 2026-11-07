using System;
using System.Collections;
using UnityEngine;

namespace WhackAMole
{
    public sealed class GameManager : MonoBehaviour
    {
        public BoardLayout board;
        public bool autoStart = true;
        public bool saveBest = true;
        public const string BestKey = "WhackAMole.Best.v1";
        public bool IsPlaying { get; private set; }
        public float Remaining { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Score { get; private set; }
        public int Best { get; private set; }
        public string Rank { get; private set; }
        public event Action Changed;
        public event Action Finished;
        private Coroutine sequence;
        private BoardConfig rules;

        private void Start() { if (autoStart) StartGame(); }
        public void StartGame()
        {
            StopAllCoroutines();
            IsPlaying = false;
            if (!board || !board.Build()) { Debug.LogError("Board, Config, Mole Prefab을 연결하세요.", this); return; }
            if (rules) Destroy(rules);
            // Freeze rules for this round; config edits apply at the next start.
            rules = Instantiate(board.config);
            Hits = Misses = Score = 0;
            Remaining = rules.gameSeconds;
            Best = PlayerPrefs.GetInt(BestKey, 0);
            Rank = "B";
            IsPlaying = true;
            Changed?.Invoke();
            sequence = StartCoroutine(Sequence());
        }
        private IEnumerator Sequence()
        {
            int index = 0;
            while (IsPlaying)
            {
                yield return board.Moles[index].Appear(rules.transitionSeconds, rules.upSeconds);
                index = (index + 1) % board.Moles.Length;
                yield return new WaitForSeconds(rules.intervalSeconds);
            }
        }
        private void Update()
        {
            if (!IsPlaying) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            if (Remaining <= 0) EndGame();
            else Changed?.Invoke();
        }
        public void Attempt(Mole target)
        {
            if (!IsPlaying || Remaining <= 0) return;
            if (target && target.TryHit()) { Hits++; Score += rules.pointsPerHit; }
            else Misses++;
            Changed?.Invoke();
        }
        public void EndGame()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            if (sequence != null) StopCoroutine(sequence);
            foreach (var mole in board.Moles) if (mole) mole.Hide();
            Best = Mathf.Max(Best, Score);
            if (saveBest) { PlayerPrefs.SetInt(BestKey, Best); PlayerPrefs.Save(); }
            Rank = Score >= rules.rankS ? "S" : Score >= rules.rankA ? "A" : "B";
            Changed?.Invoke();
            Finished?.Invoke();
        }
        private void OnDestroy() { if (rules) Destroy(rules); }
    }
}
