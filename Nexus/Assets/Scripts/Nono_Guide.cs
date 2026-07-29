using UnityEngine;
using System;
using System.Collections.Generic;

public class Nono_Guide : MonoBehaviour
{
    [Header("Auto-flight (audio trigger)")]
    public bool autoListen = true;
    public List<AudioSource> audioSources;
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
    public event Action OnArrived;

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
}
