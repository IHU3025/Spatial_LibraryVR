using UnityEngine;

public class ClickCircleIndicator : MonoBehaviour
{
    [Header("Circle Settings")]
    public Color circleColor = Color.red;
    public float circleDuration = 2f;
    public float circleSize = 0.5f;
    
    private Material originalMaterial;
    private Material circleMaterial;
    private Renderer planeRenderer;
    private Coroutine currentCircleCoroutine;

    void Start()
    {
        planeRenderer = GetComponent<Renderer>();
        originalMaterial = planeRenderer.material;
        
        // Create a new material for the circle effect
        circleMaterial = new Material(Shader.Find("Standard"));
        circleMaterial.CopyPropertiesFromMaterial(originalMaterial);
        
        // Add collider if not present
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    void OnMouseDown()
    {
        // Get the click position in world coordinates
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
        {
            ShowCircleAtPosition(hit.point, hit.textureCoord);
        }
    }

    void ShowCircleAtPosition(Vector3 worldPosition, Vector2 uvPosition)
    {
        // Stop any existing circle coroutine
        if (currentCircleCoroutine != null)
        {
            StopCoroutine(currentCircleCoroutine);
        }
        
        // Start new circle effect
        currentCircleCoroutine = StartCoroutine(CircleEffectCoroutine(worldPosition, uvPosition));
    }

    System.Collections.IEnumerator CircleEffectCoroutine(Vector3 worldPosition, Vector2 uvPosition)
    {
        // Apply circle effect
        planeRenderer.material = circleMaterial;
        planeRenderer.material.SetVector("_CircleCenter", new Vector4(uvPosition.x, uvPosition.y, 0, 0));
        planeRenderer.material.SetFloat("_CircleRadius", circleSize);
        planeRenderer.material.SetColor("_CircleColor", circleColor);
        
        // Wait for duration
        yield return new WaitForSeconds(circleDuration);
        
        // Restore original material
        planeRenderer.material = originalMaterial;
    }
}