using UnityEngine;

public class Attachment : MonoBehaviour
{
    public virtual void HandleAbility(KeyCode keyCode) { }
    public virtual void Init(PlayerManager playerManager) { }
}