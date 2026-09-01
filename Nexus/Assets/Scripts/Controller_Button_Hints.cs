using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Guias de ayuda sobre los mandos: al mantener pulsado el Grip (parametro de
/// select del interactor) en una mano, aparecen lineas desde cada boton del
/// mando apuntando a una etiqueta pequena con la accion que realiza.
///
/// Las etiquetas viven en la ESCENA y evolucionan con el modelo 3D del mando:
///   - "HintAnchor_Trigger" / "HintAnchor_Grip" / "HintAnchor_Joystick" /
///     "HintAnchor_BotonA" / "HintAnchor_BotonB" / "HintAnchor_Menu"
///     son hijos de la malla del boton correspondiente dentro del
///     "Controller Visual" (por eso la linea apunta al boton real).
///   - Cada ancla tiene un hijo "Label" (TMP worldspace -> editar el texto ahi)
///     y un hijo "Line" (LineRenderer que une el boton con la etiqueta).
///
/// Colocar este componente en el objeto raiz "XR Origin (XR Rig)".
/// </summary>
public class Controller_Button_Hints : MonoBehaviour
{
    [Header("Comportamiento")]
    [Tooltip("En el Editor, mantener esta tecla fuerza la aparicion de las guias (para probar sin visor).")]
    public KeyCode debugSimulateKey = KeyCode.Space;

    [Tooltip("Fuerza las guias visibles siempre (solo para pruebas).")]
    public bool debugForceShow = false;

    [Tooltip("Muestra en consola el estado de cada mano una vez por segundo.")]
    public bool debugLog = true;

    [Tooltip("Si true, las etiquetas giran para quedar siempre de cara a la camara. Si false, respeta la rotacion puesta en la escena.")]
    public bool billboardFaceCamera = true;

    private class HintUnit
    {
        public Transform anchor;
        public Transform label;
        public LineRenderer line;
    }

    private HintUnit[] _leftUnits;
    private HintUnit[] _rightUnits;
    private Transform _leftController;
    private Transform _rightController;
    private Camera _camera;
    private float _lastLogTime;
    private NearFarInteractor _leftInteractor;
    private NearFarInteractor _rightInteractor;

    private void Start()
    {
        BindControllers();
    }

    private void Update()
    {
        bool moved = EnsureControllersAlive();
        if (moved || _leftUnits == null || _rightUnits == null)
            BindControllers();

        bool simulate = (Application.isEditor && SimulateKeyPressed(debugSimulateKey)) || debugForceShow;

        bool leftActive = simulate || IsSelectActive(_leftController);
        bool rightActive = simulate || IsSelectActive(_rightController);

        UpdateHand(_leftUnits, leftActive);
        UpdateHand(_rightUnits, rightActive);

        LogDebug(_leftController, _rightController, leftActive, rightActive);
    }

    private void BindControllers()
    {
        _leftController = FindController("Left");
        _rightController = FindController("Right");

        _leftUnits = FindHintUnits(_leftController);
        _rightUnits = FindHintUnits(_rightController);

        _leftInteractor = _leftController != null ? _leftController.GetComponentInChildren<NearFarInteractor>() : null;
        _rightInteractor = _rightController != null ? _rightController.GetComponentInChildren<NearFarInteractor>() : null;

        if (_leftController == null && _rightController == null)
            Debug.LogWarning("[Controller_Button_Hints] No se encontro NINGUN mando. El script debe estar en un padre de 'Left Controller' y 'Right Controller' (p. ej. el XR Origin).", this);

        if ((_leftController != null && _leftUnits.Length == 0) || (_rightController != null && _rightUnits.Length == 0))
            Debug.LogWarning("[Controller_Button_Hints] No se encontraron HintAnchor_* bajo los mandos. Crea las guias (ver doc del script) o estas no se mostraran.", this);
    }

    private bool EnsureControllersAlive()
    {
        bool changed = false;
        if (_leftUnits != null && _leftUnits.Length > 0 && _leftUnits[0].anchor == null) changed = true;
        if (_rightUnits != null && _rightUnits.Length > 0 && _rightUnits[0].anchor == null) changed = true;
        if (_leftController == null && _rightController == null) changed = true;
        return changed;
    }

    private Transform FindController(string handName)
    {
        string targetName = handName + " Controller";
        return FindChildRecursive(transform, targetName);
    }

    private Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var hit = FindChildRecursive(root.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    private HintUnit[] FindHintUnits(Transform controller)
    {
        if (controller == null) return new HintUnit[0];
        var found = new System.Collections.Generic.List<HintUnit>();
        CollectUnits(controller, found);
        return found.ToArray();
    }

    private void CollectUnits(Transform root, System.Collections.Generic.List<HintUnit> list)
    {
        if (root == null) return;
        if (root.name.StartsWith("HintAnchor_"))
        {
            Transform label = root.Find("Label");
            Transform lineTrans = root.Find("Line");
            if (label != null && lineTrans != null)
                list.Add(new HintUnit { anchor = root, label = label, line = lineTrans.GetComponent<LineRenderer>() });
        }
        for (int i = 0; i < root.childCount; i++)
            CollectUnits(root.GetChild(i), list);
    }

    private bool IsSelectActive(Transform controller)
    {
        if (controller == null) return false;
        NearFarInteractor nf;
        if (controller == _leftController) nf = _leftInteractor;
        else if (controller == _rightController) nf = _rightInteractor;
        else nf = controller.GetComponentInChildren<NearFarInteractor>();
        if (nf != null) return nf.isSelectActive;
        return false;
    }

    private void UpdateHand(HintUnit[] units, bool active)
    {
        Camera cam = GetMainCamera();

        for (int i = 0; i < units.Length; i++)
        {
            HintUnit u = units[i];
            if (u == null || u.anchor == null) continue;

            if (u.anchor.gameObject.activeSelf != active)
                u.anchor.gameObject.SetActive(active);

            if (!active) continue;

            if (billboardFaceCamera && cam != null && u.label != null)
                u.label.rotation = Quaternion.LookRotation(u.label.position - cam.transform.position);

            if (u.line != null)
            {
                Vector3 toward = u.label.localPosition.normalized;
                u.line.useWorldSpace = false;
                u.line.SetPosition(0, Vector3.zero);
                u.line.SetPosition(1, u.label.localPosition - toward * 0.006f);
            }
        }
    }

    private void LogDebug(Transform left, Transform right, bool leftActive, bool rightActive)
    {
        if (!debugLog) return;
        if (Time.unscaledTime - _lastLogTime < 1f) return;
        _lastLogTime = Time.unscaledTime;

        Debug.Log($"[Controller_Button_Hints] Left: ctl={left != null} guias={(_leftUnits != null ? _leftUnits.Length : 0)} activo={leftActive} | Right: ctl={right != null} guias={(_rightUnits != null ? _rightUnits.Length : 0)} activo={rightActive} | select(L,R)=({IsSelectActive(left)},{IsSelectActive(right)})", this);
    }

    private bool SimulateKeyPressed(KeyCode key)
    {
        if (key == KeyCode.None) return false;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return false;

        switch (key)
        {
            case KeyCode.Space:      return kb.spaceKey.isPressed;
            case KeyCode.LeftShift:  return kb.leftShiftKey.isPressed;
            case KeyCode.Return:     return kb.enterKey.isPressed;
            case KeyCode.Escape:     return kb.escapeKey.isPressed;
            case KeyCode.Tab:        return kb.tabKey.isPressed;
            default:                 return false;
        }
    }

    private Camera GetMainCamera()
    {
        if (_camera == null)
            _camera = Camera.main;
        if (_camera == null)
            _camera = FindFirstObjectByType<Camera>();
        return _camera;
    }
}