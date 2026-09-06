using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 12f;
    public float rotationSpeed = 15f;
    private Camera mainCam;

    void Awake()
    {
        // Also migrates scenes that were open before the PlayerShip component was added.
        PlayerShip ship = GetComponent<PlayerShip>();
        if (ship == null) ship = gameObject.AddComponent<PlayerShip>();
        ship.Configure();
    }

    void Start() {

        

        mainCam = Camera.main; Transform tp = transform.Find("TrailParticles"); if (tp != null) Destroy(tp.gameObject);
    }

    
    

    void Update() {
        float x = 0;
        float z = 0;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        }

        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            Vector2 stick = UnityEngine.InputSystem.Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.1f)
            {
                x = stick.x;
                z = stick.y;
            }
        }

        // Cria o vetor de movimento e limita a 1 para o analógico não bugar na diagonal
        Vector2 input = Vector2.ClampMagnitude(new Vector2(x, z), 1f);

        if (input.magnitude > 0.1f && mainCam != null)
        {
            // Calcula a direção do movimento baseada na visão da câmera!
            // Para cima no analógico = para cima na tela
            Vector3 camForward = Vector3.ProjectOnPlane(mainCam.transform.forward, transform.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(mainCam.transform.right, transform.up).normalized;
            
            Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

            // Vira o corpo do jogador suavemente para onde ele está andando
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, transform.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move
            Vector3 localMoveDir = transform.parent != null ? transform.parent.InverseTransformDirection(moveDir) : moveDir; float localSpeed = transform.parent != null ? moveSpeed / transform.parent.localScale.x : moveSpeed; transform.localPosition = transform.localPosition + localMoveDir * localSpeed * Time.deltaTime;
        }
    }
}
