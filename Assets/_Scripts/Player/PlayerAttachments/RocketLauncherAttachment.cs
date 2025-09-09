using UnityEngine;
using DG.Tweening;

public class RocketLauncherAttachment : AttachmentActive
{
    public override AttachmentParts[] AttachmentParts => _attachmentParts;
    public override GameObject ObjectToSpawn => _rocket;
    public override float Cooldown => _cooldown;

    [SerializeField] private GameObject _rocket;
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

            ShootRocket();
            ReloadRocketAnimation();
        }
    }

    private void ShootRocket()
    {
        _currentCooldown = Cooldown;
        var shotPoint = AttachmentParts[_gunIndexToActivate].SpawnPoint;
        var bullet = Instantiate(ObjectToSpawn.gameObject, shotPoint.position, shotPoint.rotation);
        var rigidbody = bullet.GetComponent<Rigidbody>();
        rigidbody.AddForce(bullet.transform.forward * _shotForce, ForceMode.Impulse);
    }

    private void ReloadRocketAnimation()
    {
        AttachmentParts[_gunIndexToActivate].FeedbacksPlayer.PlayFeedbacks();
    }
}

