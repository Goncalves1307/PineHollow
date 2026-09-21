# Pine Hollow — Produção

> **O que é:** os números que a produção tem de cumprir — plataformas, desempenho, orçamentos de
> geometria e textura — e a lista de assets que daí resulta.
> **O que não é:** as definições do projecto. Este documento **fixa alvos**; quem os configura é a
> FASE 1 (`Pine_Hollow_Plano_Completo_Desenvolvimento.md:97-104`). Nada aqui foi aplicado ao projecto.
>
> Companheiros: `Pine_Hollow_Direccao_Artistica.md` (o que se vê) · `ESTADO.md` (o que está feito).
> Última revisão: 2026-09-21. Valores do projecto conferidos linha a linha contra o YAML nesta data.

---

## 1. Limites técnicos

### 1.0 O que já está fixado no projecto — e o que disso foi decisão

Quase nada foi decidido: o que está no projecto é, tanto quanto se vê, o template do URP tal como
veio. Importa distinguir, porque **o template já está a fixar o orçamento por omissão**.

| Definição | Valor hoje | Onde | É decisão? |
|---|---|---|---|
| Colour space | Linear | `ProjectSettings/ProjectSettings.asset:49` | ✅ certa para realista |
| Rendering mode | **Forward+** (`2`) | `Assets/Settings/PC_Renderer.asset:56` | ❌ omissão do template |
| MSAA | **desligado** (`m_MSAA: 1` = 1 sample) | `Assets/Settings/PC_RPAsset.asset:28` | ❌ omissão |
| Render scale | 1, sem upscaler | `PC_RPAsset:29-30` | ❌ omissão |
| Shadow distance | **50 m**, 4 cascades | `PC_RPAsset:57-58` | ❌ omissão |
| Shadowmap | 2048 principal / 2048 adicionais | `PC_RPAsset:46,50` | ❌ omissão |
| Luzes adicionais por objecto | 4 | `PC_RPAsset:48` | ❌ omissão — e **inerte em Forward+**, ver `§1.4` |
| Depth + Opaque texture | **ambas ligadas** | `PC_RPAsset:22-23` | ❌ omissão, e custa por frame |
| SSAO | ligada, intensidade 0.4 | `PC_Renderer.asset:71+` | ❌ omissão |
| VSync | **desligado** nos dois níveis | `QualitySettings.asset:32,86` | ❌ omissão |
| Modo de ecrã | borderless em resolução nativa | `ProjectSettings.asset:88,116` | ✅ já está certo |
| Janela redimensionável | **não** | `ProjectSettings.asset:104` | ❌ omissão |
| Níveis de qualidade | 2: `Mobile` e `PC` | `QualitySettings.asset:10,64` | ❌ template — o `Mobile` não serve este jogo |
| APIs gráficas | Windows D3D11+D3D12, Android, iOS | `ProjectSettings.asset:482-491` | ❌ **Linux não tem entrada**, e é a máquina de desenvolvimento |

🔴 **Dois ficheiros discordam sobre sombras.** `QualitySettings.asset:70` diz `shadowDistance: 40` e
`shadowCascades: 2`; o `PC_RPAsset:57-58` diz **50 m e 4 cascades**. Com URP activo **os valores do
QualitySettings estão mortos** — quem os lê para orçamentar sombras lê o número errado. Corrigir é
trabalho da FASE 1; aqui fica registado para que nenhum documento volte a citar o ficheiro errado.

### 1.1 Plataformas alvo

| | Plataforma | API | Porquê |
|---|---|---|---|
| **Primária** | Windows 10/11 64-bit | D3D11 (D3D12 opcional) | já é a única com API definida |
| **Secundária** | Linux x86_64 | Vulkan | é a máquina de desenvolvimento; `Plano:740` já a prevê |
| **Fora** | Android, iOS, consolas | — | um open world realista de 3 km² não é um jogo móvel |

Consequência para a FASE 1: **apagar as entradas de Android e iOS** e **o nível de qualidade `Mobile`**,
e acrescentar a entrada de Linux com Vulkan. Manter um nível de qualidade que não serve o jogo é
manter um `Mobile_RPAsset` que alguém há-de editar por engano.

