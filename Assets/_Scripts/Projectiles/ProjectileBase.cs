using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    [SerializeField] private float _damage;
    
    private void OnCollisionEnter(Collision other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.Damage(_damage);
        }

        Destroy(gameObject);
    }
}
