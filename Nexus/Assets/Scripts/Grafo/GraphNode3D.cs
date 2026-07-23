using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Configura una torre como nodo 3D y crea cuatro luces interactuables en sus ventanas.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(XRGrabInteractable))]
public sealed class GraphNode3D : MonoBehaviour
{
    private const int SocketCount = 4;
    private const float DefaultGrabColliderPadding = 0.2f;
    private const float DefaultGrabColliderSize = 1f;

    [SerializeField] private float socketScale = 0.18f;
    [SerializeField] private Color socketColor = Color.cyan;
    [SerializeField] private float socketLightIntensity = 3f;
    [SerializeField] private float socketLightRange = 2.5f;
    [SerializeField] private float socketColliderRadius = 0.14f;
    [SerializeField] private string windowNameToken = "Window";

    private readonly List<GraphSocket3D> _sockets = new();
    private XRGrabInteractable _grabInteractable;

    /// <summary>
    /// Devuelve las cuatro luces creadas para este nodo.
    /// </summary>
    public IReadOnlyList<GraphSocket3D> Sockets => _sockets;

    private void Awake()
    {
        ConfigureNodeInteraction();
        CreateSocketsAtWindows();
    }

    private void ConfigureNodeInteraction()
    {
        var body = GetComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.None;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        var grabCollider = GetComponent<BoxCollider>();

        ConfigureGrabCollider(grabCollider);

        _grabInteractable = GetComponent<XRGrabInteractable>();

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
            var extents = bounds.extents;
            var center = bounds.center;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                        localBounds.Encapsulate(transform.InverseTransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z))));
                }
            }
        }

        localBounds.Expand(DefaultGrabColliderPadding);
        grabCollider.center = localBounds.center;
        grabCollider.size = localBounds.size;
    }

    private void CreateSocketsAtWindows()
    {
        if (_sockets.Count > 0)
            return;

        var anchors = FindWindowAnchors();
        for (var i = 0; i < Mathf.Min(SocketCount, anchors.Count); i++)
            CreateSocket(anchors[i], i);

        if (_sockets.Count != SocketCount)
            Debug.LogWarning($"{nameof(GraphNode3D)} en {name}: se encontraron {_sockets.Count} ventanas; se esperaban {SocketCount}.", this);
    }

    private List<Transform> FindWindowAnchors()
    {
        var anchors = new List<Transform>();
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform || child.name.IndexOf(windowNameToken, StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            anchors.Add(child);
        }

        anchors.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return anchors;
    }

    private void CreateSocket(Transform anchor, int index)
    {
        var socketObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        socketObject.name = $"GraphSocket_Light_{index + 1}";
        socketObject.transform.SetParent(anchor, false);
        socketObject.transform.localPosition = Vector3.zero;
        socketObject.transform.localRotation = Quaternion.identity;
        socketObject.transform.localScale = Vector3.one * socketScale;

        var socketRenderer = socketObject.GetComponent<Renderer>();
        var socketMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
        {
            color = socketColor
        };
        socketRenderer.sharedMaterial = socketMaterial;

        var sphereCollider = socketObject.GetComponent<SphereCollider>();
        sphereCollider.radius = socketColliderRadius / Mathf.Max(0.001f, socketScale);
        sphereCollider.isTrigger = false;

        var lightObject = new GameObject("SocketLight");
        lightObject.transform.SetParent(socketObject.transform, false);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = socketColor;
        light.intensity = socketLightIntensity;
        light.range = socketLightRange;
        light.shadows = LightShadows.None;

        var socket = socketObject.AddComponent<GraphSocket3D>();
        socket.Configure(socketColor, this);
        _sockets.Add(socket);
    }
}
