using UnityEngine;

public abstract class ProjectileBase : MonoBehaviour, IInitProjectile<Basic>
{
    protected float _damage { get; set; }
    [SerializeField] protected Rigidbody _rigidbody;

    public void Init(in Basic data)
    {
        _damage = data.Damage;
    }

    public abstract void OnCollisionEnter(Collision other);
}
