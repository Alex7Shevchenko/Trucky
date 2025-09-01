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

            ShootRocket(_gunIndexToActivate);
            ReloadRocketAnimation(_gunIndexToActivate);
        }
    }

    public override void Init(PlayerManager playerManager)
    {
        _playerRigidbody = playerManager.PlayerMovement.Rigidbody;
        _gunIndexToActivate = 0;
    }

    private void ShootRocket(int index)
    {
        _currentCooldown = _cooldown;
        var shotPoint = _gunParts[index]._shotPoint;
        var bullet = Instantiate(_bulletPrefab, shotPoint.position, shotPoint.rotation);
        var rigidbody = bullet.GetComponent<Rigidbody>();
        rigidbody.AddForce(bullet.transform.forward * _shotStrength, ForceMode.Impulse);
    }

    private void ReloadRocketAnimation(int index)
    {
        Sequence sequence = DOTween.Sequence();

        var animateTransform = _gunParts[index]._recoilObjectTarget;
        animateTransform.gameObject.SetActive(false);
        var cooldownToAnimation = _cooldown - _animationTime;

        Sequence seq = DOTween.Sequence()
            .AppendCallback(() => animateTransform.gameObject.SetActive(false))
            .AppendInterval(cooldownToAnimation)
            .AppendCallback(() =>
            {
                animateTransform.gameObject.SetActive(true);
                animateTransform.localPosition = animateTransform.localPosition - new Vector3(0f, _animationRecoilStrength, 0);
            })
            .Append(animateTransform.DOLocalMove(animateTransform.localPosition, _animationTime).SetEase(Ease.InOutBack));
    }
}

