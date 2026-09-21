# Pine Hollow — Estado do desenvolvimento

> Auditoria das 27 fases do `Pine_Hollow_Plano_Completo_Desenvolvimento.md` contra o código real.
> **Última verificação: 2026-09-21**, contra a árvore de trabalho
> (`Assets/_Project/Scripts/`, `Assets/_Project/Scenes/Prototype_Player.unity`,
> `ProjectSettings/`). A FASE 1 fechou nesse dia e os caminhos mudaram: tudo vive em
> `Assets/_Project/` agora.
>
> Este ficheiro existe porque **as checkboxes do plano estão desactualizadas** e um agente (ou uma
> pessoa) que confie nelas reimplementa sistemas que já existem. Quando isto e o plano
> discordarem, **o código é o facto**. Ver também `AGENTS.md`.

## Veredicto

Dos **12 milestones** do plano: **3 fechados, o 4.º a meio, 8 por abrir.**

| # | Milestone | Estado |
|---|---|---|
| 1 | Player funcional | ✅ |
| 2 | Player + interação | ✅ |
| 3 | Objeto examinável | ✅ |
| 4 | Primeira fotografia | ⚠️ parcial — capturar sim, fotografia-como-objecto não |
| 5 | Duas fotografias + comparação | ❌ |
| 6 | Alinhamento | ❌ |
| 7 | Primeiro flashback | ❌ |
| 8 | Alteração da fotografia | ❌ |
| 9 | Vertical Slice completo | ❌ |
| 10 | Pine Hollow | ❌ |
| 11 | Conteúdo narrativo completo | ❌ |
| 12 | Polimento e lançamento | ❌ |

Código total: **12 scripts, ~890 linhas**, duas cenas (`Bootstrap` e `Prototype_Player`),
mais um validador de editor.

---

## Fase a fase

### FASE 0 — Pré-produção · ✅ **fechada**

> Revisto a 2026-09-21 (task `869f4yfdz`). Duas entregas novas na raiz:
> `Pine_Hollow_Producao.md` (limites técnicos + lista de assets) e
> `Pine_Hollow_Direccao_Artistica.md` (direcção escolhida, paleta com valores na §7) e a pasta
> `Referencias/` com as notas. **A FASE 0 fica fechada**; o que sobra são itens de outras fases.

| Item | Estado | Nota |
|---|---|---|
| Conceito, história, GDD, core gameplay, personagens, estrutura narrativa | ✅ | Documentos na raiz do repo |
| **Mapa conceptual** | ❌ | 🔴 **o plano dava como feito e não está** — `GDD:150-169` é uma lista de 20 locais, não um mapa: sem adjacências, sem distâncias, sem planta, zero imagens no repo. Reaberto no plano. **Bloqueia a FASE 9** |
| Referências visuais finais | ✅ | `Referencias/NOTAS.md` — Twin Peaks/Snoqualmie, Alec Soth, Todd Hido, Gregory Crewdson e o Kodak Gold de 1986, cada um com o que se rouba **e o que não se rouba**. Imagens fora do repo por `.gitignore` |
| Direção artística final | ✅ | **A** (céu encoberto) é a regra de luz; **C** (cor na matéria) nos interiores importantes; **B** (luz praticável) só em momentos narrativos. Regra 1986/2026 confirmada: a geometria nunca muda entre épocas |
| Lista final de assets | ✅ | `Pine_Hollow_Producao.md` §2 — cobre os 22 locais, os 16 itens da FASE 18 e os 13 do slice |
| Limites técnicos | ✅ | `Pine_Hollow_Producao.md` §1 — plataformas, 60 fps, máquina mínima, orçamentos |

Continua a não existir um único asset de arte no projecto — mas já existe a regra que diz como
devem ser feitos.

🔴 **Duas listas de locais que não coincidem:** `Plano:343-364` tem 22 e `GDD:150-169` tem 20. Dezanove
são a mesma coisa com nomes diferentes (Lake Hollow/Lago, Zona montanhosa/Montanha, Town Hall/Câmara
municipal…). Só no GDD: **torre meteorológica**. Só no plano: **Casas**, **Trilhos**, **Miradouro** — e
«Casas» são todas as casas genéricas da vila, uma família inteira de assets. Alinhar antes do blockout.

⚠️ **Os limites técnicos foram derivados, não medidos.** Nada foi corrido em play mode. A primeira
medição real com arte na cena manda sobre os números de `Producao.md` §1.4.

### FASE 1 — Fundação técnica · ✅ fechada (2026-09-21)

