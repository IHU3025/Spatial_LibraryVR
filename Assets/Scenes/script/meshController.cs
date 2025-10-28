using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Scenes.script
{
    public class MeshController : MonoBehaviour
    {
        [Header("Plane Settings")] 
        public GameObject planePrefab;
        public float offsetDistance = 3f;
        public Vector3 scaleReduction = new Vector3(0.8f, 1f, 0.8f);
        
        [Header("UI Settings")]
        public GameObject folderUIPrefab;
        public Vector3 uiOffset = new Vector3(0, 0.2f, 0);

        private Vector3 spawnDirection = Vector3.down;
        private GameObject currentChildPlane;
        private bool hasChild = false;
        
        // ADD THESE FOLDER DATA FIELDS:
        private FolderData currentFolderData;
        private GameObject folderUI;

        void Start()
        {
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }

        // ADD THIS METHOD: Set folder data for this plane
        public void SetFolderData(FolderData folderData)
        {
            currentFolderData = folderData;
            
            // Create UI to show folder name
            if (folderUIPrefab != null && currentFolderData != null)
            {
                CreateFolderUI();
            }
        }

        void CreateFolderUI()
        {
            if (folderUIPrefab == null || currentFolderData == null) return;
            
            folderUI = Instantiate(folderUIPrefab, transform);
            folderUI.transform.localPosition = uiOffset;
            folderUI.transform.rotation = Quaternion.identity;
            
            // Update text with folder name
            TextMeshPro textMesh = folderUI.GetComponentInChildren<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = currentFolderData.folderName;
            }
        }

        // PUBLIC method that can be called by raycast
        public void TriggerInteraction()
        {
            Debug.Log(" PLANE INTERACTION TRIGGERED");

            // MODIFIED: Check if this is a content folder or subfolder
            if (currentFolderData != null && currentFolderData.IsLeafFolder)
            {
                // LAST LEVEL: Show content
                DisplayContent();
            }
            else
            {
                // SUBFOLDER LEVEL: Spawn/remove child
                if (!hasChild)
                {
                    SpawnChildPlane();
                }
                else
                {
                    RemoveChildPlane();
                }
            }
        }

        // ADD THIS METHOD: Display content for leaf folders
        void DisplayContent()
        {
            Debug.Log($"Showing content for: {currentFolderData.folderName}");
            
            if (currentFolderData.HasContent)
            {
                foreach (string contentItem in currentFolderData.contentItems)
                {
                    Debug.Log($" - {contentItem}");
                    // Here you would load and display actual photos/files
                }
                
                // Visual feedback for content mode
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.cyan;
                }
            }
            else
            {
                Debug.Log("No content in this folder");
            }
        }

        void SpawnChildPlane()
        {
            if (planePrefab == null)
            {
                currentChildPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            }
            else
            {
                currentChildPlane = Instantiate(planePrefab);
            }

            currentChildPlane.name = $"{gameObject.name}_Child";
            currentChildPlane.transform.SetParent(transform);

            Vector3 spawnPosition = CalculateChainPosition();
            currentChildPlane.transform.position = spawnPosition;
            currentChildPlane.transform.rotation = transform.rotation;

            Vector3 parentWorldScale = transform.lossyScale;
            Vector3 desiredWorldScale = new Vector3(
                parentWorldScale.x * scaleReduction.x,
                parentWorldScale.y * scaleReduction.y,
                parentWorldScale.z * scaleReduction.z
            );

            Vector3 requiredLocalScale = new Vector3(
                desiredWorldScale.x / parentWorldScale.x,
                desiredWorldScale.y / parentWorldScale.y,
                desiredWorldScale.z / parentWorldScale.z
            );

            currentChildPlane.transform.localScale = requiredLocalScale;

            // MODIFIED: Setup child with folder data
            SetupChildPlane();

            Renderer childRenderer = currentChildPlane.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.material.color = GetNextColor(GetComponent<Renderer>().material.color);
            }

            hasChild = true;
            Debug.Log("Spawned child plane");
        }

        // ADD THIS METHOD: Setup child plane with folder data
        void SetupChildPlane()
        {
            // Add controller to child
            MeshController childController = currentChildPlane.AddComponent<MeshController>();
            childController.planePrefab = planePrefab;
            childController.offsetDistance = offsetDistance;
            childController.scaleReduction = scaleReduction;
            childController.folderUIPrefab = folderUIPrefab;
            childController.uiOffset = uiOffset;

            // Pass folder data to child (first subfolder)
            if (currentFolderData != null && currentFolderData.HasSubfolders)
            {
                FolderData firstSubfolder = currentFolderData.subfolders[0];
                childController.SetFolderData(firstSubfolder);
            }
        }

        Vector3 CalculateChainPosition()
        {
            Vector3 direction = transform.TransformDirection(spawnDirection);

            int planeDepth = CountPlanesInChain();

            Debug.Log($"Calculating spawn position:");
            Debug.Log($"- Direction: {direction}");
            Debug.Log($"- Plane depth in chain: {planeDepth}");
            Debug.Log($"- Offset distance: {offsetDistance}");
            Debug.Log($"- Total offset: {offsetDistance * planeDepth}");
            Debug.Log($"- From position: {transform.position}");

            float heightAdjustment = CalculateHeightAdjustment();
            float actualOffset = offsetDistance * (1f / planeDepth);

            Vector3 spawnPos = transform.position + direction * actualOffset;
            spawnPos.y += heightAdjustment;
    
            Debug.Log($"Height adjustment: {heightAdjustment}");
            Debug.Log($"Adjusted position: {spawnPos}");
    
            return spawnPos;
        }
        
        float CalculateHeightAdjustment()
        {
            Renderer parentRenderer = GetComponent<Renderer>();
            Renderer childRenderer = currentChildPlane.GetComponent<Renderer>();
    
            if (parentRenderer != null && childRenderer != null)
            {
                float parentHeight = parentRenderer.bounds.size.y;
                float childHeight = childRenderer.bounds.size.y;
        
                return (childHeight - parentHeight) / 3f;
            }
    
            return 0f;
        }

        int CountPlanesInChain()
        {
            int count = 0;
            Transform current = transform;
    
            while (current != null && current.GetComponent<MeshController>() != null)
            {
                count++;
                Debug.Log($"  Plane in chain: {current.name}");
                current = current.parent;
            }
    
            return count;
        }

        void RemoveChildPlane()
        {
            if (currentChildPlane != null)
            {
                Destroy(currentChildPlane);
                hasChild = false;
                Debug.Log("Removed child plane");
            }
        }

        Color GetNextColor(Color currentColor)
        {
            float h, s, v;
            Color.RGBToHSV(currentColor, out h, out s, out v);
            h = (h + 0.3f) % 1f;
            s = Mathf.Clamp(s - 0.1f, 0.3f, 1f);
            v = Mathf.Clamp(v - 0.1f, 0.7f, 1f);
            return Color.HSVToRGB(h, s, v);
        }
    }
}