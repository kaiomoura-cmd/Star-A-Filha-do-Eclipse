using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    public enum PlayerMode { Luz, Sombra }

    [Header("Animação de Ataque")]
    public SpriteRenderer renderizadorSprite;
    public Sprite spriteAtaque1;
    public Sprite spriteAtaque2;
    public Movement scriptMovimento; // Para acessar a trava
    public float tempoEntreFrames = 0.1f; // Quão rápido ele muda do frame 1 pro 2

    [Header("Modo de Jogo")]
    public PlayerMode currentMode = PlayerMode.Luz;

    [Header("Fontes de Luz (Mapeamento 3D Point Light)")]
    [SerializeField] private Light lightAttackSource;
    [SerializeField] private Light shadowAttackSource;

    [Header("Parâmetros do Ataque de Luz")]
    public Color lightColor = new Color(1f, 0.95f, 0.8f, 1f);
    public float lightTargetIntensity = 15.0f; // Luzes 3D no URP requerem maior intensidade
    public float lightRange = 8.0f;
    public float lightAttackDuration = 0.5f;

    [Header("Percurso da Luz (Offsets Locais)")]
    public Vector3 chestOffset = new Vector3(0f, 0.5f, 0f);    // Esterno
    public Vector3 handOffset = new Vector3(0.5f, 0.2f, 0f);   // Mão
    public Vector3 swordOffset = new Vector3(1.2f, 0.4f, 0f);  // Ponta da Espada

    [Header("Parâmetros do Ataque de Sombra")]
    public Color shadowColor = new Color(0.2f, 0f, 0.5f, 1f); // Roxo escuro para destacar no 3D
    public float shadowTargetIntensity = 12.0f;
    public float shadowRange = 8.0f;
    public float shadowAttackDuration = 0.5f;

    [Header("Configurações de Input")]
    [Tooltip("Desative se o objeto tiver um componente PlayerInput (evita duplo disparo de ações).")]
    public bool useLegacyInput = false;

    private Coroutine lightAttackCoroutine;
    private Coroutine shadowAttackCoroutine;

    // Proteção contra duplo disparo do SwitchMode
    private float lastSwitchTime = -1f;
    private const float SWITCH_COOLDOWN = 0.2f;

    private void Start()
    {
        ValidateOrCreateLights();
    }

    private void Update()
    {
        if (useLegacyInput)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current[Key.R].wasPressedThisFrame)
                {
                    SwitchMode();
                }
                if (Keyboard.current[Key.J].wasPressedThisFrame)
                {
                    PerformAttack();
                }
            }
        }
    }

    // Método público para alternar o modo
    public void SwitchMode()
    {
        // Proteção: ignora chamadas duplicadas dentro do cooldown
        if (Time.time - lastSwitchTime < SWITCH_COOLDOWN) return;
        lastSwitchTime = Time.time;

        currentMode = (currentMode == PlayerMode.Luz) ? PlayerMode.Sombra : PlayerMode.Luz;
        Debug.Log($"Modo alterado para: {currentMode}");
    }

    // Método para realizar o ataque baseado no modo atual
    public void PerformAttack()
    {
        StartCoroutine(AnimarAtaque());

        if (currentMode == PlayerMode.Luz)
        {
            TriggerLightAttack();
        }
        else
        {
            TriggerShadowAttack();
        }
    }

    // Callbacks do Novo Input System (SendMessages)
    public void OnAttack()
    {
        PerformAttack();
    }

    public void OnSwitchMode()
    {
        SwitchMode();
    }

    private void ValidateOrCreateLights()
    {
        // 1. Validar Luz de Ataque de Luz
        if (lightAttackSource == null)
        {
            Transform found = transform.Find("LightAttackSource");
            if (found != null)
            {
                lightAttackSource = found.GetComponent<Light>();
            }
            else
            {
                GameObject go = new GameObject("LightAttackSource");
                go.transform.SetParent(transform);
                go.transform.localPosition = chestOffset;
                lightAttackSource = go.AddComponent<Light>();
                ConfigureLight(lightAttackSource, lightColor, 0f, lightRange);
            }
        }

        // 2. Validar Luz de Ataque de Sombra
        if (shadowAttackSource == null)
        {
            Transform found = transform.Find("ShadowAttackSource");
            if (found != null)
            {
                shadowAttackSource = found.GetComponent<Light>();
            }
            else
            {
                GameObject go = new GameObject("ShadowAttackSource");
                go.transform.SetParent(transform);
                go.transform.localPosition = chestOffset;
                shadowAttackSource = go.AddComponent<Light>();
                ConfigureLight(shadowAttackSource, shadowColor, 0f, shadowRange);
            }
        }
    }

    private void ConfigureLight(Light lightComp, Color col, float intensity, float range)
    {
        lightComp.type = LightType.Point;
        lightComp.color = col;
        lightComp.intensity = intensity;
        lightComp.range = range;
        lightComp.shadows = LightShadows.None; // Evita custo de sombra em tempo real
    }

    private void TriggerLightAttack()
    {
        if (lightAttackCoroutine != null) StopCoroutine(lightAttackCoroutine);
        lightAttackCoroutine = StartCoroutine(AnimateLightAttack());
    }

    private void TriggerShadowAttack()
    {
        if (shadowAttackCoroutine != null) StopCoroutine(shadowAttackCoroutine);
        shadowAttackCoroutine = StartCoroutine(AnimateShadowAttack());
    }

    // Corrotina que move a luz 3D do peito (esterno) -> mão -> espada
    private IEnumerator AnimateLightAttack()
    {
        if (lightAttackSource == null) yield break;

        float elapsed = 0f;
        lightAttackSource.transform.localPosition = chestOffset;
        lightAttackSource.intensity = 0f;

        // Fase 1: Do Esterno para a Mão (Surgimento e carregamento)
        float phase1Duration = lightAttackDuration * 0.35f;
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase1Duration;
            
            lightAttackSource.transform.localPosition = Vector3.Lerp(chestOffset, handOffset, t);
            lightAttackSource.intensity = Mathf.Lerp(0f, lightTargetIntensity, t);
            yield return null;
        }

        lightAttackSource.transform.localPosition = handOffset;
        lightAttackSource.intensity = lightTargetIntensity;

        // Fase 2: Da Mão para a Ponta da Espada (Corte e desvanecimento)
        elapsed = 0f;
        float phase2Duration = lightAttackDuration * 0.65f;
        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase2Duration;

            lightAttackSource.transform.localPosition = Vector3.Lerp(handOffset, swordOffset, t);
            lightAttackSource.intensity = Mathf.Lerp(lightTargetIntensity, 0f, t);
            yield return null;
        }

        lightAttackSource.transform.localPosition = chestOffset;
        lightAttackSource.intensity = 0f;
        Debug.Log("Ataque de Luz 3D concluído!");
    }

    // Corrotina do ataque de sombra 3D (estático no esterno)
    private IEnumerator AnimateShadowAttack()
    {
        if (shadowAttackSource == null) yield break;

        float elapsed = 0f;
        shadowAttackSource.transform.localPosition = chestOffset;
        shadowAttackSource.intensity = 0f;

        float peakTime = shadowAttackDuration * 0.2f;
        float fadeOutTime = shadowAttackDuration * 0.8f;

        // Fade In
        while (elapsed < peakTime)
        {
            elapsed += Time.deltaTime;
            shadowAttackSource.intensity = Mathf.Lerp(0f, shadowTargetIntensity, elapsed / peakTime);
            yield return null;
        }

        shadowAttackSource.intensity = shadowTargetIntensity;

        // Fade Out
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            shadowAttackSource.intensity = Mathf.Lerp(shadowTargetIntensity, 0f, elapsed / fadeOutTime);
            yield return null;
        }

        shadowAttackSource.intensity = 0f;
        Debug.Log("Ataque de Sombra 3D concluído!");
    }
    IEnumerator AnimarAtaque()
    {
        // 1. Trava o script de movimento
        if (scriptMovimento != null) scriptMovimento.isAttacking = true;

        // 2. Coloca a pose 1 (Preparação)
        if (renderizadorSprite != null && spriteAtaque1 != null)
            renderizadorSprite.sprite = spriteAtaque1;

        // Espera uma fração de segundo
        yield return new WaitForSeconds(tempoEntreFrames);

        // 3. Coloca a pose 2 (Impacto do golpe)
        if (renderizadorSprite != null && spriteAtaque2 != null)
            renderizadorSprite.sprite = spriteAtaque2;

        // Espera o ataque terminar (você pode usar sua variável lightAttackDuration aqui se quiser)
        yield return new WaitForSeconds(0.2f);

        // 4. Destrava o movimento para ele voltar a ficar "Parado"
        if (scriptMovimento != null) scriptMovimento.isAttacking = false;
    }
}
