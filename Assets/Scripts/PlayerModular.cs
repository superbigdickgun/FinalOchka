using UnityEngine;

public class PlayerModular : MonoBehaviour
{
    [Header("=== СКОРОСТЬ ===")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;

    [Header("=== УПРАВЛЕНИЕ ===")]
    public bool mouseDirectedMovement = true;

    [Header("=== АНИМАЦИЯ ===")]
    public float stepFrequency = 2f;

    [Header("=== ПРИСЕД ===")]
    public bool isCrouching = false;
    public float crouchColliderMult = 0.6f;

    [Header("=== ПРЫЖОК ===")]
    public float jumpForce = 5f;
    public float gravity = 15f;
    public float jumpZ = 0f;
    private float jumpVel = 0f;
    public bool isJumping = false;

    [Header("=== ЗВУКИ ШАГОВ (6 слотов) ===")]
    public AudioClip[] stepSounds;
    [Range(0f, 1f)] public float stepVolume = 0.6f;
    public float stepPitchVariation = 0.1f;
    public bool avoidRepeat = true;

    // ✅ ФЛАГ: когда true — обычные шаги не играют (например, на траве)
    [HideInInspector] public bool stepsMuted = false;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private Vector2 moveInput;
    private Vector2 aimDir = Vector2.right;
    private bool isMoving;
    private bool isRunning;
    private float baseColliderRadius;
    private int bodyDir = 0;
    private Vector2 pendingRecoil = Vector2.zero;

    private AudioSource audioSource;
    private float stepSoundTimer = 0f;
    private int lastStepIndex = -1;

    public Vector2 AimDirection => aimDir;
    public bool IsMoving => isMoving;
    public bool IsRunning => isRunning;
    public bool IsJumping => isJumping;
    public bool IsCrouching => isCrouching;
    public int BodyDir => bodyDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        if (col != null) baseColliderRadius = col.radius;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        HandleInput();
        HandleAim();
        HandleCrouch();
        HandleJump();
        HandleBodyDirection();
        UpdateStepSound();
    }

    void HandleInput()
    {
        Vector2 raw = Vector2.zero;
        if (Input.GetKey(KeyCode.W)) raw.y += 1;
        if (Input.GetKey(KeyCode.S)) raw.y -= 1;
        if (Input.GetKey(KeyCode.A)) raw.x -= 1;
        if (Input.GetKey(KeyCode.D)) raw.x += 1;

        moveInput = raw.normalized;
        isMoving = raw.sqrMagnitude > 0.01f;
        isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving && !isCrouching;
    }

    void HandleAim()
    {
        if (Camera.main == null) return;
        Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0;
        Vector2 delta = (Vector2)m - (Vector2)transform.position;
        if (delta.sqrMagnitude > 0.0001f)
            aimDir = delta.normalized;
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C))
            isCrouching = !isCrouching;

        if (col != null)
            col.radius = isCrouching ? baseColliderRadius * crouchColliderMult : baseColliderRadius;
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping && !isCrouching)
        {
            isJumping = true;
            jumpVel = jumpForce;
        }

        if (isJumping)
        {
            jumpZ += jumpVel * Time.deltaTime;
            jumpVel -= gravity * Time.deltaTime;
            if (jumpZ <= 0f)
            {
                jumpZ = 0f;
                jumpVel = 0f;
                isJumping = false;
            }
        }
    }

    void HandleBodyDirection()
    {
        if (aimDir.sqrMagnitude < 0.01f) return;

        if (Mathf.Abs(aimDir.y) >= Mathf.Abs(aimDir.x))
            bodyDir = aimDir.y > 0 ? 1 : 0;
        else
            bodyDir = aimDir.x > 0 ? 3 : 2;
    }

    void UpdateStepSound()
    {
        // ✅ Если шаги заглушены — не обновляем таймер
        if (stepsMuted) return;

        if (!isMoving || isJumping) return;
        if (stepSounds == null || stepSounds.Length == 0) return;

        float speed = isRunning ? runSpeed : (isCrouching ? crouchSpeed : walkSpeed);
        float interval = 1f / (stepFrequency * (speed / walkSpeed));

        stepSoundTimer -= Time.deltaTime;
        if (stepSoundTimer <= 0f)
        {
            stepSoundTimer = interval;
            PlayStepSound();
        }
    }

    void PlayStepSound()
    {
        // ✅ Если шаги заглушены — не играем
        if (stepsMuted) return;

        if (audioSource == null) return;
        if (stepSounds == null || stepSounds.Length == 0) return;

        int index = Random.Range(0, stepSounds.Length);

        if (avoidRepeat && stepSounds.Length > 1 && index == lastStepIndex)
            index = (index + 1) % stepSounds.Length;

        lastStepIndex = index;

        AudioClip clip = stepSounds[index];
        if (clip == null) return;

        audioSource.pitch = 1f + Random.Range(-stepPitchVariation, stepPitchVariation);
        audioSource.PlayOneShot(clip, stepVolume);
    }

    public void ApplyRecoil(Vector2 direction, float force)
    {
        pendingRecoil += direction * force;
    }

    void FixedUpdate()
    {
        Vector2 dir = moveInput;

        if (mouseDirectedMovement && isMoving)
        {
            Vector2 toMouse = aimDir;
            Vector2 right = new Vector2(toMouse.y, -toMouse.x);
            Vector2 worldDir = toMouse * moveInput.y + right * moveInput.x;
            dir = worldDir.normalized;
        }

        float speed = isRunning ? runSpeed : (isCrouching ? crouchSpeed : walkSpeed);
        Vector2 delta = dir * speed * Time.fixedDeltaTime + pendingRecoil;
        rb.MovePosition(rb.position + delta);
        pendingRecoil = Vector2.zero;
    }
}