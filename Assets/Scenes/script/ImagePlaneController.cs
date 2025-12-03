using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Scenes.script;
using System.IO;
using UnityEngine.Networking;

public class ImagePlaneController : MonoBehaviour
{
    public GameObject quadPrefab;
    public Texture imageTexture;
    public float worldOffset = -0.167f;
    private Camera targetCamera;

    // Hard coded the rotation and scaling 
    public float rotaionZ = -10.0f; 
    public float scaleMultiplier = 6.5f; 
    GameObject spawnedQuad;

    [Header("Label Settings")]
    public GameObject textLabelPrefab; 

    void Start()
    {
        if (targetCamera == null)
        {
            GameObject xrOriginGO = GameObject.Find("XR Origin");
            if (xrOriginGO != null)
            {
                targetCamera = xrOriginGO.GetComponentInChildren<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        if (targetCamera == null)
        {
            Debug.LogError("[ImagePlaneController] No camera found for spawning quad.");
            return;
        }

        // Just start async load – no immediate spawn here
        LoadImageTexture();
    }

    void LoadImageTexture()
    {
        if (transform.parent == null)
        {
            Debug.LogError("[ImagePlaneController] No parent transform found.");
            return;
        }

        GameObject panel = transform.parent.gameObject;
        var meshController = panel.GetComponent<MeshController>();
        if (meshController == null)
        {
            Debug.LogError("[ImagePlaneController] Parent does not have a MeshController component.");
            return;
        }

        string display = meshController.displayPath;
        string folder  = meshController.folderPath;

        Debug.Log($"[ImagePlaneController] displayPath='{display}', folderPath='{folder}'");

        string path = null;

        // Leaf planes: folderPath is usually the full file
        if (!string.IsNullOrEmpty(folder) && File.Exists(folder))
        {
            path = folder;
        }
        else if (!string.IsNullOrEmpty(display) && File.Exists(display))
        {
            path = display;
        }

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError(
                "[ImagePlaneController] No valid image file found. " +
                $"displayPath='{display}', folderPath='{folder}'"
            );
            return;
        }

        string url = path;
        if (!url.StartsWith("file://"))
            url = "file://" + url;

        Debug.Log($"[ImagePlaneController] Loading texture via URL: {url}");
        StartCoroutine(LoadImageTextureFromPath(url));
    }

    private System.Collections.IEnumerator LoadImageTextureFromPath(string url)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"[ImagePlaneController] Failed to load texture from '{url}': {req.error}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
            {
                Debug.LogError($"[ImagePlaneController] Downloaded texture was null from '{url}'");
                yield break;
            }

            imageTexture = tex;
            Debug.Log($"[ImagePlaneController] Successfully loaded texture from '{url}'");

            // ✅ Now that the texture is loaded, spawn the quad + label
            if (quadPrefab != null)
            {
                Debug.Log("[ImagePlaneController] Spawning quad with loaded texture.");
                SpawnQuadInFrontOfCard(quadPrefab, imageTexture, 0.01f);
                SpawnFolderLabel();
            }
            else
            {
                Debug.LogError("[ImagePlaneController] quadPrefab is not assigned.");
            }
        }
    }

    public GameObject SpawnQuadInFrontOfCard(GameObject quadPrefab, Texture tex, float localZOffset = 0.01f)
    {
        if (quadPrefab == null) return null;
        
        var r = GetComponent<Renderer>();
        if (r == null) return null;

        Bounds b = r.bounds;
        Vector3 c = b.center;
        Vector3 e = b.extents;
        
        var corners = new List<Vector3>(8);
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    corners.Add(c + new Vector3(sx * e.x, sy * e.y, sz * e.z));

        Vector3 outwardDirection = transform.forward;
        
        var cornerScores = new List<(Vector3 pos, float score)>(8);
        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 toCorner = (corners[i] - c).normalized;
            float score = Vector3.Dot(toCorner, outwardDirection);
            cornerScores.Add((corners[i], score));
        }
        
        cornerScores.Sort((a, b) => b.score.CompareTo(a.score));
        
        var front4 = new List<Vector3>(4);
        for (int i = 0; i < 4; i++) front4.Add(cornerScores[i].pos);

        Vector3 frontCenterWorld = Vector3.zero;
        foreach (var v in front4) frontCenterWorld += v;
        frontCenterWorld /= front4.Count;

        Vector3 localRight = Vector3.right;
        Vector3 localUp = Vector3.up;

        Vector3 frontCenterLocal = transform.InverseTransformPoint(frontCenterWorld);

        float minR = float.PositiveInfinity, maxR = float.NegativeInfinity;
        float minU = float.PositiveInfinity, maxU = float.NegativeInfinity;

        foreach (var worldCorner in front4)
        {
            Vector3 localCorner = transform.InverseTransformPoint(worldCorner);
            Vector3 rel = localCorner - frontCenterLocal;
            
            float rProj = Vector3.Dot(rel, localRight);
            float uProj = Vector3.Dot(rel, localUp);
            
            if (rProj < minR) minR = rProj;
            if (rProj > maxR) maxR = rProj;
            if (uProj < minU) minU = uProj;
            if (uProj > maxU) maxU = uProj;
        }

        float localWidth = Mathf.Max(0.0001f, maxR - minR);
        float localHeight = Mathf.Max(0.0001f, maxU - minU);

        if (localWidth < 0.01f && localHeight > 0.1f)
        {
            Debug.LogWarning($"Degenerate width detected on '{name}' — falling back to local bounds");
            localWidth = transform.localScale.x * 0.1f; 
        }

        Vector3 quadLocalPos = frontCenterLocal + new Vector3(0, 0, localZOffset);

        if (spawnedQuad != null) Destroy(spawnedQuad);
        
        spawnedQuad = Instantiate(quadPrefab);
        spawnedQuad.name = $"{name}_FrontQuad";
        spawnedQuad.transform.SetParent(transform, false);
        spawnedQuad.transform.localPosition = quadLocalPos;

        float uniformLocalScale = Mathf.Max(localWidth, localHeight) * scaleMultiplier;
        spawnedQuad.transform.localScale = new Vector3(uniformLocalScale, uniformLocalScale, uniformLocalScale);
        spawnedQuad.transform.localRotation = Quaternion.Euler(0f, 0f, rotaionZ);

        Transform child = spawnedQuad.transform.GetChild(0);
        if (child != null)
        {
            float childYOffset = -0.014f; 
            child.localPosition = new Vector3(0f, childYOffset, worldOffset);
        }

        Debug.Log($"Quad local position: {quadLocalPos}, scale: {uniformLocalScale}");

        Renderer quadR = spawnedQuad.GetComponentInChildren<Renderer>();
        if (quadR != null && tex != null)
        {
            Material mat = quadR.sharedMaterial != null ? 
                new Material(quadR.sharedMaterial) : 
                new Material(Shader.Find("Standard"));
            
            // If you still see horizontal flip, change X to -1f:
            // mat.mainTextureScale = new Vector2(-1f, -1f);
            mat.mainTextureScale = new Vector2(1f, -1f);

            mat.mainTexture = tex;
            quadR.material = mat;
        }

        return spawnedQuad;
    }

    private void SpawnFolderLabel()
    {
        if (textLabelPrefab == null)
        {
            Debug.LogWarning("[ImagePlaneController] textLabelPrefab is not assigned. Cannot create folder label.");
            return;
        }

        string sourcePath = "";
        if (transform.parent != null)
        {
            var parentMeshController = transform.parent.GetComponent<MeshController>();
            if (parentMeshController != null && !string.IsNullOrEmpty(parentMeshController.folderPath))
            {
                sourcePath = parentMeshController.folderPath;
            }
        }

        string folderName = "Unknown";
        if (!string.IsNullOrEmpty(sourcePath))
        {
            folderName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(folderName)) folderName = sourcePath;
        }

        Transform existing = transform.Find($"{gameObject.name}_FolderLabel");
        if (existing != null) Destroy(existing.gameObject);

        GameObject spawnedLabel = Instantiate(textLabelPrefab);
        spawnedLabel.name = $"{gameObject.name}_FolderLabel";
        spawnedLabel.transform.SetParent(transform, false);

        spawnedLabel.transform.localPosition = new Vector3(-0.0631f, -0.1051f, 0.0382f);
        spawnedLabel.transform.localRotation = Quaternion.Euler(-90f, 0f, 180f);

        Renderer planeR = GetComponent<Renderer>();
        Vector3 baseScale = Vector3.one;
        if (planeR != null)
        {
            Vector3 bounds = planeR.bounds.size;
            float uniform = Mathf.Max(bounds.x, bounds.y);
            baseScale = new Vector3(uniform, uniform, uniform);
        }
        float localLabelScale = 0.0003f;            
        spawnedLabel.transform.localScale = baseScale * localLabelScale;

        var tmp = spawnedLabel.GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp == null)
        {
            Debug.LogError("[ImagePlaneController] TextMeshPro component not found in textLabelPrefab!");
            return;
        }

        tmp.text = folderName;
        tmp.color = Color.white;
        tmp.fontSize = 10; 
        tmp.alignment = TMPro.TextAlignmentOptions.Center;

        tmp.ForceMeshUpdate();

        if (tmp.fontMaterial != null)
        {
            Material instMat = new Material(tmp.fontMaterial);
            instMat.SetInt("_ZWrite", 0);
            instMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;
            tmp.fontMaterial = instMat;
        }
    }
}
