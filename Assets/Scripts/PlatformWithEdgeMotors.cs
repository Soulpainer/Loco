using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PlatformWithMotors : MonoBehaviour
{
    [Header("Motors")]
    public SplineMotor frontMotor;
    public SplineMotor backMotor;

    [Header("Movement")]
    public TrackSegment spline;
    public float force;

    private float drivePower = 1;//30f;
    public float drag = 2f;
    public float gravity = 9.81f;

    private float speed = 0f;
    private float input = 0f;

    public float Speed => speed;
    public void AddSpeed(float amount)
    {
        this.speed += amount;
    }

    public void SetThrust(float thrust)
    {
        input = thrust;
    }

    private static List<PlatformWithMotors> AllPlatforms = new List<PlatformWithMotors>();

    private const float minMotorGap = 0.05f;

    void OnEnable()
    {
        if (spline == null)
        {
            Debug.LogError("SplineContainer не назначен!");
            return;
        }

        AllPlatforms.Add(this);

        frontMotor.SetSegment(spline, frontMotor.transform.localPosition);
        backMotor.SetSegment(spline, backMotor.transform.localPosition);

        frontMotor.SnapToSegmentAtStart(transform);
        backMotor.SnapToSegmentAtStart(transform);
    }

    void OnDisable()
    {
        AllPlatforms.Remove(this);
    }


    void Update()
    {
        foreach (var platform in AllPlatforms)
        {
            if (platform == this)
                continue;

            SplineMotor collidedMotor = CheckCollisionWithOtherPlatform(platform);
            if (collidedMotor)
            {
                if (collidedMotor == platform.backMotor)
                {
                    float v1 = speed;
                    float v2 = platform.speed;
                    float newV1 = GetCollisionSpeed(v1, v2);
                    float newV2 = GetCollisionSpeed(v2, v1);
                    AddSpeed(-speed + newV1);
                    platform.AddSpeed(-platform.speed + newV2);
                }
                else
                {
                    float v1 = speed;
                    float v2 = platform.speed;
                    float newV1 = GetCollisionSpeed(v1, v2);
                    float newV2 = GetCollisionSpeed(v2, v1);
                    AddSpeed(-speed + newV1);
                    platform.AddSpeed(-platform.speed + newV2);
                }
            }
        }


        float dt = Time.deltaTime;

        Vector3 forwardDir = (frontMotor.EvaluatePosition() - backMotor.EvaluatePosition()).normalized;
        float slope = Vector3.Dot(Vector3.down, forwardDir);

        float accel = input * drivePower + gravity * slope + force;
        speed += accel * dt;
        speed *= Mathf.Exp(-drag * dt);

        frontMotor.MoveAlongSegment(speed * dt);
        backMotor.MoveAlongSegment(speed * dt);

        Vector3 posFront = frontMotor.EvaluatePosition();
        Vector3 posBack = backMotor.EvaluatePosition();
        Vector3 up = (frontMotor.EvaluateUp() + backMotor.EvaluateUp()).normalized;

        transform.position = (posFront + posBack) * 0.5f;
        transform.rotation = Quaternion.LookRotation(forwardDir, up);


    }

    private float GetCollisionSpeed(float v1, float v2, float m1 = 1, float m2 = 1, float e = 0.5f)
    {
        return ((m1 - e * m2) * v1 + (1 + e) * m2 * v2) / (m1 + m2);
    }

    private SplineMotor CheckCollisionWithOtherPlatform(PlatformWithMotors other)
    {
        foreach (var otherMotor in new[] { other.frontMotor, other.backMotor })
        {

            if (otherMotor.CurrentSegment != frontMotor.CurrentSegment || otherMotor.CurrentSegment != backMotor.CurrentSegment)
                continue;

            if (frontMotor.CurrentSegment == backMotor.CurrentSegment)
            {
                if ((otherMotor.S >= frontMotor.S && otherMotor.S <= backMotor.S)
                || (otherMotor.S <= frontMotor.S && otherMotor.S >= backMotor.S))
                    return otherMotor;

                continue;
            }

            if (otherMotor.CurrentSegment == frontMotor.CurrentSegment)
            {
                if (frontMotor.GetDirectionAlongSegment() > 0)
                {
                    if (otherMotor.S < frontMotor.S)
                        return otherMotor;
                }
                else
                {
                    if (otherMotor.S > frontMotor.S)
                        return otherMotor;
                }
            }

            if (otherMotor.CurrentSegment == backMotor.CurrentSegment)
            {
                if (backMotor.GetDirectionAlongSegment() > 0)
                {
                    if (otherMotor.S > backMotor.S)
                        return otherMotor;
                }
                else
                {
                    if (otherMotor.S < backMotor.S)
                        return otherMotor;
                }
            }
        }

        return null;
    }

}
