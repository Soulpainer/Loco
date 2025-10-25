using System.Collections.Generic;
using UnityEditor;
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
    private float baseLength = 3;

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
                bool dir = true; //v1 - v2 > 0.0f;
                float newV1 = dir ? GetCollisionSpeed(v1, v2) : v1;
                float newV2 = dir ? GetCollisionSpeed(v2, v1) : v2;
                AddSpeed(-speed + newV1);
                platform.AddSpeed(-platform.speed + newV2);

                ResolvePenetration(collidedMotor, platform);
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
        return;
        if (frontMotor.CurrentSegment != backMotor.CurrentSegment)
            return;

        if (Vector3.Distance(frontMotor.transform.localPosition, backMotor.transform.localPosition) > 2.6 &&
            Vector3.Distance(frontMotor.transform.localPosition, backMotor.transform.localPosition) < 3.4)
            return;
        //if (Vector3.Distance(frontMotor.transform.localPosition, Vector3.forward * 1.5f) > 1)
        {
            frontMotor.transform.localPosition = Vector3.Lerp(frontMotor.transform.localPosition, Vector3.forward * 1.5f, 0.1f);
            frontMotor.SnapToSegmentAtStart(transform);
        }

        //if (Vector3.Distance(backMotor.transform.localPosition, Vector3.forward * -1.5f) > 1)
        {
            backMotor.transform.localPosition = Vector3.Lerp(backMotor.transform.localPosition, Vector3.forward * -1.5f, 0.1f);
            backMotor.SnapToSegmentAtStart(transform);
        }

        // Vector3 posFront = frontMotor.EvaluatePosition();
        // Vector3 posBack = backMotor.EvaluatePosition();

        // float currentLength = Vector3.Distance(posFront, posBack);
        // float delta = currentLength - baseLength;

        // // если почти без отклонений — выходим
        // if (Mathf.Abs(delta) < 1f)
        //     return;

        // // вычисляем направление (по сплайну)
        // Vector3 dir = (posFront - posBack).normalized;

        // // половину корректировки применяем к каждому мотору
        // float half = delta * 0.5f;

        // frontMotor.ShiftS(-half);
        // backMotor.ShiftS(half);
    }

    // private void ResolvePenetration(PlatformWithMotors other)
    // {
    //     // Диапазон s для каждой платформы
    //     float myMin = Mathf.Min(frontMotor.S, backMotor.S);
    //     float myMax = Mathf.Max(frontMotor.S, backMotor.S);

    //     float otherMin = Mathf.Min(other.frontMotor.S, other.backMotor.S);
    //     float otherMax = Mathf.Max(other.frontMotor.S, other.backMotor.S);

    //     float overlap = Mathf.Min(myMax, otherMax) - Mathf.Max(myMin, otherMin);
    //     if (overlap <= 0f) return; // нет пересечения

    //     // Перевод overlap в world-метры
    //     float lenF = frontMotor.CurrentSegment.GetLength();
    //     float deltaWorld = overlap * 0.5f * lenF; // половина перекрытия

    //     // Направление вдоль платформ
    //     Vector3 dirThis = (frontMotor.EvaluatePosition() - backMotor.EvaluatePosition()).normalized;
    //     Vector3 dirOther = (other.frontMotor.EvaluatePosition() - other.backMotor.EvaluatePosition()).normalized;

    //     // Сдвигаем платформы целиком
    //     frontMotor.MoveAlongSegment(deltaWorld); // можно положительное
    //     backMotor.MoveAlongSegment(deltaWorld);

    //     other.frontMotor.MoveAlongSegment(-deltaWorld);
    //     other.backMotor.MoveAlongSegment(-deltaWorld);
    // }

    private void ResolvePenetrationBad(PlatformWithMotors other)
    {
        // Проверяем, что платформы на одном сегменте
        if (frontMotor.CurrentSegment != other.frontMotor.CurrentSegment &&
            frontMotor.CurrentSegment != other.backMotor.CurrentSegment &&
            backMotor.CurrentSegment != other.frontMotor.CurrentSegment &&
            backMotor.CurrentSegment != other.backMotor.CurrentSegment)
            return;

        // Диапазоны S
        float myMin = Mathf.Min(frontMotor.S, backMotor.S);
        float myMax = Mathf.Max(frontMotor.S, backMotor.S);

        float otherMin = Mathf.Min(other.frontMotor.S, other.backMotor.S);
        float otherMax = Mathf.Max(other.frontMotor.S, other.backMotor.S);

        float overlap = Mathf.Min(myMax, otherMax) - Mathf.Max(myMin, otherMin);
        if (overlap <= 0f) return;

        // Переводим overlap в deltaS для каждого мотора
        float lenF = frontMotor.CurrentSegment.GetLength();
        float deltaS = (overlap * 0.5f); // в единицах S

        // Определяем направление движения по S для каждой платформы
        bool myForward = frontMotor.S > backMotor.S;      // если true — front правее
        bool otherForward = other.frontMotor.S > other.backMotor.S;

        // Раздвигаем платформы
        float signThis = myForward ? 1f : -1f;
        float signOther = otherForward ? -1f : 1f;

        frontMotor.MoveAlongSegment(deltaS * signThis);
        backMotor.MoveAlongSegment(deltaS * signThis);

        other.frontMotor.MoveAlongSegment(deltaS * signOther);
        other.backMotor.MoveAlongSegment(deltaS * signOther);
    }


    private void ResolvePenetration(SplineMotor motor, PlatformWithMotors other)
    {

        if (motor == other.frontMotor)
        {
            float mul = Vector3.Distance(backMotor.transform.position, motor.transform.position) * 2;
            if (mul < 0.1f)
                mul = 0;
            //if (Vector3.Distance(frontMotor.transform.position, motor.transform.position) > 2.8f)
            //    return;
            frontMotor.MoveAlongSegment(-Time.deltaTime * mul);
            backMotor.MoveAlongSegment(-Time.deltaTime * mul);
            other.frontMotor.MoveAlongSegment(Time.deltaTime * mul);
            other.backMotor.MoveAlongSegment(Time.deltaTime * mul);
        }
        else
        {
            //if (Vector3.Distance(backMotor.transform.position, motor.transform.position) > 2.8f)
            //    return;
            float mul = Vector3.Distance(frontMotor.transform.position, motor.transform.position) * 2;
            if (mul < 0.1f)
                mul = 0;
            frontMotor.MoveAlongSegment(Time.deltaTime * mul);
            backMotor.MoveAlongSegment(Time.deltaTime * mul);
            other.frontMotor.MoveAlongSegment(-Time.deltaTime * mul);
            other.backMotor.MoveAlongSegment(-Time.deltaTime * mul);
        }
    }

    private void ResolvePenetrationBad2(PlatformWithMotors other)
    {
        //     if (frontMotor.CurrentSegment != other.frontMotor.CurrentSegment &&
        // frontMotor.CurrentSegment != other.backMotor.CurrentSegment &&
        // backMotor.CurrentSegment != other.frontMotor.CurrentSegment &&
        // backMotor.CurrentSegment != other.backMotor.CurrentSegment)
        //     {
        //         return; // не на одном сегменте — не трогаем
        //     }
        // Берём диапазон s для каждой платформы
        float myMin = Mathf.Min(frontMotor.S, backMotor.S);
        float myMax = Mathf.Max(frontMotor.S, backMotor.S);

        float otherMin = Mathf.Min(other.frontMotor.S, other.backMotor.S);
        float otherMax = Mathf.Max(other.frontMotor.S, other.backMotor.S);

        float overlap = Mathf.Min(myMax, otherMax) - Mathf.Max(myMin, otherMin);
        if (overlap <= 0) return; // нет пересечения

        // Длина текущего сегмента в метрах
        float len = frontMotor.CurrentSegment.GetLength();
        float overlapWorld = overlap * len;

        // Раздвигаем обе платформы на половину перекрытия
        float half = overlapWorld * 0.5f;

        Vector3 dir = (other.frontMotor.EvaluatePosition() - frontMotor.EvaluatePosition()).normalized;

        // Сдвигаем обе платформы в противоположные стороны
        // frontMotor.ShiftS(-half);
        // backMotor.ShiftS(-half);
        // other.frontMotor.ShiftS(half);
        // other.backMotor.ShiftS(half);

        frontMotor.MoveAlongSegment(-overlap * 0.5f * 10);
        backMotor.MoveAlongSegment(-overlap * 0.5f * 10);

        other.frontMotor.MoveAlongSegment(overlap * 0.5f * 10);
        other.backMotor.MoveAlongSegment(overlap * 0.5f * 10);
    }

    private float GetCollisionSpeed(float v1, float v2, float m1 = 1, float m2 = 1, float e = 0.5f)
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
