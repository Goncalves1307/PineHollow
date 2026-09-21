using UnityEngine;

// Põe-se no objecto que se pisa. Sem isto, o FootstepSystem usa os clips por
// omissão dele.
//
// Não usa tags: o projecto não tem tags personalizadas por decisão, e a matriz
// de layers não serve para isto.
public class SurfaceAudio : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips;

    [Tooltip("Multiplica o volume do passo nesta superfície.")]
    [SerializeField] private float volumeScale = 1f;

    public float VolumeScale => volumeScale;

    public bool HasClips => footstepClips != null && footstepClips.Length > 0;

    public AudioClip GetRandomClip()
    {
        if (!HasClips)
            return null;

        return footstepClips[Random.Range(0, footstepClips.Length)];
    }
}
