using UnityEngine;
using UnityEngine.AI;

public class Nono_Guide : MonoBehaviour
{
    [Header("Movement")]
    public NavMeshAgent agent;
    public float rotationSpeed = 5f;

    [Header("Targets (orden: Panel_1, Panel_2, ...)")]
    public Transform[] guideTargets;

    [Header("Audio Sources")]
    public AudioSource[] listenerAudioSources;

    [Header("Player")]
    public Transform playerCamera;

    [Header("Positioning")]
    public float distanceFromPlayer = 2.5f;
    public float sideOffset = 1.2f;

    private int currentTargetIndex = -1;
    private bool audioWasPlaying = false;
    private bool waitingAtTarget = false;

    private enum State { IdleFollow, Presenting, Guiding }
    private State currentState = State.IdleFollow;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (listenerAudioSources == null || listenerAudioSources.Length == 0)
            FindReferences();
    }

    [ContextMenu("Find References")]
    public void FindReferences()
    {
        var progreso = FindFirstObjectByType<ProgresoAbstraccion>();
        var challenge = FindFirstObjectByType<Challenge_Progress>();

        System.Collections.Generic.List<AudioSource> sources = new System.Collections.Generic.List<AudioSource>();
        if (progreso != null && progreso.Explain_Challenge_1 != null)
            sources.Add(progreso.Explain_Challenge_1);
        if (progreso != null && progreso.Indicator_Challenge_2 != null)
            sources.Add(progreso.Indicator_Challenge_2);
        if (challenge != null && challenge.Explain_Challenge_2 != null)
            sources.Add(challenge.Explain_Challenge_2);
        listenerAudioSources = sources.ToArray();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        Debug.Log($"[Nono_Guide] Auto-encontrados {listenerAudioSources.Length} audio sources.");
    }

    void Update()
    {
        bool anyPlaying = IsAnyAudioPlaying();

        if (anyPlaying)
        {
            if (!audioWasPlaying)
                OnAudioStarted();

            currentState = State.Presenting;
            agent.isStopped = true;
            FacePlayer();
            audioWasPlaying = true;
            waitingAtTarget = false;
        }
        else
        {
            if (audioWasPlaying)
            {
                OnAudioStopped();
                audioWasPlaying = false;
            }

            switch (currentState)
            {
                case State.Guiding:
                    if (HasReachedDestination())
                    {
                        currentState = State.IdleFollow;
                        waitingAtTarget = true;
                    }
                    break;

                case State.IdleFollow:
                    if (!waitingAtTarget)
                        FollowPlayer();
                    break;
            }
        }
    }

    private bool IsAnyAudioPlaying()
    {
        if (listenerAudioSources == null) return false;
        for (int i = 0; i < listenerAudioSources.Length; i++)
        {
            if (listenerAudioSources[i] != null && listenerAudioSources[i].isPlaying)
                return true;
        }
        return false;
    }

    private void OnAudioStarted()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    private void OnAudioStopped()
    {
        currentTargetIndex++;
        if (currentTargetIndex < guideTargets.Length && guideTargets[currentTargetIndex] != null)
        {
            agent.isStopped = false;
            agent.SetDestination(guideTargets[currentTargetIndex].position);
            currentState = State.Guiding;
        }
        else
        {
            currentState = State.IdleFollow;
        }
    }

    private void FacePlayer()
    {
        if (playerCamera == null) return;
        Vector3 dir = playerCamera.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }

    private void FollowPlayer()
    {
        if (playerCamera == null) return;
        agent.isStopped = false;

        Vector3 targetPos = playerCamera.position
            + playerCamera.forward * distanceFromPlayer
            + playerCamera.right * sideOffset;

        agent.SetDestination(targetPos);
    }

    private bool HasReachedDestination()
    {
        if (!agent.hasPath && !agent.pathPending) return true;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;
        return agent.remainingDistance <= agent.stoppingDistance;
    }

    public void ForceNextTarget()
    {
        currentTargetIndex++;
        if (currentTargetIndex < guideTargets.Length && guideTargets[currentTargetIndex] != null)
        {
            agent.isStopped = false;
            agent.SetDestination(guideTargets[currentTargetIndex].position);
            currentState = State.Guiding;
        }
    }

    public void ResetGuide()
    {
        currentTargetIndex = -1;
        currentState = State.IdleFollow;
        waitingAtTarget = false;
        audioWasPlaying = false;
        agent.ResetPath();
    }
}
