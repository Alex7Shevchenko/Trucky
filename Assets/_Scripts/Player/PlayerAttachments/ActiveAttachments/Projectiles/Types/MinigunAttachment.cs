using UnityEngine;
using DG.Tweening;

public class MinigunAttachment : ProjectileActiveAttachment
{
    private int _gunIndexToActivate;

    public override void HandleAbility(KeyCode keyCode)
    {
        if (Input.GetKey(keyCode) && _currentCooldown <= 0)
        {
            _gunIndexToActivate = (_gunIndexToActivate + 1) % _gunParts.Length;
            
            ShootBullet(_gunIndexToActivate);
            Recoil(_gunIndexToActivate);
        }
    }

    public override void Init(PlayerManager playerManager)
    {
        _playerRigidbody = playerManager.PlayerMovement.Rigidbody;
        _gunIndexToActivate = 0;
    }

    private void ShootBullet(int index)
    {
        _currentCooldown = _cooldown;
        var bullet = Instantiate(_bulletPrefab);
        bullet.transform.position = _gunParts[index]._shotPoint.position;
        bullet.transform.rotation = _gunParts[index]._shotPoint.rotation;
        var rigidbody = bullet.GetComponent<Rigidbody>();
        rigidbody.AddForce(bullet.transform.forward * _shotStrength, ForceMode.Impulse);
    }

    private void Recoil(int index)
    {
        Vector3 recoilImpulse = -_gunParts[index]._shotPoint.forward * _recoilStrength;
        _playerRigidbody.AddForceAtPosition(recoilImpulse, _gunParts[index]._shotPoint.position, ForceMode.Impulse);

        float recoilTime = _animationRecoilTime * (_gunParts.Length + 1) > _cooldown ? _cooldown : _animationRecoilTime;
        float recoilHalfTime = recoilTime / 2;
        float recoilStrength = -_animationRecoilStrength;

        _gunParts[index]._recoilObjectTarget.DOLocalMoveZ(recoilStrength, recoilHalfTime)
            .SetRelative(true)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutSine);
    }
}

