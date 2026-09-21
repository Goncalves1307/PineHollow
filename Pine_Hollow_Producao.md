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

Quase nada foi decidido: o que está no projecto é o template do URP com dois ajustes. Importa
distinguir, porque **o template já está a fixar o orçamento por omissão**.

| Definição | Valor hoje | Onde | É decisão? |
|---|---|---|---|
| Colour space | Linear | `ProjectSettings/ProjectSettings.asset:49` | ✅ certa para realista |
| Rendering mode | **Forward+** (`2`) | `Assets/Settings/PC_Renderer.asset:56` | ❌ omissão do template |
| MSAA | **desligado** (`m_MSAA: 1` = 1 sample) | `Assets/Settings/PC_RPAsset.asset:28` | ❌ omissão |
| Render scale | 1, sem upscaler | `PC_RPAsset:29-30` | ❌ omissão |
| Shadow distance | **50 m**, 4 cascades | `PC_RPAsset:57-58` | ❌ omissão |
| Shadowmap | 2048 principal / 2048 adicionais | `PC_RPAsset:46,50` | ❌ omissão |
| Luzes adicionais por objecto | 4 | `PC_RPAsset:48` | ❌ omissão |
| Depth + Opaque texture | **ambas ligadas** | `PC_RPAsset:22-23` | ❌ omissão, e custa por frame |
| SSAO | ligada, intensidade 0.4 | `PC_Renderer.asset:71+` | ❌ omissão |
| VSync | **desligado** nos dois níveis | `QualitySettings.asset:32,86` | ❌ omissão |
| Resolução de arranque | **1024×768**, janela não redimensionável | `ProjectSettings.asset:44-45,104` | ❌ omissão, 4:3 num jogo de PC |
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
| Máquina **mínima** | 1920×1080 | Baixas | **30 fps**, nunca abaixo de 25 |

60 fps é o alvo de projecto porque o jogo é em 1.ª pessoa com mouse look: abaixo disso o olhar torna-se
desconfortável, e não há combate a justificar exigência maior. **Resolução de arranque: 1920×1080,
janela redimensionável, fullscreen borderless** — substitui o 1024×768 4:3 do template.

**VSync** fica ligado por omissão em ecrã inteiro (hoje está desligado nos dois níveis) e exposto nas
definições. Num jogo lento e contemplativo, o tearing custa mais do que a latência ganha.

### 1.3 Máquina mínima e recomendada

|  | Mínima | Recomendada |
|---|---|---|
| CPU | 4 núcleos, ~i5-8400 / Ryzen 5 2600 | 6 núcleos, ~i5-12400 / Ryzen 5 5600 |
| GPU | 6 GB VRAM, ~GTX 1060 6GB / RX 580 8GB | 8-12 GB, ~RTX 3060 / RX 6600 |
| RAM | 16 GB | 16 GB |
| Disco | SSD (o mundo carrega por streaming) | SSD NVMe |
| SO | Windows 10 64-bit / Linux com Vulkan | idem |

A mínima é deliberadamente uma placa de 2016-2017: é o que define os orçamentos abaixo, e é o que
permite que o jogo exista fora de máquinas recentes. **Todos os números das secções seguintes são para
a máquina mínima** — a recomendada é folga, não é outro orçamento.

### 1.4 Orçamento por frame

A 60 fps há **16,6 ms** por frame. Repartição alvo, medida separadamente no Profiler:

| | Alvo | Limite |
|---|---|---|
| GPU | ≤ 12 ms | 14 ms |
| CPU, thread principal | ≤ 8 ms | 10 ms |
| Render thread | ≤ 8 ms | 10 ms |

**Geometria visível por frame:**

| | Triângulos | Batches (SRP Batcher ligado) |
|---|---|---|
| Exterior (floresta, vila, estrada) | ≤ 2 000 000 | ≤ 1 500 |
| Interior (casa, estúdio, fábrica) | ≤ 1 200 000 | ≤ 800 |

**Luzes:** 1 direccional com sombra + **no máximo 4 luzes adicionais por objecto** (é o que o
`PC_RPAsset:48` já impõe). Luzes adicionais com sombra em tempo real: no máximo 2 visíveis ao mesmo
tempo, e só em interiores. Tudo o resto é baked ou sem sombra.

### 1.5 Texturas

