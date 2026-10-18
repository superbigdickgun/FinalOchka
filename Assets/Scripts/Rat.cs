using UnityEngine;

public class Rat : MonoBehaviour
{
    [Header("=== ДВИЖЕНИЕ ===")]
    public float moveSpeed = 1.5f;
    public float chaseSpeed = 2.5f;
    public float fleeSpeed = 3.5f;
    public float rotationSpeed = 360f;
    public float chaosStrength = 0.3f;

    [Header("=== ОРИЕНТАЦИЯ ===")]
    public float spriteAngleOffset = 180f;

    [Header("=== ПОИСК ===")]
    public float waitAtPoint = 0.5f;
    public float searchRange = 0f;

    [Header("=== СТРАХ ===")]
    public float playerViewAngle = 45f;
    public float fleeDetectionRange = 3f;
    public float fleeDurationMin = 1f;
    public float fleeDurationMax = 6f;

    [Header("=== АТАКА ===")]
    public float backAttackAngle = 90f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1f;
    public float attackDamage = 5f;

    [Header("=== ТОЛЧОК ПРИ УДАРЕ ===")]
    [Tooltip("Сила толчка игрока при ударе.")]
    public float knockbackForce = 3f;

    [Header("=== РЫВОК ===")]
    public float dashTriggerRange = 2.5f;
    public float dashDuration = 0.3f;
    public float dashSpeed = 6f;
    public float retreatDuration = 1.5f;

    [Header("=== HP ===")]
    public int maxHP = 1;
    public int currentHP = 1;

    [Header("=== СМЕРТЬ ===")]
    public GameObject bloodSplatterPrefab;
    public GameObject bloodDecalPrefab;

    [Header("=== КОРОЛЬ? ===")]
    public bool isKing = false;

    [Header("=== ССЫЛКИ ===")]
    public PlayerModular player;

