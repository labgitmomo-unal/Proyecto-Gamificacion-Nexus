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
        FlyingToFinalDestination
    }

    private ElevatorSequencePhase elevatorPhase = ElevatorSequencePhase.None;
    private Transform seqElevator;
    private Transform seqElevatorTopPoint;
    private Transform seqFinalDestination;
    private float lastElevatorY;
    private float elevatorTopY;
    private bool prevAutoListen;

    private bool audioWasPlaying = false;
    private bool hasMoved = false;
    private bool isMoving = false;
    private Vector3 moveDestination;
    private float idleY;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        idleY = transform.position.y;

        if (autoListen && (audioSources == null || audioSources.Count == 0))
            FindReferences();

        if (string.IsNullOrEmpty(autoTargetName))
            autoTargetName = "Target_Panel_1";

        if (lookAtTarget == null)
        {
            var go = GameObject.Find("Nono_Facing_Target");
            if (go != null) lookAtTarget = go.transform;
        }
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
            audioSources.Add(challenge.Explain_Challenge_2);
        var cinematic = FindFirstObjectByType<Cinematic_1_Controller>();
        if (cinematic != null && cinematic.Challenge_Indicator_1 != null)
            audioSources.Add(cinematic.Challenge_Indicator_1);
    }

    void Update()
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

        // Elevator sequence tracking — takes priority over idle and normal movement
        if (elevatorPhase == ElevatorSequencePhase.WaitingForElevatorMovement)
        {
            if (seqElevator == null)
            {
                CancelElevatorSequence();
                return;
            }

            float currentElevatorY = seqElevator.position.y;
            float deltaY = currentElevatorY - lastElevatorY;
            if (Mathf.Abs(deltaY) > elevatorMovementThreshold)
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
            float deltaY = currentElevatorY - lastElevatorY;

            Vector3 pos = transform.position;
            pos.y += deltaY;
            pos.y = Mathf.Min(pos.y, elevatorTopY);
            transform.position = pos;

            lastElevatorY = currentElevatorY;

            float nonoDistToP2 = Mathf.Abs(transform.position.y - seqElevatorTopPoint.position.y);
            float elevatorDistToP2 = Mathf.Abs(seqElevator.position.y - seqElevatorTopPoint.position.y);

            if (nonoDistToP2 < elevatorArrivalThreshold && elevatorDistToP2 < elevatorArrivalThreshold)
            {
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
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), Time.deltaTime * rotateSpeed);
            }
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y = idleY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = pos;

            if (lookAtTarget != null)
            {
                Vector3 lookDir = lookAtTarget.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized, Vector3.up), Time.deltaTime * rotateSpeed);
            }
        }
    }

    private bool IsAnyAudioPlaying()
    {
        if (audioSources == null) return false;
        for (int i = 0; i < audioSources.Count; i++)
            if (audioSources[i] != null && audioSources[i].isPlaying)
                return true;
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
        seqElevatorTopPoint = elevatorTopPoint;
        seqFinalDestination = finalDestination;
        elevatorTopY = elevatorTopPoint.position.y;

        prevAutoListen = autoListen;
        autoListen = false;

        elevatorPhase = ElevatorSequencePhase.FlyingToBoardingPoint;
        OnArrived += HandleElevatorSequenceArrival;

        FlyTo(boardingPoint);
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
                lastElevatorY = seqElevator.position.y;
                elevatorPhase = ElevatorSequencePhase.WaitingForElevatorMovement;
                break;

            case ElevatorSequencePhase.FlyingToFinalDestination:
                elevatorPhase = ElevatorSequencePhase.None;
                autoListen = prevAutoListen;
                seqElevator = null;
                seqElevatorTopPoint = null;
                seqFinalDestination = null;
                OnArrived -= HandleElevatorSequenceArrival;
                break;
        }
    }

    private void CancelElevatorSequence()
    {
        elevatorPhase = ElevatorSequencePhase.None;
        autoListen = prevAutoListen;
        seqElevator = null;
        seqElevatorTopPoint = null;
        seqFinalDestination = null;
        OnArrived -= HandleElevatorSequenceArrival;
    }

    void OnDisable()
    {
        if (elevatorPhase != ElevatorSequencePhase.None)
        {
            CancelElevatorSequence();
        }
    }
}
