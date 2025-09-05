using UnityEngine;
using DG.Tweening;

public class RocketLauncherAttachment : ProjectileActiveAttachment
{
    private int _gunIndexToActivate;

    public override void HandleAbility(KeyCode keyCode)
    {
        if (Input.GetKey(keyCode) && _currentCooldown <= 0)
        {
            _gunIndexToActivate = (_gunIndexToActivate + 1) % _gunParts.Length;

            ShootRocket();
            ReloadRocketAnimation();
        }
    }

    public override void Init(PlayerManager playerManager)
    {
        _playerRigidbody = playerManager.PlayerMovement.Rigidbody;
        _gunIndexToActivate = 0;
    }

    private void ShootRocket()
    {
        _currentCooldown = _cooldown;
        var shotPoint = _gunParts[_gunIndexToActivate].ShotPoint;
        var bullet = Instantiate(_bulletPrefab.gameObject, shotPoint.position, shotPoint.rotation);
        var rigidbody = bullet.GetComponent<Rigidbody>();
        rigidbody.AddForce(bullet.transform.forward * _shotStrength, ForceMode.Impulse);
    }

    private void ReloadRocketAnimation()
    {
        _gunParts[_gunIndexToActivate].FeedbacksPlayer.PlayFeedbacks();
    }
}

