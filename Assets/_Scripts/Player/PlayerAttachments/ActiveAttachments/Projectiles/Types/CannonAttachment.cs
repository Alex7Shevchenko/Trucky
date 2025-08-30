using DG.Tweening;
using UnityEngine;

public class CannonAttachment : ProjectileActiveAttachment
{
    public override void HandleAbility()
    {
        if (Input.GetKey(KeyCode.Mouse0) && _currentCooldown <= 0)
        {
            ShootShell();
            Recoil();
        }
    }

    private void ShootShell()
    {
        _currentCooldown = _cooldown;
        var shell = Instantiate(_shellPrefab);
        shell.transform.position = _shotPoint.position;
        shell.transform.rotation = _shotPoint.rotation;
        var rigidbody = shell.GetComponent<Rigidbody>();
        rigidbody.AddForce(shell.transform.forward * _shotStrength, ForceMode.Impulse);
    }

    private void Recoil()
    {
        Vector3 recoilImpulse = -_shotPoint.forward * _recoilStrength;
        _playerRigidibody.AddForceAtPosition(recoilImpulse, _shotPoint.position, ForceMode.Impulse);

        float recoilTime = _animationRecoilTime > _cooldown ? _cooldown : _animationRecoilTime;
        float recoilHalfTime = recoilTime / 2;
        float recoilStrength = -_animationRecoilStrength;

        _recoilObjectTarget.DOLocalMoveZ(recoilStrength, recoilHalfTime)
            .SetRelative(true)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutSine);
    }
}
