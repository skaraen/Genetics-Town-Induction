using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Bubble : MonoBehaviour
{
    public Button button;
    public RawImage previewImage;
    [HideInInspector] public Camera previewCamera;
    [HideInInspector] public RenderTexture renderTexture;
    [HideInInspector] public PlayerCustoms customsToShow;

    private float avatarYOffset = 0.0f;

    public void SetColor(Color color) => button.GetComponent<Image>().color = color;

    public void SetTitle(string title) => button.GetComponentInChildren<TextMeshProUGUI>().text = title;

    public void SetAvatarCustoms(PlayerCustoms customs)
    {
        AvatarProfileData profileData = AvatarRegistry.Instance.GetProfileById(customs.avatarType);
        avatarYOffset = profileData.canvasYOffset;
        this.customsToShow = customs;
    }

    public void InitializePreview(Camera cam, RenderTexture tex)
    {
        previewCamera = cam;
        renderTexture = tex;
        if (previewImage) previewImage.texture = tex;
    }

    public void SetTexture(RenderTexture tex) {
        renderTexture = tex;
        if (previewImage) previewImage.texture = tex;
    }

    public float GetAvatarYOffset()
    {
        return avatarYOffset;
    }
}
