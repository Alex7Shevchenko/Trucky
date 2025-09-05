using UnityEngine;

public class ProjectileBase : MonoBehaviour
{
    [SerializeField] protected Rigidbody _rigidbody;
    [SerializeField] protected float _damage;

    public virtual void OnCollisionEnter(Collision other)
    {
        IDamageable damageable = other.gameObject.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.Damage(_damage);
        }

        Destroy(gameObject);
    }
}
