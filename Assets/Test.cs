using UnityEngine;

public class WeaponDebug : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // This will log ANY collision this weapon has
        Debug.Log("Weapon hit: " + collision.gameObject.name, collision.gameObject);
    }
}