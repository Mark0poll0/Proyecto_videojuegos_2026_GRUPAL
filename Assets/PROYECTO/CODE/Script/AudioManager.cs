using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Componentes de Audio")]
    [SerializeField] private AudioSource musicSource;

    [Header("Clips de Música")]
    [SerializeField] private AudioClip backgroundMusic;

    private void Awake()
    {
        // Patrón Singleton: Asegura que solo exista un AudioManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Hace que el objeto no se destruya al cambiar de escena
        }
        else
        {
            Destroy(gameObject); // Si ya existe uno, destruye el duplicado
            return;
        }
    }

    private void Start()
    {
        // Si hay una fuente y un clip configurado, reproduce la música al iniciar
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Cambia la música de fondo actual por una nueva canción.
    /// </summary>
    /// <param name="newClip">El nuevo clip de audio.</param>
    public void ChangeBackgroundMusic(AudioClip newClip)
    {
        if (musicSource == null) return;

        if (musicSource.clip == newClip) return; // Ya se está reproduciendo este clip

        musicSource.Stop();
        musicSource.clip = newClip;
        if (newClip != null)
        {
            musicSource.Play();
        }
    }

    /// <summary>
    /// Pausa la música de fondo.
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Reanuda la música de fondo si estaba pausada.
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    /// <summary>
    /// Ajusta el volumen de la música de fondo (valor de 0 a 1).
    /// </summary>
    /// <param name="volume">Volumen entre 0.0f y 1.0f.</param>
    public void SetVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }
}
