using UnityEngine;

[RequireComponent(typeof(MovementController))]
[DefaultExecutionOrder(10000)]
public class BridgeCarFreezer : MonoBehaviour
{
    [HideInInspector] public BridgeControlManager manager;
    [HideInInspector] public Vector3 movementDirection = Vector3.forward;

    private MovementController _mc;
    private Rigidbody _rb;
    private Vector3 _frozenPos;
    private bool _frozen;

    public bool IsFrozen => _frozen;

    void Awake()
    {
        _mc = GetComponent<MovementController>();
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        _frozen = true;
        _frozenPos = transform.position;
        if (_mc != null && _mc.initialVelocity.sqrMagnitude > 0.001f)
            movementDirection = -_mc.initialVelocity.normalized;
    }

    void OnDestroy()
    {
        if (manager != null)
            manager.OnFreezerDestroyed(this);
    }

    void LateUpdate()
    {
        if (!_frozen) return;
        if (manager == null || !manager.IsActive || manager.IsComplete) return;

        if (_mc != null)
            _mc.initialVelocity = Vector3.zero;

        transform.position = _frozenPos;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = _frozenPos;
        }
    }

    public void UpdateFreezePosition()
    {
        _frozenPos = transform.position;
        if (_mc != null && _mc.initialVelocity.sqrMagnitude > 0.001f)
            movementDirection = -_mc.initialVelocity.normalized;
    }

    public void TemporalRelease()
    {
        if (!_frozen) return;
        _frozen = false;

        if (_mc != null)
            _mc.initialVelocity = Vector3.zero;
    }

    public void Refreeze()
    {
        _frozen = true;
        _frozenPos = transform.position;

        if (_mc != null)
            _mc.initialVelocity = Vector3.zero;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.position = _frozenPos;
        }
    }

    public void Release()
    {
        _frozen = false;

        if (manager == null || manager.spawnerTemplate == null) return;

        Vector3 vel = Vector3.zero;
        if (TrafficManager.Instance != null)
            vel = TrafficManager.Instance.GetBaseVelocityForPlantilla(manager.spawnerTemplate);

        if (_mc != null)
            _mc.initialVelocity = vel;
    }
}
