using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ElevatorVRButtonActivator : MonoBehaviour
{
    private const float MovementThreshold = 0.05f;
    private const float PositionChangeThreshold = 0.001f;
    private const int SettledFramesRequired = 8;

    private InputAction[] controllerButtonActions;
    private FloorChangeTrigger floorChangeTrigger;
    private bool playerInside;
    private Collider playerCollider;
    private Transform playerTransform;
    private CharacterController playerCharacterController;
    private bool waitingForMovement;
    private bool elevatorMoving;
    private Vector3 activationStartPosition;
    private Vector3 previousElevatorPosition;
    private float activationStartY;
    private float previousY;
    private int settledFrames;

    private void Awake()
    {
        floorChangeTrigger = GetComponent<FloorChangeTrigger>();

        if (floorChangeTrigger != null)
        {
            floorChangeTrigger.enabled = false;
        }

        controllerButtonActions = new[]
        {
            CreateButtonAction("ElevatorLeftPrimary", "<XRController>{LeftHand}/{PrimaryButton}"),
            CreateButtonAction("ElevatorLeftSecondary", "<XRController>{LeftHand}/{SecondaryButton}"),
            CreateButtonAction("ElevatorRightPrimary", "<XRController>{RightHand}/{PrimaryButton}"),
            CreateButtonAction("ElevatorRightSecondary", "<XRController>{RightHand}/{SecondaryButton}")
        };

        foreach (InputAction action in controllerButtonActions)
        {
            action.Enable();
        }
    }

    private void OnDestroy()
    {
        if (controllerButtonActions == null)
        {
            return;
        }

        foreach (InputAction action in controllerButtonActions)
        {
            action.Disable();
            action.Dispose();
        }
    }

    private void Update()
    {
        if (waitingForMovement)
        {
            MonitorMovementStart();
        }
        else if (elevatorMoving)
        {
            CarryPlayerWithElevator();
            MonitorMovementEnd();
        }

        if (!playerInside || waitingForMovement || elevatorMoving || !WasControllerButtonPressed())
        {
            return;
        }

        ActivateOriginalElevatorTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other == playerCollider)
        {
            playerInside = false;
            playerCollider = null;

            if (!waitingForMovement && !elevatorMoving)
            {
                playerTransform = null;
                playerCharacterController = null;
                DisableFloorChangeTrigger();
            }
        }
    }

    private void RegisterPlayer(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = true;
        playerCollider = other;
        playerCharacterController = other.GetComponentInParent<CharacterController>();
        playerTransform = playerCharacterController != null ? playerCharacterController.transform : other.transform;
    }

    private void ActivateOriginalElevatorTrigger()
    {
        if (floorChangeTrigger == null || playerCollider == null)
        {
            return;
        }

        activationStartPosition = transform.position;
        previousElevatorPosition = activationStartPosition;
        activationStartY = activationStartPosition.y;
        previousY = activationStartY;
        settledFrames = 0;
        waitingForMovement = true;
        floorChangeTrigger.enabled = true;
        floorChangeTrigger.SendMessage("OnTriggerEnter", playerCollider, SendMessageOptions.DontRequireReceiver);
    }

    private void MonitorMovementStart()
    {
        float currentY = transform.position.y;
        if (Mathf.Abs(currentY - activationStartY) <= MovementThreshold)
        {
            return;
        }

        waitingForMovement = false;
        elevatorMoving = true;
        CarryPlayerWithElevator();
        previousY = currentY;
    }

    private void CarryPlayerWithElevator()
    {
        Vector3 currentElevatorPosition = transform.position;
        Vector3 elevatorDelta = currentElevatorPosition - previousElevatorPosition;
        previousElevatorPosition = currentElevatorPosition;

        if (playerTransform == null || elevatorDelta.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        if (playerCharacterController != null && playerCharacterController.enabled)
        {
            playerCharacterController.Move(elevatorDelta);
        }
        else
        {
            playerTransform.position += elevatorDelta;
        }
    }

    private void MonitorMovementEnd()
    {
        float currentY = transform.position.y;

        if (Mathf.Abs(currentY - previousY) <= PositionChangeThreshold)
        {
            settledFrames++;
        }
        else
        {
            settledFrames = 0;
        }

        previousY = currentY;

        if (settledFrames < SettledFramesRequired)
        {
            return;
        }

        elevatorMoving = false;
        DisableFloorChangeTrigger();
    }

    private void DisableFloorChangeTrigger()
    {
        if (floorChangeTrigger != null)
        {
            floorChangeTrigger.enabled = false;
        }
    }

    private static InputAction CreateButtonAction(string actionName, params string[] controlPaths)
    {
        InputAction action = new InputAction(actionName, InputActionType.Button);
        foreach (string controlPath in controlPaths)
        {
            action.AddBinding(controlPath);
        }
        return action;
    }

    private bool WasControllerButtonPressed()
    {
        foreach (InputAction action in controllerButtonActions)
        {
            if (action.WasPressedThisFrame())
            {
                return true;
            }
        }

        return false;
    }
}
