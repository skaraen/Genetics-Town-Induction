using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7.0f;
    [SerializeField] private float rotateSpeed = 7.0f;
    [SerializeField] private float playerHeight = 0.7f;
    [SerializeField] private float playerRadius = 0.7f;
    [SerializeField] private GameInput gameInput;
    private bool isMoving;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        print(Vector3.forward);
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleInteractions();
    }

    void HandleMovement()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();

        Vector3 moveVector = new Vector3(inputVector.x, 0f, inputVector.y);
        float moveDistance = Time.deltaTime * moveSpeed;

        bool isHitting = Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveVector, moveDistance);

        if (isHitting)
        {
            Vector3 moveVecX = new Vector3(moveVector.x, 0, 0).normalized;
            bool isHittingX = Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveVecX, moveDistance);

            if (isHittingX)
            {
                Vector3 moveVecZ = new Vector3(0, 0, moveVector.z).normalized;
                bool isHittingZ = Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, moveVecZ, moveDistance);

                moveVector = isHittingZ ? Vector3.zero : moveVecZ;
            }
            else
            {
                moveVector = moveVecX;
            }
        }

        isMoving = moveVector != Vector3.zero;
        transform.position += moveVector * moveDistance;

        transform.forward = Vector3.Slerp(transform.forward, moveVector, Time.deltaTime * rotateSpeed);
    }

    void HandleInteractions()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        Vector3 moveVector = new Vector3(inputVector.x, 0f, inputVector.y);

        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider[] proximalNpcs = Physics.OverlapSphere(transform.position, 2f);
            foreach (Collider collider in proximalNpcs)
            {
                Debug.Log(collider);
            }   
        }
    }
}
