using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scenes.script
{
    public class PlaneChainMotionController : MonoBehaviour
    {
        [Header("Camera / Trigger")]
        public Transform xrCamera; // assign the XR camera (head)
        [Tooltip("Minimum camera rotation (degrees) required to trigger movement")]
        public float minAngleThresholdDegrees = 1f;

        [Header("Movement Settings")]
        [Tooltip("How fast each plane lerps to its target (larger = snappier)")]
        public float moveSmoothness = 8f;

        [Header("Rotation control")]
        [Tooltip("Multiplier for rotation sensitivity")]
        public float rotationMultiplier = 1f;

        private List<MeshController> allPlanes = new List<MeshController>();
        private Quaternion lastCameraRot;
        private Dictionary<Transform, Coroutine> runningRotations = new Dictionary<Transform, Coroutine>();

        void Start()
        {
            if (xrCamera == null)
            {
                xrCamera = Camera.main?.transform;
            }

            if (xrCamera == null)
            {
                Debug.LogError("[PlaneChainMotionController] No XR Camera assigned and no Camera.main found.");
                enabled = false;
                return;
            }

            lastCameraRot = xrCamera.rotation;
            RefreshPlanes();
        }

        void Update()
        {
            Quaternion currentRot = xrCamera.rotation;
            
            // Get only the Z-axis rotation difference
            float deltaZ = GetYRotationDelta(lastCameraRot, currentRot);
            
            // Only proceed if Z rotation exceeded threshold
            if (Mathf.Abs(deltaZ) >= minAngleThresholdDegrees)
            {
                // Apply multiplier
                float appliedZRotation = deltaZ * rotationMultiplier;
                
                // Apply rotation to all planes simultaneously (no ripple)
                ApplyRotationToAllPlanes(appliedZRotation);
                
                lastCameraRot = currentRot;
            }
        }

        float GetYRotationDelta(Quaternion from, Quaternion to)
        {
            // Extract only the Z rotation component from the quaternion delta
            Vector3 fromEuler = from.eulerAngles;
            Vector3 toEuler = to.eulerAngles;
            
            // Normalize angles to -180..180 range
            float fromY = NormalizeAngle(fromEuler.y);
            float toY = NormalizeAngle(toEuler.y);
            
            return toY - fromY;
        }

        float NormalizeAngle(float a)
        {
            while (a > 180f) a -= 360f;
            while (a < -180f) a += 360f;
            return a;
        }

        void ApplyRotationToAllPlanes(float yRotationAngle)
        {
            foreach (var plane in allPlanes)
            {
                if (plane == null) continue;
                
                Transform tf = plane.transform;
                
                // Calculate new position and rotation
                Vector3 targetPosition = CalculateOrbitPosition(tf, yRotationAngle);
                Quaternion targetRotation = CalculatePlaneRotation(tf);
                
                StartRotation(tf, targetPosition, targetRotation);
            }
        }

        Vector3 CalculateOrbitPosition(Transform planeTransform, float yRotationAngle)
        {
            // Get vector from camera to plane
            Vector3 cameraToPlane = planeTransform.position - xrCamera.position;
            
            // Rotate only around global Y axis (camera's Z rotation affects horizontal movement)
            Vector3 rotatedOffset = Quaternion.AngleAxis(yRotationAngle, Vector3.up) * cameraToPlane;
            
            // New position maintains same Y coordinate, only changes X and Z
            Vector3 newPosition = xrCamera.position + rotatedOffset;
            
            // Fix Y axis to original plane Y position
            Vector3 localOffset = planeTransform.position - xrCamera.position;
            rotatedOffset = Quaternion.AngleAxis(yRotationAngle, Vector3.up) * localOffset;
            newPosition = xrCamera.position + rotatedOffset;
            newPosition.y = planeTransform.position.y; // preserve height

            
            return newPosition;
        }

        Quaternion CalculatePlaneRotation(Transform planeTransform)
        {
            // Calculate direction from plane to camera (flattened on Y axis)
            Vector3 directionToCamera = xrCamera.position - planeTransform.position;
            directionToCamera.y = 0; // Keep the plane upright
            
            // Create rotation that faces the camera
            Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera.normalized, Vector3.up);

            // Apply correction for initial tilt (e.g., -90° X)
            Quaternion correction = Quaternion.Euler(-90f, 0f, 180f);
            targetRotation *= correction;
                        
            return targetRotation;
        }

        void StartRotation(Transform tf, Vector3 targetPos, Quaternion targetRot)
        {
            // Stop previous coroutine for this transform if running
            if (runningRotations.TryGetValue(tf, out Coroutine prev) && prev != null)
            {
                StopCoroutine(prev);
                runningRotations.Remove(tf);
            }

            Coroutine c = StartCoroutine(SmoothRotateMove(tf, targetPos, targetRot));
            runningRotations[tf] = c;
        }

        IEnumerator SmoothRotateMove(Transform tf, Vector3 targetPos, Quaternion targetRot)
        {
            Vector3 startPos = tf.position;
            Quaternion startRot = tf.rotation;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * moveSmoothness;
                tf.position = Vector3.Lerp(startPos, targetPos, t);
                tf.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            tf.position = targetPos;
            tf.rotation = targetRot;
            runningRotations.Remove(tf);
        }

        public void RefreshPlanes()
        {
            allPlanes.Clear();
            allPlanes.AddRange(FindObjectsOfType<MeshController>());
        }

        // Call this when new planes are spawned
        public void RegisterPlane(MeshController newPlane)
        {
            if (!allPlanes.Contains(newPlane))
            {
                allPlanes.Add(newPlane);
            }
        }

        // Call this when planes are destroyed
        public void UnregisterPlane(MeshController plane)
        {
            if (allPlanes.Contains(plane))
            {
                allPlanes.Remove(plane);
            }
        }
    }
}