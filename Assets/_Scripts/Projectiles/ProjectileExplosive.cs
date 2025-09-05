using System.Collections.Generic;
using UnityEngine;

public class ProjectileExplosive : ProjectileBase
{
    [SerializeField] private float _explosionRadius;

    public override void OnCollisionEnter(Collision other)
    {
        var damageablesSeen = new HashSet<IDamageable>();
        Vector3 hitPoint = other.GetContact(0).point;

        var damageable = other.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.Damage(_damage);
            damageablesSeen.Add(damageable);
        }

        var collidersInRadius = Physics.OverlapSphere(hitPoint, _explosionRadius);
        foreach (var collider in collidersInRadius)
        {
            if (collider.attachedRigidbody == _rigidbody) continue;

            var damageableInRange = collider.GetComponentInParent<IDamageable>();
            if (damageableInRange == null) continue;
            if (!damageablesSeen.Add(damageableInRange)) continue;

            float distance = Vector3.Distance(hitPoint, hitPoint);
            float normalizedDistance = 1f - Mathf.Clamp01(distance / _explosionRadius);
            damageableInRange.Damage(_damage * normalizedDistance);

            var rigidbody = damageableInRange.Rigidbody;
            Vector3 explosionDirection = (rigidbody.worldCenterOfMass - hitPoint).normalized;
            float impulse = other.relativeVelocity.magnitude * normalizedDistance;
            rigidbody.AddForceAtPosition(explosionDirection * impulse, hitPoint, ForceMode.Impulse);

        }

        Destroy(gameObject);
    }
}
