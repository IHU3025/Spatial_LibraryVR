using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; 

public class TargetDummy : MonoBehaviour
{
   [SerializeField] private int health;  
   [SerializeField] private Animator dummyAnimator;  
   [SerializeField] private float reviveDelay = 3f;
   [SerializeField] private Transform playerTarget; 
   [SerializeField] private string walkingStateName = "Walking";

   private bool isAlive = true; 
   private int hitTimes;
   private NavMeshAgent navMeshAgent;
   private bool shouldMove = false;

   private void Start()
    {
        hitTimes = 0;
        isAlive = true; 
        navMeshAgent = GetComponent<NavMeshAgent>();

         if (navMeshAgent == null)
        {
            navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
            navMeshAgent.angularSpeed = 120f;
            navMeshAgent.acceleration = 8f;
            navMeshAgent.stoppingDistance = 1.5f; 
             navMeshAgent.speed = 0f;
        }  else
        {
            navMeshAgent.speed = 0f;
        }

    }

     private void Update() {
        if (isAlive && playerTarget != null && IsInWalkingState()) 
        {
            shouldMove = true;
        }
        else
        {
            shouldMove = false;
        }
        
        if (shouldMove) 
        {
            if (navMeshAgent.isActiveAndEnabled && navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = false; 
            }
            
            navMeshAgent.speed = 2.0f;
            navMeshAgent.SetDestination(playerTarget.position);
            float speed = navMeshAgent.velocity.magnitude;
            dummyAnimator.SetFloat("Speed", speed);
        } 
        else 
        {
            if (navMeshAgent.isActiveAndEnabled && !navMeshAgent.isStopped)
            {
                navMeshAgent.isStopped = true; 
                navMeshAgent.speed = 0f;
            }
            dummyAnimator.SetFloat("Speed", 0);
        }
    }
      private bool IsInWalkingState()
    {
        AnimatorStateInfo stateInfo = dummyAnimator.GetCurrentAnimatorStateInfo(0);
        
       
        if (stateInfo.IsName(walkingStateName)) 
        {
            return true;
        }
        return false;
    }

   private void OnTriggerEnter(Collider other)
   {
        if (isAlive && other.gameObject.CompareTag("Weapon")) 
        {
            Debug.Log("Weapon TRIGGER detected on dummy");
            hitTimes += 1;
            Debug.Log("Hit count: " + hitTimes + "/" + health);
            
            if (hitTimes >= health) 
            {
                Die(); 
            }
        }

        if (isAlive && other.gameObject.CompareTag("player")){
            Debug.Log("player got attacked");
            //player.die()
        }
   }

    private void Die()
    {
        Debug.Log("Attempting to trigger death animation");
        if (dummyAnimator != null)
        {
            isAlive = false; 
            dummyAnimator.SetTrigger("Death");
            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.speed = 0f;
            }            
            Debug.Log("Death trigger set");
            StartCoroutine(ReviveAfterDelay());            
        }
    }

    private IEnumerator ReviveAfterDelay()
    {
        yield return new WaitForSeconds(reviveDelay);
        ReviveDummy();
    }

   public void ActivateDummy(){
        dummyAnimator.SetTrigger("Activate"); 
   }

   public void ReviveDummy()
    {
        if (dummyAnimator != null)
        {
            dummyAnimator.SetTrigger("Revive");
            Debug.Log("Revive trigger set");
            navMeshAgent.isStopped = false;

        }
        
        hitTimes = 0;
        isAlive = true; 
        Debug.Log("Dummy revived! Hit times reset to 0.");
    }

    
}