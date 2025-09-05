using DG.Tweening;
using UnityEngine;

public class CannonAttachment : ProjectileActiveAttachment
{
    public override void HandleAbility(KeyCode keyCode)
    {
        if (Input.GetKey(keyCode) && _currentCooldown <= 0)
        {
            ShootShell();
            Recoil();
        }
    }

    public override void Init(PlayerManager playerManager)
    {
        _playerRigidbody = playerManager.PlayerMovement.Rigidbody;
    }

    private void ShootShell()
    {
        _currentCooldown = _cooldown;
        var shotPoint = _gunParts[0].ShotPoint;
        var shell = Instantiate(_bulletPrefab, shotPoint.position, shotPoint.rotation);
        var rigidbody = shell.GetComponent<Rigidbody>();
        rigidbody.AddForce(shell.transform.forward * _shotStrength, ForceMode.Impulse);
    }

    private void Recoil() => _gunParts[0].FeedbacksPlayer.PlayFeedbacks();
}