| Item | Estado | Nota |
|---|---|---|
| Unity 6 / URP / Input System / TextMeshPro | ✅ | `6000.6.2f1`, URP 17.6, Input System 1.20 |
| Definições gráficas | ✅ | Pós-processamento ligado nas duas câmaras; `Volume` global na cena |
| Qualidade | ✅ | Um só nível (`PC`); o `Mobile` saiu com Android e iOS |
| **Layers** | ✅ | 8 `Interactable`, 9 `Player`, 10 `PhotoOnly`, 11 `IgnorePhoto` |
| **Tags** | ✅ | Nenhuma personalizada — nada no código lê tags. Faltava a built-in `MainCamera`, e essa foi posta |
| **Physics settings** | ✅ | Matriz de colisão revista: `UI` e `PhotoOnly` fora da física |
| Cenas de produção | ✅ | `Bootstrap.unity` criada, primeira na build; `Prototype_Player` a seguir |
| Estrutura de pastas | ✅ | `_Project/` adoptado, `.gitkeep` nas vazias |

**Verificação:** `Assets/_Project/Scripts/Editor/Fase1Validacao.cs` corre em batchmode com
`-executeMethod Fase1Validacao.Correr` e sai a 0. Cobre configuração, não comportamento.

🔴 **O raycast de interação continua sem layer mask — e é de propósito.**
`InteractionSystem.CheckForInteractable()` (`InteractionSystem.cs:48-52`) usa o overload de três
argumentos, sem `layerMask` e sem `QueryTriggerInteraction`, e o projecto mantém
`m_QueriesHitTriggers: 1`. A FASE 1 entregou as layers de que a correcção precisa; a correcção em
si é a `869f4yb37`, na FASE 3. **Continua a ter de estar resolvida antes da Fase 10 (casa).**
O flag global fica a `1` por escolha: mudá-lo alterava o comportamento de todos os queries do
projecto, incluindo os que ainda não existem. A alternativa é passar
`QueryTriggerInteraction.Ignore` na chamada.

⚠️ **A distância de sombras (80 m) é derivada, não medida.** Sai de `Producao.md` §1.9, pelos
3 km² do mapa. Nada correu em play mode com conteúdo a sério. A primeira medição real manda.
Pela mesma razão ficaram por decidir **MSAA vs anti-aliasing de pós-processo** e o **VSync**: são
itens de §1.9 que só se fecham com número medido.

⚠️ **O `DefaultVolumeProfile` era um perfil de teste da Unity.** Traz componentes internos
(`CopyPasteTestComponent1/2/3`, `TestVolume`, `TestAnimationCurveVolumeComponent`,
`VolumeComponentSupportedEverywhere`) que não têm nada que fazer num perfil de projecto. Os quatro
efeitos que a direcção artística proíbe — aberração cromática, distorção de lente, Panini e motion
blur — **foram removidos**, porque ligar o pós-processamento tornava-os vivos. Os componentes de
teste ficaram: limpá-los é arrumação, não risco.

⚠️ **Nada disto foi visto em play mode.** A verificação é de ficheiros e de API, pelo validador em
batchmode. O pós-processamento mudou o que se vê e **ninguém abriu o ecrã**.

### FASE 2 — Player · ~60%

**2.1 Estrutura** ✅ — Player, CharacterController, câmara, ground de teste.

**2.2 Movimento** ✅ — os sete itens, todos em `PlayerController.cs`: WASD, movimento relativo,
gravidade (`-20`, não a do Physics), sprint (`LShift`, só para a frente), mouse look, limite
vertical (`±85°`), cursor lock.

**2.3 Interação básica** ✅ **— e está por marcar no plano.**

| Item | Estado |
|---|---|
| Raycast da câmera | ✅ `InteractionSystem.cs:41-53` |
| Identificar objetos interativos | ✅ |
| Interface `IInteractable` | ✅ |
| Tecla de interação | ✅ `E` |
| Texto contextual | ✅ |
| Feedback visual | ❌ há texto, não há highlight |

**2.4 Sensação** ❌ 0% — sem aceleração/desaceleração, sem head bob, sem footsteps, sem sons por
superfície. A sensibilidade existe como campo mas não há ecrã de definições.

**2.5 Estados** ❌ 0% — não há máquina de estados. Há `bool` espalhados por quatro scripts
(`IsMovementLocked`, `IsLookLocked`, `IsInspecting`, `IsPhotographyMode`, `IsOpen`). Sem crouch,
sem estado de flashback.

### FASE 3 — Sistema de interação · ~65%

| Item | Estado |
|---|---|
| Raycast / Interface / Interactables / Prompt / Distância máxima | ✅ (3 m) |
| Highlight subtil | ❌ |
| Prioridade | ❌ o raycast devolve o primeiro collider e mais nada |

Primeiros objectos: **porta ✅**, **objecto examinável ✅**, gaveta ❌, interruptor ❌, documento ❌,
fotografia ❌. São estes quatro em falta que o Capítulo 1 precisa.

### FASE 4 — Sistema de inspeção · ~70%

