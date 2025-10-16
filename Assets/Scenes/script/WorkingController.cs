using UnityEngine;
using UnityEngine.XR;

namespace Scenes.script
{
    public class WorkingController : MonoBehaviour
    {
        public bool isLeftController = true;
        private LineRenderer laser;

        void Start()
        {
            // Create laser visual with error handling
            try
            {
                laser = gameObject.AddComponent<LineRenderer>();
                if (laser != null)
                {
                    laser.startWidth = 0.01f;
                    laser.endWidth = 0.01f;

                    // Try different shaders if Sprites/Default fails
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader == null)
                        shader = Shader.Find("Standard");

                    laser.material = new Material(shader);
                    laser.startColor = Color.green;
                    laser.endColor = Color.red;

                    Debug.Log((isLeftController ? "Left" : "Right") + " Working Controller Ready with Laser");
                }
                else
                {
                    Debug.LogError("Failed to create LineRenderer component");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error creating laser: " + e.Message);
            }
        }

        void Update()
        {
            UpdateControllerTracking();

            // Only draw laser if it was created successfully
            if (laser != null)
            {
                DrawLaser();
            }

            CheckForInput();
        }

        void UpdateControllerTracking()
        {
            InputDevice device = GetInputDevice();

            if (device.isValid)
            {
                // Update position from real controller
                if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                {
                    transform.localPosition = position;
                    Debug.Log((isLeftController ? "Left" : "Right") + " Controller Position: " + position);
                }

                // Update rotation from real controller
                if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                {
                    transform.localRotation = rotation;
                }
            }
            else
            {
                // Fallback position for testing
                transform.localPosition =
                    isLeftController ? new Vector3(-0.2f, -0.1f, 0.5f) : new Vector3(0.2f, -0.1f, 0.5f);
                Debug.LogWarning((isLeftController ? "Left" : "Right") +
                                 " Controller not detected, using fallback position");
            }
        }

        void DrawLaser()
        {
            if (laser == null) return;

            try
            {
                laser.SetPosition(0, transform.position);
                laser.SetPosition(1, transform.position + transform.forward * 5f);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error drawing laser: " + e.Message);
            }
        }

        void CheckForInput()
        {
            InputDevice device = GetInputDevice();
            if (device.isValid)
            {
                // Check for trigger press
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed))
                {
                    if (triggerPressed)
                    {
                        Debug.Log((isLeftController ? "Left" : "Right") + " TRIGGER PRESSED!");
                        ShootRaycast();
                    }
                }

                // Also check trigger value (analog)
                if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
                {
                    if (triggerValue > 0.1f) // Lower threshold
                    {
                        Debug.Log((isLeftController ? "Left" : "Right") + " TRIGGER VALUE: " + triggerValue);
                    }
                }

                // Check other buttons too
                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary)
                {
                    Debug.Log((isLeftController ? "Left" : "Right") + " PRIMARY BUTTON!");
                }
            }
            else
            {
                Debug.LogWarning((isLeftController ? "Left" : "Right") + " controller not detected");
            }
        }

        void ShootRaycast()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 10f))
            {
                Debug.Log("Ray hit: " + hit.collider.gameObject.name);

                // Try to get MeshController and trigger interaction
                MeshController meshController = hit.collider.GetComponent<MeshController>();
                if (meshController != null)
                {
                    Debug.Log("✅ SUCCESS: Found MeshController on " + hit.collider.name);
                    meshController.TriggerInteraction();
                }
                else
                {
                    Debug.LogWarning("No MeshController found on " + hit.collider.name);
                }
            }
            else
            {
                Debug.Log("Ray hit nothing");
            }
        }

        InputDevice GetInputDevice()
        {
            return InputDevices.GetDeviceAtXRNode(isLeftController ? XRNode.LeftHand : XRNode.RightHand);
        }
    }
}