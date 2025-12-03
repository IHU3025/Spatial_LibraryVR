using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

[Serializable]
public class SubfolderInfo
{
    public string name;         // e.g. "a-y-jackson"
    public string displayName;  // e.g. "A Y Jackson"
}

[Serializable]
public class FolderWithSubfolders
{
    public string name;                  // e.g. "art_nouveau"
    public string displayName;           // e.g. "Art Nouveau"
    public List<SubfolderInfo> subfolders; // artists
    public List<string> images;          // artist thumbnails, e.g. "a-y-jackson.jpg"
}

[Serializable]
public class FolderWithSubfoldersManifest
{
    public List<FolderWithSubfolders> folders;
}

[Serializable]
public class ImageFolderEntry
{
    public string name;            // e.g. "art_nouveau"
    public string displayName;     // e.g. "Art Nouveau"
    public List<string> images;    // e.g. "a-y-jackson\\painting.jpg"
}

[Serializable]
public class CollageImagesManifest
{
    public List<ImageFolderEntry> folders;
}