### 1.2 Alvo de desempenho

| Cenário | Resolução | Definições | Alvo |
|---|---|---|---|
| Máquina **recomendada** | 1920×1080 | Altas | **60 fps** estáveis |
| Máquina **mínima** | 1920×1080 | Baixas | **30 fps**, nunca abaixo de 20 (ver nota do VSync) |

60 fps é o alvo de projecto porque o jogo é em 1.ª pessoa com mouse look: abaixo disso o olhar torna-se
desconfortável, e não há combate a justificar exigência maior.

**Modo de ecrã: já está certo e não se mexe.** `ProjectSettings.asset:116` é `fullscreenMode: 1`
(borderless) e `:88` é `defaultIsNativeResolution: 1` — a build arranca em ecrã inteiro na resolução
nativa do monitor. O `defaultScreenWidth: 1024` × `768` de `:44-45` **só se aplica em modo janela**, e
por isso não é o problema que parecia. O que falta mesmo é **`resizableWindow: 0`** (`:104`): quem jogar
em janela não a consegue redimensionar.

**VSync** fica ligado por omissão em ecrã inteiro (hoje está desligado nos dois níveis) e exposto nas
definições. Num jogo lento e contemplativo, o tearing custa mais do que a latência ganha.
⚠️ Com VSync a 60 Hz as taxas possíveis são **60 / 30 / 20**, e não há 25 — por isso o mínimo aceitável
na máquina mínima é **20 fps**, ou o VSync passa a adaptativo nos presets baixos. É uma das decisões
de `§1.9`.

### 1.3 Máquina mínima e recomendada

|  | Mínima | Recomendada |
|---|---|---|
| CPU | 4 núcleos, ~i5-8400 / Ryzen 5 2600 | 6 núcleos, ~i5-12400 / Ryzen 5 5600 |
| GPU | 6 GB VRAM, ~GTX 1060 6GB / RX 580 8GB | 8-12 GB, ~RTX 3060 / RX 6600 |
| RAM | 16 GB | 16 GB |
| Disco | SSD (o mundo carrega por cenas aditivas) | SSD NVMe |
| SO | Windows 10 64-bit / Linux com Vulkan | idem |

A mínima é deliberadamente uma placa de 2016-2017: é o que define os orçamentos abaixo, e é o que
permite que o jogo exista fora de máquinas recentes. **Os orçamentos de `§1.5` a `§1.7` são para a
máquina mínima** — a recomendada é folga, não é outro orçamento.

### 1.4 Orçamento por frame

Dois orçamentos, porque `§1.2` tem dois alvos. **O que manda no conteúdo é o da máquina mínima** — é
nele que se corta geometria; o da recomendada é o que se verifica no fim.

| | Máquina mínima, 30 fps (**33,3 ms**) | Recomendada, 60 fps (**16,6 ms**) |
|---|---|---|
| GPU | ≤ 26 ms | ≤ 12 ms |
| CPU, thread principal | ≤ 20 ms | ≤ 8 ms |
| Render thread | ≤ 20 ms | ≤ 8 ms |

Medidos separadamente no Profiler, não somados: correm em paralelo.

**Geometria visível por frame:**

| | Triângulos | SetPass calls |
|---|---|---|
| Exterior (floresta, vila, estrada) | ≤ 2 000 000 | ≤ 400 SetPass |
| Interior (casa, estúdio, fábrica) | ≤ 1 200 000 | ≤ 250 SetPass |

A métrica são **SetPass calls**, não batches: com o SRP Batcher ligado (`PC_RPAsset:72`) o número de
batches deixa de ser o que dói.

⚠️ **E o que mata a máquina mínima numa floresta não é a contagem de triângulos — é o overdraw.**
Folhagem alpha-tested a 1080p enche a placa a desenhar o mesmo pixel muitas vezes, e o
`m_EnableLODCrossFade: 1` (`PC_RPAsset:33`) acrescenta mais alpha-test nas transições de LOD.
**Alvo: ≤ 3× de overdraw a 1080p em exterior**, medido no modo de visualização de overdraw. Sem este
número, os 2 M de triângulos dão uma falsa sensação de folga.

