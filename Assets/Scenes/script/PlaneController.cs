using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlaneController : MonoBehaviour
{
    [Header("Plane Settings")]
    public GameObject planePrefab;
    public float offsetDistance = 3f;
    public Vector3 scaleReduction = new Vector3(0.8f, 1f, 0.8f);
    
    private Vector3 spawnDirection = Vector3.down; 
    private GameObject currentChildPlane;
    private bool hasChild = false;
    
    void Start()
    {
        // Add collider for ray detection
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
        
        // Add XR Simple Interactable component
        XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }
        
        // Listen for controller selection (trigger pull)
        interactable.selectEntered.AddListener(OnXRSelect);
        
        Debug.Log("Plane is ready for VR interaction");
    }
    
    void OnXRSelect(SelectEnterEventArgs args)
    {
        if (!hasChild)
        {
            SpawnChildPlane();
        }
        else
        {
            RemoveChildPlane();
        }
    }
    
    void SpawnChildPlane()
    {
        if (planePrefab == null)
        {
            currentChildPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        }
        else
        {
            currentChildPlane = Instantiate(planePrefab);
        }
    
        currentChildPlane.name = "ChildPlane";
        currentChildPlane.transform.SetParent(transform);
    
        Vector3 spawnPosition = transform.position + transform.TransformDirection(spawnDirection) * offsetDistance;
        currentChildPlane.transform.position = spawnPosition;
        currentChildPlane.transform.rotation = transform.rotation;
        currentChildPlane.transform.localScale = Vector3.Scale(transform.localScale, scaleReduction);
    
        // Add VR interaction to child
        PlaneController childController = currentChildPlane.AddComponent<PlaneController>();
        childController.planePrefab = planePrefab;
        childController.offsetDistance = offsetDistance;
        childController.scaleReduction = scaleReduction;
    
        // Visual feedback
        Renderer childRenderer = currentChildPlane.GetComponent<Renderer>();
        if (childRenderer != null)
        {
            childRenderer.material.color = GetNextColor(GetComponent<Renderer>().material.color);
        }
    
        hasChild = true;
        Debug.Log("Spawned child plane");
    }
    
    void RemoveChildPlane()
    {
        if (currentChildPlane != null)
        {
            Destroy(currentChildPlane);
            hasChild = false;
            Debug.Log("Removed child plane");
        }
    }
    
    Color GetNextColor(Color currentColor)
    {
        float h, s, v;
        Color.RGBToHSV(currentColor, out h, out s, out v);
        h = (h + 0.3f) % 1f;
        s = Mathf.Clamp(s - 0.1f, 0.3f, 1f);
        v = Mathf.Clamp(v - 0.1f, 0.7f, 1f);
        return Color.HSVToRGB(h, s, v);
    }
}