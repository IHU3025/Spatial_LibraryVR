using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDummy : MonoBehaviour
{
   [SerializeField] private int health;  
   [SerializeField] private Animator dummyAnimator;  
   private int hitTimes;
   [SerializeField] private float reviveDelay = 3f;
   
   private bool isAlive = true; // <-- NEW FLAG TO TRACK STATE

   private void Start()
    {
        hitTimes = 0;
        isAlive = true; // Start alive
    }

   private void OnTriggerEnter(Collider other)
   {
        // ONLY process hits if the dummy is alive and the hit is from a weapon
        if (isAlive && other.gameObject.CompareTag("Weapon")) 
        {
            Debug.Log("Weapon TRIGGER detected on dummy");
            hitTimes += 1;
            Debug.Log("Hit count: " + hitTimes + "/" + health);
            
            if (hitTimes >= health) 
            {
                Die(); // Call a separate function to handle death
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
            isAlive = false; // <-- CRITICAL: Set flag to false IMMEDIATELY
            dummyAnimator.SetTrigger("Death");
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
        // Use the class-level dummyAnimator, don't create a new one
        if (dummyAnimator != null)
        {
            dummyAnimator.SetTrigger("Revive");
            Debug.Log("Revive trigger set");
        }
        
        hitTimes = 0;
        isAlive = true; // <-- CRITICAL: Set flag back to true
        Debug.Log("Dummy revived! Hit times reset to 0.");
    }

    
}