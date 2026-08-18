using UnityEngine;

public sealed class ElevatorAudioTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource elevatorAudio;

    private bool playerInside;

    private void OnTriggerEnter(Collider other)
    {
        if (playerInside || !other.CompareTag("Player") || elevatorAudio == null)
        {
            return;
        }

        playerInside = true;
        elevatorAudio.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }
}
