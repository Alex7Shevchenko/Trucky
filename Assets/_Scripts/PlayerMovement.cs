using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private WheelPart[] _wheels;
    private WheelCollider[] _wheelsColliders;
    private Transform[] _wheelsTransforms;

    [Header("Drive")]
    [SerializeField] private float _maxMotorTorque = 500;
    [SerializeField] private float _turnSpeed = 20;
    [SerializeField] private float _brakeTorque = 1800;
    [SerializeField] private float _maxSpeedKPH = 25;
    [SerializeField] private float _torqueRamp = 2000;

    [Header("Input")]
    [SerializeField] private string _forwardAxis = "Vertical";
    [SerializeField] private string _turnAxis = "Horizontal";

    private void Start()
    {
        InitialWheelsSetup();
    }

    private void InitialWheelsSetup()
    {
        List<WheelCollider> wheelsColliders = new List<WheelCollider>();
        List<Transform> wheelsTransforms = new List<Transform>();

        foreach (var wheel in _wheels)
        {
            wheelsColliders.Add(wheel.WheelCollider);
            wheelsTransforms.Add(wheel.WheelTransform);
        }

        _wheelsColliders = wheelsColliders.ToArray();
        _wheelsTransforms = wheelsTransforms.ToArray();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        float input = Input.GetAxisRaw(_forwardAxis);
        float speed = _rigidbody.linearVelocity.magnitude * 3.6f;
        float speedLimiter = Mathf.Clamp01(1f - Mathf.InverseLerp(_maxSpeedKPH * 0.9f, _maxSpeedKPH, speed));

        ApplyTorque(_wheelsColliders, input, speedLimiter);
        ApplyRotation();
        UpdateMeshes(_wheelsColliders, _wheelsTransforms);
    }

    private void ApplyRotation()
    {
        float input = Input.GetAxisRaw(_turnAxis);
        _rigidbody.AddRelativeTorque(Vector3.up * input * _turnSpeed, ForceMode.Acceleration);
    }

    private void ApplyTorque(WheelCollider[] wheelsColliders, float input, float speedLimiter)
    {
        bool braking = input < 0.01 && input > -0.01;

        for (int i = 0; i < wheelsColliders.Length; i++)
        {
            var wheelCollider = wheelsColliders[i];
            wheelCollider.motorTorque = braking ? 0f : (input * _maxMotorTorque * speedLimiter);

            float brake = 0f;
            if (braking)
            {
                brake = _brakeTorque;
            }
            else
            {
                float wheelDirection = Mathf.Sign(wheelCollider.rpm);
                if (Mathf.Abs(wheelCollider.rpm) > 5f && Mathf.Sign(input) != wheelDirection)
                {
                    brake = _brakeTorque;
                }
            }
            wheelCollider.brakeTorque = brake;
        }
    }

    private void UpdateMeshes(WheelCollider[] colliders, Transform[] meshes)
    {
        for (int i = 0; i < colliders.Length; i++)
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
    public Transform WheelTransform;
}