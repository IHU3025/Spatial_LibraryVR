using UnityEngine;
using UnityEngine.XR;
// using UnityEngine.XR.Hands;
using System.Collections.Generic;

namespace Scenes.script
{
    public class GazePinchController : MonoBehaviour
    {
        [Header("Gaze Settings")]
        public Camera gazeCamera;
        public float gazeMaxDistance = 50f;
        public LineRenderer gazeIndicator;
        
        [Header("Pinch Settings")]
        public XRNode handNode = XRNode.RightHand;
        public float pinchThreshold = 0.7f;
        
        [Header("Visual Feedback")]
        public GameObject gazeReticle;
        public float reticleDistance = 2f;
        public Color highlightColor = Color.yellow;
        
        private bool wasPinching = false;
        private GameObject currentGazedObject;
        private Renderer currentGazedRenderer;
        private Color originalColor;
        private Material originalMaterial;

        void Start()
        {
            if (gazeCamera == null)
            {
                gazeCamera = Camera.main;
            }
            
            SetupGazeIndicator();
            SetupReticle();
            
            Debug.Log("GazePinchController initialized");
        }

        void SetupGazeIndicator()
        {
            if (gazeIndicator == null)
            {
                GameObject indicatorObj = new GameObject("GazeIndicator");
                indicatorObj.transform.SetParent(transform);
                gazeIndicator = indicatorObj.AddComponent<LineRenderer>();
                
                gazeIndicator.startWidth = 0.005f;
                gazeIndicator.endWidth = 0.005f;
                
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Standard");
                
                gazeIndicator.material = new Material(shader);
                gazeIndicator.startColor = new Color(1f, 1f, 0f, 0.3f);
                gazeIndicator.endColor = new Color(1f, 1f, 0f, 0.1f);
            }
        }

        void SetupReticle()
        {
            if (gazeReticle == null)
            {
                gazeReticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gazeReticle.name = "GazeReticle";
                gazeReticle.transform.SetParent(transform);
                gazeReticle.transform.localScale = Vector3.one * 0.02f;
                
                Destroy(gazeReticle.GetComponent<Collider>());
                
                Renderer rend = gazeReticle.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = highlightColor;
                }
            }
        }

        void Update()
        {
            UpdateGaze();
            CheckPinchInput();
        }

        void UpdateGaze()
        {
            if (gazeCamera == null) return;

            Vector3 origin = gazeCamera.transform.position;
            Vector3 direction = gazeCamera.transform.forward;

            if (gazeIndicator != null)
            {
                gazeIndicator.SetPosition(0, origin);
                gazeIndicator.SetPosition(1, origin + direction * gazeMaxDistance);
            }

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, gazeMaxDistance, ~0, QueryTriggerInteraction.Collide);
            
            List<RaycastHit> validHits = new List<RaycastHit>();
            
            foreach (var hit in hits)
            {
                GameObject go = hit.collider.gameObject;
                
                if (go.name.Contains("Controller") || go.name.Contains("Hand") || go.name.Contains("Reticle"))
                {
                    continue;
                }
                
                validHits.Add(hit);
            }

            if (validHits.Count > 0)
            {
                validHits.Sort((a, b) => a.distance.CompareTo(b.distance));
                RaycastHit closest = validHits[0];
                
                UpdateGazedObject(closest.collider.gameObject);
                
                if (gazeReticle != null)
                {
                    gazeReticle.transform.position = closest.point;
                    gazeReticle.SetActive(true);
                }
            }
            else
            {
                UpdateGazedObject(null);
                
                if (gazeReticle != null)
                {
                    gazeReticle.transform.position = origin + direction * reticleDistance;
                    gazeReticle.SetActive(false);
                }
            }
        }

        void UpdateGazedObject(GameObject newObject)
        {
            if (currentGazedObject != newObject)
            {
                if (currentGazedObject != null && currentGazedRenderer != null && originalMaterial != null)
                {
                    currentGazedRenderer.material = originalMaterial;
                }

                currentGazedObject = newObject;
                
                if (currentGazedObject != null)
                {
                    currentGazedRenderer = currentGazedObject.GetComponent<Renderer>();
                    
                    if (currentGazedRenderer != null)
                    {
                        originalMaterial = currentGazedRenderer.material;
                        
                        Material highlightMat = new Material(originalMaterial);
                        highlightMat.color = highlightColor;
                        currentGazedRenderer.material = highlightMat;
                    }
                }
            }
        }

        void CheckPinchInput()
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(handNode);
            
            if (!device.isValid)
            {
                return;
            }

            bool isPinching = false;

            if (device.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
            {
                isPinching = gripValue > pinchThreshold;
            }
            else if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
            {
                isPinching = triggerValue > pinchThreshold;
            }

            if (isPinching && !wasPinching)
            {
                Debug.Log("PINCH DETECTED!");
                OnPinchPerformed();
            }

            wasPinching = isPinching;
        }

        void OnPinchPerformed()
        {
            if (currentGazedObject == null)
            {
                Debug.Log("Pinch: No object gazed");
                return;
            }

            Debug.Log($"Pinch interaction with: {currentGazedObject.name}");

            SubPanelController subPanel = currentGazedObject.GetComponent<SubPanelController>();
            if (subPanel != null)
            {
                subPanel.SelectPanel();

                MeshController mainPanel = FindMainPanel(currentGazedObject.transform);
                if (mainPanel != null)
                {
                    string subpanelPath = subPanel.GetFolderPath();
                    Debug.Log($"Subpanel path: '{subpanelPath}'");
                    
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
