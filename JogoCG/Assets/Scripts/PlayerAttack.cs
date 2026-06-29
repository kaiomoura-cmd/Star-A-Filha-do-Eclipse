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
    public Movement scriptMovimento;
    public float tempoEntreFrames = 0.1f;

    [Header("Pré-visualização no Editor")]
    [Tooltip("Marque para ver a pose de ataque e posicionar os marcadores. Desmarque depois.")]
    public bool previewAttackPose = false;

    [Header("Modo de Jogo")]
    public PlayerMode currentMode = PlayerMode.Luz;

    [Header("Fontes de Luz (Mapeamento 3D Point Light)")]
    [SerializeField] private Light lightAttackSource;
    [SerializeField] private Light shadowAttackSource;

    [Header("Parâmetros do Ataque de Luz")]
    public Color lightColor = new Color(1f, 0.95f, 0.8f, 1f);
    public float lightTargetIntensity = 15.0f; // Luzes 3D no URP requerem maior intensidade
    public float lightRange = 4.0f;
    public float lightAttackDuration = 0.5f;

    [Header("Percurso da Luz (Marcadores Visuais)")]
    [Tooltip("Arraste um GameObject vazio para cá ou crie com o botão direito > Create Empty")]
    public Transform chestMarker;
    public Transform handMarker;
    public Transform swordMarker;

    [Header("Parâmetros do Ataque de Sombra")]
    public Color shadowColor = new Color(0.2f, 0f, 0.5f, 1f); // Roxo escuro para destacar no 3D
    public float shadowTargetIntensity = 12.0f;
    public float shadowRange = 4.0f;
    public float shadowAttackDuration = 0.5f;

    [Header("Configurações de Input")]
    [Tooltip("Desative se o objeto tiver um componente PlayerInput (evita duplo disparo de ações).")]
    public bool useLegacyInput = false;

    private Coroutine lightAttackCoroutine;
    private Coroutine shadowAttackCoroutine;

    // Proteção contra duplo disparo do SwitchMode
    private float lastSwitchTime = -1f;
    private const float SWITCH_COOLDOWN = 0.25f;

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
                if (Keyboard.current[Key.E].wasPressedThisFrame)
                {
                    SwitchMode();
                }
                if (Keyboard.current[Key.Z].wasPressedThisFrame || Keyboard.current[Key.J].wasPressedThisFrame)
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

#if UNITY_EDITOR
    private void Awake()
    {
        // No editor, garante que marcadores existam ao abrir o prefab
        if (!Application.isPlaying)
        {
            CriarMarcadoresSeNecessario();
        }
    }

    private void CriarMarcadoresSeNecessario()
    {
        if (transform.Find("ChestMarker") == null)  CriarMarcador("ChestMarker",  new Vector3(0f, 1.7f, -0.5f));
        if (transform.Find("HandMarker") == null)   CriarMarcador("HandMarker",   new Vector3(0.5f, 1.5f, -0.5f));
        if (transform.Find("SwordMarker") == null)  CriarMarcador("SwordMarker",  new Vector3(1.2f, 1.7f, -0.5f));
        chestMarker = transform.Find("ChestMarker");
        handMarker = transform.Find("HandMarker");
        swordMarker = transform.Find("SwordMarker");
    }
#endif

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPlaying) return;
        CriarMarcadoresSeNecessario();
        // Pré-visualizar pose de ataque se marcado
        if (previewAttackPose && renderizadorSprite != null && spriteAtaque2 != null)
            renderizadorSprite.sprite = spriteAtaque2;
        else if (!previewAttackPose && renderizadorSprite != null && scriptMovimento != null && scriptMovimento.spriteParado != null)
            renderizadorSprite.sprite = scriptMovimento.spriteParado;
