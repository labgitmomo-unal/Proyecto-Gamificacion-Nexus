using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Evaluates stable, one-to-one placement of graph nodes on intersection references.</summary>
public sealed class GraphPlacementScoreManager : MonoBehaviour
{
    private const float DefaultEnterRadius = 0.75f;
    private const float DefaultExitRadius = 0.90f;
    private const float DefaultStableContactSeconds = 0.35f;
    private const float DefaultEvaluationInterval = 0.10f;
    private const float MinimumPositiveValue = 0.0001f;

    [SerializeField] private Transform nodeRoot;
    [SerializeField] private GraphExampleSequence exampleSequence;
    [SerializeField] private float enterRadius = DefaultEnterRadius;
    [SerializeField] private float exitRadius = DefaultExitRadius;
    [SerializeField] private float stableContactSeconds = DefaultStableContactSeconds;
    [SerializeField] private float evaluationInterval = DefaultEvaluationInterval;
    [SerializeField] private bool ignoreExampleNodes = true;

    private readonly List<TargetState> targets = new List<TargetState>();
    private readonly List<GraphNode3D> playableNodes = new List<GraphNode3D>();
    private float evaluationTimer;
    private int lastPublishedScore;

    public int CurrentScore { get; private set; }
    public int MaximumScore { get; private set; }
    public bool IsEvaluationActive { get; private set; }
    public event Action<int, int> ScoreChanged;

    private sealed class TargetState
    {
        public Transform Target;
        public GraphNode3D Occupant;
        public GraphNode3D Candidate;
        public float CandidateSince;
        public bool IsOccupied;
    }

    private sealed class PlacementCandidate
    {
        public TargetState State;
        public GraphNode3D Node;
        public float DistanceSquared;
    }

    private void Awake()
    {
        ClampConfiguration();
        DiscoverTargets();
        PublishScore(true);
    }

    private void Update()
    {
        if (!IsEvaluationActive)
            return;

        evaluationTimer += Time.deltaTime;
        if (evaluationTimer < evaluationInterval)
            return;

        evaluationTimer = 0f;
        EvaluatePlacements(Time.time);
    }

    /// <summary>Starts score evaluation after the graph demonstration has completed.</summary>
    public void BeginEvaluation()
    {
        ClampConfiguration();
        DiscoverTargets();
        ClearEvaluationState();

        if (nodeRoot == null)
        {
            Debug.LogWarning("GraphPlacementScoreManager requiere nodeRoot para iniciar la evaluación.", this);
            IsEvaluationActive = false;
            PublishScore(true);
            return;
        }

        if (exampleSequence == null)
        {
            Debug.LogWarning("GraphPlacementScoreManager requiere exampleSequence para iniciar la evaluación.", this);
            IsEvaluationActive = false;
            PublishScore(true);
            return;
        }

        DiscoverPlayableNodes();
        IsEvaluationActive = true;
        evaluationTimer = evaluationInterval;
        PublishScore(true);
    }

    /// <summary>Stops score evaluation and clears all node-to-reference assignments.</summary>
    public void ResetEvaluation()
    {
        IsEvaluationActive = false;
        evaluationTimer = 0f;
        playableNodes.Clear();
        ClearEvaluationState();
        CurrentScore = 0;
        PublishScore(true);
    }

    private void DiscoverTargets()
    {
        targets.Clear();
        if (transform == null)
        {
            MaximumScore = 0;
            return;
        }

        var seenTargets = new HashSet<Transform>();
        for (var index = 0; index < transform.childCount; index++)
        {
            var target = transform.GetChild(index);
            if (target == null || !target.gameObject.activeInHierarchy || !seenTargets.Add(target))
                continue;

            targets.Add(new TargetState { Target = target });
        }

        MaximumScore = targets.Count;
    }

    private void DiscoverPlayableNodes()
    {
        playableNodes.Clear();
        if (nodeRoot == null)
            return;

        var discoveredNodes = nodeRoot.GetComponentsInChildren<GraphNode3D>(true);
        var seenNodes = new HashSet<GraphNode3D>();
        foreach (var node in discoveredNodes)
        {
            if (node == null || !node.gameObject.activeInHierarchy || !seenNodes.Add(node))
                continue;
            if (ignoreExampleNodes && exampleSequence != null && exampleSequence.IsExampleNode(node))
                continue;

            playableNodes.Add(node);
        }
    }

