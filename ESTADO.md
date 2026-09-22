# Pine Hollow — Estado do desenvolvimento

> Auditoria das 27 fases do `Pine_Hollow_Plano_Completo_Desenvolvimento.md` contra o código real.
> **Última verificação: 2026-09-22** (revista no fim da FASE 6), contra a árvore de trabalho
> (`Assets/_Project/Scripts/`, `Assets/_Project/Scenes/Prototype_Player.unity`,
> `ProjectSettings/`). A FASE 1 fechou nesse dia e os caminhos mudaram: tudo vive em
> `Assets/_Project/` agora.
>
> Este ficheiro existe porque **as checkboxes do plano estão desactualizadas** e um agente (ou uma
> pessoa) que confie nelas reimplementa sistemas que já existem. Quando isto e o plano
> discordarem, **o código é o facto**. Ver também `AGENTS.md`.

## Veredicto

Dos **12 milestones** do plano: **5 fechados, o 6.º a meio, 6 por abrir.** Fora dessa lista, a
FASE 6 (investigação) fechou a 2026-09-22.

| # | Milestone | Estado |
|---|---|---|
| 1 | Player funcional | ✅ |
| 2 | Player + interação | ✅ |
| 3 | Objeto examinável | ✅ |
| 4 | Primeira fotografia | ✅ — `PhotoData`, fotografias autoradas, álbum vivo |
| 5 | Duas fotografias + comparação | ✅ — lado a lado e sobreposição, com escala/posição/rotação |
| 6 | Alinhamento | ⚠️ parcial — detecção, clarão e gatilho sim; o flashback é FASE 7 |
| 7 | Primeiro flashback | ❌ |
| 8 | Alteração da fotografia | ❌ |
| 9 | Vertical Slice completo | ❌ |
| 10 | Pine Hollow | ❌ |
| 11 | Conteúdo narrativo completo | ❌ |
| 12 | Polimento e lançamento | ❌ |

Código total: **37 scripts** de jogo, duas cenas (`Bootstrap` e `Prototype_Player`), mais
quatro scripts de editor (validador da FASE 1, montagem e capturas da FASE 5, montagem da
FASE 6) e **211 testes EditMode** em catorze ficheiros.

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

✅ **O raycast de interação já tem layer mask** (`869f4yb37`, FASE 3). `InteractionSystem.cs:17`
tem o `raycastMask` e a chamada passa `QueryTriggerInteraction.Ignore`; o flag global
`m_QueriesHitTriggers` fica a `1` por escolha. ⚠️ **A máscara não é «só a layer Interactable», e
não pode passar a ser**: o raio tem de considerar a geometria do mundo, senão o foco atravessa as
paredes. Quem responde decide-se no fim, por ter `IInteractable`.

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

⚠️ **Duas coisas da FASE 1 ficaram deliberadamente por mexer, e ficam aqui para não voltarem a
desaparecer.** O **render scale** (`PC_RPAsset.asset:29`, `m_RenderScale: 1`) fica a `1`: o alvo é
1080p nativo e baixá-lo é matéria de medição, não de arrumação. E as **layer masks do renderer**
(`PC_Renderer.asset:42-47`, `m_OpaqueLayerMask`/`m_TransparentLayerMask` a `4294967295`) ficam a
tudo de propósito — o filtro de `PhotoOnly`/`IgnorePhoto` é feito no *culling mask* das câmaras, que
é onde deve ser feito; apertar também as do renderer duplicava a mesma regra em dois sítios e
faria objectos desaparecerem sem que ninguém percebesse porquê.

⚠️ **Nada disto foi visto em play mode.** A verificação é de ficheiros e de API, pelo validador em
batchmode. O pós-processamento mudou o que se vê e **ninguém abriu o ecrã**.

### FASE 2 — Player · ~90%

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

**2.4 Sensação** ✅ — aceleração/desaceleração e head bob feitos em `PlayerController.cs` (o bob
compõe com a altura do agachar, não é absoluto). Footsteps e sons por superfície **já soam**: nove
clips CC0 em `Assets/_Project/Audio/Footsteps/` (madeira e terra), com `CREDITOS.md`. ⚠️ `Stone/`
continua vazia, e o `CREDITOS.md` diz por que ordem é que as superfícies que faltam interessam —
betão e metal primeiro, que é o chão da fábrica. A sensibilidade existe como campo; o ecrã de
definições é FASE 12.

