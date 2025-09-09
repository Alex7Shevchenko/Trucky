using UnityEngine;
using DG.Tweening;

public class MinigunAttachment : AttachmentActive
{
    public override AttachmentParts[] AttachmentParts => _attachmentParts;
    public override GameObject ObjectToSpawn => _bullet;
    public override float Cooldown => _cooldown;

    [SerializeField] private GameObject _bullet;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _shotForce;
    [SerializeField] private AttachmentParts[] _attachmentParts;

    private int _gunIndexToActivate;

    public override void Init(PlayerManager playerManager)
    {
        _gunIndexToActivate = 0;
    }

    public override void HandleAbility(KeyCode keyCode)
    {
        if (Input.GetKey(keyCode) && _currentCooldown <= 0)
        {
            _gunIndexToActivate = (_gunIndexToActivate + 1) % AttachmentParts.Length;

            ShootBullet();
            Recoil();
        }
    }

    private void ShootBullet()
    {
        _currentCooldown = Cooldown;
        var shotPoint = AttachmentParts[_gunIndexToActivate].SpawnPoint;
        var bullet = Instantiate(ObjectToSpawn.gameObject, shotPoint.position, shotPoint.rotation);
        var rigidbody = bullet.GetComponent<Rigidbody>();
        rigidbody.AddForce(bullet.transform.forward * _shotForce, ForceMode.Impulse);
    }

    private void Recoil() => AttachmentParts[_gunIndexToActivate].FeedbacksPlayer.PlayFeedbacks();
}

