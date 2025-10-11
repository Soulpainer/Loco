using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Track/TrackController")]
public class TrackController : MonoBehaviour
{
    [System.Serializable]
    public struct JointPair
    {
        public SplineJoint from;
        public SplineJoint to;
    }

    public List<JointPair> jointPairs = new List<JointPair>();

    public SplineJoint GetConnectedJoint(SplineJoint joint)
    {
        foreach (var pair in jointPairs)
        {
            if (pair.from == joint) return pair.to;
            if (pair.to == joint) return pair.from;
        }
        return null;
    }
}
