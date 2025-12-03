using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CollageImagesManifestDatabase : MonoBehaviour
{
    public static CollageImagesManifestDatabase Instance { get; private set; }

    public CollageImagesManifest Manifest { get; private set; }
    public CollageImagesManifest ImagesManifest => Manifest;   

    public bool IsReady { get; private set; } = false;         

    [SerializeField] private string manifestFileName = "my_manifest_collage_images.json";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(LoadManifest());
    }

    private System.Collections.IEnumerator LoadManifest()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, manifestFileName);
        string url = jsonPath;

    #if !UNITY_ANDROID || UNITY_EDITOR
        if (!url.StartsWith("file://"))
            url = "file://" + url;
    #endif

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[CollageImagesManifestDatabase] Failed to load manifest: {www.error}\nPath: {url}");
                yield break;
            }

            string jsonText = www.downloadHandler.text;
            Manifest = JsonUtility.FromJson<CollageImagesManifest>(jsonText);

            if (Manifest == null || Manifest.folders == null)
            {
                Debug.LogError("[CollageImagesManifestDatabase] Manifest parsed but is null or has no folders.");
            }
            else
            {
                Debug.Log($"[CollageImagesManifestDatabase] Loaded {Manifest.folders.Count} folders from manifest.");
                IsReady = true;   
            }
        }
    }

    public ImageFolderEntry GetFolderByName(string name)
    {
        if (Manifest == null || Manifest.folders == null) return null;
        return Manifest.folders.Find(f => f.name == name);
    }
}
