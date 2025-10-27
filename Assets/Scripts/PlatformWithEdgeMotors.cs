using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlatformWithMotors : MonoBehaviour
{
    [SerializeField] private PlatformConnectionManager _connectionManager;
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

    private float collision = 0;

    public float Speed => speed;
    public void AddSpeed(float amount)
    {
        speed += amount;
    }

    public void SetThrust(float thrust)
    {
        input = thrust;
    }

    private static List<PlatformWithMotors> AllPlatforms = new List<PlatformWithMotors>();

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
        collision = 0;
        foreach (var platform in AllPlatforms)
        {
            if (platform == this)
                continue;


            SplineMotor collidedMotor = CheckCollisionWithOtherPlatform(platform);
            if (collidedMotor)
            {
                collision = 1;
                float v1 = speed;
                float v2 = platform.speed;
                float newV1 = GetCollisionSpeed(v1, v2);
                float newV2 = GetCollisionSpeed(v2, v1);
                AddSpeed(-speed + newV1);
                platform.AddSpeed(-platform.speed + newV2);

                ResolvePenetration(collidedMotor, platform);
            }
            ResolveConnection(platform);
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
        MaintainMotorDistance();
    }

    private void MaintainMotorDistance()
    {

        if (Vector3.Distance(frontMotor.transform.position, backMotor.transform.position) < 2.8)
        {
            frontMotor.MoveAlongSegment(-Time.deltaTime * 10);
            backMotor.MoveAlongSegment(Time.deltaTime * 10);
        }

        if (Vector3.Distance(frontMotor.transform.position, backMotor.transform.position) > 3.2)
        {
            frontMotor.MoveAlongSegment(Time.deltaTime * 10);
            backMotor.MoveAlongSegment(-Time.deltaTime * 10);
        }
    }

    private void ResolvePenetration(SplineMotor motor, PlatformWithMotors other)
    {

        float myMin = Mathf.Min(frontMotor.S, backMotor.S);
        float myMax = Mathf.Max(frontMotor.S, backMotor.S);

        float otherMin = Mathf.Min(other.frontMotor.S, other.backMotor.S);
        float otherMax = Mathf.Max(other.frontMotor.S, other.backMotor.S);

        float overlap = Mathf.Min(myMax, otherMax) - Mathf.Max(myMin, otherMin);
        float len = frontMotor.CurrentSegment.GetLength();
        float overlapWorld = overlap * len;

        if (motor == other.frontMotor)
        {
            float mul = overlapWorld;
            //motor.TryToConnect(backMotor);
            _connectionManager.TryAutoConnect(motor, backMotor);
            if (motor.ConnectedTo == backMotor)
            {

            }
            else
            {
                if (overlapWorld <= 0)
                    mul *= -0.175f;//-speed * 0.9f;//-0.175f;
            }

            AddSpeed(-mul);
            other.AddSpeed(mul);

        }
        else
        {
            float mul = overlapWorld;
            //motor.TryToConnect(frontMotor);
            _connectionManager.TryAutoConnect(motor, frontMotor);
            if (motor.ConnectedTo == frontMotor)
            {

            }
            else
            {
                if (overlapWorld <= 0)
                    mul *= -0.175f;//-speed * 0.9f;//-0.175f;
            }

            AddSpeed(mul);
            other.AddSpeed(-mul);

        }
    }

    private void ResolveConnection(PlatformWithMotors other)
    {
        var connectedFront = frontMotor.ConnectedTo == other.frontMotor || frontMotor.ConnectedTo == other.backMotor;
        var connectedBack = backMotor.ConnectedTo == other.frontMotor || backMotor.ConnectedTo == other.backMotor;

        if (!connectedBack && !connectedFront)
            return;

        if (connectedFront)
        {
            var dst = Vector3.Distance(frontMotor.transform.position, frontMotor.ConnectedTo.transform.position);
            var dst2 = Vector3.Distance(backMotor.transform.position, frontMotor.ConnectedTo.transform.position);
            var dstB = Vector3.Distance(backMotor.transform.position, frontMotor.transform.position);

            float mul = dst * 0.5f;
            if (dst2 < dstB)
            {
                mul = -dst * 0.25f;
            }

            AddSpeed(-mul);
            other.AddSpeed(mul);

        }

        if (connectedBack)
        {
            var dst = Vector3.Distance(backMotor.transform.position, backMotor.ConnectedTo.transform.position);
            var dst2 = Vector3.Distance(frontMotor.transform.position, backMotor.ConnectedTo.transform.position);
            var dstB = Vector3.Distance(frontMotor.transform.position, backMotor.transform.position);

            float mul = dst * 0.5f;
            if (dst2 < dstB)
            {
                mul = -dst * 0.25f;
            }

            AddSpeed(mul);
            other.AddSpeed(-mul);

        }
    }

    private float GetCollisionSpeed(float v1, float v2, float m1 = 1, float m2 = 1, float e = 0.55f)
    {
        return ((m1 - e * m2) * v1 + (1 + e) * m2 * v2) / (m1 + m2);
    }

    const float minGap = 0.005f;

    private SplineMotor CheckCollisionWithOtherPlatform(PlatformWithMotors other)
    {
        foreach (var otherMotor in new[] { other.frontMotor, other.backMotor })
        {

            if (otherMotor.CurrentSegment != frontMotor.CurrentSegment || otherMotor.CurrentSegment != backMotor.CurrentSegment)//if (otherMotor.CurrentSegment != frontMotor.CurrentSegment || otherMotor.CurrentSegment != backMotor.CurrentSegment)
                continue;

            if (frontMotor.CurrentSegment == backMotor.CurrentSegment)
            {
                if ((otherMotor.S >= frontMotor.S - minGap && otherMotor.S <= backMotor.S + minGap)
                || (otherMotor.S <= frontMotor.S + minGap && otherMotor.S >= backMotor.S - minGap))
                    return otherMotor;

                continue;
            }

            if (otherMotor.CurrentSegment == frontMotor.CurrentSegment)
            {
                if (frontMotor.GetDirectionAlongSegment() > 0)
                {
                    if (otherMotor.S <= frontMotor.S)
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
                    if (otherMotor.S >= backMotor.S)
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

    void OnDrawGizmos()
    {
        if (collision > 0)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            Handles.Label(transform.position + Vector3.up, "!", style);
        }
    }
}
