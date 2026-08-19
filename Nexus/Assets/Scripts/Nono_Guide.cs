using UnityEngine;
using System;
using System.Collections.Generic;

public class Nono_Guide : MonoBehaviour
{
    [Header("Auto-flight (audio trigger)")]
    public bool autoListen = true;
    public List<AudioSource> audioSources;

    [Header("Graph introduction")]
    public AudioSource graphIntroAudio;
    public string autoTargetName = "Target_Panel_1";

    [Header("Look-at when idle")]
    public bool facePlayer = true;
    public Transform lookAtTarget;
    public float rotateSpeed = 3f;

    [Header("Flight")]
    public float flySpeed = 3f;

    [Header("Float")]
    public float floatAmplitude = 0.05f;
    public float floatFrequency = 1.5f;

    public static Nono_Guide Instance { get; private set; }
    public bool IsMoving => isMoving;
    public bool IsElevatorSequenceActive => elevatorPhase != ElevatorSequencePhase.None;
    public event Action OnArrived;

    [Header("Elevator Sequence")]
    [SerializeField] private float elevatorMovementThreshold = 0.05f;
    [SerializeField] private float elevatorArrivalThreshold = 0.5f;

    private enum ElevatorSequencePhase
    {
        None,
        FlyingToBoardingPoint,
        WaitingForElevatorMovement,
        FollowingElevator,
        FlyingToFinalDestination,
        ReturningToElevatorTop,
        WaitingForElevatorDescent,
        FollowingElevatorDown,
        ReturningToBoardingPoint
    }

    private ElevatorSequencePhase elevatorPhase = ElevatorSequencePhase.None;
    private Transform seqElevator;
    private Transform seqBoardingPoint;
    private Transform seqElevatorTopPoint;
    private Transform seqFinalDestination;
    private float lastElevatorY;
    private float elevatorTopY;
    private float elevatorBottomY;
    private bool hasReachedGraphDestination;
    private bool prevAutoListen;
    private bool isAttachedToElevator;

    private bool audioWasPlaying;
    private bool hasMoved;
    private bool isMoving;
    private Vector3 moveDestination;
    private float idleY;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        idleY = transform.position.y;

        if (autoListen && (audioSources == null || audioSources.Count == 0))
        {
            FindReferences();
        }

        if (string.IsNullOrEmpty(autoTargetName))
        {
            autoTargetName = "Target_Panel_1";
        }

