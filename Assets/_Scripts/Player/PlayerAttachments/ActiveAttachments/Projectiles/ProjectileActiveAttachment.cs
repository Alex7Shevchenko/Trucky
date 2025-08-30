using UnityEngine;

public class ProjectileActiveAttachment : ActiveAttachment
{
    [SerializeField] protected GameObject _shellPrefab;
    [SerializeField] protected Transform _shotPoint;
    [SerializeField] protected Transform _recoilObjectTarget;
    [SerializeField] protected Rigidbody _playerRigidibody;
    [SerializeField] protected float _shotStrength;
    [SerializeField] protected float _recoilStrength;

    [Header("Animation Settings")]
    [SerializeField] protected float _animationRecoilTime;
    [SerializeField] protected float _animationRecoilStrength;
}
