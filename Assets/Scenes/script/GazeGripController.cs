using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace Scenes.script
{
    public class GazeGripController : MonoBehaviour
    {
        [Header("Gaze Settings")]
        public Camera gazeCamera;
        public float rayDistance = 30f;
        public LayerMask raycastMask = ~0;
        
        [Header("Grip Settings")]
        public XRNode controllerNode = XRNode.RightHand;
        public float gripThreshold = 0.7f;
        public bool useTrigger = false;
        
        [Header("Visual Settings")]
        public bool showGazeRay = true;
        public Color gazeRayColor = Color.cyan;
        public float rayWidth = 0.003f;
        public bool highlightGazedObject = true;
        public Color highlightColor = Color.cyan;
        
        [Header("Debug")]
        public bool verboseLogging = false;
        
        private InputDevice device;
        private LineRenderer gazeRay;
        private GameObject currentGazedObject;
        private Renderer currentGazedRenderer;
        private Material originalMaterial;
        
        private bool wasGripping = false;

        void Start()
        {
            if (gazeCamera == null)
                gazeCamera = Camera.main;
            
            if (showGazeRay)
            {
                CreateGazeRay();
            }
            
            Debug.Log("GazeGripController initialized");
        }

        void CreateGazeRay()
        {
            GameObject rayObj = new GameObject("GazeRayGrip");
            rayObj.transform.SetParent(transform);
            
            gazeRay = rayObj.AddComponent<LineRenderer>();
            gazeRay.startWidth = rayWidth;
            gazeRay.endWidth = rayWidth;
            
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            
            gazeRay.material = new Material(shader);
            gazeRay.startColor = gazeRayColor;
            gazeRay.endColor = new Color(gazeRayColor.r, gazeRayColor.g, gazeRayColor.b, 0.1f);
            
            Debug.Log("Gaze ray (grip) created");
        }

        void Update()
        {
            UpdateDevice();
            UpdateGazeRaycast();
            CheckGripInput();
        }

        void UpdateDevice()
        {
            if (!device.isValid)
            {
                device = InputDevices.GetDeviceAtXRNode(controllerNode);
            }
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
                
                if (currentGazedRenderer != null)
                {
                    originalMaterial = currentGazedRenderer.material;
                    
                    Material highlightMat = new Material(originalMaterial);
                    highlightMat.color = highlightColor;
                    currentGazedRenderer.material = highlightMat;
                }
            }
            
            if (verboseLogging)
            {
                Debug.Log($"Gazing at: {obj.name}");
            }
        }

        void ClearGazedObject()
        {
            if (currentGazedRenderer != null && originalMaterial != null)
            {
                currentGazedRenderer.material = originalMaterial;
            }
            
            currentGazedObject = null;
            currentGazedRenderer = null;
            originalMaterial = null;
        }

        void CheckGripInput()
        {
            if (!device.isValid) return;
            
            float inputValue = 0f;
            
            if (useTrigger)
            {
                device.TryGetFeatureValue(CommonUsages.trigger, out inputValue);
            }
            else
            {
                device.TryGetFeatureValue(CommonUsages.grip, out inputValue);
            }
            
            bool isGripping = inputValue > gripThreshold;
            
            if (isGripping && !wasGripping)
            {
                Debug.Log("Gaze + Grip detected!");
                PerformGazeSelection();
            }
            
            wasGripping = isGripping;
        }

        void PerformGazeSelection()
        {
            if (currentGazedObject == null)
            {
                Debug.Log("No object being gazed at");
                return;
            }
            
            Debug.Log($"Selected with gaze + grip: {currentGazedObject.name}");
            
            SubPanelController subPanel = currentGazedObject.GetComponent<SubPanelController>();
            if (subPanel != null)
            {
                subPanel.SelectPanel();
                
                MeshController mainPanel = FindMainPanel(currentGazedObject.transform);
                if (mainPanel != null)
                {
                    string subpanelPath = subPanel.GetFolderPath();
                    string subpanelDisplayPath = subPanel.GetDisplayPath();
                    
                    if (!mainPanel.hasChild)
                    {
                        mainPanel.folderPath = subpanelPath;
                        mainPanel.displayPath = subpanelDisplayPath;
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
