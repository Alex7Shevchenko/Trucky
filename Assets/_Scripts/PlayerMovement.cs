using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private WheelPart[] _wheels;
    private WheelCollider[] _wheelsColliders;
    private Transform[] _wheelsTransforms;
    private WheelCollider[] _drivingColliders;
    private WheelCollider[] _steeringColliders;

    [Header("Drive")]
    [SerializeField] private float _maxMotorTorque = 500f;
    [SerializeField] private float _brakeTorque = 1800f;
    [SerializeField] private float _maxSpeedKPH = 25f;

    [Header("Steering")]
    [SerializeField] private float _maxSteerAngle = 25f;
    [SerializeField] private float _steerSpeedDegPerSec = 360f;
    [Tooltip("0 = no dampening, 1 = strong reduction at top speed")]
    [Range(0f,1f)] [SerializeField] private float _steerDampenBySpeed = 0.5f;

    private string _forwardAxis = "Vertical";
    private string _turnAxis = "Horizontal";

    private void Start()
    {
        InitialWheelsSetup();
    }

    private void InitialWheelsSetup()
    {
        var wheelsColliders = new List<WheelCollider>();
        var wheelsTransforms = new List<Transform>();
        var driving = new List<WheelCollider>();
        var steering = new List<WheelCollider>();

        foreach (var wheel in _wheels)
        {
            if (wheel.WheelCollider == null || wheel.WheelTransform == null) continue;

            wheelsColliders.Add(wheel.WheelCollider);
            wheelsTransforms.Add(wheel.WheelTransform);

            if (wheel.Drives)  driving.Add(wheel.WheelCollider);
            if (wheel.Steers)  steering.Add(wheel.WheelCollider);
        }

        _wheelsColliders   = wheelsColliders.ToArray();
        _wheelsTransforms  = wheelsTransforms.ToArray();
        _drivingColliders  = driving.ToArray();
        _steeringColliders = steering.ToArray();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        float fwd = Input.GetAxisRaw(_forwardAxis);
        float turn = Input.GetAxisRaw(_turnAxis);

        float speed = _rigidbody.linearVelocity.magnitude * 3.6f;
        float speedLimiter = Mathf.Clamp01(1f - Mathf.InverseLerp(_maxSpeedKPH * 0.9f, _maxSpeedKPH, speed));


        ApplyTorque(_drivingColliders, fwd, speedLimiter);
        ApplySteer(_steeringColliders, turn, speed);
        UpdateMeshes(_wheelsColliders, _wheelsTransforms);
    }

    private void ApplyTorque(WheelCollider[] wheelsColliders, float input, float speedLimiter)
    {
        if (wheelsColliders == null) return;

        bool braking = input > -0.01f && input < 0.01f;

        for (int i = 0; i < wheelsColliders.Length; i++)
        {
            var wc = wheelsColliders[i];

            wc.motorTorque = braking ? 0f : (input * _maxMotorTorque * speedLimiter);

            float brake = 0f;
            if (braking)
            {
                brake = _brakeTorque;
            }
            else
            {
                float wheelDirection = Mathf.Sign(wc.rpm);
                if (Mathf.Abs(wc.rpm) > 5f && Mathf.Sign(input) != wheelDirection)
                {
                    brake = _brakeTorque;
                }
            }
            wc.brakeTorque = brake;
        }
    }

    private void ApplySteer(WheelCollider[] colliders, float steerInput, float speedKph)
    {
        if (colliders == null) return;

        float dampen = Mathf.Lerp(1f, 1f - _steerDampenBySpeed, Mathf.InverseLerp(0f, _maxSpeedKPH, speedKph));
        float targetAngle = steerInput * _maxSteerAngle * dampen;
        float step = _steerSpeedDegPerSec * Time.fixedDeltaTime;

        for (int i = 0; i < colliders.Length; i++)
        {
            var wc = colliders[i];
            wc.steerAngle = Mathf.MoveTowards(wc.steerAngle, targetAngle, step);
        }
    }

    private void UpdateMeshes(WheelCollider[] colliders, Transform[] meshes)
    {
        if (colliders == null || meshes == null) return;

        for (int i = 0; i < colliders.Length && i < meshes.Length; i++)
        {
            colliders[i].GetWorldPose(out Vector3 position, out Quaternion rotation);
            meshes[i].SetPositionAndRotation(position, rotation);
        }
    }
}

[System.Serializable]
public class WheelPart
{
    public WheelCollider WheelCollider;
    public Transform   WheelTransform;
    public bool Drives = true;
    public bool Steers = false;
}
