using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Debugger Data")]
public class DebuggerData : ScriptableObject
{
    public GameObject[] mainAttachments;
    public GameObject[] secondaryAttachments;
    public GameObject[] thirdAttachments;
}
