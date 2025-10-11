using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[System.Serializable]
public class SplineJoint : MonoBehaviour
{
    public TrackSegment parentSegment;   // сегмент, к которому относится джойнт
    [Range(0f, 1f)]
    public float s;                       // положение на сегменте

    public bool attachToStart => s <= 0;
    public bool attachToEnd => s >= 1;

    void OnValidate()
    {
        if (parentSegment == null)
            return;
        transform.parent = parentSegment.transform;
        SplineUtility.Evaluate(parentSegment.spline.Spline, s, out float3 pos, out _, out _);
        transform.localPosition = pos;
    }
    public Vector3 EvaluateWorldPosition()
    {
        if (parentSegment == null || parentSegment.spline == null)
            return Vector3.zero;

        SplineUtility.Evaluate(parentSegment.spline.Spline, s, out float3 pos, out _, out _);
        return math.transform(parentSegment.spline.transform.localToWorldMatrix, pos);
    }

    public Vector3 EvaluateForward()
    {
        if (parentSegment == null || parentSegment.spline == null)
            return Vector3.forward;

        SplineUtility.Evaluate(parentSegment.spline.Spline, s, out _, out float3 tangent, out _);
        return math.normalize((Vector3)tangent);
    }
}