**Luzes:** 1 direccional com sombra + **no máximo 4 luzes adicionais a iluminar o mesmo pixel**.

🔴 **Este limite é de disciplina, não de motor.** O `m_AdditionalLightsPerObjectLimit: 4` do
`PC_RPAsset:48` **não o impõe em Forward+**: com `m_RenderingMode: 2` (`PC_Renderer.asset:56`) o URP
liga o *cluster light loop*, e aí `GetAdditionalLight()` indexa o cluster directamente em vez de passar
pela lista por objecto — `GetAdditionalLightsCount()` devolve `0` e a contagem por objecto deixa de
existir. Verificado no pacote instalado: `UniversalRenderer.cs:122`, `RealtimeLights.hlsl:247-251` e
`:292-297`. **Consequência prática:** vinte candeeiros numa rua acendem os vinte que caírem no cluster,
e o custo por pixel dispara sem nenhum aviso do motor. Quem iluminar tem de contar à mão — ou a FASE 1
troca Forward+ por Forward, onde o limite volta a valer (`§1.9`).

Luzes adicionais com sombra em tempo real: no máximo **2 visíveis ao mesmo tempo, em qualquer sítio**
— interior ou exterior. Tudo o resto é baked ou sem sombra.

### 1.5 Texturas

| Classe | Resolução máxima | Exemplos |
|---|---|---|
| Superfícies tileable | 2048 | madeira, betão, terra, folhagem |
| Props narrativos (o jogador encosta a cara) | 2048 | máquina fotográfica, documentos, objectos-pista |
| Props correntes | 1024 | mobiliário, ferramentas, louça |
| Props pequenos / decorativos | 512 | garrafas, livros fechados, cabos |
| Detalhe e máscaras | 256 | sujidade, decalques |

Compressão: BC7 para albedo de props narrativos, BC1 para o resto, **BC5 para normal maps**.

⚠️ **Excepção obrigatória: tudo o que tem máscara de recorte não vai a BC1.** BC1 só tem 1 bit de
alpha, e `§2.2` encomenda 8-12 espécies de rasteira, 4 arbustos e 4-6 de árvore — todos alpha-tested.
Em BC1 a folha sai recortada aos blocos de 4×4, ou o importador cai sozinho para BC3/RGBA32 e rebenta
o orçamento de `§1.7` sem ninguém dar conta. **Folhagem e recortes: BC7.** (BC3 é alternativa por compatibilidade ou tempo de compressão, não
por memória: ambos ocupam 8 bpp.)

Mipmaps sempre ligados — num mundo de 3 km² são eles que salvam a largura de banda.

### 1.6 Fotografias — o limite que hoje contradiz o design

🔴 **Hoje as fotografias são 256×256.** Não está no código — o `PhotographySystem.cs:195` lê
`photoRenderTexture.width`, e o 256 vem do asset `Assets/Photography/PhotoRenderTexture.renderTexture:15-16`.
É **dado**, e portanto corrige-se sem tocar em código.

256×256 é incompatível com o jogo que o GDD descreve: sobre as fotografias o jogador **faz zoom**
(`GDD:359`), **compara** (`GDD:360`) e **alinha** (`GDD:376`, secção inteira) — e tudo isso vive de
detalhes que a 256 não existem. Alvo:

| Uso | Resolução |
|---|---|
| Fotografia-objecto (encontrada no mundo, é a do vertical slice) | **2048×1368** |
| Verso da fotografia (textura própria, com a escrita) | 1024×684 |
| Captura in-game, quando a máquina existir | 1024×684 |

O rácio é o de 35 mm, ~3:2 — é o que uma fotografia de 1986 teria. **As fotografias nunca são quadradas.**

