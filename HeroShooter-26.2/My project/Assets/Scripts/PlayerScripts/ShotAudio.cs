using UnityEngine;

// Toca o som do tiro em 3D na posição de quem atirou.
// Vai no prefab do player E dos inimigos (junto do HeroCombat).
[RequireComponent(typeof(HeroCombat))]
public class ShotAudio : MonoBehaviour
{
    [Header("Sons")]
    public AudioClip[] shotClips;           // vários clips = variação a cada tiro
    [Range(0f, 1f)] public float volume = 0.8f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Áudio espacial")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 1 = totalmente 3D
    public float minDistance = 3f;   // até aqui o volume é máximo
    public float maxDistance = 60f;  // além daqui não se ouve mais

    private HeroCombat combat;
    private AudioSource source;

    void Awake()
    {
        combat = GetComponent<HeroCombat>();

        // O AudioSource é criado e configurado por código.
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }

    void OnEnable()
    {
        combat.OnShot += PlayShot;
    }

    void OnDisable()
    {
        combat.OnShot -= PlayShot;
    }

    void PlayShot()
    {
        if (shotClips == null || shotClips.Length == 0)
            return;

        AudioClip clip = shotClips[Random.Range(0, shotClips.Length)];

        if (clip == null)
            return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, volume);
    }
}
