using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ElevatorVRButtonActivator : MonoBehaviour
{
    private const float MovementThreshold = 0.05f;
    private const float PositionChangeThreshold = 0.001f;
    private const float DescentHorizontalCorrectionThreshold = 0.01f;
    private const int SettledFramesRequired = 8;

    private InputAction[] controllerButtonActions;
    private FloorChangeTrigger floorChangeTrigger;
    private bool playerInside;
    private Collider playerCollider;
    private Transform playerTransform;
    private bool waitingForMovement;
    private bool elevatorMoving;
    private bool correctingDescent;
    private Vector3 descentLocalPlayerPosition;
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
            CreateButtonAction("ElevatorLeftPrimary", "<XRController>{LeftHand}/primaryButton", "<XRController>{LeftHand}/{PrimaryButton}", "<XRController>{LeftHand}/{PrimaryAction}", "<XRController>/{PrimaryAction}"),
            CreateButtonAction("ElevatorLeftSecondary", "<XRController>{LeftHand}/secondaryButton", "<XRController>{LeftHand}/{SecondaryButton}", "<XRController>/secondaryButton"),
            CreateButtonAction("ElevatorRightPrimary", "<XRController>{RightHand}/primaryButton", "<XRController>{RightHand}/{PrimaryButton}", "<XRController>{RightHand}/{PrimaryAction}", "<XRController>/{PrimaryAction}"),
            CreateButtonAction("ElevatorRightSecondary", "<XRController>{RightHand}/secondaryButton", "<XRController>{RightHand}/{SecondaryButton}", "<XRController>/secondaryButton")
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
            MonitorMovementEnd();
        }

        if (!playerInside || waitingForMovement || elevatorMoving || !WasControllerButtonPressed())
        {
            return;
        }

        ActivateOriginalElevatorTrigger();
    }

    private void LateUpdate()
    {
        CorrectDescentHorizontalDrift();
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
                correctingDescent = false;
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
        CharacterController characterController = other.GetComponentInParent<CharacterController>();
        playerTransform = characterController != null ? characterController.transform : other.transform;
    }

    private void ActivateOriginalElevatorTrigger()
    {
        if (floorChangeTrigger == null || playerCollider == null)
        {
            return;
        }

        activationStartY = transform.position.y;
        previousY = activationStartY;
        settledFrames = 0;
        waitingForMovement = true;
        correctingDescent = false;
        floorChangeTrigger.enabled = true;
        floorChangeTrigger.SendMessage("OnTriggerEnter", playerCollider, SendMessageOptions.DontRequireReceiver);
    }

    private void MonitorMovementStart()
    {
        float verticalDisplacement = transform.position.y - activationStartY;
        if (Mathf.Abs(verticalDisplacement) <= MovementThreshold)
        {
            return;
        }

        waitingForMovement = false;
        elevatorMoving = true;
        previousY = transform.position.y;

        if (verticalDisplacement < 0f && playerTransform != null)
        {
            descentLocalPlayerPosition = transform.InverseTransformPoint(playerTransform.position);
            correctingDescent = true;
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
        correctingDescent = false;
        DisableFloorChangeTrigger();
    }

    private void CorrectDescentHorizontalDrift()
    {
        if (!correctingDescent || playerTransform == null)
        {
            return;
        }

        Vector3 expectedPosition = transform.TransformPoint(new Vector3(
            descentLocalPlayerPosition.x,
            0f,
            descentLocalPlayerPosition.z));
        Vector3 currentPosition = playerTransform.position;
        Vector2 horizontalError = new Vector2(
            expectedPosition.x - currentPosition.x,
            expectedPosition.z - currentPosition.z);

        if (horizontalError.sqrMagnitude <= DescentHorizontalCorrectionThreshold * DescentHorizontalCorrectionThreshold)
        {
            return;
        }

        currentPosition.x = expectedPosition.x;
        currentPosition.z = expectedPosition.z;
        playerTransform.position = currentPosition;
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
