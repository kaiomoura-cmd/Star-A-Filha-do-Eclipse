using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite starLight;
    public Sprite starShadow;

    public float starSize = 80f;
    public PlayerHealth playerHealth;

    private Image[] starImages;

    void Start()
    {
        // Criar Canvas
        GameObject canvasGO = new GameObject("HealthCanvas");
        canvasGO.transform.SetParent(transform);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Criar painel no canto superior esquerdo
        GameObject panel = new GameObject("StarPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(20, 0, 20, 0);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600, 80);

        // Criar 6 estrelas
        starImages = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            GameObject starGO = new GameObject($"Star_{i}");
            starGO.transform.SetParent(panel.transform, false);

            Image img = starGO.AddComponent<Image>();
            img.sprite = starLight;
            img.preserveAspect = true;

            RectTransform rect = starGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(starSize, starSize);

            starImages[i] = img;
        }
    }

    void Update()
    {
        // Buscar Player se ainda não encontrou (spawn pode atrasar)
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null) return;

        int lightCount = playerHealth.LightStars;
        int shadowCount = playerHealth.ShadowStars;

        for (int i = 0; i < 6; i++)
        {
            if (starImages[i] == null) continue;

            if (i < lightCount)
                starImages[i].sprite = starLight;
            else if (i < lightCount + shadowCount)
                starImages[i].sprite = (starShadow != null) ? starShadow : starLight;
        }
    }
}
