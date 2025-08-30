using System;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    [SerializeField] private AttachmentProperties _mainAttachment;

    private void Update()
    {
        HandleMainAttachment();
    }

    private void HandleMainAttachment()
    {
        if (_mainAttachment.Attachment == null) return;
        _mainAttachment.Attachment.HandleAbility();
    }
}

[System.Serializable]
public class AttachmentProperties
{
    public Attachment Attachment;
    public Transform AttachmentPosition;
}
