using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace WhackAMole
{
    public sealed class MolePointerInput : MonoBehaviour
    {
        public Camera worldCamera;
        public GameManager game;
        public LayerMask moleLayers = ~0;
        [Min(1)] public float rayDistance = 100f;
        private InputAction click;
        private void Awake() => click = new InputAction("Hit", InputActionType.Button, "<Mouse>/leftButton");
        private void OnEnable() => click.Enable();
        private void OnDisable() => click.Disable();
        private void OnDestroy() => click.Dispose();
        private void Update()
        {
            if (!game || !game.IsPlaying || !worldCamera || Mouse.current == null || !click.WasPressedThisFrame()) return;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, rayDistance, moleLayers);
            game.Attempt(hit.collider ? hit.collider.GetComponentInParent<Mole>() : null);
        }
    }
}
