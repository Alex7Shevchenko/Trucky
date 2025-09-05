using UnityEngine;

public class DummyBrain : MonoBehaviour, IDamageable, IDestructible
{
    [SerializeField] Rigidbody _rigidBody;
    public Rigidbody Rigidbody => _rigidBody;

    public void Damage(float amount)
    {
        
    }

    public void DestroyToParts()
    {

    }
}
