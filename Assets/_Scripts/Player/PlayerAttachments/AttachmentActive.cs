using MoreMountains.Feedbacks;
using UnityEngine;

public abstract class AttachmentActive : Attachment
{
    public abstract AttachmentParts[] AttachmentParts { get; }
    public abstract GameObject ObjectToSpawn { get; }
    public abstract float Cooldown { get; }
    protected float _currentCooldown;

    private void Update() { if (_currentCooldown > 0) _currentCooldown -= Time.deltaTime; }
}

[System.Serializable]
public class AttachmentParts
{
    public Transform SpawnPoint;
    public MMF_Player FeedbacksPlayer;
}
