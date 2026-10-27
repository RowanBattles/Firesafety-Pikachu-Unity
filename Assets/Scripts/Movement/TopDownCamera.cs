using UnityEngine;

namespace FireSafety.Movement
{
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCamera : MonoBehaviour
    {
        [SerializeField] private float height = 20f;
        [SerializeField] private float viewSize = 8f;
        private Transform target;

        public void Follow(Transform player)
        {
            target = player;
            var view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = viewSize;
            view.nearClipPlane = 0.1f;
            view.farClipPlane = 60f;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(0.12f, 0.19f, 0.19f);
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            // Exact follow deliberately keeps the player in the screen centre.
            transform.SetPositionAndRotation(target.position + Vector3.up * height,
                Quaternion.Euler(90f, 0f, 0f));
        }
    }
}