**2.5 Estados** ✅ — `PlayerStateMachine.cs` é o dono único do estado, do cursor e do `Esc`.
`PlayerState.cs` tem os sete estados do plano (`Normal`, `Sprinting`, `Crouching`, `Interacting`,
`Inspecting`, `Photographing`, `Flashback`) mais as três camadas de UI que o `Esc` precisava de
distinguir (`PhotoPreview`, `ViewingAlbum`, `ViewingPhoto`). Crouch em `LeftCtrl`, com verificação
de tecto antes de levantar. **`Interacting` e `Flashback` estão declarados mas ainda sem
transições** — a interacção de hoje é instantânea, e o `Flashback` espera o world state da FASE 7.

### FASE 3 — Sistema de interação · ✅ **fechada**

| Item | Estado |
|---|---|
| Raycast / Interface / Interactables / Prompt / Distância máxima | ✅ (3 m) |
| Layer mask e oclusão | ✅ `869f4yb37` |
| Highlight subtil | ✅ `HighlightSystem.cs`, por `MaterialPropertyBlock` |
| Prioridade | ⚠️ o raycast devolve o primeiro collider; nunca fez falta até hoje |

Primeiros objectos: **porta ✅**, **objecto examinável ✅**, **gaveta ✅**, **interruptor ✅**,
**documento ✅**, fotografia ❌ (é FASE 5).

🔴 **O realce vai por `MaterialPropertyBlock` e nunca por `renderer.material`**: o projecto não tem
materiais próprios e todos os objectos partilham o material por omissão — pintar um pintava a cena
inteira. E é matéria a aquecer sobre o `_BaseColor`, **nunca emissão**: com a keyword `_EMISSION`
desligada, escrever `_EmissionColor` não produz efeito nenhum e o realce sai mudo sem erro.

### FASE 4 — Sistema de inspeção · ✅ **fechada**

Pick up ✅, modo de inspeção ✅, rotação ✅, zoom ✅ (scroll, 0.7–2.5 m), movimento controlado ✅,
sair ✅, **descrições ✅**, **sons ✅** (pegar e pousar; oito clips CC0 em
`Assets/_Project/Audio/Inspection/`).

A descrição vive numa linha própria do HUD da inspecção e **não** no `ReadingSystem` — aquele painel
é opaco e centrado, e taparia o objecto que se está a descrever. Entra por parâmetro **opcional**
(`Inspect(target, descricao = null)`), para o `PhotoInteractable` da FASE 5 poder reutilizar o
sistema sem estorvo.

Aplicação: o objecto genérico de teste ✅ e os documentos ✅ (`DocumentInteractable` +
`ReadingSystem`). Fotografias ❌ (FASE 5) e pistas ❌ (FASE 6).

✅ `inspectionDistance` já é respeitado (corrigido na FASE 2), e desde a FASE 4 **também é
limitado**: um valor de inspector fora de 0.7–2.5 punha o objecto onde o scroll não chegava.

⚠️ **O som de rodar ficou deliberadamente de fora.** `PlayOneShot` serve eventos pontuais; um som
de rotação é um *loop* com volume em função da velocidade angular, padrão que o projecto não tem.

🔴 **Os clips de inspecção foram escolhidos por medição, não de ouvido** — ver o `CREDITOS.md` da
pasta. Falta o veredicto em play mode.

### FASE 5 — Sistema de fotografia · ✅ fechada (2026-09-21), menos o flashback

> Revisto no fim da corrida da FASE 5 (task `869f4yfg4`). **O número antigo («~15%, e o número
> engana») estava certo sobre o protótipo e já não descreve o que existe.**

| Bloco | Itens | Estado |
|---|---|---|
| Fotografias | Photo Data, data, local, personagens, metadados, frente, **verso**, imagem | ✅ 8/8 — `PhotoData.cs`, autoradas em `PhotoAsset` |
| Inspeção | zoom, rodar, virar, examinar, ler verso | ✅ 5/5 — os três primeiros vinham da FASE 4; **virar** e **ler verso** são novos (`Q`) |
| Comparação | selecionar duas, lado a lado, sobreposição, escala, posição, rotação, **alinhamento** | ✅ 7/7 — `PhotoComparisonSystem.cs` |
| Descobertas | estruturas, pessoas, portas, sombras, objetos, símbolos, alterações | ⚠️ o tipo e a revelação existem (`PhotoDiscovery.cs`, 7 categorias); **o conteúdo é FASE 18** |

