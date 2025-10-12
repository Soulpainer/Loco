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
        return _inverseFactor;
    }

    public void MoveAlongSegment(float ds)
    {
        if (currentSegment == null || currentSegment.spline == null) return;
        if (segmentLength < 0.0001f) return;

        float worldDir = Mathf.Sign(ds);

        float targetS = s + (ds / segmentLength) * _inverseFactor;

        s = targetS;

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

                float localDir = (next.s < 0.5f) ? +1 : -1;
                _inverseFactor = localDir * worldDir;

                s = next.s;
            }
            else
            {
                s = 1f;
            }
        }
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

        SplineUtility.Evaluate(currentSegment.spline.Spline, s, out float3 pos, out _, out _);
        transform.position = math.transform(currentSegment.spline.transform.localToWorldMatrix, pos);
    }

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
}