| Classe | Resolução máxima | Exemplos |
|---|---|---|
| Superfícies tileable | 2048 | madeira, betão, terra, folhagem |
| Props narrativos (o jogador encosta a cara) | 2048 | máquina fotográfica, documentos, objectos-pista |
| Props correntes | 1024 | mobiliário, ferramentas, louça |
| Props pequenos / decorativos | 512 | garrafas, livros fechados, cabos |
| Detalhe e máscaras | 256 | sujidade, decalques |

Compressão: BC7 para albedo de props narrativos, BC1 para o resto, **BC5 para normal maps**.
Mipmaps sempre ligados — num mundo de 3 km² são eles que salvam a largura de banda.

### 1.6 Fotografias — o limite que hoje contradiz o design

🔴 **Hoje as fotografias são 256×256.** Não está no código — o `PhotographySystem.cs:195` lê
`photoRenderTexture.width`, e o 256 vem do asset `Assets/Photography/PhotoRenderTexture.renderTexture:15-16`.
É **dado**, e portanto corrige-se sem tocar em código.

256×256 é incompatível com o jogo que o GDD descreve: as fotografias são examinadas em ecrã inteiro
(`GDD:349`), comparadas lado a lado e **alinhadas** (`GDD:376`) — e a comparação vive de detalhes que a
256 não existem. Alvo:

| Uso | Resolução | Rácio |
|---|---|---|
| Fotografia-objecto (encontrada no mundo, é a do vertical slice) | **2048** na maior dimensão | 3:2 (35 mm) |
| Verso da fotografia (textura própria, com a escrita) | 1024 na maior dimensão | 3:2 |
| Captura in-game, quando a máquina existir | 1024 na maior dimensão | 3:2 |

O 3:2 é o rácio de 35 mm — é o que uma fotografia de 1986 teria. **As fotografias nunca são quadradas.**

⚠️ Duas restrições que vêm do código e não da arte, ambas fora desta task:
- a câmara de captura tem `m_RenderPostProcessing: 0` (`Prototype_Player.unity:2069`), portanto capta a
  imagem **crua** — qualquer look próprio das fotografias é trabalho de código;
- as `Texture2D` capturadas nunca são libertadas (vazam por disparo). A 2048 o vazamento passa de
  incómodo a problema: 64× mais memória por foto.

### 1.7 Memória

| | Alvo na máquina mínima |
|---|---|
| VRAM total | ≤ 4 GB (placa de 6 GB) |
| Só texturas | ≤ 2,5 GB |
| RAM do processo | ≤ 6 GB |
| Tempo de carregamento até jogável | ≤ 20 s em SSD |

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
será dividido em cenas carregadas aditivamente, e é na FASE 22 (`Plano:667-675`) que isso se resolve.
Consequência para a lista de assets: **todo o modelo entregue traz LODs**, mesmo antes de haver sistema
que os use, porque acrescentá-los depois é refazer o asset.

### 1.9 O que fica delegado à FASE 1 — com o alvo já fixado

A subtask `[Fase 1] Definições gráficas e níveis de qualidade (URP)` escolhe as definições; este
documento diz o que elas têm de cumprir. Ela decide, dentro destes alvos:

- **MSAA vs pós-processo.** Hoje MSAA está desligado e não há AA nenhum. Com Forward+ o MSAA é viável;
  com SSAO ligada e o mundo cheio de folhagem, o TAA costuma sair melhor. Quem decidir mede.
- **Forward+ ou Deferred.** Forward+ hoje. Mudar depois obriga a revisitar todos os materiais — decidir
  **antes** de existir arte, não depois.
- **Shadow distance e cascades** (hoje 50 m / 4). É a decisão que amarra os 3 km².
- **Render scale e upscaler** (hoje 1, nenhum). É o que dá ou tira folga na máquina mínima.
- **`m_RequireOpaqueTexture`** (hoje ligada): custa por frame e só é precisa se houver água ou refracção.
- **Número de níveis de qualidade** e o fim do `Mobile`.
- **Resolver a divergência 40/50 m** entre `QualitySettings` e `PC_RPAsset`.

### 1.10 Como se verifica que os alvos se cumprem

Nenhum destes números vale sem medição. Quando houver blockout com arte (FASE 9 em diante):

1. Cena de exterior mais densa (vila + floresta) e cena de interior mais carregada (a fábrica), ambas
   percorridas com o Profiler ligado, na máquina mínima ou com render scale equivalente.
