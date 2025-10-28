using UnityEngine;

namespace Scenes.script
{
    public class MeshController : MonoBehaviour
    {
        [Header("Plane Settings")] public GameObject planePrefab;
        public float offsetDistance = 3f;
        public Vector3 scaleReduction = new Vector3(0.8f, 1f, 0.8f);

        private Vector3 spawnDirection = Vector3.down;
        private GameObject currentChildPlane;
        private bool hasChild = false;

        void Start()
        {
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }

        }

        // PUBLIC method that can be called by raycast
        public void TriggerInteraction()
        {
            Debug.Log(" PLANE INTERACTION TRIGGERED");

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

            currentChildPlane.name = $"{gameObject.name}_Child";
            currentChildPlane.transform.SetParent(transform);

            Vector3 spawnPosition = CalculateChainPosition();
            currentChildPlane.transform.position = spawnPosition;
            currentChildPlane.transform.rotation = transform.rotation;

            Vector3 parentWorldScale = transform.lossyScale;
            Vector3 desiredWorldScale = new Vector3(
                parentWorldScale.x * scaleReduction.x,
                parentWorldScale.y * scaleReduction.y,
                parentWorldScale.z * scaleReduction.z
            );

            Vector3 requiredLocalScale = new Vector3(
                desiredWorldScale.x / parentWorldScale.x,
                desiredWorldScale.y / parentWorldScale.y,
                desiredWorldScale.z / parentWorldScale.z
            );
            Debug.Log($"ACTUAL POSITIONS:");
            Debug.Log($"- Parent center: {transform.position}");
            Debug.Log($"- Child center: {currentChildPlane.transform.position}");

            currentChildPlane.transform.localScale = requiredLocalScale;

            // Add controller to child
            //MeshController childController = currentChildPlane.AddComponent<MeshController>();
            //childController.planePrefab = planePrefab;
            //childController.offsetDistance = offsetDistance;
            //childController.scaleReduction = scaleReduction;

            Renderer childRenderer = currentChildPlane.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.material.color = GetNextColor(GetComponent<Renderer>().material.color);
            }

            hasChild = true;
            Debug.Log("Spawned child plane");
        }

        Vector3 CalculateChainPosition()
        {
            Vector3 direction = transform.TransformDirection(spawnDirection);

            int planeDepth = CountPlanesInChain();

            Debug.Log($"Calculating spawn position:");
            Debug.Log($"- Direction: {direction}");
            Debug.Log($"- Plane depth in chain: {planeDepth}");
            Debug.Log($"- Offset distance: {offsetDistance}");
            Debug.Log($"- Total offset: {offsetDistance * planeDepth}");
            Debug.Log($"- From position: {transform.position}");

            float heightAdjustment = CalculateHeightAdjustment();
            float actualOffset = offsetDistance * (1f / planeDepth);

            Vector3 spawnPos = transform.position + direction * actualOffset;
            spawnPos.y += heightAdjustment;
    
            Debug.Log($"Height adjustment: {heightAdjustment}");
            Debug.Log($"Adjusted position: {spawnPos}");
    
            return spawnPos;
        }
        
        float CalculateHeightAdjustment()
        {
            Renderer parentRenderer = GetComponent<Renderer>();
            Renderer childRenderer = currentChildPlane.GetComponent<Renderer>();
    
            if (parentRenderer != null && childRenderer != null)
            {
                float parentHeight = parentRenderer.bounds.size.y;
                float childHeight = childRenderer.bounds.size.y;
        
                // Since child is smaller, we need to move it down by half the height difference
                // to keep the centers aligned horizontally
                return (childHeight - parentHeight) / 3f;
            }
    
            return 0f;
        }


        int CountPlanesInChain()
        {
            int count = 0;
            Transform current = transform;
    
            while (current != null && current.GetComponent<MeshController>() != null)
            {
                count++;
                Debug.Log($"  Plane in chain: {current.name}");
                current = current.parent;
            }
    
            return count;
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
}