**O alicerce passou a suportar o resto.** `capturedPhotos` é `List<PhotoData>` e não
`List<Texture2D>`: data, local, pessoas, metadados, frente, verso e imagem. As **encontradas** são
`PhotoAsset` autorados no editor (o primeiro `ScriptableObject` do projecto) e entregam sempre uma
**cópia** — revelar uma descoberta é estado de jogo e escrevê-lo no asset sujava o ficheiro em
disco. As **tiradas** nascem de `PhotoData.DeCaptura` e distinguem-se por um campo, não por dois
caminhos de código.

**O álbum está vivo.** O `Tab` abre e fecha; o painel é activado **antes** do `OpenAlbum()`,
porque o `PhotoAlbumSystem` vive dentro dele e com ele desligado o `Update` não corre. Clique
esquerdo na miniatura abre o viewer, direito marca para comparar; ao marcar a segunda, a
comparação abre e a selecção é consumida.

**O alinhamento é declarado, não adivinhado.** Um `PhotoAlignment` numa fotografia diz com qual
alinha, em que posição, rotação e escala, e com que tolerância. Basta declarar de um lado: lido do
outro, o alvo vem invertido a sério (semelhança 2D — a translação também roda e escala). Alinhar
acende um clarão e revela as descobertas daquele par. O gatilho sai por `ConsumirAlinhamento()`,
uma vez só, à maneira do `ConsumeBack` — **é o gancho que a FASE 7 vai consumir.**

**Há conteúdo no jogo:** `Fotografia_Estudio_1986` e `Fotografia_Estudio_1994` em
`Assets/_Project/Photography/`, com alinhamento declarado entre si e uma descoberta que só sai
desse alinhamento. O verso de 1986 é canon à letra. Estão na cena como objectos apanháveis
(`PhotoInteractable`), em cubos finos de 18×12 cm — **um quad tem uma face só e desaparecia ao
virar**. Sem arte: levam um material provisório da paleta (`Fotografia_SemImagem.mat`).

🔴 **O que continua por fazer nesta fase:** o **flashback** que o alinhamento dispara é a FASE 7 e
não existe — nenhum script tem noção de época. E o bloco «Descobertas» tem o mecanismo mas só uma
descoberta autorada.

⚠️ **A captura de ecrã em jogo continua a 256×256 e só em RAM.** As texturas passam a ser
destruídas no `OnDestroy` (as autoradas não se tocam, são assets), mas a lista continua a crescer
sem limite dentro de uma sessão. As **autoradas** têm a resolução validada a 2048×1368 — um aviso
no `OnValidate`, que nunca dispara enquanto não houver imagem nenhuma.

### FASE 6 — Sistema de investigação · ✅ fechada (2026-09-22)

> Task `869f4yfgg` e as duas subtasks. O board entrou nesta corrida **por decisão do Diogo** —
> a recomendação era adiá-lo até a FASE 11 dar conteúdo para organizar.

| Bloco | Itens | Estado |
|---|---|---|
| Registo | modelo, arquivo, categorias fechadas | ✅ `Conhecimento.cs`, `RegistoDeConhecimento.cs` |
| Journal | pessoas, locais, datas, fotografias, documentos, descobertas | ✅ 6/6 — `JournalSystem.cs`, tecla `J` |
| Investigation Board | ligações do jogador, sem setas automáticas, sem checklist | ✅ 3/3 — `InvestigationBoardSystem.cs`, tecla `B` |
| Progressão | a cadeia do GDD §17 | ✅ dez elos à letra em `Investigation/CadeiaDeConhecimento.asset` |

**O que faltava não eram os ecrãs — era o andar de baixo.** Não havia registo de conhecimento
nenhum, e as três coisas que o jogo já produzia não desaguavam em lado nenhum: a
`DescobertasReveladas` da comparação é limpa a cada alinhamento (é o registo do *último*, não um
arquivo), o `HasBeenRead` do documento não tinha leitor, e apanhar uma fotografia só a punha no
álbum. As três passaram a escrever no `RegistoDeConhecimento`.

