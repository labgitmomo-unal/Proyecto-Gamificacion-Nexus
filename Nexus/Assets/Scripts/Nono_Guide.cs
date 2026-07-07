using UnityEngine;

public class Nono_Guide : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource[] listenerAudioSources;

    [Header("Targets")]
    public Transform[] guideTargets;

    [Header("Flight")]
    public float flySpeed = 3f;

    private Animator animator;
    private int currentTargetIndex = -1;
    private bool audioWasPlaying = false;
    private bool isMoving = false;
    private Vector3 moveDestination;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = true;

        if (listenerAudioSources == null || listenerAudioSources.Length == 0)
            FindReferences();
    }

    [ContextMenu("Find References")]
    public void FindReferences()
    {
        var progreso = FindFirstObjectByType<ProgresoAbstraccion>();
        var challenge = FindFirstObjectByType<Challenge_Progress>();
        var sources = new System.Collections.Generic.List<AudioSource>();
        if (progreso != null && progreso.Explain_Challenge_1 != null)
            sources.Add(progreso.Explain_Challenge_1);
        if (progreso != null && progreso.Indicator_Challenge_2 != null)
            sources.Add(progreso.Indicator_Challenge_2);
        if (challenge != null && challenge.Explain_Challenge_2 != null)
            sources.Add(challenge.Explain_Challenge_2);
        listenerAudioSources = sources.ToArray();
        Debug.Log($"[Nono_Guide] {listenerAudioSources.Length} audio sources.");
    }

    void Update()
    {
        bool anyPlaying = IsAnyAudioPlaying();

        if (anyPlaying)
        {
            audioWasPlaying = true;
            if (isMoving) StopMoving();
        }
        else if (audioWasPlaying)
        {
            audioWasPlaying = false;
            MoveToNextTarget();
        }

        if (isMoving)
        {
            Vector3 dir = moveDestination - transform.position;
            float dist = dir.magnitude;

            if (dist < 0.3f)
            {
                transform.position = moveDestination;
                StopMoving();
            }
            else
            {
                transform.position += dir.normalized * Mathf.Min(flySpeed * Time.deltaTime, dist);

                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
    }

    private bool IsAnyAudioPlaying()
    {
        if (listenerAudioSources == null) return false;
        for (int i = 0; i < listenerAudioSources.Length; i++)
            if (listenerAudioSources[i] != null && listenerAudioSources[i].isPlaying)
                return true;
        return false;
    }

    private void MoveToNextTarget()
    {
        currentTargetIndex++;
        if (currentTargetIndex < guideTargets.Length && guideTargets[currentTargetIndex] != null)
        {
            moveDestination = guideTargets[currentTargetIndex].position;
            isMoving = true;
            if (animator != null) animator.enabled = false;
        }
    }

    private void StopMoving()
    {
        isMoving = false;
        if (animator != null) animator.enabled = true;
    }

    public void ForceNextTarget()
    {
        currentTargetIndex++;
        if (currentTargetIndex < guideTargets.Length && guideTargets[currentTargetIndex] != null)
        {
            moveDestination = guideTargets[currentTargetIndex].position;
            isMoving = true;
            if (animator != null) animator.enabled = false;
        }
    }

    public void ResetGuide()
    {
        currentTargetIndex = -1;
        audioWasPlaying = false;
        StopMoving();
    }
}
