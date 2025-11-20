using UnityEngine;
using System.IO;
using Scenes.script;


namespace Scenes.script
{
    public class SubPanelController : MonoBehaviour
    {
        [Header("SubPanel Settings")]
        public string dataPath; 
        public string level2CollagesPathReturn;
        public string baseCollagesPathReturn;

        public bool isSelected = false; 
        public int index; 
        
        public bool setImage = false; 
        private Renderer panelRenderer;
        private Color originalColor;
        private static SubPanelController currentlySelectedPanel = null;

        void Start()
        {
            panelRenderer = GetComponent<Renderer>();
            if (panelRenderer != null)
            {
                originalColor = panelRenderer.material.color;
            }

            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
            PropagatePath();
            Debug.Log($"SubPanel {name} initialized with path: '{dataPath}'");


        }

        void PropagatePath()
        {
            GameObject panel = transform.parent.gameObject;
            var meshController = panel.GetComponent<MeshController>();
            if (meshController == null)
            {
                Debug.LogError("Parent does not have a MeshController component.");
                return;
            }

            string basePath = meshController.folderPath;
            bool isLeafNode = meshController.isLeafNode;
            bool isBasePlane = meshController.isBasePlane;
            bool isLevel2Plane = meshController.isLevel2Plane;

            Debug.Log($"Propagating path - isLeafNode: {isLeafNode}, isBasePlane: {isBasePlane}, isLevel2Plane: {isLevel2Plane}");

            // Reset all return paths
            dataPath = null;
            baseCollagesPathReturn = null;
            level2CollagesPathReturn = null;
            setImage = false;

            if (isBasePlane)
            {
                // BASE PLANE: Advance all three paths
                string[] baseSubfolders = Directory.GetDirectories(basePath);
                string[] level2Subfolders = Directory.GetDirectories(meshController.level2CollagesPath);
                string[] baseCollageImages = Directory.GetFiles(meshController.baseCollagesPath, "*.jpg");

                if (index >= 0 && index < baseSubfolders.Length)
                {
                    dataPath = baseSubfolders[index]; // Advance basePath → subfolder
                    Debug.Log($"Base plane - Card {index} advances basePath to: {dataPath}");
                }else if (baseSubfolders.Length > 0)
                {
                    // Fallback: use first subfolder if index is out of range
                    dataPath = baseSubfolders[0];
                    Debug.Log($"Base plane - Card {index} using fallback basePath: {dataPath}");
                }

                if (index >= 0 && index < level2Subfolders.Length)
                {
                    level2CollagesPathReturn = level2Subfolders[index]; // Advance level2CollagesPath → subfolder
                    Debug.Log($"Base plane - Card {index} advances level2CollagesPath to: {level2CollagesPathReturn}");
                }

                if (index >= 0 && index < baseCollageImages.Length)
                {
                    baseCollagesPathReturn = baseCollageImages[index]; // Advance baseCollagesPath → image
                    setImage = true;
                    Debug.Log($"Base plane - Card {index} advances baseCollagesPath to image: {baseCollagesPathReturn}");
                }
            }
            else if (isLevel2Plane)
            {
                // LEVEL 2 PLANE: Advance two paths
                string[] baseSubfolders = Directory.GetDirectories(basePath);
                string[] level2CollageImages = Directory.GetFiles(meshController.level2CollagesPath, "*.jpg");

                if (index >= 0 && index < baseSubfolders.Length)
                {
                    dataPath = baseSubfolders[index]; // Advance basePath → subfolder
                    Debug.Log($"Level 2 plane - Card {index} advances basePath to: {dataPath}");
                }

                if (index >= 0 && index < level2CollageImages.Length)
                {
                    level2CollagesPathReturn = level2CollageImages[index]; // Advance level2CollagesPath → image
                    setImage = true;
                    Debug.Log($"Level 2 plane - Card {index} advances level2CollagesPath to image: {level2CollagesPathReturn}");
                }
            }
            else if (isLeafNode)
            {
                // LEAF NODE: Advance one path
                string[] images = Directory.GetFiles(basePath, "*.jpg");
                if (index >= 0 && index < images.Length)
                {
                    dataPath = images[index]; // Advance basePath → image
                    setImage = true;
                    Debug.Log($"Leaf node - Card {index} advances basePath to image: {dataPath}");
                }
            }
            else
            {
                // REGULAR FOLDER: Just advance basePath
                string[] subfolders = Directory.GetDirectories(basePath);
                if (index >= 0 && index < subfolders.Length)
                {
                    dataPath = subfolders[index]; // Advance basePath → subfolder
                    Debug.Log($"Regular folder - Card {index} advances basePath to: {dataPath}");
                }
            }
        }

        //select and deselect panel only deal with color change, meshController call the getfolderPath to pass in the path
        public void SelectPanel()
        {
            if (currentlySelectedPanel != null && currentlySelectedPanel != this)
            {
                currentlySelectedPanel.DeselectPanel();
            }

            isSelected = true;
            currentlySelectedPanel = this;
            
            if (panelRenderer != null)
            {
                panelRenderer.material.color = Color.red;
            }

            Debug.Log($"Selected panel with path: {dataPath}");
        }

        public void DeselectPanel()
        {
            isSelected = false;
            
            if (panelRenderer != null)
            {
                panelRenderer.material.color = originalColor;
            }

            if (currentlySelectedPanel == this)
            {
                currentlySelectedPanel = null;
            }
        }

        public string GetFolderPath()
        {
            return dataPath;
        }

         public string GetLevel2Path()
        {
            return level2CollagesPathReturn;
        }

        public string BaseCollagePath()
        {
            return baseCollagesPathReturn;
        }
    }
}