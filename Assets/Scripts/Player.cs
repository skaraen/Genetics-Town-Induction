using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7.0f;
    [SerializeField] private float rotateSpeed = 7.0f;
    [SerializeField] private float playerHeight = 0.7f;
    [SerializeField] private float playerRadius = 0.7f;
    [SerializeField] private GameInput gameInput;
    private bool isMoving;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Avatar Prefabs")]
    public GameObject seaAvatarPrefab;
    public GameObject mountainAvatarPrefab;
    public GameObject skyAvatarPrefab;
    public GameObject forestAvatarPrefab;

    private GameObject currentAvatar;
    void Start()
    {
        print(Vector3.forward);
        Debug.Log($"Received Avatar Data: Biome={AvatarDataHolder.biome}, Intelligence={AvatarDataHolder.intelligence}, Ability={AvatarDataHolder.ability}, Color={AvatarDataHolder.color}");

        GenerateAvatarBasedOnData();
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovement();
        HandleInteractions();
    }

    void GenerateAvatarBasedOnData()
    {
        if (currentAvatar != null)
        {
            Destroy(currentAvatar);
        }

        GameObject prefabToUse = null;
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.Euler(0, -90, 0);

        switch (AvatarDataHolder.biome)
        {
            case "Sea":
                prefabToUse = seaAvatarPrefab;
                break;
            case "Mountain":
                prefabToUse = mountainAvatarPrefab;
                position.y = 1.5f;
                rotation = Quaternion.Euler(0, 90, 0);
                break;
            case "Sky":
                prefabToUse = skyAvatarPrefab;
                break;
            case "Forest":
                prefabToUse = forestAvatarPrefab;
                break;
            default:
                prefabToUse = seaAvatarPrefab;
                break;
        }

        if (prefabToUse != null)
        {
            currentAvatar = Instantiate(prefabToUse, transform.position, prefabToUse.transform.rotation, transform);
            currentAvatar.transform.localPosition = position;
            // currentAvatar.transform.localRotation = Quaternion.identity;
            currentAvatar.transform.localRotation = rotation;
            currentAvatar.transform.localScale = Vector3.one;

            ApplyAvatarColor(currentAvatar);

            int headIndex = MapIntelligenceToIndex();
            int torsoIndex = MapAbilityToIndex();

            ToggleParts("head", headIndex, 2);
            ToggleParts("torso", torsoIndex, 2);
        }
    }

    void TogglePart(string partName, bool enable)
    {
        Transform part = currentAvatar.transform.Find(partName);
        if (part != null)
        {
            part.gameObject.SetActive(enable);
        }
    }

    void ToggleParts(string baseName, int activeIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            string partName = $"{baseName}{i + 1}";
            TogglePart(partName, i == activeIndex);
        }
    }

    int MapIntelligenceToIndex()
    {
        if (AvatarDataHolder.intelligence == "Smart" || AvatarDataHolder.intelligence == "Camouflage" || AvatarDataHolder.intelligence == "Balance")
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    int MapAbilityToIndex()
    {
        if (AvatarDataHolder.ability == "Flying" || AvatarDataHolder.ability == "Burst Swim" || AvatarDataHolder.ability == "Insulation")
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    HashSet<string> GetColorTargets()
    {
        HashSet<string> colorTargets = new HashSet<string>();

        switch (AvatarDataHolder.biome)
        {
            case "Sea":
                colorTargets.Add("head1Emission");
                colorTargets.Add("head2Emission");
                break;

            case "Mountain":
                colorTargets.Add("torso1");
                colorTargets.Add("torso2");
                break;

            case "Sky":
                colorTargets.Add("wings/emission");
                colorTargets.Add("body/emission");
                break;

            case "Forest":
                colorTargets.Add("leaves/emission");
                colorTargets.Add("trunk/emission");
                break;
        }

        return colorTargets;
    }

    void ApplyAvatarColor(GameObject avatar)
    {
        HashSet<string> targetNames = GetColorTargets();
        Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (targetNames.Contains(renderer.gameObject.name))
            {
                Material mat = renderer.material;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", AvatarDataHolder.color);
                }

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", AvatarDataHolder.color);
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }
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
