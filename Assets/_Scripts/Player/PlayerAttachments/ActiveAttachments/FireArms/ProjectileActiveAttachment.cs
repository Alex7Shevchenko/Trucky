using MoreMountains.Feedbacks;
using UnityEngine;

public class ProjectileActiveAttachment : ActiveAttachment
{
    [SerializeField] protected GameObject _bulletPrefab;
    [SerializeField] protected float _shotStrength;
    [SerializeField] protected GunParts[] _gunParts;

    protected Rigidbody _playerRigidbody;
}

[System.Serializable]
public class GunParts
{
    [SerializeField] public Transform ShotPoint;
    [SerializeField] public MMF_Player FeedbacksPlayer;
}
