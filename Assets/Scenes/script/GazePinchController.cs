using UnityEngine;
using UnityEngine.XR.Hands;

namespace Scenes.script
{
    public class GazePinchController : MonoBehaviour
    {
        [Header("Gaze Settings")]
        public Camera gazeCamera;
        public float rayDistance = 30f;
        public LayerMask raycastMask = ~0;
        
        [Header("Pinch Settings")]
        public float pinchThreshold = 0.7f;
        public bool requireBothHands = false;
        
        [Header("Visual Settings")]
        public bool showGazeRay = true;
        public Color gazeRayColor = Color.yellow;
        public float rayWidth = 0.003f;
        public bool highlightGazedObject = true;
        public Texture highlightTexture;
        
        [Header("Debug")]
        public bool verboseLogging = false;
        
        private XRHandTrackingEvents leftHandTracking;
        private XRHandTrackingEvents rightHandTracking;
        private XRHand leftHand;
        private XRHand rightHand;
        
        private LineRenderer gazeRay;
        private GameObject currentGazedObject;
        private Renderer currentGazedRenderer;
        private Texture originalTexture;
        
        private bool wasLeftPinching = false;
        private bool wasRightPinching = false;

        void Start()
        {
            if (gazeCamera == null)
                gazeCamera = Camera.main;
            
            FindHandTrackingEvents();
            
            if (showGazeRay)
            {
                CreateGazeRay();
            }
            
            Debug.Log("GazePinchController initialized");
        }

        void FindHandTrackingEvents()
        {
            XRHandTrackingEvents[] allHandEvents = FindObjectsOfType<XRHandTrackingEvents>();
            
            foreach (var hte in allHandEvents)
            {
                if (hte.handedness == Handedness.Left)
                {
                    leftHandTracking = hte;
                    leftHandTracking.jointsUpdated.AddListener(OnLeftHandJointsUpdated);
                    Debug.Log("Left hand tracking found");
                }
                else if (hte.handedness == Handedness.Right)
                {
                    rightHandTracking = hte;
                    rightHandTracking.jointsUpdated.AddListener(OnRightHandJointsUpdated);
                    Debug.Log("Right hand tracking found");
                }
            }
            
            if (leftHandTracking == null && rightHandTracking == null)
            {
                Debug.LogWarning("No XRHandTrackingEvents found! Add them to your hand controllers.");
            }
        }

        void OnDestroy()
        {
            if (leftHandTracking != null)
                leftHandTracking.jointsUpdated.RemoveListener(OnLeftHandJointsUpdated);
            
            if (rightHandTracking != null)
                rightHandTracking.jointsUpdated.RemoveListener(OnRightHandJointsUpdated);
        }

        void OnLeftHandJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            leftHand = args.hand;
        }

