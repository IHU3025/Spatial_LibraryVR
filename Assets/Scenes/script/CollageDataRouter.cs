using System.Collections.Generic;
using UnityEngine;

public static class CollageDataRouter
{
    //----------------------------------------------------------------------
    // 1. Get movement list  (Base plane → list of movements)
    //----------------------------------------------------------------------
    public static List<string> GetMovementFolders()
    {
        if (CollageFolderManifestDatabase.Instance == null ||
            CollageFolderManifestDatabase.Instance.Manifest == null)
            return new List<string>();

        var result = new List<string>();
        foreach (var folder in CollageFolderManifestDatabase.Instance.Manifest.folders)
            result.Add(folder.name);       // e.g. "baroque", "art_nouveau"

        return result;
    }

    //----------------------------------------------------------------------
    // 2. Get artist list for a movement  (Level 1 planes)
    //----------------------------------------------------------------------
    public static List<string> GetArtistFolders(string movement)
    {
        if (CollageFolderManifestDatabase.Instance == null ||
            CollageFolderManifestDatabase.Instance.Manifest == null)
            return new List<string>();

        var entry = CollageFolderManifestDatabase.Instance.GetFolderByName(movement);
        if (entry == null || entry.subfolders == null)
            return new List<string>();

        var list = new List<string>();
        foreach (var sf in entry.subfolders)
            list.Add(sf.name);              // e.g. "a-y-jackson"

        return list;
    }

    //----------------------------------------------------------------------
    // 3. Get all paintings inside movement → artist (Level 2 planes)
    //----------------------------------------------------------------------
    public static List<string> GetPaintings(string movement, string artist)
    {
        if (CollageImagesManifestDatabase.Instance == null ||
            CollageImagesManifestDatabase.Instance.ImagesManifest == null)
            return new List<string>();

        var entry = CollageImagesManifestDatabase.Instance.GetFolderByName(movement);
        if (entry == null || entry.images == null)
            return new List<string>();

        var results = new List<string>();

        foreach (string img in entry.images)
        {
            // JSON stores: "a-y-jackson\\painting1.jpg"
            if (img.StartsWith(artist))
            {
                string onlyFile = img.Substring(img.IndexOf("\\") + 1);
                results.Add(onlyFile);     // → "painting1.jpg"
            }
        }

        return results;
    }
}
