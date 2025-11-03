using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace Scenes.script
{
    public class HandPinchSelector : MonoBehaviour
    {
        [Header("Hand Settings")]
        public bool useLeftHand = false;
        public bool useRightHand = true;
        
        [Header("Ray Settings")]
        public float rayDistance = 5f;
        public float rayOffset = 0.05f;
        
        [Header("Pinch Settings")]
        public float pinchThreshold = 0.03f;
        
        [Header("Visual Feedback")]
        public bool showHandRay = true;
        public Color rayColor = Color.cyan;
        
        [Header("Debug")]
        public bool verboseLogging = true;
        
        private InputDevice leftHandDevice;
        private InputDevice rightHandDevice;
        
        private bool leftWasPinching = false;
        private bool rightWasPinching = false;
        
        private LineRenderer leftRay;
        private LineRenderer rightRay;
        
        private float lastLogTime = 0f;
        private float logInterval = 2f;

        void Start()
        {
            FindHandDevices();
            
            if (useLeftHand)
            {
                leftRay = CreateHandRay("LeftHandRay");
            }
            
            if (useRightHand)
            {
                rightRay = CreateHandRay("RightHandRay");
            }
            
            Debug.Log("HandPokeSelector initialized - checking for hand tracking...");
            InvokeRepeating("CheckHandTrackingStatus", 1f, 3f);
        }

        void CheckHandTrackingStatus()
        {
            if (!leftHandDevice.isValid && !rightHandDevice.isValid)
            {
                Debug.LogWarning("No hand tracking devices found! Make sure hand tracking is enabled on your device.");
                FindHandDevices();
            }
            else
            {
                if (leftHandDevice.isValid)
                    Debug.Log($"Left hand device valid: {leftHandDevice.name}");
                if (rightHandDevice.isValid)
                    Debug.Log($"Right hand device valid: {rightHandDevice.name}");
            }
        }

        void Update()
        {
            if (!leftHandDevice.isValid || !rightHandDevice.isValid)
            {
                FindHandDevices();
            }
            
            if (useLeftHand && leftHandDevice.isValid)
            {
                ProcessHand(leftHandDevice, ref leftWasPinching, leftRay, "Left");
            }
            else if (useLeftHand && leftRay != null)
            {
                leftRay.enabled = false;
            }
            
            if (useRightHand && rightHandDevice.isValid)
            {
                ProcessHand(rightHandDevice, ref rightWasPinching, rightRay, "Right");
            }
            else if (useRightHand && rightRay != null)
            {
                rightRay.enabled = false;
            }
        }

        void FindHandDevices()
        {
            List<InputDevice> devices = new List<InputDevice>();
            
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Left, 
                devices);
            
            if (devices.Count > 0)
            {
                leftHandDevice = devices[0];
                Debug.Log($"✅ Found left hand device: {leftHandDevice.name}");
            }
            
            devices.Clear();
            
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HandTracking | InputDeviceCharacteristics.Right, 
                devices);
            
            if (devices.Count > 0)
            {
                rightHandDevice = devices[0];
                Debug.Log($"✅ Found right hand device: {rightHandDevice.name}");
            }
            
            if (leftHandDevice.isValid == false && rightHandDevice.isValid == false)
            {
                if (Time.time - lastLogTime > logInterval)
                {
                    Debug.LogWarning("No hand tracking devices found. Looking for all XR devices...");
                    
                    devices.Clear();
                    InputDevices.GetDevices(devices);
                    Debug.Log($"Total XR devices found: {devices.Count}");
                    foreach (var device in devices)
                    {
                        Debug.Log($"  Device: {device.name}, characteristics: {device.characteristics}");
                    }
                    
                    lastLogTime = Time.time;
                }
            }
        }

        void ProcessHand(InputDevice handDevice, ref bool wasPinching, LineRenderer ray, string handName)
        {
            Hand handData;
            if (!handDevice.TryGetFeatureValue(CommonUsages.handData, out handData))
            {
                if (verboseLogging && Time.time - lastLogTime > logInterval)
                {
                    Debug.LogWarning($"{handName} hand device is valid but no hand data available");
                }
                
                if (ray != null) ray.enabled = false;
                return;
            }

            Vector3 indexTipPos;
            Quaternion indexTipRot;
            
            if (!GetIndexFingerTip(handData, out indexTipPos, out indexTipRot))
            {
                if (verboseLogging && Time.time - lastLogTime > logInterval)
                {
                    Debug.LogWarning($"{handName} hand data found but could not get index finger tip");
                }
                
                if (ray != null) ray.enabled = false;
                return;
            }

            if (verboseLogging && Time.time - lastLogTime > logInterval)
            {
                Debug.Log($"✅ {handName} index finger at: {indexTipPos}");
                lastLogTime = Time.time;
            }

            Vector3 rayOrigin = indexTipPos + indexTipRot * Vector3.forward * rayOffset;
            Vector3 rayDirection = indexTipRot * Vector3.forward;

            if (ray != null && showHandRay)
            {
                ray.enabled = true;
                ray.SetPosition(0, rayOrigin);
                ray.SetPosition(1, rayOrigin + rayDirection * rayDistance);
            }

            bool isPinching = DetectPinch(handData);

            if (isPinching && !wasPinching)
            {
                Debug.Log($"✋ {handName} hand PINCH!");
                PerformRaySelection(rayOrigin, rayDirection);
            }

            wasPinching = isPinching;
        }

        bool GetIndexFingerTip(Hand handData, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            
            List<Bone> indexBones = new List<Bone>();
            if (handData.TryGetFingerBones(HandFinger.Index, indexBones))
            {
                if (indexBones.Count > 0)
                {
                    Bone tipBone = indexBones[indexBones.Count - 1];
                    
                    if (tipBone.TryGetPosition(out position) && 
                        tipBone.TryGetRotation(out rotation))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        bool DetectPinch(Hand handData)
        {
            List<Bone> thumbBones = new List<Bone>();
            List<Bone> indexBones = new List<Bone>();
            
            if (!handData.TryGetFingerBones(HandFinger.Thumb, thumbBones) ||
                !handData.TryGetFingerBones(HandFinger.Index, indexBones))
            {
                return false;
            }
            
            if (thumbBones.Count == 0 || indexBones.Count == 0)
            {
                return false;
            }
            
            Bone thumbTip = thumbBones[thumbBones.Count - 1];
            Bone indexTip = indexBones[indexBones.Count - 1];
            
            Vector3 thumbPos, indexPos;
            
            if (thumbTip.TryGetPosition(out thumbPos) && 
                indexTip.TryGetPosition(out indexPos))
            {
                float distance = Vector3.Distance(thumbPos, indexPos);
                return distance < pinchThreshold;
            }
            
            return false;
        }

        void PerformRaySelection(Vector3 origin, Vector3 direction)
        {
            Debug.DrawRay(origin, direction * rayDistance, Color.red, 1f);
            
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, rayDistance, ~0, QueryTriggerInteraction.Collide);
            
            if (hits.Length == 0)
            {
                Debug.Log("Hand ray: no hits");
                return;
            }
            
            List<RaycastHit> validHits = new List<RaycastHit>();
            
            foreach (var hit in hits)
            {
                GameObject go = hit.collider.gameObject;
                
                if (go.name.Contains("Controller") || 
                    go.name.Contains("Hand") || 
                    go.name.Contains("Ray") ||
                    go.name.Contains("Reticle"))
                {
                    continue;
                }
                
                validHits.Add(hit);
            }
            
            if (validHits.Count == 0)
            {
                Debug.Log("Hand ray: no valid hits");
                return;
            }
            
            validHits.Sort((a, b) => a.distance.CompareTo(b.distance));
            RaycastHit chosen = validHits[0];
            GameObject hitObject = chosen.collider.gameObject;
            
            Debug.Log($"Hand selected: {hitObject.name}");
            
            SubPanelController subPanel = hitObject.GetComponent<SubPanelController>();
            if (subPanel != null)
            {
                subPanel.SelectPanel();
                
                MeshController mainPanel = FindMainPanel(hitObject.transform);
                if (mainPanel != null)
                {
                    string subpanelPath = subPanel.GetFolderPath();
                    
                    if (!mainPanel.hasChild)
                    {
                        mainPanel.folderPath = subpanelPath;
                        mainPanel.SpawnChildPlane();
                    }
                    else
                    {
                        mainPanel.RemoveChildPlane();
                    }
                }
                return;
            }
            
            MeshController meshController = hitObject.GetComponent<MeshController>();
            if (meshController != null)
            {
                if (!meshController.hasChild)
                {
                    Debug.Log("Select a subpanel to proceed");
                }
                else
                {
                    meshController.RemoveChildPlane();
                }
            }
        }

        MeshController FindMainPanel(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                MeshController mc = current.GetComponent<MeshController>();
                if (mc != null && mc.GetComponent<SubPanelController>() == null)
                {
                    return mc;
                }
                current = current.parent;
            }
            return null;
        }

        LineRenderer CreateHandRay(string name)
        {
            GameObject rayObj = new GameObject(name);
            rayObj.transform.SetParent(transform);
            
            LineRenderer lr = rayObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.005f;
            lr.endWidth = 0.005f;
            
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            
            lr.material = new Material(shader);
            lr.startColor = rayColor;
            lr.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.2f);
            lr.enabled = false;
            
            return lr;
        }
    }
}
