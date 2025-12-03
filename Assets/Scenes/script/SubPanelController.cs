using UnityEngine;
using System.IO;
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

        private void Start()
        {
            panelRenderer = GetComponent<Renderer>();
            if (panelRenderer != null)
                originalColor = panelRenderer.material.color;

            if (GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();

            StartCoroutine(InitAfterManifestsReady());
        }

        private System.Collections.IEnumerator InitAfterManifestsReady()
        {
            while (CollageFolderManifestDatabase.Instance == null ||
                   CollageFolderManifestDatabase.Instance.Manifest == null ||
                   CollageImagesManifestDatabase.Instance == null ||
                   CollageImagesManifestDatabase.Instance.Manifest == null)
            {
                yield return null;
            }

            PropagatePath();

            Debug.Log($"SubPanel {name} initialized with path: '{dataPath}' and {displayPath}");
        }

        private void PropagatePath()
        {
            var mesh = transform.parent.GetComponent<MeshController>();
            if (mesh == null)
                return;

            string basePath = mesh.folderPath;          
            bool isBasePlane = mesh.isBasePlane;

            string streamingRoot = Application.streamingAssetsPath;
            string collagesRoot = Path.Combine(streamingRoot, "collages_fixed");

            string baseFolderName = System.IO.Path.GetFileName(
                basePath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
            );
            string parentDir = System.IO.Path.GetDirectoryName(basePath);
            string parentFolderName = string.IsNullOrEmpty(parentDir)
                ? ""
                : System.IO.Path.GetFileName(
                    parentDir.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
                );

            Debug.Log($"[SubPanelController] PropagatePath for {name}: basePath='{basePath}', isBasePlane={isBasePlane}, index={index}");

            // ROOT BASE PLANE → movement list (Level 0 → Level 1)
            if (isBasePlane)
            {
                var movements = CollageDataRouter.GetMovementFolders(); 

                if (index < movements.Count)
                {
                    string movementName = movements[index];

                    //root/movement
                    dataPath = System.IO.Path.Combine(basePath, movementName); 
                    //collages_fixed/<movement>.jpg
                    displayPath = System.IO.Path.Combine(collagesRoot, movementName + ".jpg");

                    setImage = true;

                    Debug.Log($"[SubPanelController] {name} AFTER routing (BASE): dataPath='{dataPath}', displayPath='{displayPath}', setImage={setImage}");
                }
                return;
            }

            // MOVEMENT PLANE → artist list (Level 1 → Level 2)
        
            var movementArtists = CollageDataRouter.GetArtistFolders(baseFolderName); 

            if (movementArtists != null && movementArtists.Count > 0)
            {
                // This plane is a MOVEMENT plane.
                if (index < movementArtists.Count)
                {
                    string artist = movementArtists[index]; 

                    // ...\collage_images\renaissance\andrea-mantegna
                    dataPath = System.IO.Path.Combine(basePath, artist);

                    //collages_fixed/<movement>/<artist>.jpg
                    displayPath = System.IO.Path.Combine(collagesRoot, baseFolderName, artist + ".jpg");

                    setImage = true;

                    Debug.Log($"[SubPanelController] {name} AFTER routing (MOVEMENT): dataPath='{dataPath}', displayPath='{displayPath}', setImage={setImage}");
                }
                return;
            }

            // ARTIST PLANE → paintings (Level 2 → Level 3)
    
            string leafMovementName = parentFolderName;   
            string artistFolderName = baseFolderName;     

            var paintings = CollageDataRouter.GetPaintings(leafMovementName, artistFolderName);

            if (paintings != null && paintings.Count > 0 && index < paintings.Count)
            {
                string file = paintings[index]; 

                // ...\collage_images\renaissance\andrea-mantegna\some_painting.jpg
                dataPath = Path.Combine(basePath, file);
                displayPath = dataPath;         

                setImage = true;

                Debug.Log($"[SubPanelController] {name} AFTER routing (ARTIST/LEAF): dataPath='{dataPath}', displayPath='{displayPath}', setImage={setImage}");
            }
            else
            {
                Debug.LogWarning(
                    $"[SubPanelController] {name} could not find paintings for movement='{leafMovementName}', artist='{artistFolderName}', index={index}."
                );
                setImage = false;
                dataPath = "";
                displayPath = "";
            }
        }


        public void SelectPanel()
        {
            if (currentlySelectedPanel != null && currentlySelectedPanel != this)
            {
                currentlySelectedPanel.DeselectPanel();
            }

            isSelected = true;
            currentlySelectedPanel = this;

            if (panelRenderer != null)
                panelRenderer.material.color = Color.red;

            Debug.Log($"Selected panel with path: {dataPath}");
        }

        public void DeselectPanel()
        {
            isSelected = false;

            if (panelRenderer != null)
                panelRenderer.material.color = originalColor;

            if (currentlySelectedPanel == this)
                currentlySelectedPanel = null;
        }

        public string GetFolderPath() => dataPath;
        public string GetDisplayPath() => displayPath;
    }
}
