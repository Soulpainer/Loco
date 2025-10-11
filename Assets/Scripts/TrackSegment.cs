using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[System.Serializable]
public class TrackSegment : MonoBehaviour
{
    public SplineContainer spline;

    // Джойнты на концах сегмента
    public SplineJoint startJoint;
    public SplineJoint endJoint;

    public float GetLength()
    {
        if (spline == null) return 0f;
        return SplineUtility.CalculateLength(spline.Spline, spline.transform.localToWorldMatrix);
    }

    private readonly List<SplineMotor> _motors = new();

    public IReadOnlyList<SplineMotor> Motors => _motors;

    public void RegisterMotor(SplineMotor motor)
    {
        if (!_motors.Contains(motor))
            _motors.Add(motor);
    }

    public void UnregisterMotor(SplineMotor motor)
    {
        _motors.Remove(motor);
    }
}
