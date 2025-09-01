using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private Transform _mainAttachmentPositionOverride;
    [SerializeField] private AttachmentProperties[] _attachmentProperties;

    private void Update() => HandleAttachments();

    private void HandleAttachments()
    {
        foreach (var attachmentProperty in _attachmentProperties)
        {
            if (attachmentProperty.Attachment == null) continue;

            attachmentProperty.Attachment.HandleAbility(attachmentProperty.AttachmentButton);
        }
    }

    public void AttachAttachment(GameObject attachmentPrefab, AttachmentType attachmentType)
    {
        int index = (int)attachmentType;
        var mountPoint = _attachmentProperties[index].AttachmentPosition;

        if (_attachmentProperties[index].Attachment != null)
            Destroy(_attachmentProperties[index].Attachment.gameObject);

        var attachment = Instantiate(attachmentPrefab, mountPoint);
        _attachmentProperties[index].Attachment = attachment.GetComponent<Attachment>();
        _attachmentProperties[index].Attachment.Init(_playerManager);

        if (attachmentType == AttachmentType.Main || attachmentType == AttachmentType.Secondary)
            RepositionMainGun();
    }

    private void RepositionMainGun()
    {
        int main = (int)AttachmentType.Main;
        int secondary = (int)AttachmentType.Secondary;
        Attachment mainAttachment = _attachmentProperties[main].Attachment;
        Attachment secondaryAttachment = _attachmentProperties[secondary].Attachment;

        if (mainAttachment != null && secondaryAttachment != null)
            mainAttachment.transform.position = _mainAttachmentPositionOverride.position;
    }
}

[System.Serializable]
public class AttachmentProperties
{
    [HideInInspector] public Attachment Attachment;
    public Transform AttachmentPosition;
    public KeyCode AttachmentButton;
}

public enum AttachmentType
{
    Main,
    Secondary,
    third
}
