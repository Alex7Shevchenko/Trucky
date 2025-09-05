using UnityEngine;

public interface IDamageable
{
    public Rigidbody Rigidbody { get; }

    public void Damage(float amount);
}
