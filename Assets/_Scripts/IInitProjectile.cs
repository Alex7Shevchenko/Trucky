using UnityEngine;

public interface IInitProjectile<T> { void Init(in T data); }

public struct Basic
{
    public float Damage;
}

public struct Homing
{
    public Transform Target;
    public float TurnRateDegPerSec;
}

public struct Explosive
{
    public float Radius;
    public float Force;
}