using DG.Tweening;
using MoreMountains.Feedbacks;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class CannonAttachment : AttachmentActive
{
    public override AttachmentParts[] AttachmentParts => _attachmentParts;
    public override GameObject ObjectToSpawn => _shell;
    public override float Cooldown => _cooldown;

    [SerializeField] private GameObject _shell;
    [SerializeField] private float _damage;
    [SerializeField] private float _explosionRadius;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _shotForce;
    [SerializeField] private AttachmentParts[] _attachmentParts;

    public override void Init(PlayerManager playerManager) { }

    public override void HandleAbility(KeyCode keyCode)
    {
        if (Input.GetKey(keyCode) && _currentCooldown <= 0)
        {
            ShootShell();
            Recoil();
        }
    }

    private void ShootShell()
    {
        _currentCooldown = Cooldown;
        var shotPoint = AttachmentParts[0].SpawnPoint;
        var shell = Instantiate(ObjectToSpawn, shotPoint.position, shotPoint.rotation);
        var rigidbody = shell.GetComponent<Rigidbody>();
        rigidbody.AddForce(shell.transform.forward * _shotForce, ForceMode.Impulse);

        if (ObjectToSpawn.TryGetComponent<IInitProjectile<Basic>>(out var basic))
        {
            basic.Init(new Basic
            {
                Damage = _damage
            });
        }
    }

    private void Recoil() => AttachmentParts[0].FeedbacksPlayer.PlayFeedbacks();
}