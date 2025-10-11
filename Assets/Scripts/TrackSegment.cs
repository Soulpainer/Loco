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
}
