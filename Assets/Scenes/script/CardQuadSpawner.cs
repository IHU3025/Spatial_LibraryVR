using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


public class CardQuadSpawner : MonoBehaviour
{
    public GameObject quadPrefab;
    public Texture imageTexture;
    public float worldOffset = -0.001f;
    private Camera targetCamera;

    //Hard coded the rotation and scaling 
    public float rotaionZ = -10.0f; 
    public float scaleMultiplier = 4.5f; 
    GameObject spawnedQuad;

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
            Debug.LogError("[CardQuadSpawner] No camera found for spawning quad.");
            return;
        }


        SpawnQuadInFrontOfCard(quadPrefab, imageTexture, targetCamera, worldOffset);
    }

    
    public GameObject SpawnQuadInFrontOfCard(GameObject quadPrefab, Texture tex, Camera cam, float offsetWorld)
    {
        if (quadPrefab == null || cam == null) return null;
        //get 8 corner of boundng box
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

        var cornerScores = new List<(Vector3 pos, float score)>(8);
        for (int i = 0; i < corners.Count; i++)
        {
            float score = Vector3.Dot(corners[i] - cam.transform.position, -cam.transform.forward);
            cornerScores.Add((corners[i], score));
        }
        cornerScores.Sort((a, b) => b.score.CompareTo(a.score));
        var front4 = new List<Vector3>(4);
        for (int i = 0; i < 4; i++) front4.Add(cornerScores[i].pos);

        foreach (var v in front4)
            Debug.DrawLine(v, v + Vector3.up * 0.05f, Color.green, 20f);

        



        Vector3 frontCenterWorld = Vector3.zero;
        foreach (var v in front4) frontCenterWorld += v;
        frontCenterWorld /= front4.Count;


        Vector3 worldRight = transform.right;
        Vector3 worldUp = transform.up;

        float minR = float.PositiveInfinity, maxR = float.NegativeInfinity;
        float minU = float.PositiveInfinity, maxU = float.NegativeInfinity;
        List<float> rProjs = new List<float>(4), uProjs = new List<float>(4);

        foreach (var v in front4)
        {
            Vector3 rel = v - frontCenterWorld;               
            float rProj = Vector3.Dot(rel, worldRight);      
            float uProj = Vector3.Dot(rel, worldUp);       
            rProjs.Add(rProj); uProjs.Add(uProj);

            if (rProj < minR) minR = rProj;
            if (rProj > maxR) maxR = rProj;
            if (uProj < minU) minU = uProj;
            if (uProj > maxU) maxU = uProj;
        }

        float worldWidth = Mathf.Max(0.0001f, maxR - minR);
        float worldHeight = Mathf.Max(0.0001f, maxU - minU);

        if (worldWidth < 0.01f && worldHeight > 0.1f)
        {
            Debug.LogWarning($"Degenerate width detected on '{name}' — falling back to bounds.size.x");
            worldWidth = r.bounds.size.x;
        }
        Debug.DrawLine(frontCenterWorld, worldRight * 0.1f, Color.red, 10f);
        Debug.DrawLine(frontCenterWorld, worldUp * 0.1f, Color.blue, 10f);

        Vector3 toCamera = (cam.transform.position - frontCenterWorld).normalized;
        
        Debug.Log($"toCamera: {toCamera.magnitude}");


        float dotForward = Vector3.Dot(transform.forward, toCamera);
        float dotBackward = Vector3.Dot(-transform.forward, toCamera);
        
        Vector3 outwardTowardCamera = (dotForward > dotBackward) ? transform.forward : -transform.forward;
        Vector3 quadWorldPos = frontCenterWorld + outwardTowardCamera * offsetWorld;

      

        if (spawnedQuad != null) Destroy(spawnedQuad);
        spawnedQuad = Instantiate(quadPrefab);
        spawnedQuad.name = $"{name}_FrontQuad";
        spawnedQuad.transform.SetParent(transform, false);
        spawnedQuad.transform.localPosition = transform.InverseTransformPoint(quadWorldPos);

        Vector3 lossy = transform.lossyScale;
        
        float localScaleX = worldWidth / Mathf.Max(1e-8f, lossy.x);
        float localScaleY = worldHeight / Mathf.Max(1e-8f, lossy.y);
        
    

        float uniformLocal = Mathf.Max(localScaleX, localScaleY);
        
        uniformLocal *= scaleMultiplier;
        
        spawnedQuad.transform.localScale = new Vector3(uniformLocal, uniformLocal, uniformLocal);
        spawnedQuad.transform.localRotation = Quaternion.Euler(0f, 0f, rotaionZ);

        Transform child = spawnedQuad.transform.GetChild(0);

        float safeDist = Mathf.Max(0.01f, toCamera.magnitude);

        float scaledOffset = Mathf.Clamp(0.00094f * (4f / safeDist), 0.001f, 0.2f);
        child.localPosition = new Vector3(0f, scaledOffset, 0f);
        if(rotaionZ == 0){
            child.localPosition += new Vector3(0f, -0.014f, 0f);
        }

        Debug.Log($"Final local scale: {uniformLocal} (from world size {worldWidth}x{worldHeight})");
      
        // set material/texture
        Renderer quadR = spawnedQuad.GetComponentInChildren<Renderer>();

        if (quadR != null)
        {
            Material mat = quadR.sharedMaterial != null ? new Material(quadR.sharedMaterial) : new Material(Shader.Find("Standard"));
            if (tex != null) { 
                
                mat.mainTexture = tex; 
                mat.mainTextureScale = new Vector2(-1f, -1f); 
                mat.mainTextureOffset = Vector2.zero; 
            }
            quadR.material = mat;
        }


        return spawnedQuad;
    }



}
