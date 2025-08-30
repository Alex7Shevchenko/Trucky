using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public PlayerMovement PlayerMovement => _playerMovement;
    public PlayerTurretRotation PlayerTurretRotation => _playerTurretRotation;
    public PlayerLoadout PlayerLoadout => _playerLoadout;

    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerTurretRotation _playerTurretRotation;
    [SerializeField] private PlayerLoadout _playerLoadout;
}
