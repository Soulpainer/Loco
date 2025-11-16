using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public class TrackSwitch : MonoBehaviour
{
    public enum EndPoint { Start, End }

    [Header("References")]
    [Tooltip("TrackSegment on the same GameObject — will be auto-assigned if left empty.")]
    public TrackSegment segment;

    [Header("Which end of this segment this switch controls")]
    public EndPoint controlledEnd = EndPoint.End;

    [Header("Possible attachment points (other joints / anchors on other segments)")]
    public List<Transform> attachPoints = new List<Transform>();

    [Tooltip("Index in attachPoints that is currently used (-1 = none / uses its default transform)")]
    public int currentIndex = -1;

    [Header("Options")]
    public bool alignRotation = true; // align joint rotation to target attach point
    public float smoothMoveTime = 0.06f; // runtime smoothing when moving joint

    private void Reset()
    {
        segment = GetComponent<TrackSegment>();
    }

    private void Awake()
    {
        if (segment == null)
            segment = GetComponent<TrackSegment>();
    }

    public void SwitchNext()
    {
        if (!isSwitchAllowed() || attachPoints == null || attachPoints.Count == 0) return;
        int next = (currentIndex + 1) % attachPoints.Count;
        SetIndex(next);
    }

    public void SwitchPrev()
    {
        if (!isSwitchAllowed() || attachPoints == null || attachPoints.Count == 0) return;
        int next = (currentIndex - 1 + attachPoints.Count) % attachPoints.Count;
        SetIndex(next);
    }

    public void SetIndex(int index)
    {
        if (attachPoints == null || attachPoints.Count == 0) return;
        index = Mathf.Clamp(index, 0, attachPoints.Count - 1);
        if (currentIndex == index) return;

        Transform target = attachPoints[index];
        if (target == null)
        {
            Debug.LogWarning($"TrackSwitch ({name}): attachPoints[{index}] is null");
            return;
        }

        ApplyConnection(target);
        currentIndex = index;

        // Try to update TrackController pairs (best-effort)
        TryUpdateTrackController(target);
    }

    public void Disconnect()
    {
        // set index to -1 meaning no special connection
        currentIndex = -1;
    }

    private void ApplyConnection(Transform target)
    {
        if (segment == null)
        {
            Debug.LogError($"TrackSwitch ({name}): no TrackSegment assigned");
            return;
        }

        // determine which joint transform we need to move
        Transform jointTransform = controlledEnd == EndPoint.Start ?
            (segment.startJoint != null ? segment.startJoint.transform : null) :
            (segment.endJoint != null ? segment.endJoint.transform : null);

        if (jointTransform == null)
        {
            Debug.LogError($"TrackSwitch ({name}): controlled joint transform is null");
            return;
        }

        // Immediately set position and optionally rotation. We purposely set transform directly
        // because most spline/joint setups will take joint GameObject position as anchor.
        jointTransform.position = target.position;
        if (alignRotation)
            jointTransform.rotation = target.rotation;

        // if you prefer smoothing instead of teleport, uncomment this block and remove direct assignment above
        // StartCoroutine(SmoothMoveJoint(jointTransform, target.position, target.rotation));
    }

    // best-effort attempt to keep TrackController.jointPairs consistent
    private void TryUpdateTrackController(Transform targetTransform)
    {
        // find SplineJoint components if present
        SplineJoint ownJoint = controlledEnd == EndPoint.Start ? segment.startJoint : segment.endJoint;
        SplineJoint targetJoint = targetTransform != null ? targetTransform.GetComponent<SplineJoint>() : null;

        if (ownJoint == null || targetJoint == null)
            return; // nothing to keep in TrackController

        var tc = FindObjectOfType<TrackController>();
        if (tc == null)
            return;

        // update existing pair entries that referenced this segment's joint
        bool updatedExisting = true;
        for (int i = 0; i < tc.jointPairs.Count; i++)
        {
            var p = tc.jointPairs[i];
            if (p.from == ownJoint)
            {
                p.to = targetJoint; // swap our end to target
                tc.jointPairs[i] = p;
                updatedExisting = true;
            }
            else if (p.to == ownJoint)
            {
                p.from = targetJoint;
                tc.jointPairs[i] = p;
                updatedExisting = true;
            }
        }

        if (!updatedExisting)
        {
            var newPair = new TrackController.JointPair { from = ownJoint, to = targetJoint };
            tc.jointPairs.Add(newPair);
        }

        SplineJoint joint = controlledEnd == EndPoint.Start ? segment.startJoint : segment.endJoint;

        MoveKnotRuntime(
            joint.parentSegment.spline,
            FindClosestKnotIndex(joint.parentSegment.spline, ownJoint.transform.position),
            targetTransform.position,
            targetTransform.rotation
        );
    }

    public static int FindClosestKnotIndex(SplineContainer container, Vector3 worldPos)
    {
        var spline = container.Spline;
        float minDist = float.MaxValue;
        int closest = 0;
        var knots = spline.Knots.ToList();
        for (int i = 0; i < knots.Count; i++)
        {
            Vector3 knotWorld =
                container.transform.TransformPoint(knots[i].Position);

            float d = (knotWorld - worldPos).sqrMagnitude;
            if (d < minDist)
            {
                minDist = d;
                closest = i;
            }
        }

        return closest;
    }

    public static void MoveKnotRuntime(SplineContainer container, int knotIndex, Vector3 worldPos, Quaternion worldRot)
    {
        if (container == null || container.Spline == null)
            return;

        var spline = container.Spline;

        Vector3 localPos = container.transform.InverseTransformPoint(worldPos);
        Quaternion localRot = Quaternion.Inverse(container.transform.rotation) * worldRot;

        var knot = spline.Knots.ToList()[knotIndex];
        knot.Position = localPos;
        knot.Rotation = localRot;
        spline.SetKnot(knotIndex, knot);
    }

    private bool isSwitchAllowed()
    {
        return segment.Motors.Count == 0;
    }

}

