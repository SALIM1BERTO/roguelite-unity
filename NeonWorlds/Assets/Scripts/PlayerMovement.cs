using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 12f;
    public float rotationSpeed = 15f;
    private Camera mainCam;

    [Header("Dash Neon")]
    public float dashCooldown = 2.0f;
    public float dashDuration = 0.22f;
    public float dashSpeedMultiplier = 3.2f;
    private float dashTimer = 0f;
    public bool isDashing = false;
    private Vector3 dashDir;

    public float DashCooldownNormalized => dashCooldown > 0f ? Mathf.Clamp01(dashTimer / dashCooldown) : 0f;

    void Awake()
    {
        // Also migrates scenes that were open before the PlayerShip component was added.
        PlayerShip ship = GetComponent<PlayerShip>();
        if (ship == null) ship = gameObject.AddComponent<PlayerShip>();
        ship.Configure();
    }

    void Start()
    {
        mainCam = Camera.main;
        Transform tp = transform.Find("TrailParticles");
        if (tp != null) Destroy(tp.gameObject);
    }

    void Update()
    {
        if (Time.timeScale == 0) return;

        if (dashTimer > 0f)
            dashTimer -= Time.deltaTime;

        float x = 0;
        float z = 0;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.1f)
            {
                x = stick.x;
                z = stick.y;
            }
        }

        // Cria o vetor de movimento e limita a 1 para o analógico não bugar na diagonal
        Vector2 input = Vector2.ClampMagnitude(new Vector2(x, z), 1f);

        Vector3 moveDir = Vector3.zero;
        if (mainCam != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(mainCam.transform.forward, transform.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(mainCam.transform.right, transform.up).normalized;
            if (input.sqrMagnitude > 0.01f)
            {
                moveDir = (camForward * input.y + camRight * input.x).normalized;
            }
        }

        // Check Dash Input (Space, LeftShift, Right Mouse, Gamepad South/Shoulders)
        bool dashInput = false;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.leftShiftKey.wasPressedThisFrame)
                dashInput = true;
        }
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            dashInput = true;
        if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.leftShoulder.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame))
            dashInput = true;

        if (dashInput && dashTimer <= 0f && !isDashing)
        {
            Vector3 chosenDir = moveDir.sqrMagnitude > 0.01f ? moveDir : transform.forward;
            StartCoroutine(PerformDash(chosenDir));
        }

        if (isDashing)
        {
            // Move rápido na direção do dash acompanhando a curvatura planetária
            Vector3 localDashDir = transform.parent != null ? transform.parent.InverseTransformDirection(dashDir) : dashDir;
            float localSpeed = transform.parent != null ? (moveSpeed * dashSpeedMultiplier) / transform.parent.localScale.x : (moveSpeed * dashSpeedMultiplier);
            transform.localPosition += localDashDir * localSpeed * Time.deltaTime;
            return;
        }

        if (moveDir.sqrMagnitude > 0.01f)
        {
            // Vira o corpo do jogador suavemente para onde ele está andando
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, transform.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move
            Vector3 localMoveDir = transform.parent != null ? transform.parent.InverseTransformDirection(moveDir) : moveDir;
            float localSpeed = transform.parent != null ? moveSpeed / transform.parent.localScale.x : moveSpeed;
            transform.localPosition += localMoveDir * localSpeed * Time.deltaTime;
        }
    }

    IEnumerator PerformDash(Vector3 dir)
    {
        isDashing = true;
        dashDir = dir;
        dashTimer = dashCooldown;

        if (GameManager.Instance != null)
            GameManager.Instance.isInvincible = true;

        GameAudio.Play(AudioCue.Teleport);

        float elapsed = 0f;
        float ghostInterval = 0.04f;
        float ghostTimer = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            ghostTimer += Time.deltaTime;

            if (ghostTimer >= ghostInterval)
            {
                ghostTimer = 0f;
                SpawnGhostAfterimage();
            }

            yield return null;
        }

        isDashing = false;

        if (GameManager.Instance != null)
            GameManager.Instance.isInvincible = false;
    }

    void SpawnGhostAfterimage()
    {
        Transform visual = transform.Find("ShipVisual");
        if (visual == null) visual = transform;

        MeshFilter mf = visual.GetComponent<MeshFilter>();
        if (mf == null) mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        GameObject ghost = new GameObject("DashGhost");
        ghost.transform.position = visual.position;
        ghost.transform.rotation = visual.rotation;
        ghost.transform.localScale = visual.lossyScale;

        MeshFilter ghostMf = ghost.AddComponent<MeshFilter>();
        ghostMf.sharedMesh = mf.sharedMesh;

        MeshRenderer ghostMr = ghost.AddComponent<MeshRenderer>();
        Material ghostMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color neonCyan = new Color(0f, 1f, 0.9f, 0.5f);
        ghostMat.SetColor("_BaseColor", neonCyan);
        ghostMat.EnableKeyword("_EMISSION");
        ghostMat.SetColor("_EmissionColor", neonCyan * 2f);
        ghostMr.material = ghostMat;

        StartCoroutine(FadeAndDestroyGhost(ghost, ghostMat));
    }

    IEnumerator FadeAndDestroyGhost(GameObject ghost, Material mat)
    {
        float dur = 0.25f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0.6f, 0f, t / dur);
            if (mat != null)
            {
                Color c = new Color(0f, 1f, 0.9f, alpha);
                mat.SetColor("_BaseColor", c);
                mat.SetColor("_EmissionColor", c * (alpha * 2f));
            }
            yield return null;
        }
        if (ghost != null) Destroy(ghost);
        if (mat != null) Destroy(mat);
    }
}
