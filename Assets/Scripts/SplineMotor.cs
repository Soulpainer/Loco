// using UnityEngine;
// using UnityEngine.Splines;
// using Unity.Mathematics;

// public class SplineMotor : MonoBehaviour
// {

//     [Range(0f, 1f)]
//     [SerializeField] private float s;  // положение вдоль сплайна

//     private SplineContainer _spline;
//     private float splineLength;

//     // Локальный оффсет относительно платформы (для визуальной позиции)
//     [HideInInspector] public Vector3 localOffset;

//     public void SetSpline(SplineContainer spline, Vector3 initialLocalOffset)
//     {
//         _spline = spline;
//         localOffset = initialLocalOffset;

//         if (_spline != null)
//             splineLength = SplineUtility.CalculateLength(_spline.Spline, _spline.transform.localToWorldMatrix);
//     }

//     // Snap один раз при старте
//     public void SnapToSplineAtStart(Transform platform)
//     {
//         if (_spline == null) return;

//         float3 worldPos = platform.TransformPoint(localOffset);
//         float3 localSplinePos = math.transform(math.inverse(_spline.transform.localToWorldMatrix), worldPos);

//         SplineUtility.GetNearestPoint(_spline.Spline, localSplinePos, out float3 nearestLocal, out float t);
//         s = t;

//         transform.position = math.transform(_spline.transform.localToWorldMatrix, nearestLocal);
//     }

//     // Движение по сплайну с учётом скорости
//     public void MoveAlongSpline(float ds)
//     {
//         if (_spline == null || splineLength < 0.0001f) return;

//         s += ds / splineLength;

//         if (_spline.Spline.Closed)
//         {
//             if (s > 1f) s -= 1f;
//             if (s < 0f) s += 1f;
//         }
//         else
//         {
//             s = Mathf.Clamp01(s);
//         }

//         SplineUtility.Evaluate(_spline.Spline, s, out float3 pos, out _, out _);
//         transform.position = math.transform(_spline.transform.localToWorldMatrix, pos);
//     }

//     public Vector3 EvaluatePosition()
//     {
//         if (_spline == null) return transform.position;

//         SplineUtility.Evaluate(_spline.Spline, s, out float3 pos, out _, out _);
//         return math.transform(_spline.transform.localToWorldMatrix, pos);
//     }

//     public Vector3 EvaluateUp()
//     {
//         if (_spline == null) return Vector3.up;

//         SplineUtility.Evaluate(_spline.Spline, s, out _, out _, out float3 up);
//         return math.rotate(_spline.transform.localToWorldMatrix, up);
//     }
// }
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEditor;
using System;

