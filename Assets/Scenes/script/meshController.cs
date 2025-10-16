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
            // Add collider for ray detection
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            Debug.Log("Mesh Controller Ready - Manual Raycast Mode");
        }

        // PUBLIC method that can be called by raycast
        public void TriggerInteraction()
        {
            Debug.Log("🎯 PLANE INTERACTION TRIGGERED!");

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

            currentChildPlane.transform.localScale = requiredLocalScale;

            // Add controller to child
            MeshController childController = currentChildPlane.AddComponent<MeshController>();
            childController.planePrefab = planePrefab;
            childController.offsetDistance = offsetDistance;
            childController.scaleReduction = scaleReduction;

            Renderer childRenderer = currentChildPlane.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                childRenderer.material.color = GetNextColor(GetComponent<Renderer>().material.color);
            }

            hasChild = true;
            Debug.Log("✅ Spawned child plane");
        }

        Vector3 CalculateChainPosition()
        {
            Vector3 direction = transform.TransformDirection(spawnDirection);
            int depth = 0;
            Transform current = transform;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return transform.position + direction * (offsetDistance * depth);
        }

        void RemoveChildPlane()
        {
            if (currentChildPlane != null)
            {
                Destroy(currentChildPlane);
                hasChild = false;
                Debug.Log("✅ Removed child plane");
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