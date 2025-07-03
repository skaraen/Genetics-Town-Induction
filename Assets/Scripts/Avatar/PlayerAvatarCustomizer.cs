using UnityEngine;

public class PlayerAvatarCustomizer : MonoBehaviour
{
    [SerializeField] private string avatarType;
    [SerializeField] private string headOption;
    [SerializeField] private string torsoOption;

    private Transform headSlot;
    private Transform torsoSlot;

    private GameObject currentHead;
    private GameObject currentTorso;

    void Start()
    {
        if (headSlot == null || torsoSlot == null) {
            var body = transform.Find("Body");
            headSlot = body.Find("Head");
            torsoSlot = body.Find("Torso");
        }

        EquipHead("head-" + headOption);
        EquipTorso("torso-" + torsoOption);
    }

    public void EquipHead(string headPrefabName)
    {
        ReplacePart(ref currentHead, headPrefabName, headSlot);
    }

    public void EquipTorso(string torsoPrefabName)
    {
        ReplacePart(ref currentTorso, torsoPrefabName, torsoSlot);
    }

    private void ReplacePart(ref GameObject currentPart, string prefabName, Transform parent)
    {
        if (currentPart != null)
            Destroy(currentPart);

        GameObject prefab = Resources.Load<GameObject>($"Avatars/{avatarType}/FBX/{prefabName}");
        if (prefab != null)
        {
            currentPart = Instantiate(prefab, parent);
            currentPart.transform.localPosition = Vector3.zero;
            currentPart.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning($"Prefab {prefabName} not found in Resources/Avatars/{avatarType}/FBX");
        }
    }
}
