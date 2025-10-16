using UnityEngine;
using System.Collections;

public class ClickCircleIndicator3D: MonoBehaviour
{
    [Header("Circle Settings")]
    public GameObject circlePrefab;
    public float circleDuration = 2f;
    public float circleSize = 0.5f;
    public float circleOffset = 0.01f; // Slightly above the plane
    
    private GameObject currentCircle;
    private Coroutine currentCircleCoroutine;

    void Start()
    {
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        
        if (circlePrefab == null)
        {
            CreateDefaultCirclePrefab();
        }
    }

    void OnMouseDown()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
        {
            ShowCircleAtPosition(hit.point, hit.normal);
        }
    }

    void ShowCircleAtPosition(Vector3 worldPosition, Vector3 normal)
    {
        if (currentCircle != null)
        {
            Destroy(currentCircle);
        }
        
        if (currentCircleCoroutine != null)
        {
            StopCoroutine(currentCircleCoroutine);
        }
        
        currentCircle = Instantiate(circlePrefab);
        currentCircle.transform.position = worldPosition + normal * circleOffset;
        currentCircle.transform.rotation = Quaternion.LookRotation(normal) * circlePrefab.transform.rotation;
        
        currentCircle.transform.localScale = circlePrefab.transform.localScale * circleSize;
        
        currentCircleCoroutine = StartCoroutine(RemoveCircleAfterDelay());
    }

    System.Collections.IEnumerator RemoveCircleAfterDelay()
    {
        yield return new WaitForSeconds(circleDuration);
        
        if (currentCircle != null)
        {
            Destroy(currentCircle);
            currentCircle = null;
        }
    }

    void CreateDefaultCirclePrefab()
    {
        // Create a simple circle GameObject
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circle.transform.localScale = new Vector3(1f, 0.05f, 1f);
        
        // Make it look like a circle (flattened cylinder)
        Renderer renderer = circle.GetComponent<Renderer>();
        
        // FIX: Use Destroy instead of DestroyImmediate to avoid editor pauses
        // Remove collider so it doesn't interfere with clicks
        Collider collider = circle.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        
        circlePrefab = circle;
        circle.SetActive(false); // Deactivate the template
        
        Debug.Log("Default circle prefab created");
    }
}