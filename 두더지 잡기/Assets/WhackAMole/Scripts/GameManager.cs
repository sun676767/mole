using System;
using System.Collections;
using UnityEngine;

namespace WhackAMole
{
    public enum MoleType
    {
        Normal,     // 일반 두더지: 기본 점수
        Golden,     // 황금 두더지: 점수 2배 + 빠른 속도
        TimeBonus,  // 시간 두더지: 남은 시간 +3초
        Bomb,       // 폭탄 두더지: 때리면 감점 + 강한 카메라 진동
        Giant       // 대왕 두더지: 크기 1.5배 + 3번 때려야 처치 + 점수 3배
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public BoardLayout board;
        public bool autoStart = true;
        public bool saveBest = true;

        [Header("Camera Shake")]
        public Camera targetCamera;
        public float missShakeDuration = 0.2f;
        public float missShakeMagnitude = 0.2f;
        public float bombShakeDuration = 0.45f;
        public float bombShakeMagnitude = 0.5f;

        [Header("Mole Ability Settings")]
        public float bonusSeconds = 3f;
        public int bombPenalty = 50;
        public int giantMaxHp = 3;         // 대왕 두더지 처치에 필요한 타격 횟수
        public float giantScale = 1.5f;    // 대왕 두더지 크기 배율

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
        private Coroutine shakeRoutine;
        private BoardConfig rules;
        private Vector3 camOriginPos;

        // 각 구멍(Mole)에 배정된 현재 두더지 타입, 체력, 원래 색상 및 크기 저장
        private MoleType[] moleTypes;
        private int[] moleHps;
        private Color[] originalColors;
        private Vector3[] originalScales;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(gameObject); return; }

