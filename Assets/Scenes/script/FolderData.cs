using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FolderData
{
    public string folderName;
    public string folderPath;
    public List<FolderData> subfolders;
    public List<string> files; // Optional: if you want to show files too
    
    public FolderData(string name, string path)
    {
        folderName = name;
        folderPath = path;
        subfolders = new List<FolderData>();
        files = new List<string>();
    }
}