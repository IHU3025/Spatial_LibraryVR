using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Hands;

namespace Scenes.script
{
    public class XRIHandSelector : MonoBehaviour
    {
        [Header("References")]
        public ActionBasedController controller;
        public XRRayInteractor rayInteractor;
        public XRHandTrackingEvents handTrackingEvents;
        
        [Header("Selection Settings")]
        public float rayDistance = 30f;
        public LayerMask raycastMask = ~0;
        public float pinchThreshold = 0.7f;
        
        [Header("Visual Settings")]
        public bool hideHandModel = true;
        public bool showCustomRay = true;
        public Color handRayColor = Color.cyan;
        public float rayWidth = 0.005f;
        
        [Header("Debug")]
        public bool verboseLogging = true;
        
        private XRInteractorLineVisual lineVisual;
        private GameObject handModel;
        private LineRenderer customRay;
        private bool wasPinching = false;
        private float lastLogTime = 0f;
        private float logInterval = 2f;
        private XRHand xrHand;
        private bool isLeftHand;

        void Start()
        {
            if (controller == null)
                controller = GetComponent<ActionBasedController>();
            
            if (rayInteractor == null)
                rayInteractor = GetComponent<XRRayInteractor>();
            
            lineVisual = GetComponent<XRInteractorLineVisual>();
            
            isLeftHand = gameObject.name.Contains("Left");
            
            FindHandTrackingEvents();
            
            if (hideHandModel && controller != null)
            {
                StartCoroutine(HideHandModelDelayed());
            }
            
            if (showCustomRay)
            {
                CreateCustomRay();
            }
            
            Debug.Log($"{gameObject.name} XRIHandSelector initialized");
        }

        void FindHandTrackingEvents()
        {
            if (handTrackingEvents == null)
            {
                handTrackingEvents = GetComponentInChildren<XRHandTrackingEvents>();
            }
            
            if (handTrackingEvents == null)
            {
                XRHandTrackingEvents[] allHandEvents = FindObjectsOfType<XRHandTrackingEvents>();
                foreach (var hte in allHandEvents)
                {
                    if ((isLeftHand && hte.handedness == Handedness.Left) ||
                        (!isLeftHand && hte.handedness == Handedness.Right))
                    {
                        handTrackingEvents = hte;
                        Debug.Log($"{gameObject.name} found matching XRHandTrackingEvents");
                        break;
                    }
                }
            }
            
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
                Debug.Log($"{gameObject.name} subscribed to hand tracking events");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} - No XRHandTrackingEvents found! Add XR Hand Tracking rig or use fallback input.");
            }
        }

        void OnDestroy()
        {
            if (handTrackingEvents != null)
            {
                handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
            }
        }

        void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            xrHand = args.hand;
        }

        System.Collections.IEnumerator HideHandModelDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (controller.model != null)
            {
                handModel = controller.model.gameObject;
                handModel.SetActive(false);
                Debug.Log($"Hand model hidden on {gameObject.name}");
            }
        }

        void CreateCustomRay()
        {
            GameObject rayObj = new GameObject($"{gameObject.name}_CustomRay");
            rayObj.transform.SetParent(transform);
            
            customRay = rayObj.AddComponent<LineRenderer>();
            customRay.startWidth = rayWidth;
            customRay.endWidth = rayWidth;
            
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            
            customRay.material = new Material(shader);
            customRay.startColor = handRayColor;
            customRay.endColor = new Color(handRayColor.r, handRayColor.g, handRayColor.b, 0.3f);
            
            Debug.Log($"Custom ray created for {gameObject.name}");
        }

        void Update()
        {
            UpdateCustomRay();
            CheckPinchGesture();
        }

        void UpdateCustomRay()
        {
            if (customRay == null) return;
            
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            
            customRay.SetPosition(0, origin);
            customRay.SetPosition(1, origin + direction * rayDistance);
        }

        void CheckPinchGesture()
        {
            bool isPinching = false;
            float pinchStrength = 0f;
            
            if (xrHand.isTracked)
            {
                XRHandJoint thumbTip = xrHand.GetJoint(XRHandJointID.ThumbTip);
                XRHandJoint indexTip = xrHand.GetJoint(XRHandJointID.IndexTip);
                
                if (thumbTip.TryGetPose(out Pose thumbPose) && indexTip.TryGetPose(out Pose indexPose))
                {
                    float distance = Vector3.Distance(thumbPose.position, indexPose.position);
                    pinchStrength = Mathf.Clamp01(1f - (distance / 0.05f));
                    isPinching = pinchStrength > pinchThreshold;
                    
                    if (verboseLogging && Time.time - lastLogTime > logInterval)
                    {
                        Debug.Log($"{gameObject.name} - pinch distance: {distance:F4}, strength: {pinchStrength:F2}, isPinching: {isPinching}");
                        lastLogTime = Time.time;
                    }
                }
            }
            else
            {
                if (verboseLogging && Time.time - lastLogTime > logInterval)
                {
                    Debug.Log($"{gameObject.name} - hand not tracked, using fallback input");
                    lastLogTime = Time.time;
                }
                
                if (controller != null && controller.selectAction.action != null)
                {
                    float selectValue = controller.selectAction.action.ReadValue<float>();
                    isPinching = selectValue > 0.5f;
                }
            }
            
            if (isPinching && !wasPinching)
            {
                Debug.Log($"{gameObject.name} PINCH detected! Strength: {pinchStrength:F2}");
                PerformSelection();
            }
            
            wasPinching = isPinching;
        }

        void PerformSelection()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            
            Debug.DrawRay(origin, direction * rayDistance, Color.red, 1f);
            Debug.Log($"{gameObject.name} performing raycast");
            
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, rayDistance, raycastMask, QueryTriggerInteraction.Collide);
            
            Debug.Log($"{gameObject.name} raycast found {hits.Length} hits");
            
            if (hits.Length == 0)
            {
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
                Debug.Log($"{gameObject.name}: No valid hits after filtering");
                return;
            }
            
            validHits.Sort((a, b) => a.distance.CompareTo(b.distance));
            RaycastHit chosen = validHits[0];
            GameObject hitObject = chosen.collider.gameObject;
            
            Debug.Log($"{gameObject.name} selected: {hitObject.name}");
            
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
        
        public void ToggleHandModel()
        {
            if (handModel != null)
            {
                handModel.SetActive(!handModel.activeSelf);
            }
        }
    }
}
