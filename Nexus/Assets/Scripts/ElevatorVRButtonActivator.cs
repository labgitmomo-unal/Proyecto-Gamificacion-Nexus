using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public sealed class ElevatorVRButtonActivator : MonoBehaviour
{
    private const float MovementThreshold = 0.05f;
    private const float PositionChangeThreshold = 0.001f;
    private const float MovementStartTimeout = 5f;
    private const int SettledFramesRequired = 8;

    private InputAction[] controllerButtonActions;
    private FloorChangeTrigger floorChangeTrigger;
    private bool playerInside;
    private Collider playerCollider;
    private Transform playerTransform;
    private CharacterController playerCharacterController;
    private LocomotionProvider[] playerLocomotionProviders;
    private bool[] locomotionProviderStates;
    private bool characterControllerState;
    private bool waitingForMovement;
    private bool elevatorMoving;
    private bool playerAttached;
    private Transform previousPlayerParent;
    private Vector3 lockedLocalPosition;
    private float activationStartY;
    private float previousY;
    private float movementStartElapsed;
    private int settledFrames;

    private void Awake()
    {
        floorChangeTrigger = GetComponent<FloorChangeTrigger>();
        DisableFloorChangeTrigger();

        controllerButtonActions = new[]
        {
            CreateButtonAction("ElevatorLeftPrimary", "<XRController>{LeftHand}/primaryButton"),
            CreateButtonAction("ElevatorLeftSecondary", "<XRController>{LeftHand}/secondaryButton"),
            CreateButtonAction("ElevatorRightPrimary", "<XRController>{RightHand}/primaryButton"),
            CreateButtonAction("ElevatorRightSecondary", "<XRController>{RightHand}/secondaryButton")
        };

        foreach (InputAction action in controllerButtonActions)
        {
            action.Enable();
        }
    }

    private void OnDisable()
    {
        DisableFloorChangeTrigger();
        waitingForMovement = false;
        elevatorMoving = false;
        DetachPlayerFromElevator();
    }

    private void OnDestroy()
    {
        DetachPlayerFromElevator();

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

        bool buttonPressed = WasControllerButtonPressed();
        if (playerInside)
        {
            Debug.Log($"[Elevator] Update: playerInside=true, waitingForMovement={waitingForMovement}, elevatorMoving={elevatorMoving}, buttonPressed={buttonPressed}");
        }
        if (!playerInside || waitingForMovement || elevatorMoving || !buttonPressed)
        {
            return;
        }

        Debug.Log($"[Elevator] Button pressed! Activating elevator.");
        ActivateElevator();
    }

    private void LateUpdate()
    {
        if (!playerAttached || playerTransform == null)
        {
            return;
        }

        Vector3 localPosition = playerTransform.localPosition;
        if (Mathf.Abs(localPosition.x - lockedLocalPosition.x) <= PositionChangeThreshold &&
            Mathf.Abs(localPosition.z - lockedLocalPosition.z) <= PositionChangeThreshold)
        {
            return;
        }

        localPosition.x = lockedLocalPosition.x;
        localPosition.z = lockedLocalPosition.z;
        playerTransform.localPosition = localPosition;
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
        Debug.Log($"[Elevator] RegisterPlayer called. Tag={other.tag}, ComparePlayer={other.CompareTag("Player")}, rootTag={other.transform.root.tag}, GO={other.gameObject.name}");

        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = true;
        playerCollider = other;
        playerCharacterController = other.GetComponentInParent<CharacterController>();
        playerTransform = playerCharacterController != null ? playerCharacterController.transform : other.transform;
        Debug.Log($"[Elevator] Player registered. playerInside={playerInside}, collider={other.gameObject.name}");
    }

    private void ActivateElevator()
    {
        if (floorChangeTrigger == null || playerCollider == null || playerTransform == null)
        {
            Debug.Log($"[Elevator] ActivateElevator ABORTED: trigger={floorChangeTrigger != null}, collider={playerCollider != null}, transform={playerTransform != null}");
            return;
        }

        Debug.Log($"[Elevator] ActivateElevator: Enabling FloorChangeTrigger and sending OnTriggerEnter...");
        activationStartY = transform.position.y;
        previousY = activationStartY;
        movementStartElapsed = 0f;
        settledFrames = 0;
        waitingForMovement = true;

        AttachPlayerToElevator();
        floorChangeTrigger.enabled = true;
        floorChangeTrigger.SendMessage("OnTriggerEnter", playerCollider, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[Elevator] FloorChangeTrigger enabled, message sent. Elevator isMoving={GetComponent<Elevator>().IsMoving()}");
    }

    private void MonitorMovementStart()
    {
        movementStartElapsed += Time.deltaTime;
        float currentY = transform.position.y;

        if (Mathf.Abs(currentY - activationStartY) > MovementThreshold)
        {
            waitingForMovement = false;
            elevatorMoving = true;
            previousY = currentY;
            return;
        }

        if (movementStartElapsed < MovementStartTimeout)
        {
            return;
        }

        waitingForMovement = false;
        DisableFloorChangeTrigger();
        DetachPlayerFromElevator();
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
        DetachPlayerFromElevator();
    }

    private void AttachPlayerToElevator()
    {
        if (playerAttached || playerTransform == null)
        {
            return;
        }

        previousPlayerParent = playerTransform.parent;
        playerLocomotionProviders = playerTransform.GetComponentsInChildren<LocomotionProvider>(true);
        locomotionProviderStates = new bool[playerLocomotionProviders.Length];

        for (int i = 0; i < playerLocomotionProviders.Length; i++)
        {
            locomotionProviderStates[i] = playerLocomotionProviders[i].enabled;
            playerLocomotionProviders[i].enabled = false;
        }

        if (playerCharacterController != null)
        {
            characterControllerState = playerCharacterController.enabled;
            playerCharacterController.enabled = false;
        }

        playerTransform.SetParent(transform, true);
        lockedLocalPosition = playerTransform.localPosition;
        playerAttached = true;
    }

    private void DetachPlayerFromElevator()
    {
        if (playerAttached && playerTransform != null)
        {
            playerTransform.SetParent(previousPlayerParent, true);
        }

        if (playerLocomotionProviders != null && locomotionProviderStates != null)
        {
            int count = Mathf.Min(playerLocomotionProviders.Length, locomotionProviderStates.Length);
            for (int i = 0; i < count; i++)
            {
                if (playerLocomotionProviders[i] != null)
                {
                    playerLocomotionProviders[i].enabled = locomotionProviderStates[i];
                }
            }
        }

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = characterControllerState;
        }

        playerAttached = false;
        previousPlayerParent = null;
        lockedLocalPosition = Vector3.zero;
        playerLocomotionProviders = null;
        locomotionProviderStates = null;
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
        if (controllerButtonActions == null)
        {
            Debug.Log("[Elevator] WasControllerButtonPressed: controllerButtonActions is NULL!");
            return false;
        }

        foreach (InputAction action in controllerButtonActions)
        {
            if (action.WasPressedThisFrame())
            {
                Debug.Log($"[Elevator] Button detected: {action.name}");
                return true;
            }
        }

        return false;
    }
}
