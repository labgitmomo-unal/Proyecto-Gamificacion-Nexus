using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
public sealed class GraphNode3D : MonoBehaviour
{
    private const int SocketCount = 4;
    private const float DefaultGrabColliderPadding = 0.2f;
    private const float DefaultGrabColliderSize = 1f;

    [SerializeField] private GraphSocket3D socketPrefab;
    [SerializeField] private float socketScale = 0.18f;
    [SerializeField] private Color socketColor = Color.cyan;
    [SerializeField] private float socketLightIntensity = 3f;
    [SerializeField] private float socketLightRange = 2.5f;
    [SerializeField] private string windowNameToken = "Window";

    private readonly List<GraphSocket3D> _sockets = new();
    private XRGrabInteractable _grabInteractable;
    private bool _initialized;

    public IReadOnlyList<GraphSocket3D> Sockets => _sockets;

    private void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized)
            return;
        _initialized = true;
        ConfigureNodeInteraction();
        CreateSocketsAtWindows();
    }

    private void ConfigureNodeInteraction()
    {
        var body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        var grabCollider = GetComponent<BoxCollider>();
        if (grabCollider == null)
            grabCollider = gameObject.AddComponent<BoxCollider>();
        ConfigureGrabCollider(grabCollider);

        _grabInteractable = GetComponent<XRGrabInteractable>();
        if (_grabInteractable == null)
            _grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        _grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        _grabInteractable.trackPosition = true;
        _grabInteractable.trackRotation = true;
        _grabInteractable.trackScale = false;
        _grabInteractable.throwOnDetach = false;
        _grabInteractable.forceGravityOnDetach = false;
        _grabInteractable.retainTransformParent = true;
        _grabInteractable.selectMode = InteractableSelectMode.Single;
    }

    private void ConfigureGrabCollider(BoxCollider grabCollider)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            grabCollider.center = Vector3.zero;
            grabCollider.size = Vector3.one * DefaultGrabColliderSize;
            return;
        }

        var localBounds = new Bounds(transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (var renderer in renderers)
        {
            var bounds = renderer.bounds;
            for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                    for (var z = -1; z <= 1; z += 2)
                        localBounds.Encapsulate(transform.InverseTransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z))));
        }
        localBounds.Expand(DefaultGrabColliderPadding);
        grabCollider.center = localBounds.center;
        grabCollider.size = localBounds.size;
    }

    private void CreateSocketsAtWindows()
    {
        if (_sockets.Count > 0)
            return;
        if (socketPrefab == null)
        {
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: socketPrefab no está asignado; no se crearán sockets.", this);
            return;
        }

        var anchors = FindWindowAnchors();
        for (var i = 0; i < Mathf.Min(SocketCount, anchors.Count); i++)
        {
            var anchor = anchors[i];
            if (anchor == null)
            {
                Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: la ventana {i} no tiene un Transform válido.", this);
                continue;
            }
            var socket = Instantiate(socketPrefab, anchor, false);
            socket.name = $"GraphSocket_Light_{i + 1}";
            socket.transform.localPosition = Vector3.zero;
            socket.transform.localRotation = Quaternion.identity;
            socket.transform.localScale = Vector3.one * socketScale;
            socket.Configure(socketColor, this, socketLightIntensity, socketLightRange);
            _sockets.Add(socket);
        }

        if (_sockets.Count != SocketCount)
            Debug.LogWarning($"[{nameof(GraphNode3D)}] {name}: se encontraron {_sockets.Count} ventanas; se esperaban {SocketCount}.", this);
    }

    private List<Transform> FindWindowAnchors()
    {
        var anchors = new List<Transform>();
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child != transform && child.name.IndexOf(windowNameToken, StringComparison.OrdinalIgnoreCase) >= 0)
                anchors.Add(child);
        }
        anchors.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return anchors;
    }
}
