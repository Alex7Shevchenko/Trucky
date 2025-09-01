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
        var shotPoint = _gunParts[0]._shotPoint;
        var shell = Instantiate(_bulletPrefab, shotPoint.position, shotPoint.rotation);
        var rigidbody = shell.GetComponent<Rigidbody>();
        rigidbody.AddForce(shell.transform.forward * _shotStrength, ForceMode.Impulse);
    }

    private void Recoil()
    {
        Vector3 recoilImpulse = -_gunParts[0]._shotPoint.forward * _recoilStrength;
        _playerRigidbody.AddForceAtPosition(recoilImpulse, _gunParts[0]._shotPoint.position, ForceMode.Impulse);

        float recoilTime = _animationTime * (_gunParts.Length + 1) > _cooldown ? _cooldown : _animationTime;
        float recoilHalfTime = recoilTime / 2;
        float recoilStrength = -_animationRecoilStrength;

        _gunParts[0]._recoilObjectTarget.DOLocalMoveZ(recoilStrength, recoilHalfTime)
            .SetRelative(true)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutSine);
    }
}
