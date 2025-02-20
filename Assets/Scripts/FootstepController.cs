using UnityEngine;

public class FootstepController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource leftFootstepAudio;
    public AudioSource rightFootstepAudio;

    void Awake()
    {
        // No need for Instance setup
    }

    public void PlayLeftFootstep()
    {
        if (GameManager.Instance.HasGameStarted && !GameManager.Instance.IsPaused && leftFootstepAudio != null)
        {
            leftFootstepAudio.pitch = Random.Range(0.95f, 1.05f);
            leftFootstepAudio.Play();
        }
    }

    public void PlayRightFootstep()
    {
        if (GameManager.Instance.HasGameStarted && !GameManager.Instance.IsPaused && rightFootstepAudio != null)
        {
            rightFootstepAudio.pitch = Random.Range(0.95f, 1.05f);
            rightFootstepAudio.Play();
        }
    }
} 