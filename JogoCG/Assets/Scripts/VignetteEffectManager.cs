using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class VignetteEffectManager : MonoBehaviour
{
    private static VignetteEffectManager instance;
    public static VignetteEffectManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<VignetteEffectManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("VignetteEffectManager");
                    instance = go.AddComponent<VignetteEffectManager>();
                }
            }
            return instance;
        }
    }

    [Header("Configurações do Efeito")]
    [Range(0f, 1f)] public float maxLightIntensity = 0.85f;
    [Range(0f, 1f)] public float maxShadowIntensity = 0.85f;
    public float fadeInDuration = 0.08f;
    public float fadeOutDuration = 0.5f;

    [Header("Teclas de Teste")]
    public bool enableDebugKeys = true;
    public KeyCode debugLightDamageKey = KeyCode.U;
    public KeyCode debugShadowDamageKey = KeyCode.I;

    private Canvas vignetteCanvas;
    private Image lightVignetteImage;
    private Image shadowVignetteImage;

    private Coroutine lightFadeCoroutine;
    private Coroutine shadowFadeCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupCanvas();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (enableDebugKeys)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.U].wasPressedThisFrame)
                {
                    TriggerLightDamage();
                }
                if (Keyboard.current[Key.I].wasPressedThisFrame)
                {
                    TriggerShadowDamage();
                }
            }
        }
    }

    private void SetupCanvas()
    {
        // Cria um novo Canvas dinamicamente para que seja plug-and-play
        GameObject canvasObj = new GameObject("VignetteEffectCanvas");
        canvasObj.transform.SetParent(transform);
        
        vignetteCanvas = canvasObj.AddComponent<Canvas>();
        vignetteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        vignetteCanvas.sortingOrder = 999; // Fica por cima da UI do jogo

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>(); 

        // Cria a imagem da Vinheta de Luz (Amarela)
        lightVignetteImage = CreateVignetteImage("LightVignette", new Color(1f, 1f, 1f, 0f), new Color(1f, 0.95f, 0.2f, 1f));
        
        // Cria a imagem da Vinheta de Sombra (Preta)
        shadowVignetteImage = CreateVignetteImage("ShadowVignette", new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 1f));
    }

    private Image CreateVignetteImage(string name, Color centerColor, Color borderColor)
    {
        GameObject imgObj = new GameObject(name);
        imgObj.transform.SetParent(vignetteCanvas.transform, false);

        RectTransform rect = imgObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image img = imgObj.AddComponent<Image>();
        img.sprite = Sprite.Create(GenerateVignetteTexture(centerColor, borderColor), new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0f); // Começa invisível
        img.raycastTarget = false; // Não bloqueia cliques de mouse no jogo

        return img;
    }

    private Texture2D GenerateVignetteTexture(Color centerColor, Color borderColor)
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Calcula a distância normalizada do centro
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                dist = Mathf.Clamp01(dist);

                // Aplica um SmoothStep para que o centro fique bem transparente e as bordas opacas
                float t = Mathf.SmoothStep(0.2f, 1.0f, dist);
                Color pixelColor = Color.Lerp(centerColor, borderColor, t);
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    public void TriggerLightDamage()
    {
        if (lightFadeCoroutine != null) StopCoroutine(lightFadeCoroutine);
        lightFadeCoroutine = StartCoroutine(FadeVignette(lightVignetteImage, maxLightIntensity));
    }

    public void TriggerShadowDamage()
    {
        if (shadowFadeCoroutine != null) StopCoroutine(shadowFadeCoroutine);
        shadowFadeCoroutine = StartCoroutine(FadeVignette(shadowVignetteImage, maxShadowIntensity));
    }

    private IEnumerator FadeVignette(Image vignetteImage, float maxIntensity)
    {
        // Fade In
        float elapsed = 0f;
        float startAlpha = vignetteImage.color.a;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, maxIntensity, elapsed / fadeInDuration);
            vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, alpha);
            yield return null;
        }

        vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, maxIntensity);

        // Fade Out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(maxIntensity, 0f, elapsed / fadeOutDuration);
            vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, alpha);
            yield return null;
        }

        vignetteImage.color = new Color(vignetteImage.color.r, vignetteImage.color.g, vignetteImage.color.b, 0f);
    }
}
