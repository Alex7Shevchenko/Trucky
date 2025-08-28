using System;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private WheelCollider[] _leftWheels;
    [SerializeField] private WheelCollider[] _rightWheels;
    [SerializeField] private Transform[] _leftWheelsTransforms;
    [SerializeField] private Transform[] _rightWheelsTransforms;

    [Header("Drive")]
    [SerializeField] private float _maxMotorTorque = 1800;
    [SerializeField] private float _maxBrakeTorque = 6000;
    [SerializeField] private float _holdBrakeTorque = 1800;
    [SerializeField] private float _maxSpeedKPH = 1800;
    [SerializeField] private float _torqueRamp = 1800;

    [Header("Steering Feel")]
    [Range(0f, 1f)][SerializeField] private float _turnDampenBySpeed = 0.6f;
    [SerializeField] private float _deadzone = 0.05f;

    [Header("Input")]
    [SerializeField] private string _forwardAxis = "Vertical";
    [SerializeField] private string _turnAxis = "Horizontal";
    [SerializeField] private bool _rawInput = true;

    [Header("Stability")]
    [SerializeField] private float _antiRoll = 8000f;

    private float _currLeft;
    private float _currRight;

    private void FixedUpdate()
    {
        // Inputs
        float fwd = _rawInput ? Input.GetAxisRaw(_forwardAxis) : Input.GetAxis(_forwardAxis);
        float turn = _rawInput ? Input.GetAxisRaw(_turnAxis) : Input.GetAxis(_turnAxis);

        // Deadzone
        if (Mathf.Abs(fwd) < _deadzone) fwd = 0f;
        if (Mathf.Abs(turn) < _deadzone) turn = 0f;

        // Speed-based turn dampening (so it feels sharp at low speed, stable at high speed)
        float speed = _rigidbody.linearVelocity.magnitude * 3.6f; // kph
        float turnScale = Mathf.Lerp(1f, 1f - _turnDampenBySpeed, Mathf.InverseLerp(0f, _maxSpeedKPH, speed));
        turn *= turnScale;

        // Differential mix
        // Left gets fwd + turn, Right gets fwd - turn
        float leftCmd = Mathf.Clamp(fwd + turn, -1f, 1f);
        float rightCmd = Mathf.Clamp(fwd - turn, -1f, 1f);

        // Optional: pivot boost when nearly stationary for crisp spins-in-place
        if (Mathf.Abs(fwd) < 0.01f && Mathf.Abs(turn) > 0.01f && speed < 2f)
        {
            leftCmd = Mathf.Clamp(+turn, -1f, 1f);
            rightCmd = Mathf.Clamp(-turn, -1f, 1f);
        }

        // Smooth but snappy ramp (in FixedUpdate)
        _currLeft = Mathf.MoveTowards(_currLeft, leftCmd, Time.fixedDeltaTime * _torqueRamp);
        _currRight = Mathf.MoveTowards(_currRight, rightCmd, Time.fixedDeltaTime * _torqueRamp);

        // Speed cap (fade torque near limit)
        float speedLimiter = Mathf.Clamp01(1f - Mathf.InverseLerp(_maxSpeedKPH * 0.9f, _maxSpeedKPH, speed));

        // Apply to wheels
        ApplySide(_leftWheels, _currLeft, speedLimiter);
        ApplySide(_rightWheels, _currRight, speedLimiter);

        // Anti-roll for stability
        ApplyAntiRoll(_leftWheels, _rightWheels);

        // Optional: sync wheel meshes
        UpdateMeshes(_leftWheels, _leftWheelsTransforms);
        UpdateMeshes(_rightWheels, _rightWheelsTransforms);
    }

    private void ApplySide(WheelCollider[] side, float cmd, float speedLimiter)
    {
        bool braking = Mathf.Abs(cmd) < 0.001f;

        for (int i = 0; i < side.Length; i++)
        {
            var wc = side[i];

            // Motor torque (fade near top speed)
            wc.motorTorque = braking ? 0f : (cmd * _maxMotorTorque * speedLimiter);

            // Brakes: slight hold when idle; strong brake if opposite input while moving
            float brake = 0f;
            if (braking)
                brake = _holdBrakeTorque;
            else
            {
                // If wheel angular velocity opposes commanded direction, add some brake to flip fast
                float wheelDir = Mathf.Sign(wc.rpm);
                if (Mathf.Abs(wc.rpm) > 5f && Mathf.Sign(cmd) != wheelDir)
                    brake = _maxBrakeTorque * 0.25f; // bitey direction change
            }
            wc.brakeTorque = brake;
        }
    }

    private void ApplyAntiRoll(WheelCollider[] left, WheelCollider[] right)
    {
        // Simple per-axle anti-roll: assumes arrays are ordered front->back and matching counts
        int count = Mathf.Min(left.Length, right.Length);
        for (int i = 0; i < count; i++)
        {
            WheelHit hitL, hitR;
            bool groundedL = left[i].GetGroundHit(out hitL);
            bool groundedR = right[i].GetGroundHit(out hitR);

            float travelL = 1.0f;
            float travelR = 1.0f;

            if (groundedL)
                travelL = (-left[i].transform.InverseTransformPoint(hitL.point).y - left[i].radius) / left[i].suspensionDistance;
            if (groundedR)
                travelR = (-right[i].transform.InverseTransformPoint(hitR.point).y - right[i].radius) / right[i].suspensionDistance;

            float antiRollForce = (travelL - travelR) * _antiRoll;

            if (groundedL) _rigidbody.AddForceAtPosition(left[i].transform.up * -antiRollForce, left[i].transform.position);
            if (groundedR) _rigidbody.AddForceAtPosition(right[i].transform.up * antiRollForce, right[i].transform.position);
        }
    }

    private void UpdateMeshes(WheelCollider[] cols, Transform[] meshes)
    {
        if (meshes == null || meshes.Length != cols.Length) return;
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
            meshes[i].SetPositionAndRotation(pos, rot);
        }
    }
}