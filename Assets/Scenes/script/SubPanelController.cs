using UnityEngine;

namespace Scenes.script
{
    public class SubPanelController : MonoBehaviour
    {
        [Header("SubPanel Settings")]
        public string folderPath; //manually pass in the path 
        public bool isSelected = false; 
        
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
            Debug.Log($"SubPanel {name} initialized with path: '{folderPath}'");
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

            Debug.Log($"Selected panel with path: {folderPath}");
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
            return folderPath;
        }
    }
}