**O registo vive no `Bootstrap`** — é o objecto que sobrevive às trocas de cena que a FASE 7 vai
fazer. É alcançado por `RegistoDeConhecimento.Instancia` e não por `[SerializeField]` porque o
Unity **não serializa referências entre cenas**. Entrar directamente no `Prototype_Player` cria
um registo de emergência (`Automatico == true`), e o autorado toma-lhe o lugar quando aparece,
levando com ele o que o outro já tinha aprendido.

**A regra dura do GDD está em código, não em intenção.** O `LigacaoDoQuadro` não tem onde guardar
um veredicto — só dois ids —, `AlternarLigacao` é a única porta por onde nasce uma ligação, e não
há contador de «x de y». Três testes seguram cada uma dessas invariantes.

**Progressão sem checklist:** a cadeia do §17 mostra a frase dos elos sabidos e um traço nos
outros. Sem número a dizer quantos faltam — isso seria contar quanta história falta.

⚠️ **Com o conteúdo de hoje acendem-se três dos dez elos** (Thomas, Thomas aparece em 1994, as
fotografias ligam diferentes épocas). Os outros sete estão à espera das descobertas da FASE 11 —
os `exigeId` do asset são o contrato com essa fase.

⚠️ **Os cartões do quadro não têm onde crescer.** Nove cabem numa grelha de três colunas; com o
conteúdo da FASE 11 vão transbordar por baixo. Falta scroll ou zoom no quadro.

### FASES 7 a 27 · 0%

World state e flashbacks · vertical slice · blockout · casa · Ethan · NPCs · fábrica · túneis ·
câmara subterrânea · capítulos · áudio · arte · iluminação · UI · saves · optimização · QA ·
polimento · playtests · build · finais. **Nada começado** — mas a FASE 7 já tem por onde entrar:
`PhotoComparisonSystem.ConsumirAlinhamento()`, que continua sem consumidor.

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

- [x] **Dar um dono único ao cursor** — FASE 2. `PlayerStateMachine` é a única classe que escreve
      `Cursor.lockState`/`Cursor.visible`. Antes eram **4 scripts em 9 escritas** (o `ESTADO.md` e
      o `AGENTS.md` diziam «cinco scripts em oito sítios»; a contagem estava errada e propagou-se
      daqui para a descrição da task).
- [x] **Arbitrar o `Esc`** — FASE 2. Um único leitor de `escapeKey`, e os sistemas pedem
      `ConsumeBack(o-seu-modo)`: só o modo no topo da pilha recebe `true`. Eram **7 linhas em 4
      scripts** (não 5). A máquina corre a `[DefaultExecutionOrder(-100)]` para registar o `Esc`
      antes de qualquer consumidor.
- [x] **Ligar o álbum** — FASE 5. O `Tab` abre e fecha, e o painel é activado antes do
      `OpenAlbum()`. Era o defeito que tornava inverificável tudo o que estava a jusante.
- [x] Layer mask no raycast de interação (+ `QueryTriggerInteraction`) — FASE 3.
- [x] **Meter o `Prototype_Player.unity` nas build settings** — já estava feito desde `00df4f8`
      (FASE 1) e este ficheiro continuava a pedi-lo.
- [x] `[SerializeField] InspectionSystem` no `InspectionInteractable` — FASE 4. O projecto
      compila com **zero avisos** e é assim que se mantém.
- [ ] **O contraste da linha de descrição da inspecção.** Texto claro sobre o chão bege da cena:
      a captura da FASE 5 mostrou que não se lê. Tentou-se um fundo escuro e reverteu-se — um
      filho do texto desenha-se por cima dele, e um irmão fica inactivo porque quem liga aquela
      linha é o `InspectionSystem` e ele não sabe do fundo. Resolve-se dando-lhe a referência.
- [ ] **A lista de capturas cresce sem limite dentro de uma sessão.** Só é libertada no
      `OnDestroy`.
- [ ] **O quadro de investigação não tem scroll.** Com mais de ~12 cartões a grelha sai da
      superfície. Barato agora, caro quando a FASE 11 despejar as descobertas do Ethan.
- [ ] **O `local` das duas fotografias autoradas é o sítio onde a fotografia ESTÁ, não onde foi
      tirada.** As duas dizem «Estúdio do Ethan» e a frente da de 1986 diz «Três pessoas à porta
      da fábrica». O caderno mostra o que lá está, portanto mostra o dado errado. É conteúdo e
      mexe em canon — ficou fora do âmbito da FASE 6 por decisão.