        void OnRightHandJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            rightHand = args.hand;
        }

        void CreateGazeRay()
        {
            GameObject rayObj = new GameObject("GazeRay");
            rayObj.transform.SetParent(transform);
            
            gazeRay = rayObj.AddComponent<LineRenderer>();
            gazeRay.startWidth = rayWidth;
            gazeRay.endWidth = rayWidth;
            
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            
            gazeRay.material = new Material(shader);
            gazeRay.startColor = gazeRayColor;
            gazeRay.endColor = new Color(gazeRayColor.r, gazeRayColor.g, gazeRayColor.b, 0.1f);
            
            Debug.Log("Gaze ray created");
        }

        void Update()
        {
            UpdateGazeRaycast();
            CheckPinchGestures();
        }

        void UpdateGazeRaycast()
        {
            if (gazeCamera == null) return;
            
            Vector3 origin = gazeCamera.transform.position;
            Vector3 direction = gazeCamera.transform.forward;
            
            if (gazeRay != null)
            {
                gazeRay.SetPosition(0, origin);
            }
            
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, rayDistance, raycastMask, QueryTriggerInteraction.Collide);
            
            if (hits.Length == 0)
            {
                ClearGazedObject();
                if (gazeRay != null)
                {
                    gazeRay.SetPosition(1, origin + direction * rayDistance);
                }
                return;
            }
            
            System.Collections.Generic.List<RaycastHit> validHits = new System.Collections.Generic.List<RaycastHit>();
            
            foreach (var hit in hits)
            {
                GameObject go = hit.collider.gameObject;
                
                if (go.name.Contains("Controller") || 
                    go.name.Contains("Hand") || 
                    go.name.Contains("Ray"))
                {
                    continue;
                }
                
                validHits.Add(hit);
            }
            
            if (validHits.Count == 0)
            {
                ClearGazedObject();
                if (gazeRay != null)
                {
                    gazeRay.SetPosition(1, origin + direction * rayDistance);
                }
                return;
            }
            
            validHits.Sort((a, b) => a.distance.CompareTo(b.distance));
            RaycastHit closest = validHits[0];
            
            if (gazeRay != null)
            {
                gazeRay.SetPosition(1, closest.point);
            }
            
            GameObject hitObject = closest.collider.gameObject;
            
            if (hitObject != currentGazedObject)
            {
                ClearGazedObject();
                SetGazedObject(hitObject);
            }
        }

        void SetGazedObject(GameObject obj)
        {
            currentGazedObject = obj;
            
            if (highlightGazedObject)
            {
                currentGazedRenderer = obj.GetComponent<Renderer>();
                if (currentGazedRenderer != null && highlightTexture != null)
                {
                    originalTexture = currentGazedRenderer.material.mainTexture;
                    currentGazedRenderer.material.mainTexture = highlightTexture;
                }
            }
            
            if (verboseLogging)
            {
                Debug.Log($"Gazing at: {obj.name}");
            }
        }

        void ClearGazedObject()
        {
            if (currentGazedRenderer != null && originalTexture != null)
            {
                currentGazedRenderer.material.mainTexture = originalTexture;
            }
            
            currentGazedObject = null;
            currentGazedRenderer = null;
            originalTexture = null;
        }

        void CheckPinchGestures()
        {
            bool isLeftPinching = CheckHandPinch(leftHand, "Left");
            bool isRightPinching = CheckHandPinch(rightHand, "Right");
            
            bool shouldSelect = false;
            
            if (requireBothHands)
            {
                shouldSelect = isLeftPinching && isRightPinching && 
                              !(wasLeftPinching && wasRightPinching);
            }
            else
            {
                shouldSelect = (isLeftPinching && !wasLeftPinching) || 
                              (isRightPinching && !wasRightPinching);
            }
            
            if (shouldSelect)
            {
                Debug.Log("👀✋ Gaze + Pinch detected!");
                PerformGazeSelection();
            }
            
            wasLeftPinching = isLeftPinching;
            wasRightPinching = isRightPinching;
        }

        bool CheckHandPinch(XRHand hand, string handName)
        {
            if (!hand.isTracked) return false;
            
            XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
            XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);
            
            if (thumbTip.TryGetPose(out Pose thumbPose) && indexTip.TryGetPose(out Pose indexPose))
            {
                float distance = Vector3.Distance(thumbPose.position, indexPose.position);
                float pinchStrength = Mathf.Clamp01(1f - (distance / 0.05f));
                
                return pinchStrength > pinchThreshold;
            }
            
            return false;
        }

        void PerformGazeSelection()
        {
            if (currentGazedObject == null)
            {
                Debug.Log("No object being gazed at");
                return;
            }
            
            Debug.Log($"👀✋ Selected with gaze + pinch: {currentGazedObject.name}");
            
            SubPanelController subPanel = currentGazedObject.GetComponent<SubPanelController>();
            if (subPanel != null)
            {
                subPanel.SelectPanel();
                
                MeshController mainPanel = FindMainPanel(currentGazedObject.transform);
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
            
            MeshController meshController = currentGazedObject.GetComponent<MeshController>();
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
    }
}