public class SplineMotor : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float s;
    [SerializeField] private bool _showLog;
    private TrackSegment currentSegment;
    private float segmentLength;
    private float _inverseFactor = 1;
    [HideInInspector] public Vector3 localOffset;

    public float S => s;
    public TrackSegment CurrentSegment => currentSegment;
    public TrackController controller;
    private PlatformWithMotors _platform;

    public void SetSegment(TrackSegment segment, Vector3 initialLocalOffset)
    {
        currentSegment = segment;
        localOffset = initialLocalOffset;
        currentSegment.RegisterMotor(this);

        if (currentSegment != null && currentSegment.spline != null)
            segmentLength = currentSegment.GetLength();
    }

    public void SnapToSegmentAtStart(Transform platform)
    {
        if (currentSegment == null || currentSegment.spline == null) return;

        float3 worldPos = platform.TransformPoint(localOffset);
        float3 localSplinePos = math.transform(math.inverse(currentSegment.spline.transform.localToWorldMatrix), worldPos);

        SplineUtility.GetNearestPoint(currentSegment.spline.Spline, localSplinePos, out float3 nearestLocal, out float t);
        s = t;

        transform.position = math.transform(currentSegment.spline.transform.localToWorldMatrix, nearestLocal);
    }

    public float GetDirectionAlongSegment()
    {
        // возвращает направление вдоль сегмента: +1 или -1
        return _inverseFactor; // у тебя уже хранится направление мотора на сегменте
    }

    public void MoveAlongSegment(float ds)
    {
        if (currentSegment == null || currentSegment.spline == null) return;
        if (segmentLength < 0.0001f) return;

        float worldDir = Mathf.Sign(ds);
        //s += (ds / segmentLength) * _inverseFactor;
        float targetS = s + (ds / segmentLength) * _inverseFactor;

        s = targetS;

        // --- Переход вперёд ---
        if (s > 1f)
        {
            var joint = currentSegment.endJoint;
            var next = controller.GetConnectedJoint(joint);

            if (next != null)
            {
                currentSegment.UnregisterMotor(this);
                currentSegment = next.parentSegment;
                currentSegment.RegisterMotor(this);
                segmentLength = currentSegment.GetLength();

                // сохраняем направление движения
                float localDir = (next.s < 0.5f) ? +1 : -1; // если 0 — идём 0→1, если 1 — идём 1→0
                _inverseFactor = localDir * worldDir;

                s = next.s;
            }
            else
            {
                s = 1f;
            }
        }
        // --- Переход назад ---
        else if (s < 0f)
        {
            var joint = currentSegment.startJoint;
            var next = controller.GetConnectedJoint(joint);

            if (next != null)
            {
                currentSegment.UnregisterMotor(this);
                currentSegment = next.parentSegment;
                currentSegment.RegisterMotor(this);
                segmentLength = currentSegment.GetLength();

                float localDir = (next.s < 0.5f) ? +1 : -1;
                _inverseFactor = localDir * worldDir;

                s = next.s;
            }
            else
            {
                s = 0f;
            }
        }

        // --- обновление позиции ---
        SplineUtility.Evaluate(currentSegment.spline.Spline, s, out float3 pos, out _, out _);
        transform.position = math.transform(currentSegment.spline.transform.localToWorldMatrix, pos);
    }


    public void MoveAlongSegment1(float ds)
    {
        if (currentSegment == null || currentSegment.spline == null) return;
        if (segmentLength < 0.0001f) return;

        float worldDir = Mathf.Sign(ds);
        s += (ds / segmentLength) * _inverseFactor;

        if (_showLog)
            Debug.Log($"s={s:F3}, _inverseFactor={_inverseFactor}, worldDir={worldDir}");

        // --- Переход вперёд ---
        if (s > 1f)
        {
            var joint = currentSegment.endJoint;
            var next = controller.GetConnectedJoint(joint);

            if (next != null)
            {
                currentSegment.UnregisterMotor(this);
                currentSegment = next.parentSegment;
                currentSegment.RegisterMotor(this);
                segmentLength = currentSegment.GetLength();

                // сохраняем направление движения
                float localDir = (next.s < 0.5f) ? +1 : -1; // если 0 — идём 0→1, если 1 — идём 1→0
                _inverseFactor = localDir * worldDir;

                s = next.s;
            }
            else
            {
                s = 1f;
            }
        }
        // --- Переход назад ---
        else if (s < 0f)
        {
            var joint = currentSegment.startJoint;
            var next = controller.GetConnectedJoint(joint);

            if (next != null)
            {
                currentSegment.UnregisterMotor(this);
                currentSegment = next.parentSegment;
                currentSegment.RegisterMotor(this);
                segmentLength = currentSegment.GetLength();

                float localDir = (next.s < 0.5f) ? +1 : -1;
                _inverseFactor = localDir * worldDir;

                s = next.s;
            }
            else
            {
                s = 0f;
            }
        }

        // --- обновление позиции ---
        SplineUtility.Evaluate(currentSegment.spline.Spline, s, out float3 pos, out _, out _);
        transform.position = math.transform(currentSegment.spline.transform.localToWorldMatrix, pos);
    }

    // public void MoveAlongSegment2(float ds)
    // {
    //     if (currentSegment == null || currentSegment.spline == null) return;
    //     if (segmentLength < 0.0001f) return;

    //     float remaining = (ds) / segmentLength;

    //     if (Mathf.Sign(ds) != Mathf.Sign(_inverseFactor))
    //         _inverseFactor *= -1;
    //     s += remaining * _inverseFactor;
    //     if (_showLog) Debug.Log(s);
    //     // --- перепрыгивание через джойнт через TrackController ---
    //     int ttr = 0;
    //     int maxttr = 10000;

    //     // //while (ttr < maxttr && (s > 1f || s < 0f))
    //     // //{
    //     // //ttr++;
    //     // if (s > 1f)
    //     // {
    //     //     SplineJoint joint = currentSegment.endJoint;
    //     //     if (controller != null && joint != null)
    //     //     {
    //     //         var next = controller.GetConnectedJoint(joint);
    //     //         if (next != null)
    //     //         {
    //     //             _inverseFactor = next.s < 0.5f ? 1 : -1;

    //     //             float excess = s - 1f;
    //     //             currentSegment = next.parentSegment;
    //     //             segmentLength = currentSegment.GetLength();
    //     //             s = next.s;// + excess;

    //     //         }
    //     //         // else
    //     //         // {
    //     //         //     s = 1f;
    //     //         //     //break;
    //     //         // }
    //     //     }
    //     //     // else
    //     //     // {
    //     //     //     s = 1f;
    //     //     //     //break;
    //     //     // }
    //     // }
    //     // else if (s < 0f)
    //     // {
    //     //     SplineJoint joint = currentSegment.startJoint;
    //     //     if (controller != null && joint != null)
    //     //     {
    //     //         var next = controller.GetConnectedJoint(joint);
    //     //         if (next != null)
    //     //         {
    //     //             _inverseFactor = next.s < 0.5f ? -1 : 1;

    //     //             float deficit = s;
    //     //             currentSegment = next.parentSegment;
    //     //             segmentLength = currentSegment.GetLength();
    //     //             s = next.s;// + deficit;
    //     //         }
    //     //         // else
    //     //         // {
    //     //         //     s = 0f;
    //     //         //     //break;
    //     //         // }
    //     //     }
    //     //     // else
    //     //     // {
    //     //     //     s = 0f;
    //     //     //     //break;
    //     //     // }
    //     // }
    //     // //}
    //     if (s > 1f)
    //     {
    //         var joint = currentSegment.endJoint;
    //         var next = controller.GetConnectedJoint(joint);
    //         if (next != null)
    //         {
    //             float excess = s - 1f;

    //             // запоминаем, откуда и куда
    //             var jointFromS = next.s;                     // на какой стороне мы влетели
    //             float jointToS = (jointFromS == 0f) ? 1f : 0f;//var jointTo = jointFrom.OppositeOn(next.parentSegment); // на другой конец

    //             currentSegment = next.parentSegment;
    //             segmentLength = currentSegment.GetLength();

    //             // направление вдоль нового сегмента
    //             _inverseFactor = Mathf.Sign(jointToS - jointFromS);

    //             // переносим остаток
    //             s = jointFromS + excess * _inverseFactor;
    //         }
    //     }
    //     else if (s < 0f)
    //     {
    //         var joint = currentSegment.startJoint;
    //         var next = controller.GetConnectedJoint(joint);
    //         if (next != null)
    //         {
    //             float deficit = s;

    //             var jointFromS = next.s;
    //             float jointToS = (jointFromS == 0f) ? 1f : 0f;//var jointTo = jointFrom.OppositeOn(next.parentSegment);

    //             currentSegment = next.parentSegment;
    //             segmentLength = currentSegment.GetLength();

    //             _inverseFactor = Mathf.Sign(jointToS - jointFromS);

    //             s = jointFromS + deficit * _inverseFactor;
    //         }
    //     }

    //     // --- обновляем позицию на сегменте ---
    //     SplineUtility.Evaluate(currentSegment.spline.Spline, s, out float3 pos, out _, out _);
    //     transform.position = math.transform(currentSegment.spline.transform.localToWorldMatrix, pos);
    // }

    public Vector3 EvaluatePosition()
    {
        if (currentSegment == null || currentSegment.spline == null) return transform.position;

        SplineUtility.Evaluate(currentSegment.spline.Spline, s, out float3 pos, out _, out _);
        return math.transform(currentSegment.spline.transform.localToWorldMatrix, pos);
    }

    public Vector3 EvaluateUp()
    {
        if (currentSegment == null || currentSegment.spline == null) return Vector3.up;

        SplineUtility.Evaluate(currentSegment.spline.Spline, s, out _, out _, out float3 up);
        return math.rotate(currentSegment.spline.transform.localToWorldMatrix, up);
    }

    void OnDrawGizmos()
    {
        Handles.Label(transform.position, s.ToString("F2"));
    }

    internal void SetPlatform(PlatformWithMotors platformWithMotors)
    {
        _platform = platformWithMotors;
    }
}
