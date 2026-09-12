# Star: A Filha do Eclipse

![Logo do jogo — Star: A Filha do Eclipse](JogoCG/Assets/Logo%20do%20Jogo.png)

*Arte do logo por Miguel Nogueira Rangel.*

Jogo de plataforma 2D feito em **Unity 6 (URP)**. A protagonista é a **Star**, e a mecânica central
é a troca entre **modo Luz** e **modo Sombra** — cada modo muda a física do personagem, o tipo de
ataque e em quais inimigos ele consegue causar dano.

> **Este é um fork** de [simplicibr/JogoCG](https://github.com/simplicibr/JogoCG), o repositório
> original do projeto — a pasta do projeto Unity continua se chamando `JogoCG/`. O trabalho foi
> feito em grupo, e a divisão de autoria está documentada na seção
> [Créditos](#créditos-e-autoria).

---

## Mecânicas

### Dois modos, duas físicas

| | Modo **Luz** | Modo **Sombra** |
|---|---|---|
| Pulo | alto e fixo (força 22) | baixo e variável (força 10) |
| Ataque | dano de Luz | dano de Sombra |
| Dash | direcional | qualquer direção, a partir do input analógico 2D |

Inimigos são vulneráveis a um tipo específico de dano — trocar de modo no meio da luta é a mecânica,
não um detalhe cosmético.

### Movimento

Além do pulo, o personagem tem **dash com cooldown** (que zera os timers de pulo para impedir pulo
duplo no ar) e **wall jump** com penalidade ao subir repetidamente na mesma parede — o pulo perde
força a cada repetição consecutiva no mesmo lado.

### Conteúdo do jogo

- **6 cenas:** `Menu`, `Tutorial`, `SampleScene` (fase 1), `Fase2`, `Fase3` e `Fase4`
- **Inimigos:** terrestre com 5 estados de IA, voador tipo cristal, espinhos (perigo estático)
- **Miniboss:** Brokk
- **Boss final:** Seraphin — 16 sprites de animação, invoca espinhos e exige acertos no modo Sombra
- **Sistemas:** HUD de vida, fontes de dano por tipo, portões travados, portas de transição de cena,
  fundos com **parallax**, câmeras com **Cinemachine** e vinheta

### Controles

| Ação | Tecla |
|---|---|
| Mover | `W` `A` `S` `D` ou setas |
| Pular | `Espaço` |
| Atacar | `Z` ou `J` (também `Enter` / botão esquerdo do mouse) |
| Alternar modo Luz/Sombra | `E` |
| Dash | `C` |

Os bindings do **Input System** (novo sistema de input da Unity) ficam em
`Assets/InputSystem_Actions.inputactions`.

**Teclas de depuração** (desativáveis pelo `enableDebugKeys` no Inspector): `K` causa dano de Luz,
`L` dano de Sombra, `O` cura Luz, `P` cura Sombra.

---

## Como rodar

O projeto **não** inclui build pronta — o `.gitignore` exclui `Library/`, `Temp/` e artefatos de
build. É preciso abrir na Unity Editor.

1. **Unity 6000.3.12f1** (Unity 6) — recomendado instalar exatamente essa versão
2. Unity Hub → **Add project from disk** → selecione a pasta `JogoCG/` (a que está dentro do repo)
3. Abra a cena `Assets/Scenes/Menu.unity`
4. **Play**

O projeto foi desenvolvido em **Windows**. A Unity Editor não roda nativamente em Linux, então para
abrir o projeto você precisa de Windows ou macOS.

**Pacotes principais:** Universal Render Pipeline 17.3.0, Cinemachine 3.1.6, Input System 1.19.0,
2D Sprite, Timeline, TextMesh Pro, Splines.

---

## Estrutura

```
JogoCG/
├── Assets/
│   ├── Scripts/          # 23 scripts do jogo (movimento, combate, IA, câmera, UI)
│   ├── Scenes/           # Menu, Tutorial, SampleScene, Fase2, Fase3, Fase4
│   ├── Protagonista/     # sprites e animações do personagem
│   ├── inimigos/         # sprites, prefabs e animações dos inimigos e bosses
│   ├── Materials/        # materiais do templo
│   ├── Settings/         # configurações de URP e render
│   └── TutorialInfo/     # informações do template
├── Packages/manifest.json
└── ProjectSettings/
```

---

## Créditos e autoria

Projeto em grupo. A divisão real do trabalho:

| Área | Responsável |
|---|---|
| **Arte** — sprites do protagonista, inimigos, bosses, cenários e materiais (~95% dos assets visuais) | **Miguel Nogueira Rangel** |
| **Construção de parte do mapa** — nível 0, plataformas, texturas, fundos com parallax, transição de cenas e cutscene | **Pedro Lopes Guerra** |
| **Criação do repositório e protótipo inicial** (primeira etapa) | **Gustavo Simplício Bernardo** |
| **Programação** — mecânicas de Luz/Sombra, ataque, dash, wall jump, IA dos inimigos, bosses (Brokk e Seraphin), HUD, câmeras, integração das artes, montagem das fases e ajustes finais | **Kaio Moura Pontes** |

O histórico de commits do repositório está preservado e disponível para consulta.

**Todo o crédito da arte é de Miguel Nogueira Rangel.** Ela está neste repositório como parte do
projeto original do grupo, não como material livre para reuso.

---

## Estado do projeto

O jogo é jogável do início ao fim (Menu → Tutorial → 4 fases → boss final), mas tem **bugs
conhecidos** que não foram corrigidos — é um projeto acadêmico entregue, não um produto acabado.
Entre eles: ajustes de colisão em algumas plataformas e comportamentos inconsistentes de IA em
situações específicas de combate.

Pontos que eu melhoraria com mais tempo:

- Refatorar `Movement.cs`, que concentra movimento, pulo, dash e wall jump num só script
- Migrar os bindings hardcoded (`Z`/`J`/`E` em `PlayerAttack.cs`) para o Input System, junto com o resto
- Escrever testes para as transições de estado dos inimigos

---

## Contexto acadêmico

- **Disciplina:** Computação Gráfica — Universidade Federal Rural do Rio de Janeiro
- **Período:** 2026.1
- **Repositório original:** [simplicibr/JogoCG](https://github.com/simplicibr/JogoCG)

---

## Uso e licença

Este repositório é um fork do projeto original e **não possui licença** — o projeto original também
não tem. Em particular, **as artes são de autoria de Miguel Nogueira Rangel** e não podem ser
reutilizadas sem autorização dele.

O código está publicado para fins de estudo e portfólio.
