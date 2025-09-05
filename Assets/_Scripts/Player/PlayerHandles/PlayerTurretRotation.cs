using UnityEngine;

public class PlayerTurretRotation : MonoBehaviour
{
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _turretBase;
    [SerializeField] private Transform _gunBase;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private float _maxPitch;
    [SerializeField] private float _minPitch;
    [SerializeField] private float _pitchSpeed;

    private void Update()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            HandleTurretRotation(hit.point);
            HandleGunPitch(hit.point);
        }
    }

    private void HandleTurretRotation(Vector3 point)
    {
        Vector3 dir = point - _turretBase.position;
        dir = Vector3.ProjectOnPlane(dir, -_turretBase.up);
        Quaternion target = Quaternion.LookRotation(dir, _turretBase.up);
        _turretBase.rotation = Quaternion.RotateTowards(
            _turretBase.rotation, target, _rotationSpeed * Time.deltaTime);
    }

    private void HandleGunPitch(Vector3 point)
    {
        Vector3 toTarget = point - _gunBase.position;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        float desiredPitch = -Mathf.Atan2(toTarget.y, flat.magnitude) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(desiredPitch, _minPitch, _maxPitch);
        Quaternion targetRotation = Quaternion.Euler(desiredPitch, 0f, 0f);
        _gunBase.localRotation = Quaternion.RotateTowards(_gunBase.localRotation, targetRotation, _pitchSpeed * Time.deltaTime);
    }
}
