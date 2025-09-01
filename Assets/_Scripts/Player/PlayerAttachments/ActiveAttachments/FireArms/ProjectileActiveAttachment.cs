using UnityEngine;

public class ProjectileActiveAttachment : ActiveAttachment
{
    [SerializeField] protected GameObject _bulletPrefab;
    [SerializeField] protected Rigidbody _playerRigidbody;
    [SerializeField] protected float _shotStrength;
    [SerializeField] protected float _recoilStrength;
    [SerializeField] protected GunParts[] _gunParts;

    [Header("Animation Settings")]
    [SerializeField] protected float _animationTime;
    [SerializeField] protected float _animationRecoilStrength;
}

[System.Serializable]
public class GunParts
{
    [SerializeField] public Transform _recoilObjectTarget;
    [SerializeField] public Transform _shotPoint;
}
