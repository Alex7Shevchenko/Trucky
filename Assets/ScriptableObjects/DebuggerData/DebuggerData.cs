using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Debugger Data")]
public class DebuggerData : ScriptableObject
{
    [Header("Attachments")]
    public GameObject[] mainAttachments;
    public GameObject[] secondaryAttachments;
    public GameObject[] thirdAttachments;
}
