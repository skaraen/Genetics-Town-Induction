using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Bubble : MonoBehaviour
{
    public Button button;
    public RawImage previewImage;
    [HideInInspector] public Camera previewCamera;
    [HideInInspector] public RenderTexture renderTexture;
    [HideInInspector] public GameObject prefabToShow;

    public void SetColor(Color color) => button.GetComponent<Image>().color = color;

    public void SetTitle(string title) => button.GetComponentInChildren<TextMeshProUGUI>().text = title;

    public void InitializePreview(Camera cam, RenderTexture tex, GameObject prefab)
    {
        previewCamera = cam;
        renderTexture = tex;
        prefabToShow = prefab;
        if (previewImage) previewImage.texture = tex;
    }

    public void SetTexture(RenderTexture tex) {
        renderTexture = tex;
        if (previewImage) previewImage.texture = tex;
    }
}
