using UnityEngine;
using System.IO;
using Scenes.script;


namespace Scenes.script
{
    public class SubPanelController : MonoBehaviour
    {
        [Header("SubPanel Settings")]
        public string dataPath; 
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
            Debug.Log($"isLeafNode{isLeafNode}");
            if (!isLeafNode){
                string[] subfolders = Directory.GetDirectories(basePath);
                if (index >= 0 && index <= subfolders.Length){
                    dataPath = subfolders[index];
                }
            // if the folder has less child then cards, remaining card's path should be null and display nothing
            } else {
                string[] images = Directory.GetFiles(basePath);
                if (index >= 0 && index <= images.Length){
                    dataPath = images[index];
                    Debug.Log($"has image on card{index}, setting setImage to True, isLeafNode{isLeafNode}");
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
    }
}