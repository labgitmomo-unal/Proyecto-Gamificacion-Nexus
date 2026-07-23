using UnityEngine;

[RequireComponent(typeof(MovementController))]
[DefaultExecutionOrder(10000)]
public class BridgeCarFreezer : MonoBehaviour
{
    [HideInInspector] public BridgeControlManager manager;
    [HideInInspector] public Vector3 originalVelocity = Vector3.zero;

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
    }

    public void TemporalRelease()
    {
        if (!_frozen) return;
        _frozen = false;

        if (_mc != null && originalVelocity.sqrMagnitude > 0.001f)
            _mc.initialVelocity = originalVelocity;
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

        if (_mc != null && originalVelocity.sqrMagnitude > 0.001f)
            _mc.initialVelocity = originalVelocity;
    }
}