⚠️ **As dimensões são múltiplos de 4 de propósito.** O 3:2 exacto daria 2048×1365 e 1024×683, e a
compressão em bloco (BC1/BC5/BC7) exige blocos de 4×4: uma textura de 2048×1365 importa **sem
compressão** — ~11 MB em RGBA32 em vez de ~2,8 MB em BC7. Com frente, verso e versão alterada por
fotografia, isso come o orçamento de `§1.7` em poucas imagens. Os pares escolhidos dão 1,497:1,
a 0,2 % do 3:2 — não se vê.

⚠️ Duas restrições que vêm do código e não da arte, ambas fora desta task:
- a câmara de captura (`PhotoCaptureCamera`, `Prototype_Player.unity:206`) tem
  `m_RenderPostProcessing: 0` (`:235`), portanto capta a imagem **crua** — qualquer look próprio das
  fotografias é trabalho de código, na **FASE 5**, e depende de o `PhotoData` existir primeiro;
- as `Texture2D` capturadas nunca são libertadas (vazam por disparo). A 2048×1368 o vazamento passa
  de incómodo a problema: **42,75× mais memória por foto** — de 192 KB para 8 MB, em `RGB24`
  (`PhotographySystem.cs:196`), que é o formato que o código usa.

### 1.7 Memória

| | Alvo na máquina mínima |
|---|---|
| VRAM total | ≤ 4 GB (placa de 6 GB) |
| Só texturas | ≤ 2,5 GB **residentes** |
| RAM do processo | ≤ 6 GB |
| Tempo de carregamento até jogável | ≤ 20 s em SSD |

⚠️ `streamingMipmapsActive: 0` nos dois níveis de qualidade (`QualitySettings.asset:40,94`): **não há
texture streaming**, logo 2,5 GB são 2,5 GB em memória ao mesmo tempo. Ligar o streaming é uma das
decisões de `§1.9` — e se não for ligado, este número tem de descer.

### 1.8 Os 3 km² contra os 50 m de sombra — a restrição que manda em tudo

`GDD:9` e `Plano:366` fixam **3 km²**. Com shadow distance de 50 m, **a esmagadora maioria do mundo
não tem sombras dinâmicas** — o que só é aceitável com iluminação baked. E aqui a produção cruza com a
arte: se as duas épocas se distinguirem **por luz**, cada interior reutilizado precisa de **dois
conjuntos de lightmaps**, o que duplica o tempo de bake e o espaço em disco desses interiores.

Isto não se decide nesta task. Fica escrito porque é a consequência que liga
`Pine_Hollow_Direccao_Artistica.md` (a regra 1986/2026) ao orçamento, e porque é a razão pela qual a
decisão de arte tem de vir **antes** do blockout e não depois.

O que **está** decidido aqui: **não há streaming nem LODs por agora**, e não há nada no projecto que os
implique — zero terrenos, zero Addressables, zero LODGroups (não há um único modelo). O mundo de 3 km²
será dividido em cenas carregadas aditivamente, e é na FASE 22 que isso se resolve — `Plano:675`
(occlusion culling), `:676` (LOD) e `:681` (streaming), todos por abrir.
Consequência para a lista de assets: **todo o modelo entregue traz LODs**, mesmo antes de haver sistema
que os use, porque acrescentá-los depois é refazer o asset.

### 1.9 O que fica delegado à FASE 1 — com o alvo já fixado

A subtask `[Fase 1] Definições gráficas e níveis de qualidade (URP)` escolhe as definições; este
documento diz o que elas têm de cumprir. Ela decide, dentro destes alvos:

- **MSAA vs pós-processo.** Hoje MSAA está desligado e não há AA nenhum. Com SSAO em DepthNormals
  (`PC_Renderer.asset:78`) já corre um prepass, e empilhar MSAA por cima disso numa placa de 2016 é
  caro; num mundo de folhagem alpha-tested o TAA resolve melhor o mesmo problema. Quem decidir mede.