2. Ler **GPU frame time**, **SetPass calls** e **triângulos** no Frame Debugger, comparar com 1.4.
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

| Marca | Significa |
|---|---|
| 💰 | **Candidato a compra** — genérico e repetido. Preço e licença **por confirmar**. |
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

| Item | Quantidade alvo | | Nota |
|---|---|---|---|
| Vegetação | 8-12 espécies de rasteira + 4 arbustos | 💰 | com variação de estação |
| Árvores | 4-6 espécies, 3 idades cada | 💰 | é o maior custo de desempenho do jogo |
| Rochas | 1 conjunto de 10-15, granito de montanha | 💰 | |
| Estradas | asfalto, terra batida, trilho, mais bermas e juntas | 💰 superfícies · 🔨 traçado | |
| Lama | 3-4 superfícies, com poça e transição | 💰 | o «húmido» do GDD vive aqui |
| Água | lago, rio, poças — shader partilhado | 💰 | ⚠️ obriga a `m_RequireOpaqueTexture` (`§1.0`) |
| Montanha | superfícies de encosta, escarpa, neve de cume | 💰 | |
| Construções | kit modular: paredes, telhados, caixilhos, portas, alpendres | 💰 kit · 🔨 os 4 edifícios narrativos | ver `§2.4` |

### 2.3 Props — a checklist da `Plano:618-626`, fechada

| Item | Quantidade alvo | | Nota |
|---|---|---|---|
| Móveis | 1 conjunto de casa dos anos 60-80, ~30 peças | 💰 | serve as duas épocas: as casas não se remobilaram |
| Livros | fechados (💰) e **os que se lêem** (🔨) | ambos | os legíveis são documentos, não decoração |
| Computadores | 1 moderno (2026) + 1 terminal de 1986 | 💰 | o de 1986 é props-marcador da época |
| Telefones | fixo de parede (1986) + telemóvel (2026) | 💰 · ♻️ | |
| Ferramentas | oficina e fábrica, ~20 peças | 💰 | |
| **Equipamento fotográfico** | máquina, lentes, tripé, ampliador, tinas, negativos, álbum | 🔨 | é a identidade do jogo — não se compra |
| Objetos de 1986 | ~10 marcadores por interior (`Direcção Artística §5`) | 💰 · 🔨 | |
| Objetos modernos | idem, para 2026 | 💰 | |

### 2.4 Os 22 locais do blockout (`Plano:343-364`)

Nem todos custam o mesmo. Três grupos:

**🔨 Narrativos — feitos à mão, um a um** (5): Casa do protagonista, Fábrica abandonada, Câmara
subterrânea, Cabana, Estúdio fotográfico (dentro da casa). São eles o jogo; nenhum pack os tem e cada um
existe **em duas épocas**.

**💰+♻️ Kit modular — construídos a partir do mesmo conjunto** (10): Centro, Café, Mercearia, Polícia,
Câmara municipal, Biblioteca, Igreja, Motel, Casas, Cemitério. Um kit de construção de vila americana
pequena resolve os dez; a identidade de cada um vem da placa, da montra e dos props, não da geometria.

**💰 Terreno e natureza** (7): Lago, Rio, Ponte, Floresta, Trilhos, Montanha, Miradouro, mais os Túneis
(💰 kit de túnel + 🔨 os troços que a história usa).

⚠️ **Discrepância entre as duas listas de locais.** `Plano:343-364` tem 22 entradas e `GDD:150-169` tem
20, e não são a mesma lista: o GDD tem **Antiga torre/estação meteorológica** e **Zona montanhosa**, que
o Plano não tem; o Plano tem **Trilhos** e **Miradouro**, que o GDD não tem. Alinhar as duas antes do
blockout — é trabalho do mapa conceptual, que está por fazer (ver `ESTADO.md`).

### 2.5 Personagens — a lista está bloqueada

As quatro principais estão escritas (`GDD:207-264`). Os secundários **não**: `GDD:265-277` é uma lista de
«**Possíveis** NPCs» — 8 bullets sem nome, sem idade, sem relação. **Não há cast para listar**, e por isso
esta secção fica deliberadamente vazia.

O que se sabe: 🔨 **Thomas em 1986** é obrigatório para o vertical slice (`GDD:776`) e é o único
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
