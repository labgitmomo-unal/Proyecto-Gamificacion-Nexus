using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Runs the example graph movement sequence from the K key or the graph panel.</summary>
public sealed class GraphExampleSequence : MonoBehaviour
{
    private const float DefaultNodeMoveDuration = 1.5f;
    private const float DefaultSocketMoveDuration = 1.2f;
    private const float DefaultNodeSettleDuration = 1.25f;

    [SerializeField] private GraphNode3D firstExampleNode;
    [SerializeField] private GraphNode3D secondExampleNode;
    [SerializeField] private Transform firstDestination;
    [SerializeField] private Transform secondDestination;
    [SerializeField] private GraphSocket3D markedSocket;
    [SerializeField] private GraphSocket3D targetSocket;
    [SerializeField] private float nodeMoveDuration = DefaultNodeMoveDuration;
    [SerializeField] private float socketMoveDuration = DefaultSocketMoveDuration;
    [SerializeField] private float nodeSettleDuration = DefaultNodeSettleDuration;

    private bool sequenceRunning;
    private bool sequenceFailed;

    /// <summary>Raised when the example finishes moving the nodes and connecting the socket.</summary>
    public event Action SequenceCompleted;
    /// <summary>Returns whether a node belongs to the protected demonstration.</summary>
    public bool IsExampleNode(GraphNode3D node)
    {
        return node != null && (node == firstExampleNode || node == secondExampleNode);
    }

    /// <summary>Returns whether an edge is the protected demonstration edge.</summary>
    public bool IsExampleEdge(GraphEdge edge)
    {
        return edge != null && edge.PreserveOnReset;
    }


    private void Update()
    {
        if (Keyboard.current != null
            && Keyboard.current.kKey.wasPressedThisFrame)
        {
            StartSequence();
        }
    }

    /// <summary>Starts the configured example sequence unless it is already running.</summary>
    public void StartSequence()
    {
        if (sequenceRunning)
        {
            return;
        }

        StartCoroutine(RunSequence());
    }

    /// <summary>Returns whether the example sequence is currently moving nodes or sockets.</summary>
    public bool IsSequenceRunning()
    {
        return sequenceRunning;
    }

    private IEnumerator RunSequence()
    {
        if (!AreReferencesValid())
        {
            yield break;
        }

        sequenceRunning = true;
        sequenceFailed = false;
        SetExampleSocketInterpolation(RigidbodyInterpolation.None);
        yield return MoveNode(firstExampleNode, firstDestination);
        yield return MoveNode(secondExampleNode, secondDestination);
        SetExampleSocketInterpolation(RigidbodyInterpolation.Interpolate);
        yield return MoveSocketToTarget();
        if (sequenceFailed)
        {
            sequenceRunning = false;
            yield break;
        }

        sequenceRunning = false;
        SequenceCompleted?.Invoke();
    }

    private IEnumerator MoveNode(GraphNode3D node, Transform destination)
    {
        var body = node.PhysicsBody;
        if (body == null)
        {
            yield break;
        }

        var startPosition = body.position;
        var elapsed = 0f;
        node.SetGrabbedPhysicsState();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        while (elapsed < nodeMoveDuration)
        {
            elapsed += Time.fixedDeltaTime;
            var t = SmoothStep(elapsed / nodeMoveDuration);
            body.MovePosition(Vector3.Lerp(startPosition, destination.position, t));
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(destination.position);
        yield return new WaitForFixedUpdate();
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        node.SetReleasedPhysicsState();
        yield return new WaitForSeconds(nodeSettleDuration);
    }

    private IEnumerator MoveSocketToTarget()
    {
        if (!markedSocket.StartDrag())
        {
            sequenceFailed = true;
            yield break;
        }

        markedSocket.MarkNextConnectionAsExample();
        var startPosition = markedSocket.transform.position;
        var elapsed = 0f;

        while (elapsed < socketMoveDuration)
        {
            elapsed += Time.fixedDeltaTime;
            var t = SmoothStep(elapsed / socketMoveDuration);
            var position = Vector3.Lerp(startPosition, targetSocket.transform.position, t);
            markedSocket.MoveDragTarget(position);
            markedSocket.ApplyDragMovement();
            yield return new WaitForFixedUpdate();
        }

        markedSocket.MoveDragTarget(targetSocket.transform.position);
        markedSocket.ApplyDragMovement();
        yield return new WaitForFixedUpdate();
        markedSocket.ReleaseDrag();
    }

    private void SetExampleSocketInterpolation(RigidbodyInterpolation interpolation)
    {
        SetSocketInterpolation(firstExampleNode, interpolation);
        SetSocketInterpolation(secondExampleNode, interpolation);
    }

    private static void SetSocketInterpolation(GraphNode3D node, RigidbodyInterpolation interpolation)
    {
        if (node == null)
        {
            return;
        }

        foreach (var socket in node.Sockets)
        {
            var socketBody = socket != null ? socket.GetComponent<Rigidbody>() : null;
            if (socketBody == null)
            {
                continue;
            }

            socketBody.interpolation = interpolation;
            socketBody.linearVelocity = Vector3.zero;
            socketBody.angularVelocity = Vector3.zero;
        }
    }

    private bool AreReferencesValid()
    {
        return firstExampleNode != null
            && secondExampleNode != null
            && firstDestination != null
            && secondDestination != null
            && markedSocket != null
            && targetSocket != null
            && markedSocket != targetSocket
            && firstExampleNode != secondExampleNode;
    }

    private static float SmoothStep(float value)
    {
        var t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }
}