        ResolvePlayerLookTarget();
    }

    [ContextMenu("Find References")]
    public void FindReferences()
    {
        audioSources = new List<AudioSource>();
        var progreso = FindFirstObjectByType<ProgresoAbstraccion>();
        if (progreso != null)
        {
            if (progreso.Explain_Challenge_1 != null) audioSources.Add(progreso.Explain_Challenge_1);
            if (progreso.Indicator_Challenge_2 != null) audioSources.Add(progreso.Indicator_Challenge_2);
        }

        var challenge = FindFirstObjectByType<Challenge_Progress>();
        if (challenge != null && challenge.Explain_Challenge_2 != null)
        {
            audioSources.Add(challenge.Explain_Challenge_2);
        }

        var cinematic = FindFirstObjectByType<Cinematic_1_Controller>();
        if (cinematic != null && cinematic.Challenge_Indicator_1 != null)
        {
            audioSources.Add(cinematic.Challenge_Indicator_1);
        }
    }

    private void Update()
    {
        if (autoListen && !hasMoved)
        {
            bool isPlaying = IsAnyAudioPlaying();

            if (isPlaying)
            {
                audioWasPlaying = true;
            }
            else if (audioWasPlaying)
            {
                audioWasPlaying = false;
                MoveToAutoTarget();
            }
        }

        ResolvePlayerLookTarget();

        if (isAttachedToElevator)
        {
            RotateTowardsLookAtTarget();
        }

        if (elevatorPhase == ElevatorSequencePhase.WaitingForElevatorMovement)
        {
            if (seqElevator == null)
            {
                CancelElevatorSequence();
                return;
            }

            float currentElevatorY = seqElevator.position.y;
            float elevatorDisplacement = currentElevatorY - elevatorBottomY;
            if (elevatorDisplacement > elevatorMovementThreshold)
            {
                elevatorPhase = ElevatorSequencePhase.FollowingElevator;
                lastElevatorY = currentElevatorY;
            }
            return;
        }

        if (elevatorPhase == ElevatorSequencePhase.FollowingElevator)
        {
            if (seqElevator == null)
            {
                CancelElevatorSequence();
                return;
            }

            float currentElevatorY = seqElevator.position.y;
            lastElevatorY = currentElevatorY;

            float elevatorDistToP2 = Mathf.Abs(currentElevatorY - (seqElevatorTopPoint.position.y - (seqBoardingPoint.position.y - elevatorBottomY)));
            if (elevatorDistToP2 < elevatorArrivalThreshold)
            {
                DetachFromElevator();
                idleY = transform.position.y;
                autoListen = prevAutoListen;
                elevatorPhase = ElevatorSequencePhase.FlyingToFinalDestination;
                hasMoved = true;
                if (graphIntroAudio != null)
                {
                    graphIntroAudio.Play();
                }
                FlyTo(seqFinalDestination);
            }
            return;
        }

        if (elevatorPhase == ElevatorSequencePhase.WaitingForElevatorDescent)
        {
            if (seqElevator == null)
            {
                CancelElevatorSequence();
                return;
            }

            float currentElevatorY = seqElevator.position.y;
            float deltaY = currentElevatorY - lastElevatorY;
            if (deltaY < -elevatorMovementThreshold)
            {
                elevatorPhase = ElevatorSequencePhase.FollowingElevatorDown;
                lastElevatorY = currentElevatorY;
            }
            return;
        }

        if (elevatorPhase == ElevatorSequencePhase.FollowingElevatorDown)
        {
            if (seqElevator == null || seqBoardingPoint == null)
            {
                CancelElevatorSequence();
                return;
            }

            float currentElevatorY = seqElevator.position.y;
            lastElevatorY = currentElevatorY;

            if (Mathf.Abs(currentElevatorY - elevatorBottomY) < elevatorArrivalThreshold)
            {
                DetachFromElevator();
                idleY = transform.position.y;
                elevatorPhase = ElevatorSequencePhase.ReturningToBoardingPoint;
                FlyTo(seqBoardingPoint);
            }
            return;
        }

        if (isMoving)
        {
            Vector3 dir = moveDestination - transform.position;
            float dist = dir.magnitude;

            if (dist < 0.3f)
            {
                transform.position = moveDestination;
                idleY = moveDestination.y;
                StopMoving();
                OnArrived?.Invoke();
            }
            else
            {
                transform.position += dir.normalized * Mathf.Min(flySpeed * Time.deltaTime, dist);
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), Time.deltaTime * rotateSpeed);
                }
            }
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = idleY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = pos;

            RotateTowardsLookAtTarget();
        }
    }

    private void ResolvePlayerLookTarget()
    {
        if (!facePlayer)
        {
            if (lookAtTarget == null)
            {
                var fallback = GameObject.Find("Nono_Facing_Target");
                if (fallback != null)
                {
                    lookAtTarget = fallback.transform;
                }
            }
            return;
        }

        Camera playerCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (playerCamera != null)
        {
            lookAtTarget = playerCamera.transform;
        }
    }

    private void RotateTowardsLookAtTarget()
    {
        if (lookAtTarget == null) return;

        Vector3 lookDir = lookAtTarget.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude <= 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotateSpeed);
    }

    private bool IsAnyAudioPlaying()
    {
        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i] != null && audioSources[i].isPlaying)
            {
                return true;
            }
        }
        return false;
    }

    private void MoveToAutoTarget()
    {
        var obj = GameObject.Find(autoTargetName);
        if (obj == null)
        {
            Debug.LogWarning($"[Nono_Guide] Auto-target '{autoTargetName}' not found");
            return;
        }
        hasMoved = true;
        FlyTo(obj.transform);
    }

    public void FlyTo(Transform target)
    {
        if (target == null) return;
        moveDestination = target.position;
        isMoving = true;
        Debug.Log($"[Nono_Guide] Flying to {target.name}");
    }

    public void FlyToPosition(Vector3 position)
    {
        moveDestination = position;
        isMoving = true;
    }

    public void DisableAutoListen()
    {
        autoListen = false;
    }

    public void EnableAutoListen()
    {
        autoListen = true;
    }

    public void ResetAutoTrigger()
    {
        audioWasPlaying = false;
        hasMoved = false;
    }

    private void StopMoving()
    {
        isMoving = false;
    }

    public void StartElevatorSequence(Transform elevator, Transform boardingPoint, Transform elevatorTopPoint, Transform finalDestination)
    {
        if (elevatorPhase != ElevatorSequencePhase.None)
        {
            Debug.LogWarning("[Nono_Guide] Elevator sequence already active, ignoring call.");
            return;
        }

        if (elevator == null || boardingPoint == null || elevatorTopPoint == null || finalDestination == null)
        {
            Debug.LogWarning("[Nono_Guide] Cannot start elevator sequence: one or more references are null.");
            return;
        }

        seqElevator = elevator;
        seqBoardingPoint = boardingPoint;
        seqElevatorTopPoint = elevatorTopPoint;
        seqFinalDestination = finalDestination;
        elevatorBottomY = elevator.position.y;
        elevatorTopY = elevatorTopPoint.position.y;
        hasReachedGraphDestination = false;

        prevAutoListen = autoListen;
        autoListen = false;
        elevatorPhase = ElevatorSequencePhase.FlyingToBoardingPoint;
        OnArrived += HandleElevatorSequenceArrival;
        FlyTo(boardingPoint);
    }

    /// <summary>Returns Nono to the elevator, follows its descent, and finishes at the boarding point.</summary>
    public void StartReturnElevatorSequence(Transform elevator, Transform boardingPoint, Transform elevatorTopPoint)
    {
        if (elevatorPhase != ElevatorSequencePhase.None)
        {
            Debug.LogWarning("[Nono_Guide] Cannot start return sequence while another sequence is active.");
            return;
        }

        if (!hasReachedGraphDestination)
        {
            Debug.LogWarning("[Nono_Guide] Cannot start return sequence before reaching the graph destination.");
            return;
        }

        if (elevator == null || boardingPoint == null || elevatorTopPoint == null)
        {
            Debug.LogWarning("[Nono_Guide] Cannot start return sequence: one or more references are null.");
            return;
        }

        seqElevator = elevator;
        seqBoardingPoint = boardingPoint;
        seqElevatorTopPoint = elevatorTopPoint;
        seqFinalDestination = null;
        elevatorTopY = elevatorTopPoint.position.y;

        prevAutoListen = autoListen;
        autoListen = false;
        elevatorPhase = ElevatorSequencePhase.ReturningToElevatorTop;
        OnArrived += HandleElevatorSequenceArrival;
        FlyTo(seqElevatorTopPoint);
    }

    private void AttachToElevator()
    {
        if (seqElevator == null || isAttachedToElevator) return;

        transform.SetParent(seqElevator, true);
        isAttachedToElevator = true;
        Debug.Log("[Nono_Guide] Nono attached to elevator for synchronized movement.");
    }

    private void DetachFromElevator()
    {
        if (!isAttachedToElevator) return;

        transform.SetParent(null, true);
        isAttachedToElevator = false;
        Debug.Log("[Nono_Guide] Nono detached from elevator at upper reference.");
    }

    private void HandleElevatorSequenceArrival()
    {
        switch (elevatorPhase)
        {
            case ElevatorSequencePhase.FlyingToBoardingPoint:
                if (seqElevator == null)
                {
                    CancelElevatorSequence();
                    return;
                }
                AttachToElevator();
                lastElevatorY = seqElevator.position.y;
                elevatorPhase = ElevatorSequencePhase.WaitingForElevatorMovement;
                break;

            case ElevatorSequencePhase.FlyingToFinalDestination:
                hasReachedGraphDestination = true;
                elevatorPhase = ElevatorSequencePhase.None;
                autoListen = prevAutoListen;
                seqElevator = null;
                seqBoardingPoint = null;
                seqElevatorTopPoint = null;
                seqFinalDestination = null;
                OnArrived -= HandleElevatorSequenceArrival;
                break;

            case ElevatorSequencePhase.ReturningToElevatorTop:
                if (seqElevator == null)
                {
                    CancelElevatorSequence();
                    return;
                }
                AttachToElevator();
                lastElevatorY = seqElevator.position.y;
                elevatorPhase = ElevatorSequencePhase.WaitingForElevatorDescent;
                break;

            case ElevatorSequencePhase.ReturningToBoardingPoint:
                hasReachedGraphDestination = false;
                elevatorPhase = ElevatorSequencePhase.None;
                autoListen = prevAutoListen;
                seqElevator = null;
                seqBoardingPoint = null;
                seqElevatorTopPoint = null;
                seqFinalDestination = null;
                OnArrived -= HandleElevatorSequenceArrival;
                break;
        }
    }

    private void CancelElevatorSequence()
    {
        DetachFromElevator();
        elevatorPhase = ElevatorSequencePhase.None;
        autoListen = prevAutoListen;
        seqElevator = null;
        seqBoardingPoint = null;
        seqElevatorTopPoint = null;
        seqFinalDestination = null;
        OnArrived -= HandleElevatorSequenceArrival;
    }

    private void OnDisable()
    {
        if (elevatorPhase != ElevatorSequencePhase.None)
        {
            CancelElevatorSequence();
        }
    }
}