- **Forward+, Forward ou Deferred.** Forward+ hoje. **Forward** é a opção que devolve o tecto de luzes
  por objecto que o Forward+ desliga (`§1.4`); **Deferred** aguenta muitas luzes mas perde o MSAA.
  Mudar depois obriga a revisitar todos os materiais — decidir **antes** de existir arte, não depois.
- **Shadow distance e cascades** (hoje 50 m / 4). É a decisão que amarra os 3 km² — e 50 m é curto
  para um jogo de olhar longe: a linha onde as sombras acabam vê-se a andar. 80-100 m com 4 cascades
  ainda cabe no orçamento e vale a medição.
- **Render scale e upscaler** (hoje 1, nenhum). É o que dá ou tira folga na máquina mínima.
- **`m_RequireOpaqueTexture`** (hoje ligada): custa por frame e só é precisa se houver água ou refracção.
- **Número de níveis de qualidade** e o fim do `Mobile`.
- **Ligar ou não o texture streaming** (hoje desligado) — decide se o tecto de `§1.7` é realista.
- **VSync adaptativo nos presets baixos**, ou aceitar 20 fps como mínimo (ver nota de `§1.2`).
- **Resolver a divergência 40/50 m** entre `QualitySettings` e `PC_RPAsset`.
- **`resizableWindow`** (`ProjectSettings.asset:104`), hoje a `0`.

### 1.10 Como se verifica que os alvos se cumprem

Nenhum destes números vale sem medição. Quando houver blockout com arte (FASE 9 em diante):

1. Cena de exterior mais densa (vila + floresta) e cena de interior mais carregada (a fábrica), ambas
   percorridas com o Profiler ligado, na máquina mínima ou com render scale equivalente.
2. Ler **GPU frame time** no Profiler e **SetPass calls** e **triângulos** no painel *Stats* da Game
   view — o Frame Debugger mostra a ordem dos passes, não estes números. Comparar com `§1.4`, e medir o
   overdraw no modo de visualização próprio.
3. Memory Profiler para VRAM e texturas, comparar com 1.7.
4. Se falhar, corta-se **detalhe de props** antes de cortar distância de visão: o jogo é de olhar longe.

**Estes alvos são revisíveis.** Foram derivados do que está escrito (3 km², solo, 2-4 h, realista,
URP Forward+), não medidos — nada aqui foi testado em play mode, e a primeira medição real manda.

---

## 2. Lista final de assets

### 2.0 Como se lê

Orçamento de compra hoje: **0 €**. A coluna não é «comprar» — é **candidato a compra**, e reavalia-se
depois do vertical slice. Nada aqui tem preço ou licença confirmados: os termos das lojas de assets
mudaram nos últimos anos e **não foram verificados**. Antes de contar com qualquer candidato, confirmar
preço e licença, e escrevê-los na linha.

**Cada linha 💰 leva preço e licença próprios**, nas colunas `€` e `Lic.`. Enquanto ninguém os
confirmar, ambas ficam a `?` — e um `?` significa *não contes com isto*, não *é grátis*.

| Marca | Significa |
|---|---|
| 💰 | **Candidato a compra** — genérico e repetido. |
| 🔨 | **Fazer** — narrativo e único. Nenhum pack o tem, e comprá-lo seria comprar outro jogo. |
| ♻️ | **Derivar** — sai de outro asset desta lista com retoque, não nasce do zero. |

A regra que governa tudo: **compra-se o que é genérico e se repete; faz-se o que é narrativo e único.**
Num mundo de 3 km² feito por uma pessoa, é a modelar árvores que o projecto morre.
Enquanto o orçamento for 0 €, **todo o 💰 é trabalho a fazer à mão** — a coluna existe para mostrar
onde é que o dinheiro compraria tempo, no dia em que houver.

⚠️ **Todo o modelo entregue traz LODs** e respeita `§1.5` (texturas). Ver `§1.8` para o porquê.

### 2.1 Prioridade 1 — o que o vertical slice precisa, e mais nada

Os 13 itens de `GDD:770-782`. **Cinco não são assets, são sistemas** — estão aqui só para que a lista
não os pareça esquecer: `Sistema de interação` ✅ feito, `Inspeção de fotografia`, `Comparação de
fotografias`, `Primeiro alinhamento` e `Primeiro flashback` são código (FASES 5-7), não arte.

