using UnityEngine;
using System.Collections.Generic;  // Add this line!

namespace Scenes.script
{
    [System.Serializable]
    public class FolderData
    {
        public string folderName;
        public string folderPath;
        public List<FolderData> subfolders;  // This needs System.Collections.Generic
        public List<string> contentItems;    // This also needs it
        
        public bool HasContent => contentItems != null && contentItems.Count > 0;
        public bool HasSubfolders => subfolders != null && subfolders.Count > 0;
        public bool IsLeafFolder => !HasSubfolders;

        public FolderData(string name, string path)
        {
            folderName = name;
            folderPath = path;
            subfolders = new List<FolderData>();
            contentItems = new List<string>();
        }
    }
}