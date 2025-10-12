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
        frontMotor.SetPlatform(this);
        backMotor.SetPlatform(this);
        // Инициализация моторных точек с локальными оффсетами
        frontMotor.SetSegment(spline, frontMotor.transform.localPosition);
        backMotor.SetSegment(spline, backMotor.transform.localPosition);

        // Snap один раз на старте
        frontMotor.SnapToSegmentAtStart(transform);
        backMotor.SnapToSegmentAtStart(transform);
    }

    void OnDisable()
    {
        AllPlatforms.Remove(this);
    }


    void Update()
    {
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

        foreach (var platform in AllPlatforms)
        {
            if (platform == this)
                continue;

            SplineMotor collidedMotor = CheckCollisionWithOtherPlatform(platform);
            if (collidedMotor)
            {
                Debug.Log("BLO");
                if (collidedMotor == platform.backMotor)
                {
                    platform.AddSpeed(speed / 2);
                }
                else
                {
                    platform.AddSpeed(-speed / 2);
                }
                //speed *= 0.5f;
            }
        }
    }

    private SplineMotor CheckCollisionWithOtherPlatform(PlatformWithMotors other)
    {

        // if (frontMotor.CurrentSegment == backMotor.CurrentSegment)
        // {
        //     if (other.frontMotor.CurrentSegment == frontMotor.CurrentSegment)
        //     {
        //         return (other.frontMotor.S >= frontMotor.S && other.frontMotor.S <= backMotor.S)
        //         || (other.frontMotor.S <= frontMotor.S && other.frontMotor.S >= backMotor.S);
        //     }
        //     if (other.backMotor.CurrentSegment == frontMotor.CurrentSegment)
        //     {
        //         return (other.backMotor.S >= frontMotor.S && other.backMotor.S <= backMotor.S)
        //         || (other.backMotor.S <= frontMotor.S && other.backMotor.S >= backMotor.S);
        //     }
        // }
        foreach (var otherMotor in new[] { other.frontMotor, other.backMotor })
        {
            //foreach (var myMotor in new[] { frontMotor, backMotor })
            //{

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
            //}
        }

        return null;
    }

}
