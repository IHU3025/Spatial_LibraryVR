using UnityEngine;
using System.IO;
using Scenes.script;
using System.Linq; 


namespace Scenes.script
{
    public class SubPanelController : MonoBehaviour
    {
        [Header("SubPanel Settings")]
        public string dataPath; 
        public string displayPath; 
        public bool isSelected = false; 
        public bool isLevel2 = false;

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
            Debug.Log($"SubPanel {name} initialized with path: '{dataPath}' and {displayPath}");


        }
        
        private string[] GetFilteredFiles(string folderPath)
        {
            return Directory.GetFiles(folderPath)
                .Where(file => !file.EndsWith(".meta") && 
                            !file.EndsWith(".DS_Store") &&
                            !Path.GetFileName(file).StartsWith("."))
                .ToArray();
        }

        private string[] GetFilteredDirectories(string folderPath)
        {
            return Directory.GetDirectories(folderPath)
                .Where(path => !Path.GetFileName(path).StartsWith("."))
                .ToArray();
        }



        void PropagatePath(){
            GameObject panel = transform.parent.gameObject;
            var meshController = panel.GetComponent<MeshController>();
            if (meshController == null)
            {
                Debug.LogError("Parent does not have a DataPathHolder component.");
                return;
            }
            string basePath = meshController.folderPath;
            bool isLeafNode = meshController.isLeafNode;
            bool isBasePlane = meshController.isBasePlane;

            Debug.Log($"isLeafNode{isLeafNode}");
            if (!isLeafNode){
                string[] subfolders = GetFilteredDirectories(basePath);
                if (index >= 0 && index <= subfolders.Length){
                    dataPath = subfolders[index];
                }
            // if the folder has less child then cards, remaining card's path should be null and display nothing
            } else {
                string[] images = GetFilteredFiles(basePath);

                int reversedIndex = images.Length - 1 - index;
                if (reversedIndex >= 0 && reversedIndex < images.Length) {
                    dataPath = images[reversedIndex];
                    Debug.Log($"has image on card{index}, setting setImage to True, isLeafNode{isLeafNode}");
                    setImage = true;
                }

            }
            if(isBasePlane){
                displayPath = meshController.displayPath; 
                Debug.Log($"card {index} on base plane doing display path logic, setting to card {index} in {displayPath}");
                string[] subfolders = GetFilteredDirectories(displayPath);
                if (index >= 0 && index <= subfolders.Length){
                    displayPath = subfolders[index];
                }
            }
            
            if (isLevel2) {
                displayPath = meshController.displayPath; 
                Debug.Log($"card {index} isLevel2{isLevel2} doing display path logic, setting to image {index} in {displayPath}");
                string[] images = GetFilteredFiles(displayPath);
                if (index >= 0 && index <= images.Length){
                    displayPath = images[index];
                    setImage = true; 
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

        public string GetDisplayPath()
        {
            return displayPath; 
        }
    }
}