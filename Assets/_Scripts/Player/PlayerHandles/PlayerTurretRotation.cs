using UnityEngine;

public class PlayerTurretRotation : MonoBehaviour
{
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _turretBase;
    [SerializeField] private Transform _gunBase;
    [SerializeField] private float _rotationSpeed;
    [SerializeField] private float _maxYaw;

    private void Update()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        bool hasTarget = Physics.Raycast(ray, out RaycastHit raycastHit);

        if (hasTarget)
        {
            HandleTurretRotation(raycastHit.point);
            HandleGunYaw(raycastHit.point);
        }
    }

    private void HandleTurretRotation(Vector3 point)
    {
        Vector3 directionToTarget = point - _turretBase.position;
        directionToTarget = Vector3.ProjectOnPlane(directionToTarget, -_turretBase.up);
        Quaternion turretTarget = Quaternion.LookRotation(directionToTarget, _turretBase.up);
        _turretBase.rotation = Quaternion.RotateTowards(_turretBase.rotation, turretTarget, _rotationSpeed * Time.deltaTime);
    }

    private void HandleGunYaw(Vector3 point)
    {
        Vector3 directionToTarget = point - _gunBase.position;
        Vector3 flatDirection = new Vector3(directionToTarget.x, 0f, directionToTarget.z);
        float pitchAngle = -Mathf.Atan2(directionToTarget.y, flatDirection.magnitude) * Mathf.Rad2Deg;
        pitchAngle = Mathf.Clamp(pitchAngle, -_maxYaw, _maxYaw);
        _gunBase.localRotation = Quaternion.Euler(pitchAngle, 0, 0);
    }
}
