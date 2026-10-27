using System.Collections.Generic;
using FireSafety.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FireSafety.Movement
{
    public sealed class ClickToMovePlayer : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 720f;
        private HousePrototype house;
        private Camera view;
        private Transform marker;
        private readonly List<int> cells = new List<int>();
        private readonly List<Vector3> waypoints = new List<Vector3>();
        private int waypoint;
        private ObjectInteraction interaction;
        public bool IsMoving => waypoint < waypoints.Count;

        public void Initialize(HousePrototype layout, Camera camera, Transform destinationMarker)
        {
            house = layout;
            view = camera;
            marker = destinationMarker;
            marker.gameObject.SetActive(false);
        }

        public void SetInteraction(ObjectInteraction controller) => interaction = controller;

        public void StopMovement()
        {
            waypoints.Clear();
            waypoint = 0;
            if (marker != null) marker.gameObject.SetActive(false);
        }

        public void FollowPath(IReadOnlyList<int> path)
        {
            StopMovement();
            // Include the current cell centre so new orders remain safe even during a step.
            foreach (int cell in path) waypoints.Add(house.CellToWorld(cell));
            if (waypoints.Count == 0) return;
            marker.position = waypoints[waypoints.Count - 1] + Vector3.up * 0.03f;
            marker.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (house == null || view == null) return;
            if ((interaction == null || !interaction.BlocksMovementInput)
                && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                SetDestination(Mouse.current.position.ReadValue());

            float distance = moveSpeed * Time.deltaTime;
            while (waypoint < waypoints.Count && distance > 0f)
            {
                Vector3 delta = waypoints[waypoint] - transform.position;
                float step = Mathf.Min(distance, delta.magnitude);
                if (delta.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(delta), turnSpeed * Time.deltaTime);
                    transform.position = Vector3.MoveTowards(transform.position, waypoints[waypoint], step);
                }
                distance -= step;
                if ((transform.position - waypoints[waypoint]).sqrMagnitude < 0.0001f)
                {
                    transform.position = waypoints[waypoint];
                    waypoint++;
                }
                else break;
            }
            if (waypoint >= waypoints.Count) marker.gameObject.SetActive(false);
        }

        private void SetDestination(Vector2 screenPosition)
        {
            Ray ray = view.ScreenPointToRay(screenPosition);
            // Raycast the actual surface so clicking walls/furniture cannot select the floor behind them.
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f) || !house.IsFloor(hit.collider))
            {
                return;
            }
            int start = house.WorldToCell(transform.position);
            int goal = house.WorldToCell(hit.point);
            if (!house.Navigation.TryFindPath(start, goal, cells))
            {
                // A blocked click leaves the existing valid route intact.
                return;
            }
            FollowPath(cells);
        }
    }
}
