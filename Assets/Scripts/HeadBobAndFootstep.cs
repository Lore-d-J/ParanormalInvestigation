using UnityEngine;

public class HeadBobAndFootsteps : MonoBehaviour
{
    [Header("Head Bob Settings")]
    public float walkBobSpeed = 12f;      // Bilis ng pag-alog habang naglalakad
    public float walkBobAmount = 0.05f;   // Taas-baba ng galaw (lakihan kung kulang)

    [Header("Footstep Audio")]
    public AudioSource footstepSource;
    public AudioClip[] footstepClips;     // Pwede maglagay ng 2-4 na tunog ng yapak para natural

    private float defaultYPos;
    private float timer;
    private bool stepPlayed;

    void Start()
    {
        // Sine-save ang original height ng camera
        defaultYPos = transform.localPosition.y;
    }

    void LateUpdate()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Kapag gumagalaw ang player (WASD / Arrow keys)
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            timer += Time.deltaTime * walkBobSpeed;
            float wave = Mathf.Sin(timer);

            // Pinapagalaw nang taas-baba ang camera
            Vector3 newPos = transform.localPosition;
            newPos.y = defaultYPos + wave * walkBobAmount;
            transform.localPosition = newPos;

            // Pinapatunog ang footstep tuwing bababa ang "hakbang" (ilalim ng wave)
            if (wave < -0.8f && !stepPlayed)
            {
                PlayFootstep();
                stepPlayed = true;
            }
            else if (wave > 0f)
            {
                stepPlayed = false;
            }
        }
        else
        {
            // Smooth na ibinabalik ang camera sa gitna kapag huminto
            timer = 0f;
            stepPlayed = false;
            Vector3 resetPos = transform.localPosition;
            resetPos.y = Mathf.Lerp(resetPos.y, defaultYPos, Time.deltaTime * 8f);
            transform.localPosition = resetPos;
        }
    }

    void PlayFootstep()
    {
        if (footstepSource != null && footstepClips.Length > 0)
        {
            int randomIndex = Random.Range(0, footstepClips.Length);
            footstepSource.pitch = Random.Range(0.9f, 1.1f); // Para hindi robotic ang tunog
            footstepSource.PlayOneShot(footstepClips[randomIndex]);
        }
    }
}