Pick up ✅, modo de inspeção ✅, rotação ✅, zoom ✅ (scroll, 0.7–2.5 m), movimento controlado ✅,
sair ✅. Descrições ❌, sons ❌.

Aplicação inicial: só o objecto genérico de teste. Documentos, fotografias e pistas ❌.

🔴 `inspectionDistance` é `[SerializeField]` mas `Inspect()` reescreve-o com `1.5f` fixo
(`InspectionSystem.cs:32`) — o valor do inspector é ignorado.

### FASE 5 — Sistema de fotografia · ~15%, e o número engana

**Feito:** capturar para RenderTexture 256×256, preview, grelha do álbum, viewer em ecrã cheio,
clique na miniatura.

**Por fazer — a checklist inteira da Fase 5:**

| Bloco | Itens | Estado |
|---|---|---|
| Fotografias | Photo Data, data, local, personagens, metadados, frente, **verso**, imagem | ❌ 0/8 |
| Inspeção | zoom, rodar, virar, examinar, ler verso | ❌ 0/5 |
| Comparação | selecionar duas, lado a lado, sobreposição, escala, posição, rotação, **alinhamento** | ❌ 0/7 |
| Descobertas | estruturas, pessoas, portas, sombras, objetos, símbolos, alterações | ❌ 0/7 |

🔴 **O alicerce não suporta o resto.** O que existe é `List<Texture2D>`. Uma `Texture2D` não tem
data, verso, local nem pessoas — e a comparação e o alinhamento são operações sobre esses
metadados, não sobre pixéis. Mais código em cima da lista actual não aproxima o alinhamento.

🔴 **Esses 15% também não são jogáveis:** o álbum não abre (`Tab` é no-op, `OpenAlbum()` não tem
chamador), as fotos são 256×256, vivem só em RAM e nunca são libertadas.

### FASES 6 a 27 · 0%

Investigação/journal/board · world state e flashbacks · vertical slice · blockout · casa · Ethan ·
NPCs · fábrica · túneis · câmara subterrânea · capítulos · áudio · arte · iluminação · UI · saves ·
optimização · QA · polimento · playtests · build · finais. **Nada começado.**

---

## As duas conclusões que mudam o próximo passo

**1. O world state é pré-requisito de tudo o que é narrativo, e não existe.** A Fase 7 pede estado
2026 e estado 1986, e o Capítulo 4 depende de um flashback **criar** uma porta que passa a existir
em 2026. Nenhum script tem noção de época, e o `DoorInteractable` guarda o estado em campos
privados que ninguém lê nem grava. Cada interactable novo escrito sem isto é um que se reescreve.

**2. O vertical slice não precisa da máquina fotográfica.** A lista do GDD §26 e da Fase 8 é:
estrada → casa → estúdio → 1.ª e 2.ª fotografia → pista do Ethan → inspeção → comparação →
primeiro alinhamento → flashback de 1986 → regresso → fotografia alterada. **«Tirar fotografia» não
aparece em nenhuma das duas.** As fotografias do slice são objectos que o jogador **encontra**, não
que tira. A máquina é mecânica real do jogo, mas de capítulos posteriores.

## Ordem proposta

1. **`PhotoData`** — data, local, pessoas, frente, verso, imagem. Desbloqueia o resto da Fase 5 e é
   o que a Fase 21 (saves, «fotografias descobertas») vai persistir.
2. **`PhotoInteractable`** — apanhar uma fotografia do mundo, reusando o `InspectionSystem`, mais
   virar e ler o verso.
3. **Comparação e alinhamento**, sobre `PhotoData`.
4. **World state 2026/1986**, antes do primeiro flashback e não depois.
5. **Primeiro flashback** → milestone 7.

## Dívida a pagar antes de crescer

Barata agora, cara depois — cada uma destas compõe com o conteúdo que vier a seguir.

- [ ] Dar um dono único ao cursor. `PlayerController.HandleCursor()` corre incondicionalmente e
      retranca o cursor ao primeiro clique — mata o álbum e o viewer. Cinco scripts escrevem
      `Cursor.lockState` em oito sítios. O GDD ainda vai acrescentar journal, inventário,
      comparação e investigation board, todos ecrãs de rato.
- [ ] Arbitrar o `Esc`. Quatro scripts lêem `escapeKey.wasPressedThisFrame` no mesmo frame e a
      ordem de `Update` entre MonoBehaviours não é garantida.
- [ ] Ligar o álbum, ou tirar o `Tab` até haver o que mostrar.
- [ ] Layer mask no raycast de interação (+ decidir `QueryTriggerInteraction`).
- [ ] Meter o `Prototype_Player.unity` nas build settings — hoje a build só tem a `SampleScene`
      vazia do template.
- [ ] `[SerializeField] InspectionSystem` no `InspectionInteractable`, em vez do
      `FindFirstObjectByType` por interacção (é o único aviso de compilação do projecto, CS0618).