            if (!targetCamera) targetCamera = Camera.main;
            if (targetCamera) camOriginPos = targetCamera.transform.localPosition;
        }

        private void Start() { if (autoStart) StartGame(); }

        public void StartGame()
        {
            StopAllCoroutines();
            shakeRoutine = null;
            if (targetCamera) targetCamera.transform.localPosition = camOriginPos;

            IsPlaying = false;
            if (!board || !board.Build()) { Debug.LogError("Board, Config, Mole Prefab을 연결하세요.", this); return; }
            if (rules) Destroy(rules);

            rules = Instantiate(board.config);
            InitMoleData();

            Hits = Misses = Score = 0;
            Remaining = rules.gameSeconds;
            Best = PlayerPrefs.GetInt(BestKey, 0);
            Rank = "B";
            IsPlaying = true;
            Changed?.Invoke();
            sequence = StartCoroutine(Sequence());
        }

        private void InitMoleData()
        {
            int count = board.Moles.Length;
            moleTypes = new MoleType[count];
            moleHps = new int[count];
            originalColors = new Color[count];
            originalScales = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                if (!board.Moles[i]) continue;
                originalScales[i] = board.Moles[i].transform.localScale;

                var renderer = board.Moles[i].GetComponentInChildren<Renderer>();
                var sprite = board.Moles[i].GetComponentInChildren<SpriteRenderer>();

                if (renderer) originalColors[i] = renderer.material.color;
                else if (sprite) originalColors[i] = sprite.color;
            }
        }

        private IEnumerator Sequence()
        {
            int index = 0;
            while (IsPlaying)
            {
                // 1. 랜덤 두더지 타입 결정 및 외형 적용
                MoleType selectedType = GetRandomMoleType();
                SetMoleType(index, selectedType);

                // 2. 황금 두더지는 빠르게(0.7배), 대왕 두더지는 묵직하게 오래(1.5배) 머무름
                float speedMultiplier = 1f;
                if (selectedType == MoleType.Golden) speedMultiplier = 0.7f;
                else if (selectedType == MoleType.Giant) speedMultiplier = 1.5f;

                yield return board.Moles[index].Appear(
                    rules.transitionSeconds * speedMultiplier,
                    rules.upSeconds * speedMultiplier
                );

                // 대왕 두더지가 아직 살아있다면 처치되거나 사라질 때까지 대기
                while (IsPlaying && moleTypes[index] == MoleType.Giant && moleHps[index] > 0)
                {
                    yield return null;
                }

                // 3. 다음 구멍 인덱스 선택
                index = UnityEngine.Random.Range(0, board.Moles.Length);
                yield return new WaitForSeconds(rules.intervalSeconds);
            }
        }

        private MoleType GetRandomMoleType()
        {
            float roll = UnityEngine.Random.value; // 0.0 ~ 1.0
            if (roll < 0.50f) return MoleType.Normal;    // 50%
            if (roll < 0.65f) return MoleType.Golden;    // 15%
            if (roll < 0.80f) return MoleType.TimeBonus; // 15%
            if (roll < 0.90f) return MoleType.Bomb;      // 10%
            return MoleType.Giant;                       // 10% (대왕 두더지)
        }

        private void SetMoleType(int index, MoleType type)
        {
            if (index < 0 || index >= board.Moles.Length) return;
            moleTypes[index] = type;
            moleHps[index] = (type == MoleType.Giant) ? giantMaxHp : 1;

            Mole mole = board.Moles[index];
            if (!mole) return;

            Color targetColor = originalColors[index];
            Vector3 targetScale = originalScales[index];

            switch (type)
            {
                case MoleType.Golden:
                    targetColor = Color.yellow;
                    break;
                case MoleType.TimeBonus:
                    targetColor = Color.cyan;
                    break;
                case MoleType.Bomb:
                    targetColor = Color.red;
                    break;
                case MoleType.Giant:
                    targetColor = Color.magenta;
                    targetScale = originalScales[index] * giantScale;
                    break;
            }

            mole.transform.localScale = targetScale;

            // 3D(Renderer)와 2D(SpriteRenderer) 모두 색상 변경 지원
            var renderer = mole.GetComponentInChildren<Renderer>();
            if (renderer) renderer.material.color = targetColor;

            var sprite = mole.GetComponentInChildren<SpriteRenderer>();
            if (sprite) sprite.color = targetColor;
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

            if (target && target.TryHit())
            {
                int index = Array.IndexOf(board.Moles, target);
                MoleType hitType = (index >= 0) ? moleTypes[index] : MoleType.Normal;

                ApplyMoleAbility(index, target, hitType);
            }
            else
            {
                Misses++;
                ShakeCamera(missShakeDuration, missShakeMagnitude);
            }
            Changed?.Invoke();
        }

        private void ApplyMoleAbility(int index, Mole target, MoleType type)
        {
            switch (type)
            {
                case MoleType.Normal:
                    Hits++;
                    Score += rules.pointsPerHit;
                    break;

                case MoleType.Golden:
                    Hits++;
                    Score += rules.pointsPerHit * 2;
                    ShakeCamera(0.1f, 0.1f); // 경쾌한 타격 진동
                    break;

                case MoleType.TimeBonus:
                    Hits++;
                    Score += rules.pointsPerHit;
                    Remaining += bonusSeconds; // 시간 추가
                    break;

                case MoleType.Bomb:
                    Misses++;
                    Score = Mathf.Max(0, Score - bombPenalty); // 점수 차감 (0점 미만 방지)
                    ShakeCamera(bombShakeDuration, bombShakeMagnitude); // 강한 폭발 진동
                    break;

                case MoleType.Giant:
                    Hits++;
                    moleHps[index]--;

                    if (moleHps[index] > 0)
                    {
                        // 체력이 남았으면 10점씩 추가하며 다시 올라오게 유지
                        Score += 10;
                        ShakeCamera(0.1f, 0.15f);
                        StartCoroutine(target.Appear(0.05f, rules.upSeconds));
                    }
                    else
                    {
                        // 마지막 타격으로 대왕 두더지 처치
                        moleTypes[index] = MoleType.Normal;
                        Score += rules.pointsPerHit * 3;
                        ShakeCamera(0.3f, 0.35f);
                    }
                    break;
            }
        }

        public void ShakeCamera(float duration, float magnitude)
        {
            if (!targetCamera) targetCamera = Camera.main;
            if (!targetCamera) return;

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                targetCamera.transform.localPosition = camOriginPos;
            }
            shakeRoutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                targetCamera.transform.localPosition = camOriginPos + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            targetCamera.transform.localPosition = camOriginPos;
            shakeRoutine = null;
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
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (rules) Destroy(rules);
        }
    }
}