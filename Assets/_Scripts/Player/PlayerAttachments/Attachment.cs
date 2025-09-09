using UnityEngine;

public abstract class Attachment : MonoBehaviour
{
    public abstract void HandleAbility(KeyCode keyCode);
    public abstract void Init(PlayerManager playerManager);
}