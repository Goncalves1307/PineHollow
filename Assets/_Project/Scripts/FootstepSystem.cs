using UnityEngine;

// Passos por distância percorrida, não por temporizador: assim o ritmo segue a
// velocidade real e não desliza quando se acelera ou trava.
//
// As distâncias estão calibradas para uma pisada por meio ciclo do head bob do
// PlayerController, às velocidades por omissão: andar pi/8 rad a 3 m/s ≈ 1.2 m,
// sprint pi/11 a 5 m/s ≈ 1.4 m, agachado pi/5 a 1.5 m/s ≈ 0.95 m. Se mexeres
// nas velocidades ou nas frequências do bob, isto sai do sítio — e ouve-se.
//
// O projecto ainda não tem um único clip de áudio (Assets/_Project/Audio/ está
// vazia). Sem clips isto corre em silêncio e não estoira — a estrutura fica
// pronta para quando os assets existirem.
[RequireComponent(typeof(AudioSource))]
public class FootstepSystem : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private PlayerStateMachine stateMachine;

    [Header("Fallback clips")]
    [Tooltip("Usados quando o que se pisa não tem SurfaceAudio.")]
    [SerializeField] private AudioClip[] defaultFootstepClips;

    [Header("Step distance")]
    [Tooltip("Uma pisada é meio ciclo do head bob — ver o comentário do topo.")]
    [SerializeField] private float walkStepDistance = 1.2f;
    [SerializeField] private float sprintStepDistance = 1.4f;
    [SerializeField] private float crouchStepDistance = 0.95f;

    [Header("Volume")]
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float sprintVolume = 0.75f;
    [SerializeField] private float crouchVolume = 0.25f;

    [Header("Pitch")]
    [SerializeField] private float minPitch = 0.92f;
    [SerializeField] private float maxPitch = 1.08f;

    [Header("Ground check")]
    [SerializeField] private float groundCheckDistance = 1.6f;

    private float travelled;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (footstepSource == null)
            footstepSource = GetComponent<AudioSource>();

        // O RequireComponent só actua quando alguém adiciona este componente
        // pelo editor. A cena foi montada à mão, por isso garante-se aqui —
        // senão os passos ficavam mudos mesmo depois de haver clips.
        if (footstepSource == null)
            footstepSource = gameObject.AddComponent<AudioSource>();

        if (footstepSource != null)
        {
            // Os passos são um efeito pontual: nada de tocar sozinho ao
            // arrancar nem em ciclo.
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;

            // Passos são do próprio jogador: 2D, sem atenuação por distância.
            footstepSource.spatialBlend = 0f;
        }
    }

    private void Update()
    {
        if (characterController == null)
            return;

        bool locked =
            stateMachine != null && stateMachine.IsMovementLocked;

        if (locked || !characterController.isGrounded)
        {
            travelled = 0f;

            return;
        }

        Vector3 horizontal = characterController.velocity;
        horizontal.y = 0f;

        float speed = horizontal.magnitude;

        if (speed < 0.1f)
        {
            travelled = 0f;

            return;
        }

        travelled += speed * Time.deltaTime;

        if (travelled < StepDistance())
            return;

        travelled = 0f;

        PlayStep();
    }

    private float StepDistance()
    {
        if (stateMachine == null)
            return walkStepDistance;

        switch (stateMachine.Current)
        {
            case PlayerState.Crouching:
                return crouchStepDistance;

            case PlayerState.Sprinting:
                return sprintStepDistance;

            default:
                return walkStepDistance;
        }
    }

    private float StepVolume()
    {
        if (stateMachine == null)
            return walkVolume;

        switch (stateMachine.Current)
        {
            case PlayerState.Crouching:
                return crouchVolume;

            case PlayerState.Sprinting:
                return sprintVolume;

            default:
                return walkVolume;
        }
    }

    private void PlayStep()
    {
        if (footstepSource == null)
            return;

        SurfaceAudio surface = FindSurface();

        AudioClip clip = surface != null
            ? surface.GetRandomClip()
            : null;

        if (clip == null)
            clip = RandomDefaultClip();

        // Ainda não há clips no projecto: até os haver, isto não toca nada.
        if (clip == null)
            return;

        float volume = StepVolume();

        if (surface != null)
            volume *= surface.VolumeScale;

        footstepSource.pitch = Random.Range(minPitch, maxPitch);

        footstepSource.PlayOneShot(clip, volume);
    }

    private SurfaceAudio FindSurface()
    {
        Vector3 origin =
            transform.position + Vector3.up * 0.1f;

        // Sem layer mask de propósito: é da FASE 3. Aqui só se ignoram os
        // triggers, que o projecto apanharia por omissão.
        if (!Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit hit,
                groundCheckDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return null;
        }

        return hit.collider.GetComponent<SurfaceAudio>();
    }

    private AudioClip RandomDefaultClip()
    {
        if (defaultFootstepClips == null ||
            defaultFootstepClips.Length == 0)
        {
            return null;
        }

        return defaultFootstepClips[
            Random.Range(0, defaultFootstepClips.Length)];
    }
}