| Item do slice | Assets que obriga | |
|---|---|---|
| Estrada de chegada | asfalto envelhecido, bermas, sinalização, 2-3 árvores, rochas | 💰 |
| Exterior da casa | casa completa (fachada, telhado, alpendre, caixilhos), quintal, cerca | 🔨 casa · 💰 vegetação |
| Casa | interior completo: 4-6 divisões, mobiliário, portas, escada | 💰 mobiliário · 🔨 layout e props-pista |
| Estúdio fotográfico | ampliador, tinas de revelação, varal de negativos, luz vermelha, arquivo | 🔨 **é o coração do jogo** |
| Primeiras fotografias | **as fotografias-objecto**: frente + verso, 2048/1024 (`§1.6`) | 🔨 sempre |
| Thomas em 1986 | personagem visível em flashback | 🔨 bloqueado — ver `§2.5` |
| Regresso a 2026 / Fotografia alterada | segunda versão da mesma fotografia | ♻️ da primeira |

**Nada fora desta tabela entra em produção antes do slice estar de pé.** É a lista mais curta que este
documento tem, e é a única que interessa nos próximos meses.

### 2.2 Mundo — a checklist da `Plano:608-616`, fechada

| Item | Quantidade alvo | | € | Lic. | Nota |
|---|---|---|---|---|---|
| Vegetação | 8-12 espécies de rasteira + 4 arbustos | 💰 | ? | ? | com variação de estação; **BC7, não BC1** (`§1.5`) |
| Árvores | 4-6 espécies, 3 idades cada | 💰 | ? | ? | é o maior custo de desempenho do jogo |
| Rochas | 1 conjunto de 10-15, granito de montanha | 💰 | ? | ? | |
| Estradas | asfalto, terra batida, trilho, mais bermas e juntas | 💰 superfícies · 🔨 traçado | ? | ? | |
| Lama | 3-4 superfícies, com poça e transição | 💰 | ? | ? | o «húmido» do GDD vive aqui |
| Água | lago, rio, poças — shader partilhado | 💰 | ? | ? | ⚠️ obriga a `m_RequireOpaqueTexture` (`§1.0`) |
| Montanha | superfícies de encosta, escarpa, neve de cume | 💰 | ? | ? | |
| Construções | kit modular: paredes, telhados, caixilhos, portas, alpendres | 💰 kit · 🔨 os 4 edifícios narrativos | ? | ? | ver `§2.4` |

### 2.3 Props — a checklist da `Plano:618-626`, fechada

| Item | Quantidade alvo | | € | Lic. | Nota |
|---|---|---|---|---|---|
| Móveis | 1 conjunto de casa dos anos 60-80, ~30 peças | 💰 | ? | ? | serve as duas épocas: as casas não se remobilaram |
| Livros | fechados (💰) e **os que se lêem** (🔨) | ambos | ? | ? | os legíveis são documentos, não decoração |
| Computadores | 1 moderno (2026) + 1 terminal de 1986 | 💰 | ? | ? | o de 1986 é props-marcador da época |
| Telefones | fixo de parede (1986) + telemóvel (2026) | 💰 · ♻️ | ? | ? | |
| Ferramentas | oficina e fábrica, ~20 peças | 💰 | ? | ? | |
| **Equipamento fotográfico** | máquina, lentes, tripé, ampliador, tinas, negativos, álbum | 🔨 | — | — | é a identidade do jogo — não se compra |
| Objetos de 1986 | ~10 marcadores por interior (`Direcção Artística §5`) | 💰 · 🔨 | ? | ? | |
| Objetos modernos | idem, para 2026 | 💰 | ? | ? | |

### 2.4 Os 22 locais do blockout (`Plano:343-364`)

Nem todos custam o mesmo. Três grupos:

**🔨 Narrativos — feitos à mão, um a um** (4): Casa, Fábrica, Cabana, Câmara subterrânea. São eles o
jogo; nenhum pack os tem e cada um existe **em duas épocas**. (O **estúdio fotográfico** é o quinto
espaço feito à mão, mas vive *dentro* da Casa — por isso não conta como local próprio dos 22.)

**💰+♻️ Kit modular — construídos a partir do mesmo conjunto** (10): Centro, Café, Mercearia, Polícia,
Câmara municipal, Biblioteca, Igreja, Motel, Casas, Cemitério. Um kit de construção de vila americana
pequena resolve os dez; a identidade de cada um vem da placa, da montra e dos props, não da geometria.

**💰 Terreno e natureza** (8): Lago, Rio, Ponte, Floresta, Trilhos, Montanha, Miradouro e os Túneis
(estes 💰 kit de túnel + 🔨 os troços que a história usa).

**4 + 10 + 8 = 22.**

⚠️ **As duas listas de locais não coincidem.** `Plano:343-364` tem 22 entradas e `GDD:150-169` tem 20.
**19 são a mesma coisa com nomes diferentes** — Grocery store/Mercearia, Esquadra/Polícia, Town
Hall/Câmara municipal, Lake Hollow/Lago, Zona montanhosa/Montanha. O que difere mesmo:

| Só no GDD | Só no Plano |
|---|---|
| Antiga torre/estação meteorológica (`GDD:169`) | **Casas** (`Plano:352`) · Trilhos (`:360`) · Miradouro (`:362`) |

19 + 1 = 20 e 19 + 3 = 22. **O que importa é o «Casas»**: são as casas genéricas da vila, uma família
inteira de assets que só existe num dos lados. Alinhar antes do blockout — é trabalho do mapa
conceptual, que está por fazer (ver `ESTADO.md`).

### 2.5 Personagens — a lista está bloqueada

As quatro principais estão escritas (`GDD:207-264`). Os secundários **não**: `GDD:265-277` é uma lista de
«**Possíveis** NPCs» — 8 bullets sem nome, sem idade, sem relação. **Não há cast para listar**, e por isso
esta secção fica deliberadamente vazia.

O que se sabe: 🔨 **Thomas em 1986** é obrigatório para o vertical slice (`GDD:780`) e é o único
personagem visível de que o slice precisa. Um humano realista credível é, a solo, o asset mais caro do
projecto — e o único cuja alternativa (nunca o mostrar de corpo inteiro, resolvê-lo por voz, silhueta e
fotografia) **é melhor design e mais barata**. Fica como decisão, não como compra.

### 2.6 Áudio — não está em lista nenhuma e o jogo depende dele

`GDD:74-92` constrói a tensão com **silêncio, sons distantes e ambientes vazios**. Nenhuma checklist de
assets menciona áudio. Mínimo para o slice: ambiente de floresta (dia/noite), vento, chuva, interior de
casa, passos por superfície (4 superfícies × 4 amostras), portas, e o som do estúdio. 💰 tudo, excepto o
que for identidade do jogo.

### 2.7 O que falta às checklists do plano

Três famílias de assets que o jogo precisa e que nenhuma lista prevê:

1. **As fotografias da história.** São o núcleo do jogo e cada uma é um asset feito à mão: frente,
   **verso escrito**, e a versão alterada. Não aparecem na FASE 18. São 🔨 sem excepção.
2. **Documentos legíveis** — recortes de jornal, relatórios da fábrica, cartas. O `GDD` conta com eles e
   a checklist só tem «Livros».
3. **Decalques e sujidade** — o «evoluiu ao longo de décadas» de `GAME_DESIGN:53` faz-se com decalques
   por cima do kit modular. Sem eles, dez edifícios do mesmo kit lêem-se como dez cópias.

### 2.8 Cobertura

Esta lista cobre: os **22 locais** de `Plano:343-364`, os **16 itens** de `Plano:608-626` (Mundo e Props),
e os **13 itens** de `GDD:770-782` — cinco deles marcados como sistemas e não assets. Personagens
secundários ficam por listar, com o motivo escrito em `§2.5`.
