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
    [SerializeField] private GraphTrafficRoad[] expectedRoads = Array.Empty<GraphTrafficRoad>();
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
    private readonly HashSet<string> warnedEdgePairs = new HashSet<string>();
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
        public Collider DetectionCollider;
        public GraphNode3D Occupant;
        public GraphNode3D Candidate;
        public float CandidateSince;
        public bool IsOccupied;
    }

    private sealed class PlacementCandidate
    {
        public TargetState State;
        public GraphNode3D Node;
    }

    private void Awake()
    {
        ClampConfiguration();
        DiscoverTargets();
        DiscoverRoads();
        RefreshEdges();
        PublishScore(true);
    }

    private void OnEnable()
    {
        GraphEdge.TopologyChanged += HandleTopologyChanged;
    }

    private void OnDisable()
    {
        GraphEdge.TopologyChanged -= HandleTopologyChanged;
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
        RefreshEdges();
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

    /// <summary>Stops score evaluation while preserving the final score for the session.</summary>
    public void EndEvaluation()
    {
        IsEvaluationActive = false;
        evaluationTimer = 0f;
    }

    /// <summary>Stops evaluation and clears all node-to-reference assignments.</summary>
    public void ResetEvaluation()
    {
        IsEvaluationActive = false;
        evaluationTimer = 0f;
        playableNodes.Clear();
        ClearEvaluationState();
        DiscoverTargets();
        DiscoverRoads();
        RefreshEdges();
        CurrentScore = 0f;
        PublishScore(true);
    }

    private void HandleTopologyChanged()
    {
        RefreshEdges();
    }

    private void DiscoverTargets()
    {
        targets.Clear();
        var seenTargets = new HashSet<Transform>();
        for (var index = 0; index < transform.childCount; index++)
        {
            var target = transform.GetChild(index);
            if (target == null || !target.gameObject.activeInHierarchy || !seenTargets.Add(target))
                continue;

            var detectionCollider = target.GetComponentInChildren<BoxCollider>(true);
            if (detectionCollider == null || !detectionCollider.enabled)
                continue;

            targets.Add(new TargetState
            {
                Target = target,
                DetectionCollider = detectionCollider
            });
        }
    }

    private void DiscoverRoads()
    {
        roads.Clear();
        MaximumScore = 0f;
        var telemetryAdapter = FindAnyObjectByType<GraphTrafficTelemetryAdapter>();
        var candidates = new List<GraphTrafficRoad>();
        var seenRoads = new HashSet<GraphTrafficRoad>();
        if (expectedRoads == null)
        {
            Debug.LogWarning("GraphPlacementScoreManager no tiene una lista determinista de carreteras.", this);
            return;
        }

        foreach (var road in expectedRoads)
        {
            if (road == null || !seenRoads.Add(road))
                continue;
            if (!road.IsConfigured)
            {
                WarnRoadOnce(road, $"GraphPlacementScoreManager ignora la carretera incompleta '{road.RoadName}'.");
                continue;
            }
            if (telemetryAdapter == null || !telemetryAdapter.CanObserveRoad(road))
            {
                WarnRoadOnce(road, $"GraphPlacementScoreManager ignora la carretera no observable '{road.RoadName}'.");
                continue;
            }

            candidates.Add(road);
        }

        var pairGroups = new Dictionary<string, List<GraphTrafficRoad>>();
        var spawnerGroups = new Dictionary<int, List<GraphTrafficRoad>>();
        foreach (var road in candidates)
        {
            var pairKey = GetIntersectionPairKey(road.StartIntersection, road.EndIntersection);
            if (!pairGroups.TryGetValue(pairKey, out var pairGroup))
            {
                pairGroup = new List<GraphTrafficRoad>();
                pairGroups.Add(pairKey, pairGroup);
            }
            pairGroup.Add(road);

            var spawnerId = road.SpawnerRoot.GetInstanceID();
            if (!spawnerGroups.TryGetValue(spawnerId, out var spawnerGroup))
            {
                spawnerGroup = new List<GraphTrafficRoad>();
                spawnerGroups.Add(spawnerId, spawnerGroup);
            }
            spawnerGroup.Add(road);
        }

        var invalidRoads = new HashSet<GraphTrafficRoad>();
        foreach (var pairGroup in pairGroups.Values)
        {
            if (pairGroup.Count <= 1)
                continue;
            foreach (var road in pairGroup)
            {
                invalidRoads.Add(road);
                WarnRoadOnce(road, $"GraphPlacementScoreManager invalida la pareja de intersecciones duplicada de '{road.RoadName}'.");
            }
        }

        foreach (var spawnerGroup in spawnerGroups.Values)
        {
            if (spawnerGroup.Count <= 1)
                continue;
            foreach (var road in spawnerGroup)
            {
                invalidRoads.Add(road);
                WarnRoadOnce(road, $"GraphPlacementScoreManager invalida el spawner duplicado de '{road.RoadName}'.");
            }
        }

        foreach (var road in candidates)
        {
            if (invalidRoads.Contains(road))
                continue;
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
                || !IsNodeOverlappingTarget(state.Occupant, state))
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

                if (IsNodeOverlappingTarget(node, state))
                {
                    candidates.Add(new PlacementCandidate
                    {
                        State = state,
                        Node = node
                    });
                }
            }
        }

        candidates.Sort((left, right) => string.CompareOrdinal(left.State.Target.name, right.State.Target.name));
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
                || !IsNodeOverlappingTarget(state.Candidate, state))
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

            var matchingEdges = FindPersistentEdges(startNode, endNode);
            if (matchingEdges.Count == 0)
                continue;
            if (matchingEdges.Count > 1)
            {
                var pairKey = $"{startNode.GetInstanceID()}:{endNode.GetInstanceID()}";
                if (warnedEdgePairs.Add(pairKey))
                    Debug.LogWarning($"GraphPlacementScoreManager encontró conexiones duplicadas entre los nodos de '{road.RoadName}'.", road);
                continue;
            }

            var selectedColor = GraphTrafficColorUtility.Classify(matchingEdges[0].SelectedEdgeColor);
            score += GraphTrafficColorUtility.CalculateScore(road.ExpectedColor, selectedColor);
        }

        CurrentScore = score;
        PublishScore(false);
    }

    private List<GraphEdge> FindPersistentEdges(GraphNode3D firstNode, GraphNode3D secondNode)
    {
        var matchingEdges = new List<GraphEdge>();
        foreach (var edge in edges)
        {
            if (edge == null || !edge.gameObject.activeInHierarchy || edge.PreserveOnReset
                || edge.StartSocket == null || edge.EndSocket == null)
                continue;

            var startNode = edge.StartSocket.AssignedOwnerNode;
            var endNode = edge.EndSocket.AssignedOwnerNode;
            if ((startNode == firstNode && endNode == secondNode)
                || (startNode == secondNode && endNode == firstNode))
                matchingEdges.Add(edge);
        }

        return matchingEdges;
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

    private bool IsNodeOverlappingTarget(GraphNode3D node, TargetState state)
    {
        if (node == null || state == null || state.DetectionCollider == null
            || !state.DetectionCollider.enabled || !state.DetectionCollider.gameObject.activeInHierarchy)
            return false;

        var box = state.DetectionCollider as BoxCollider;
        if (box == null)
            return false;

        var scale = box.transform.lossyScale;
        var halfExtents = new Vector3(
            Mathf.Abs(box.size.x * scale.x) * 0.5f,
            Mathf.Abs(box.size.y * scale.y) * 0.5f,
            Mathf.Abs(box.size.z * scale.z) * 0.5f);
        var center = box.transform.TransformPoint(box.center);
        var overlaps = Physics.OverlapBox(center, halfExtents, box.transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide);
        foreach (var overlap in overlaps)
        {
            if (overlap == null || overlap == box)
                continue;

            var overlappingNode = overlap.GetComponentInParent<GraphNode3D>();
            if (overlappingNode == node)
                return true;
        }

        return false;
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
