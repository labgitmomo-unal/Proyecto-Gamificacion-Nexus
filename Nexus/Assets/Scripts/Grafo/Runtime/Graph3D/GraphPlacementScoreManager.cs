using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Evaluates stable graph-node placement and scores persistent road connections.</summary>
public sealed class GraphPlacementScoreManager : MonoBehaviour
{
    private const float DefaultEnterRadius = 0.75f;
    private const float DefaultExitRadius = 0.90f;
    private const float DefaultStableContactSeconds = 0.35f;
    private const float DefaultEvaluationInterval = 0.10f;
    private const float MinimumPositiveValue = 0.0001f;
    private const float PointsPerRoad = 2f;

    [SerializeField] private Transform nodeRoot;
    [SerializeField] private GraphExampleSequence exampleSequence;
    [SerializeField] private float enterRadius = DefaultEnterRadius;
    [SerializeField] private float exitRadius = DefaultExitRadius;
    [SerializeField] private float stableContactSeconds = DefaultStableContactSeconds;
    [SerializeField] private float evaluationInterval = DefaultEvaluationInterval;
    [SerializeField] private bool ignoreExampleNodes = true;

    private readonly List<TargetState> targets = new List<TargetState>();
    private readonly List<GraphNode3D> playableNodes = new List<GraphNode3D>();
    private readonly List<GraphTrafficRoad> roads = new List<GraphTrafficRoad>();
    private readonly List<GraphEdge> edges = new List<GraphEdge>();
    private readonly HashSet<GraphTrafficRoad> warnedRoads = new HashSet<GraphTrafficRoad>();
    private float evaluationTimer;
    private float lastPublishedScore;
    private float lastPublishedMaximum;

    public float CurrentScore { get; private set; }
    public float MaximumScore { get; private set; }
    public bool IsEvaluationActive { get; private set; }
    public event Action<float, float> ScoreChanged;

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
        DiscoverRoads();
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
        DiscoverRoads();
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
        DiscoverTargets();
        DiscoverRoads();
        CurrentScore = 0f;
        PublishScore(true);
    }

    private void DiscoverTargets()
    {
        targets.Clear();
        if (transform == null)
        {
            MaximumScore = 0f;
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
    }

    private void DiscoverRoads()
    {
        roads.Clear();
        var seenRoads = new HashSet<GraphTrafficRoad>();
        var seenPairs = new HashSet<string>();
        var discoveredRoads = FindObjectsByType<GraphTrafficRoad>(FindObjectsSortMode.None);
        foreach (var road in discoveredRoads)
        {
            if (road == null || !seenRoads.Add(road))
                continue;

            if (!road.IsConfigured)
            {
                WarnIncompleteRoad(road);
                continue;
            }

            var pairKey = GetIntersectionPairKey(road.StartIntersection, road.EndIntersection);
            if (!seenPairs.Add(pairKey))
            {
                WarnRoadOnce(road, $"GraphPlacementScoreManager ignora la carretera duplicada '{road.RoadName}'.");
                continue;
            }

            roads.Add(road);
        }

        MaximumScore = roads.Count * PointsPerRoad;
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

        RefreshEdges();
        RefreshRoadSnapshots();
        PublishRoadScore();
    }

    private void RefreshRoadSnapshots()
    {
        foreach (var road in roads)
            road.RefreshTrafficSnapshot();
    }

    private void RefreshEdges()
    {
        edges.Clear();
        var discoveredEdges = FindObjectsByType<GraphEdge>(FindObjectsSortMode.None);
        foreach (var edge in discoveredEdges)
        {
            if (edge != null)
                edges.Add(edge);
        }
    }

    private void PublishRoadScore()
    {
        var score = 0f;
        foreach (var road in roads)
        {
            var startNode = GetOccupant(road.StartIntersection);
            var endNode = GetOccupant(road.EndIntersection);
            if (startNode == null || endNode == null || startNode == endNode)
                continue;

            var edge = FindPersistentEdge(startNode, endNode);
            if (edge == null)
                continue;

            var selectedColor = GraphTrafficColorUtility.Classify(edge.SelectedEdgeColor);
            score += GraphTrafficColorUtility.CalculateScore(road.ExpectedColor, selectedColor);
        }

        CurrentScore = score;
        PublishScore(false);
    }

    private GraphNode3D GetOccupant(Transform target)
    {
        foreach (var state in targets)
        {
            if (state.Target == target && state.IsOccupied)
                return state.Occupant;
        }

        return null;
    }

    private GraphEdge FindPersistentEdge(GraphNode3D firstNode, GraphNode3D secondNode)
    {
        foreach (var edge in edges)
        {
            if (edge == null || !edge.gameObject.activeInHierarchy || edge.PreserveOnReset
                || edge.StartSocket == null || edge.EndSocket == null)
                continue;

            var startNode = edge.StartSocket.AssignedOwnerNode;
            var endNode = edge.EndSocket.AssignedOwnerNode;
            if ((startNode == firstNode && endNode == secondNode)
                || (startNode == secondNode && endNode == firstNode))
                return edge;
        }

        return null;
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

        CurrentScore = 0f;
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
        if (!force && Mathf.Approximately(CurrentScore, lastPublishedScore)
            && Mathf.Approximately(MaximumScore, lastPublishedMaximum))
            return;

        lastPublishedScore = CurrentScore;
        lastPublishedMaximum = MaximumScore;
        ScoreChanged?.Invoke(CurrentScore, MaximumScore);
    }

    private void WarnIncompleteRoad(GraphTrafficRoad road)
    {
        WarnRoadOnce(road, $"GraphPlacementScoreManager ignora la carretera incompleta '{road.RoadName}'.");
    }

    private void WarnRoadOnce(GraphTrafficRoad road, string message)
    {
        if (road != null && warnedRoads.Add(road))
            Debug.LogWarning(message, road);
    }

    private static string GetIntersectionPairKey(Transform first, Transform second)
    {
        var firstId = first.GetInstanceID();
        var secondId = second.GetInstanceID();
        return firstId < secondId ? $"{firstId}:{secondId}" : $"{secondId}:{firstId}";
    }

    private void ClampConfiguration()
    {
        enterRadius = Mathf.Max(enterRadius, MinimumPositiveValue);
        exitRadius = Mathf.Max(exitRadius, MinimumPositiveValue);
        stableContactSeconds = Mathf.Max(stableContactSeconds, 0f);
        evaluationInterval = Mathf.Max(evaluationInterval, MinimumPositiveValue);
    }
}
