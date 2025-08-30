using System;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private Transform _mainAttachmentPositionOverride;
    [SerializeField] private AttachmentProperties _mainAttachment;
    [SerializeField] private AttachmentProperties _secondaryAttachment;

    private void Update()
    {
        HandleMainAttachment();
        HandleSecondaryAttachment();
    }

    public void AttachMainAttachment(GameObject attachmentPrefab)
    {
        var attachment = Instantiate(attachmentPrefab, _mainAttachment.AttachmentPosition);
        _mainAttachment.Attachment = attachment.GetComponent<Attachment>();
        _mainAttachment.Attachment.Init(_playerManager);
    }

    private void HandleMainAttachment()
    {
        if (_mainAttachment.Attachment == null) return;
        _mainAttachment.Attachment.HandleAbility(_mainAttachment.AttachmentButton);
    }

    public void AttachSecondaryAttachment(GameObject attachmentPrefab)
    {
        _mainAttachment.Attachment.transform.position = _mainAttachmentPositionOverride.position;
        var attachment = Instantiate(attachmentPrefab, _secondaryAttachment.AttachmentPosition);
        _secondaryAttachment.Attachment = attachment.GetComponent<Attachment>();
        _secondaryAttachment.Attachment.Init(_playerManager);
    }

    private void HandleSecondaryAttachment()
    {
        if (_secondaryAttachment.Attachment == null) return;
        _secondaryAttachment.Attachment.HandleAbility(_secondaryAttachment.AttachmentButton);
    }
}

[System.Serializable]
public class AttachmentProperties
{
    public Attachment Attachment;
    public Transform AttachmentPosition;
    public KeyCode AttachmentButton;
}