#endif
    }

    private void OnDestroy()
    {
        chestMarker = null;
        handMarker = null;
        swordMarker = null;
    }

    private void ValidateOrCreateLights()
    {
        // Criar marcadores visuais se não atribuídos
        if (chestMarker == null)  chestMarker  = CriarMarcador("ChestMarker",  new Vector3(0f, 1.7f, -0.5f));
        if (handMarker == null)   handMarker   = CriarMarcador("HandMarker",   new Vector3(0.5f, 1.5f, -0.5f));
        if (swordMarker == null)  swordMarker  = CriarMarcador("SwordMarker",  new Vector3(1.2f, 1.7f, -0.5f));

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
                go.transform.localPosition = chestMarker.localPosition;
                lightAttackSource = go.AddComponent<Light>();
                ConfigureLight(lightAttackSource, lightColor, 0f, lightRange);
                go.SetActive(false); // Começa desligada, só ativa no ataque
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
                go.transform.localPosition = chestMarker.localPosition;
                shadowAttackSource = go.AddComponent<Light>();
                ConfigureLight(shadowAttackSource, shadowColor, 0f, shadowRange);
                go.SetActive(false); // Começa desligada, só ativa no ataque
            }
        }
    }

    private Transform CriarMarcador(string nome, Vector3 posicao)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(transform);
        go.transform.localPosition = posicao;
        return go.transform;
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
        if (lightAttackSource == null || chestMarker == null || handMarker == null || swordMarker == null) yield break;

        // Ativar luz só durante o ataque
        lightAttackSource.gameObject.SetActive(true);

        float elapsed = 0f;
        lightAttackSource.transform.localPosition = chestMarker.localPosition;
        lightAttackSource.intensity = 0f;

        // Fase 1: Do Esterno para a Mão
        float phase1Duration = lightAttackDuration * 0.35f;
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase1Duration;
            lightAttackSource.transform.localPosition = Vector3.Lerp(chestMarker.localPosition, handMarker.localPosition, t);
            lightAttackSource.intensity = Mathf.Lerp(0f, lightTargetIntensity, t);
            yield return null;
        }

        lightAttackSource.transform.localPosition = handMarker.localPosition;
        lightAttackSource.intensity = lightTargetIntensity;

        // Fase 2: Da Mão para a Ponta da Espada
        elapsed = 0f;
        float phase2Duration = lightAttackDuration * 0.65f;
        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase2Duration;
            lightAttackSource.transform.localPosition = Vector3.Lerp(handMarker.localPosition, swordMarker.localPosition, t);
            lightAttackSource.intensity = Mathf.Lerp(lightTargetIntensity, 0f, t);
            yield return null;
        }

        lightAttackSource.intensity = 0f;
        // Desativar luz ao terminar
        lightAttackSource.gameObject.SetActive(false);
    }

    // Corrotina do ataque de sombra 3D (estático no esterno)
    private IEnumerator AnimateShadowAttack()
    {
        if (shadowAttackSource == null || chestMarker == null || handMarker == null || swordMarker == null) yield break;

        // Ativar luz só durante o ataque
        shadowAttackSource.gameObject.SetActive(true);
        shadowAttackSource.transform.localPosition = chestMarker.localPosition;
        shadowAttackSource.intensity = 0f;

        // Fase 1: Peito → Mão (carregando)
        float elapsed = 0f;
        float phase1Duration = shadowAttackDuration * 0.35f;
        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase1Duration;
            shadowAttackSource.transform.localPosition = Vector3.Lerp(chestMarker.localPosition, handMarker.localPosition, t);
            shadowAttackSource.intensity = Mathf.Lerp(0f, shadowTargetIntensity, t);
            yield return null;
        }

        shadowAttackSource.transform.localPosition = handMarker.localPosition;
        shadowAttackSource.intensity = shadowTargetIntensity;

        // Fase 2: Mão → Espada (golpe, fade out)
        elapsed = 0f;
        float phase2Duration = shadowAttackDuration * 0.65f;
        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase2Duration;
            shadowAttackSource.transform.localPosition = Vector3.Lerp(handMarker.localPosition, swordMarker.localPosition, t);
            shadowAttackSource.intensity = Mathf.Lerp(shadowTargetIntensity, 0f, t);
            yield return null;
        }

        shadowAttackSource.intensity = 0f;
        shadowAttackSource.gameObject.SetActive(false);
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