    private void EvaluatePlacements(float currentTime)
    {
        var assignedNodes = new HashSet<GraphNode3D>();
        foreach (var state in targets)
        {
            if (!state.IsOccupied)
                continue;

            if (!IsTargetUsable(state.Target)
                || !IsNodeUsable(state.Occupant)
                || DistanceSquaredOnLocalXZ(state.Occupant.transform, state.Target) > exitRadius * exitRadius)
            {
                ReleaseOccupant(state);
                continue;
            }

            assignedNodes.Add(state.Occupant);
        }

        var candidates = new List<PlacementCandidate>();
        foreach (var state in targets)
        {
            if (state.IsOccupied || !IsTargetUsable(state.Target))
            {
                state.Candidate = null;
                continue;
            }

            foreach (var node in playableNodes)
            {
                if (!IsNodeUsable(node) || assignedNodes.Contains(node))
                    continue;

                var distanceSquared = DistanceSquaredOnLocalXZ(node.transform, state.Target);
                if (distanceSquared <= enterRadius * enterRadius)
                {
                    candidates.Add(new PlacementCandidate
                    {
                        State = state,
                        Node = node,
                        DistanceSquared = distanceSquared
                    });
                }
            }
        }

        candidates.Sort((left, right) => left.DistanceSquared.CompareTo(right.DistanceSquared));
        var matchedTargets = new HashSet<TargetState>();
        var matchedNodes = new HashSet<GraphNode3D>();
        foreach (var candidate in candidates)
        {
            if (!matchedTargets.Add(candidate.State) || !matchedNodes.Add(candidate.Node))
                continue;

            var state = candidate.State;
            if (state.Candidate != candidate.Node)
            {
                state.Candidate = candidate.Node;
                state.CandidateSince = currentTime;
            }
        }

        foreach (var state in targets)
        {
            if (state.IsOccupied)
                continue;

            if (!matchedTargets.Contains(state))
            {
                state.Candidate = null;
                continue;
            }

            if (!IsNodeUsable(state.Candidate)
                || DistanceSquaredOnLocalXZ(state.Candidate.transform, state.Target) > enterRadius * enterRadius)
            {
                state.Candidate = null;
                continue;
            }

            if (currentTime - state.CandidateSince < stableContactSeconds)
                continue;

            state.Occupant = state.Candidate;
            state.Candidate = null;
            state.IsOccupied = true;
            assignedNodes.Add(state.Occupant);
        }

        PublishScore(false);
    }

    private void ReleaseOccupant(TargetState state)
    {
        state.Occupant = null;
        state.Candidate = null;
        state.CandidateSince = 0f;
        state.IsOccupied = false;
    }

    private void ClearEvaluationState()
    {
        foreach (var state in targets)
        {
            state.Occupant = null;
            state.Candidate = null;
            state.CandidateSince = 0f;
            state.IsOccupied = false;
        }

        CurrentScore = 0;
    }

    private bool IsTargetUsable(Transform target)
    {
        return target != null && target.gameObject.activeInHierarchy;
    }

    private bool IsNodeUsable(GraphNode3D node)
    {
        return node != null
            && node.gameObject.activeInHierarchy
            && (!ignoreExampleNodes || exampleSequence == null || !exampleSequence.IsExampleNode(node));
    }

    private float DistanceSquaredOnLocalXZ(Transform nodeTransform, Transform targetTransform)
    {
        var nodePosition = transform.InverseTransformPoint(nodeTransform.position);
        var targetPosition = transform.InverseTransformPoint(targetTransform.position);
        var deltaX = nodePosition.x - targetPosition.x;
        var deltaZ = nodePosition.z - targetPosition.z;
        return deltaX * deltaX + deltaZ * deltaZ;
    }

    private void PublishScore(bool force)
    {
        var occupiedCount = 0;
        foreach (var state in targets)
        {
            if (state.IsOccupied)
                occupiedCount++;
        }

        CurrentScore = occupiedCount;
        if (!force && CurrentScore == lastPublishedScore)
            return;

        lastPublishedScore = CurrentScore;
        ScoreChanged?.Invoke(CurrentScore, MaximumScore);
    }

    private void ClampConfiguration()
    {
        enterRadius = Mathf.Max(enterRadius, MinimumPositiveValue);
        exitRadius = Mathf.Max(exitRadius, MinimumPositiveValue);
        stableContactSeconds = Mathf.Max(stableContactSeconds, 0f);
        evaluationInterval = Mathf.Max(evaluationInterval, MinimumPositiveValue);
    }
}
