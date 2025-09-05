using UnityEngine;

public class ActiveAttachment : Attachment
{
    [SerializeField] protected float _cooldown;

    protected float _currentCooldown;

    private void Update() => HandleCooldown();

    private void HandleCooldown()
    {
        if (_currentCooldown > 0)
        {
            _currentCooldown -= Time.deltaTime;
        }
    }
}