    // Внутренние
    private Rigidbody2D rb;
    private Vector2 targetPoint;
    private float waitTimer;
    private bool isDead = false;
    private float currentAngle;
    private Vector2 moveDir = Vector2.zero;
    private float lastAttackTime = 0f;
    private float currentSpeed = 0f;
    private BarnFloor barnFloor;
    private float dashTimer = 0f;
    private Vector2 dashDir = Vector2.zero;
    private float fleeTimer = 0f;
    private bool isFleeing = false;
    private float chaosTimer = 0f;
    private Vector2 chaosOffset = Vector2.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
        }

        currentHP = maxHP;
        currentSpeed = moveSpeed;
        barnFloor = FindObjectOfType<BarnFloor>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerModular>();
        }

        // Крысы не толкают Player в обычном движении, но не отталкиваются сами
        if (player != null)
        {
            var playerCol = player.GetComponent<Collider2D>();
            var myCol = GetComponent<Collider2D>();
            if (playerCol != null && myCol != null)
                Physics2D.IgnoreCollision(myCol, playerCol);
        }

        if (bloodSplatterPrefab == null)
            bloodSplatterPrefab = Resources.Load<GameObject>("BloodSplatter");
        if (bloodDecalPrefab == null)
            bloodDecalPrefab = Resources.Load<GameObject>("BloodDecal");

        PickNewTarget();
    }

    public void ScareFromPlayer()
    {
        if (isDead || isKing) return;
        isFleeing = true;
        fleeTimer = Random.Range(fleeDurationMin, fleeDurationMax);
    }

    void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerModular>();
            else return;
        }

        // === РЫВОК (только обычные) ===
        if (!isKing && dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            moveDir = dashDir;
            currentSpeed = dashSpeed;
            RotateToDir(moveDir);
            return;
        }

        // === КОРОЛЬ УБЕГАЕТ ===
        if (isKing)
        {
            FleeFromPlayer();
            return;
        }

        // === УБЕГАНИЕ ===
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;
            if (fleeTimer <= 0f)
            {
                isFleeing = false;
                currentSpeed = chaseSpeed;
            }
            else
            {
                FleeFromPlayer();
                return;
            }
        }

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float distToPlayer = toPlayer.magnitude;

        Vector2 playerToRat = -toPlayer.normalized;
        Vector2 playerAim = player.AimDirection;
        float angleToRat = Vector2.Angle(playerAim, playerToRat);
        bool playerLookingAtRat = angleToRat < playerViewAngle;
        bool ratBehindPlayer = angleToRat > backAttackAngle;

        // === АТАКА (только обычные) ===
        if (!isKing && distToPlayer < attackRange && Time.time - lastAttackTime > attackCooldown && ratBehindPlayer)
        {
            lastAttackTime = Time.time;
            AttackPlayer();
            return;
        }

        // === УГРОЗА ===
        if (distToPlayer < fleeDetectionRange && playerLookingAtRat)
        {
            isFleeing = true;
            fleeTimer = Random.Range(fleeDurationMin, fleeDurationMax);
            return;
        }

        // === РЫВОК ===
        if (!isKing && distToPlayer < dashTriggerRange && distToPlayer > attackRange && !playerLookingAtRat && Time.time - lastAttackTime > attackCooldown)
        {
            dashTimer = dashDuration;
            dashDir = toPlayer.normalized;
            RotateToDir(dashDir);
            return;
        }

        // === ПОИСК ===
        bool inSearchRange = (searchRange <= 0f) || (distToPlayer < searchRange);
        if (inSearchRange)
        {
            Vector2 behindPlayer = (Vector2)player.transform.position - playerAim * 1.5f;

            chaosTimer -= Time.deltaTime;
            if (chaosTimer <= 0f)
            {
                chaosTimer = Random.Range(0.3f, 0.8f);
                chaosOffset = Random.insideUnitCircle * chaosStrength;
            }

            targetPoint = behindPlayer + chaosOffset;

            Vector2 toTarget = targetPoint - (Vector2)transform.position;
            if (toTarget.magnitude > 0.1f)
            {
                moveDir = toTarget.normalized;
                currentSpeed = chaseSpeed;
                RotateToDir(moveDir);
            }
            return;
        }

        // === ПАТРУЛЬ ===
        currentSpeed = moveSpeed;

        if (waitTimer > 0f) { waitTimer -= Time.deltaTime; moveDir = Vector2.zero; return; }

        Vector2 toT = targetPoint - (Vector2)transform.position;
        if (toT.magnitude < 0.2f) { waitTimer = waitAtPoint; PickNewTarget(); moveDir = Vector2.zero; }
        else { moveDir = toT.normalized; RotateToDir(moveDir); }
    }

    void FleeFromPlayer()
    {
        if (barnFloor == null) return;

        if (targetPoint == Vector2.zero || Vector2.Distance(transform.position, targetPoint) < 0.3f)
            targetPoint = barnFloor.GetRandomCorner();

        Vector2 toTarget = targetPoint - (Vector2)transform.position;
        if (toTarget.magnitude > 0.2f)
        {
            moveDir = toTarget.normalized;
            currentSpeed = fleeSpeed;
            RotateToDir(moveDir);
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (moveDir.sqrMagnitude < 0.01f) return;
        if (rb == null) return;

        Vector2 velocity = moveDir * currentSpeed * Time.fixedDeltaTime;
        Vector2 newPos = rb.position + velocity;

        if (barnFloor != null && barnFloor.areaCollider != null)
        {
            if (!barnFloor.areaCollider.OverlapPoint(newPos))
            {
                Vector2 side = Vector2.Perpendicular(moveDir);
                newPos = rb.position + side * currentSpeed * Time.fixedDeltaTime * 0.5f;
                if (barnFloor.areaCollider.OverlapPoint(newPos))
                    moveDir = side;
                else return;
            }
        }

        rb.MovePosition(newPos);
    }

    void RotateToDir(Vector2 dir)
    {
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f + (spriteAngleOffset - 180f);
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    void PickNewTarget()
    {
        if (barnFloor != null) { targetPoint = barnFloor.GetRandomPoint(); return; }
        targetPoint = (Vector2)transform.position + Random.insideUnitCircle * 3f;
    }

    void AttackPlayer()
    {
        Debug.Log("[Rat] АТАКА! Урон: " + attackDamage);

        // Урон
        var health = player.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(attackDamage);

        // === ТОЛЧОК ИГРОКА ЧЕРЕЗ AddForce ===
        PushPlayer();

        // Отбегаем
        Vector2 side = Vector2.Perpendicular(((Vector2)player.transform.position - (Vector2)transform.position).normalized);
        moveDir = side * (Random.value > 0.5f ? 1f : -1f);
        currentSpeed = chaseSpeed * 1.5f;
        lastAttackTime = Time.time + 1f;
    }

    void PushPlayer()
    {
        if (player == null) return;

        Vector2 dir = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;

        // Пытаемся толкнуть Rigidbody2D игрока
        var playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null && playerRb.bodyType == RigidbodyType2D.Dynamic)
        {
            playerRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            Debug.Log("[Rat] Толчок игрока, сила: " + knockbackForce);
        }
        else
        {
            // Игрок Kinematic — толкаем через MovePosition
            player.ApplyRecoil(-dir, knockbackForce * 0.1f);
            Debug.Log("[Rat] Толчок через ApplyRecoil");
        }
    }

    public void TakeHit(Vector2 hitPoint)
    {
        if (isDead) return;
        currentHP--;
        Debug.Log("[Rat] HP: " + currentHP + (isKing ? " (КОРОЛЬ)" : ""));
        if (currentHP <= 0) Die(hitPoint);
    }

    void Die(Vector2 hitPoint)
    {
        isDead = true;

        if (bloodSplatterPrefab != null)
            Instantiate(bloodSplatterPrefab, transform.position, Quaternion.identity);

        if (bloodDecalPrefab != null)
        {
            var decal = Instantiate(bloodDecalPrefab, transform.position, Quaternion.identity);
            decal.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }

        if (isKing)
        {
            var kingScript = FindObjectOfType<RatKing>();
            if (kingScript != null) kingScript.spawnEnabled = false;
        }

        Debug.Log("[Rat] Умер" + (isKing ? " (КОРОЛЬ)" : ""));
        Destroy(gameObject);
    }
}