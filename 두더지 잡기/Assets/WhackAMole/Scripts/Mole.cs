using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace WhackAMole
{
    public enum MoleState { Hidden, Rising, Up, Hit, Falling }

    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public sealed class Mole : MonoBehaviour
    {
        public Sprite idleSprite, upSprite, hitSprite, downSprite;
        public UnityEvent onHit = new UnityEvent();
        public MoleState State { get; private set; }
        private SpriteRenderer visual;
        private Collider2D hitbox;
        private void Awake() { visual = GetComponent<SpriteRenderer>(); hitbox = GetComponent<Collider2D>(); Hide(); }

        private void Show(MoleState state, Sprite sprite, bool hittable)
        {
            State = state;
            visual.sprite = sprite;
            visual.enabled = state != MoleState.Hidden;
            hitbox.enabled = hittable;
        }
        public void Hide() => Show(MoleState.Hidden, idleSprite, false);

        public IEnumerator Appear(float transition, float duration)
        {
            Show(MoleState.Rising, downSprite, false);
            yield return new WaitForSeconds(transition);
            Show(MoleState.Up, upSprite, true);
            float end = Time.time + duration;
            while (State == MoleState.Up && Time.time < end) yield return null;
            if (State == MoleState.Hit) yield return new WaitForSeconds(transition);
            Show(MoleState.Falling, downSprite, false);
            yield return new WaitForSeconds(transition);
            Hide();
        }

        public bool TryHit()
        {
            if (State != MoleState.Up) return false;
            Show(MoleState.Hit, hitSprite, false);
            onHit.Invoke();
            return true;
        }
    }
}
