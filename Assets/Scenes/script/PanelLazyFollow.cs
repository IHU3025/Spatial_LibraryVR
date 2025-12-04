using UnityEngine;

namespace Scenes.script
{
    public class PanelLazyFollow : MonoBehaviour
    {
        [Header("Gaze Settings")]
        public Camera gazeCamera;
        public float lookAwayDuration = 5f;
        public float gazeAngleThreshold = 45f;
        
        [Header("Movement Settings")]
        public float followDistance = 2f;
        public float smoothSpeed = 2f;
        public float rotationSpeed = 3f;
        
        [Header("Debug")]
        public bool verboseLogging = false;
        
        private float lookAwayTimer = 0f;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isMoving = false;
        
        void Start()
        {
            if (gazeCamera == null)
            {
                gazeCamera = Camera.main;
            }
            
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            
            Debug.Log("[PanelLazyFollow] Initialized");
        }
        
        void Update()
        {
            if (gazeCamera == null) return;
            
            bool isLookingAtPanel = IsLookingAtPanel();
            
            if (isLookingAtPanel)
            {
                lookAwayTimer = 0f;
            }
            else
            {
                lookAwayTimer += Time.deltaTime;
                
                if (lookAwayTimer >= lookAwayDuration && !isMoving)
                {
                    StartFollowing();
                }
            }
            
            if (isMoving)
            {
                MoveAndRotateTowardsTarget();
            }
        }
        
        bool IsLookingAtPanel()
        {
            Vector3 cameraPosition = gazeCamera.transform.position;
            Vector3 cameraForward = gazeCamera.transform.forward;
            Vector3 panelPosition = transform.position;
            
            Vector3 toPanelDirection = (panelPosition - cameraPosition).normalized;
            float angle = Vector3.Angle(cameraForward, toPanelDirection);
            
            Renderer panelRenderer = GetComponent<Renderer>();
            if (panelRenderer != null)
            {
                Bounds bounds = panelRenderer.bounds;
                float distanceToPanel = Vector3.Distance(cameraPosition, panelPosition);
                float angularSize = Mathf.Atan2(Mathf.Max(bounds.size.x, bounds.size.y), distanceToPanel) * Mathf.Rad2Deg;
                float effectiveThreshold = gazeAngleThreshold + angularSize;
                
                return angle <= effectiveThreshold;
            }
            
            return angle <= gazeAngleThreshold;
        }
        
        void StartFollowing()
        {
            Vector3 cameraPosition = gazeCamera.transform.position;
            Vector3 cameraForward = gazeCamera.transform.forward;
            
            targetPosition = cameraPosition + cameraForward * followDistance;
            targetPosition.y = cameraPosition.y - 5;

            Vector3 lookDirection = cameraPosition - targetPosition;
            lookDirection.y = 0f;
            
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                targetRotation = Quaternion.LookRotation(lookDirection) * Quaternion.Euler(-90f, 15f, 0f);
            }
            
            isMoving = true;
            
            if (verboseLogging)
            {
                Debug.Log($"[PanelLazyFollow] Moving to: {targetPosition}, Rotating to face camera");
            }
        }

        void MoveAndRotateTowardsTarget()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            
            float posDistance = Vector3.Distance(transform.position, targetPosition);
            float rotDistance = Quaternion.Angle(transform.rotation, targetRotation);
            
            if (posDistance < 0.01f && rotDistance < 1f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isMoving = false;
                
                if (verboseLogging)
                {
                    Debug.Log("[PanelLazyFollow] Reached target and facing user");
                }
            }
        }
    }
}
