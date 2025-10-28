using UnityEngine;
using System.Collections.Generic;
using Scenes.script;  // Add this namespace reference

public class FolderDataManager : MonoBehaviour
{
    [Header("Root Plane")]
    public MeshController rootPlaneController;
    
    void Start()
    {
        // Load folder structure and assign to root plane
        FolderData rootFolder = CreateExampleData();
        
        if (rootPlaneController != null)
        {
            rootPlaneController.SetFolderData(rootFolder);
            Debug.Log("Folder data assigned to root plane");
        }
        else
        {
            Debug.LogError("Root Plane Controller not assigned!");
        }
    }
    
    FolderData CreateExampleData()
    {
        // Level 1: Root
        FolderData animals = new FolderData("Animals", "/animals");
        
        // Level 2: Subfolders
        FolderData dogs = new FolderData("Dogs", "/animals/dogs");
        FolderData cats = new FolderData("Cats", "/animals/cats");
        
        // Level 3: Sub-subfolders
        FolderData smallDogs = new FolderData("Small Breeds", "/animals/dogs/small");
        FolderData largeDogs = new FolderData("Large Breeds", "/animals/dogs/large");
        
        // Level 4: Content folders (LAST LEVEL)
        FolderData chihuahua = new FolderData("Chihuahua", "/animals/dogs/small/chihuahua");
        chihuahua.contentItems = new List<string> { 
            "chihuahua1.jpg", 
            "chihuahua2.jpg", 
            "chihuahua3.jpg" 
        };
        
        FolderData poodle = new FolderData("Poodle", "/animals/dogs/small/poodle");
        poodle.contentItems = new List<string> { 
            "poodle1.jpg", 
            "poodle2.jpg" 
        };
        
        FolderData golden = new FolderData("Golden Retriever", "/animals/dogs/large/golden");
        golden.contentItems = new List<string> { 
            "golden1.jpg", 
            "golden2.jpg" 
        };
        
        // Build hierarchy
        smallDogs.subfolders.Add(chihuahua);
        smallDogs.subfolders.Add(poodle);
        
        largeDogs.subfolders.Add(golden);
        
        dogs.subfolders.Add(smallDogs);
        dogs.subfolders.Add(largeDogs);
        
        animals.subfolders.Add(dogs);
        animals.subfolders.Add(cats);
        
        Debug.Log("Created example folder structure:");
        Debug.Log($"- Root: {animals.folderName}");
        Debug.Log($"- Subfolders: {animals.subfolders.Count}");
        Debug.Log($"- Total hierarchy depth: 4 levels");
        
        return animals;
    }
}