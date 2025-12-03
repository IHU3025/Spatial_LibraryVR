using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CollageFolderManifestDatabase : MonoBehaviour
{
    public static CollageFolderManifestDatabase Instance { get; private set; }
    public FolderWithSubfoldersManifest Manifest { get; private set; }

    public bool IsReady { get; private set; } = false;   

    [SerializeField] private string manifestFileName = "my_manifest_with_folders.json";

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
                Debug.LogError($"[CollageFolderManifestDatabase] Failed to load manifest: {www.error}\nPath: {url}");
                yield break;
            }

            string jsonText = www.downloadHandler.text;
            Manifest = JsonUtility.FromJson<FolderWithSubfoldersManifest>(jsonText);

            if (Manifest == null || Manifest.folders == null)
            {
                Debug.LogError("[CollageFolderManifestDatabase] Manifest parsed but is null or has no folders.");
            }
            else
            {
                Debug.Log($"[CollageFolderManifestDatabase] Loaded {Manifest.folders.Count} folders from manifest.");
                IsReady = true;   
            }
        }
    }

    public FolderWithSubfolders GetFolderByName(string name)
    {
        if (Manifest == null || Manifest.folders == null) return null;
        return Manifest.folders.Find(f => f.name == name);
    }
}
