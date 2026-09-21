# Pine Hollow — Direcção artística

> **Estado: fechado a 2026-09-21.** Direcção **A** como regra de luz, **C** nos interiores
> importantes, **B** só em momentos narrativos (§3). Regra 1986/2026 confirmada (§5). Referências
> em `Referencias/NOTAS.md` (§4) e paleta com valores na §7.
> Fica em aberto um só ponto, e é de gosto: **quanto grão e registo de lente** tem o jogo em si.
>
> Companheiro: `Pine_Hollow_Producao.md` (o que custa) · Última revisão: 2026-09-21.

---

## 1. De onde se parte: três linhas

Antes desta task, tudo o que o projecto dizia sobre imagem era isto:

- `Pine_Hollow_GDD_v0.1.md:7` — **«Estilo visual: Realista»**. É uma etiqueta de género, não direcção.
- `Pine_Hollow_GDD_v0.1.md:146` — «É fria, húmida e rodeada por floresta.»
- `Pine_Hollow_GAME_DESIGN.md:46` — «clima frio e húmido», numa lista de características da vila.

As duas últimas descrevem **o clima de Pine Hollow**, não o registo visual do jogo. Não existia paleta,
referência, regra de luz, nem um único nome de filme, jogo ou fotógrafo citado em todo o repositório.
Zero imagens: não há `.png` nem `.jpg` em lado nenhum, e `Assets/Art/` está vazia.

O que **existe** e vale ouro é a §2.4 Atmosfera (`GDD:74-92`): silêncio, ambientes vazios, alterações
subtis, sons distantes. Mas é direcção de **atmosfera** — não diz uma palavra sobre imagem. E
`GAME_DESIGN:53` dá a frase que mais se aproxima de um princípio visual:

> «A cidade deve parecer um local real que evoluiu ao longo de décadas. Não deve parecer construída
> como um mapa de videojogo linear.»

## 2. A restrição que torna este jogo diferente de «mais um jogo atmosférico»

O núcleo mecânico do Pine Hollow é **comparar uma fotografia com o mundo** e depois **alinhá-la**
(`GDD:349` e `GDD:376`). Isso muda a natureza da direcção artística: aqui a imagem não é decoração,
é **a interface**. Daí saem quatro regras que qualquer direcção escolhida tem de respeitar:

1. **Legibilidade antes de humor.** Se a divisão estiver escura ou ruidosa demais, o jogador não
   consegue casar a fotografia com o que vê — e a mecânica falha em silêncio, parecendo bug.
2. **Silhueta antes de detalhe.** O alinhamento faz-se por contornos: ombreiras, caixilhos, cumeeiras.
   Esses contornos têm de ler-se a contraluz e ao longe.
3. **Nada de pós-processamento que mova a imagem.** Aberração cromática forte, distorção de lente e
   motion blur **sabotam o alinhamento** — o jogador alinha contra uma imagem que a lente deslocou.
4. **O «impossível» precisa do «normal».** `GDD:87` conta com «fotografias impossíveis»: uma imagem só
   choca se o mundo à volta for calmo e consistente. O estranho paga-se com normalidade acumulada.

## 3. As três direcções — escolhe-se uma

Os eixos que as separam: **de onde vem a cor** (da luz ou da matéria), **quão dura é a luz**, e **quanto
do registo vem da lente**. O custo está em tempo de arte, que neste projecto é o recurso escasso.

### A — «Dia encoberto permanente»

Céu coberto de fim de Outono. Luz difusa e sem direcção forte, sombras abertas, contraste médio,
saturação baixa mas presente — cinzentos esverdeados, madeira molhada, asfalto escuro. A cor vem da
luz do céu e é sempre a mesma.

- **Custo: baixo.** Uma direccional fraca + HDRI de céu encoberto. Lightmaps simples e rápidos.
- **Mecânica: a melhor.** Legibilidade máxima a toda a hora; a fotografia casa sempre com o mundo.
- **Risco:** 3 km² de cinzento uniforme cansam antes das 2 horas. Precisa que a variedade venha da
  matéria e da composição, não da luz — e isso é trabalho de blockout, não de iluminação.

### B — «Hora azul e luz praticável»

O exterior vive numa luz fria e baixa, quase sem cor; **toda a cor quente vem de fontes dentro do mundo**
— janelas acesas, candeeiros de rua, a lanterna, o brilho da fábrica. Contraste alto.

- **Custo: alto, e pior do que parece.** `Pine_Hollow_Producao.md` §1.4 permite **2 luzes com sombra em
  tempo real** ao mesmo tempo — e B quer dezenas, em exterior. Pior: em Forward+ o motor **não trava** o
  número de luzes que iluminam o mesmo pixel (§1.4), por isso uma rua de candeeiros não dá erro, dá
  quebra de fps. Obriga a baked com precisão, e a contar luzes à mão.
- **Mecânica: arriscada.** É a que melhor serve a estranheza do `GDD:74-92` — mas viola a regra 1 em
  metade dos sítios, e o alinhamento nocturno é desconfortável.
- **Nota:** funciona muito melhor como **direcção de momento** (o Capítulo em que a cidade se esvazia)
  do que como direcção do jogo inteiro.

### C — «Outono húmido, a cor está na matéria»

A luz é a de A, neutra e encoberta. **A cor vem toda das superfícies**: folha morta, musgo, ferrugem,
tinta lascada, betão manchado, cobre oxidado. A paleta é rica, mas é rica ao perto.

- **Custo: o mais alto**, e no sítio errado: cada material precisa de variação autorada à mão, e é
  exactamente isso que **um orçamento de 0 € e um programador sozinho** não têm. Com packs genéricos, C
  degrada para A com pior desempenho.
- **Mecânica: boa.** Legibilidade de A, mais informação para a comparação encontrar.
- **É a que melhor serve `GAME_DESIGN:53`** — a cidade que envelheceu ao longo de décadas está na matéria,
  não na luz.

### ✅ Decidido (2026-09-21)

**A é a direcção do jogo.** É a regra de luz: céu encoberto de fim de Outono, difusa e sem direcção
forte, sombras abertas, contraste médio, saturação baixa mas presente. Vale em todo o lado por
omissão, e é a que garante a regra 1 da §2 — a fotografia casa sempre com o mundo.

**C aplica-se aos interiores importantes** — casa do Ethan, estúdio, fábrica, câmara subterrânea. É
a regra de **matéria**: aí a cor vem das superfícies e não da luz, e o custo de autorar desgaste
paga-se porque é onde o jogador está perto e olha de verdade. A e C não competem: A diz como se
ilumina, C diz de que é feito.

**B fica para momentos narrativos**, não para o jogo inteiro — o capítulo em que a cidade se esvazia,
a fábrica de 1986 a trabalhar. Cada uso de B é uma excepção com nome, e paga o preço da §3: em
Forward+ o motor não trava as luzes por pixel, portanto conta-se à mão.

Com 0 € de orçamento e uma pessoa, A é o único arranque possível; C entra onde se justifica, uma
divisão de cada vez.

## 4. Referências visuais — o que eu não posso fazer

Não tenho como ir buscar imagens, e **uma lista de referências inventada é pior do que nenhuma**: dá ar
de decidido a uma coisa que não foi vista. O que fica montado é o sítio e o método.

**Protocolo:** criar `Referencias/` na raiz, com `01_Exterior/`, `02_Interiores/`, `03_Luz/`,
`04_Fotografia_1986/`, `05_Materia/`. Vinte imagens por pasta chegam; mais do que isso é indecisão.
Cada pasta leva um `NOTAS.md` com uma linha por imagem — **o que exactamente se está a roubar dali**
(«a forma como a luz da janela morre a 2 m», não «gosto disto»).

⚠️ **Antes de as pôr no repositório**, ver o `.gitignore`: imagens de referência não são assets do jogo,
mas ocupam espaço em cada clone. Alternativa: `Referencias/` ignorada e guardada fora do repo, com o
`NOTAS.md` versionado. Decisão tua.

**Candidatos a validar** (não são canon — são pontos de partida que tens de ver e aprovar ou rejeitar):

| Direcção | Onde olhar |
|---|---|
| A / C | fotografia documental de cidades industriais em declínio no Noroeste americano; Alec Soth |
| B | Todd Hido (casas de subúrbio à noite, janelas acesas); Gregory Crewdson (luz praticável encenada) |
| Fotografia de 1986 | filme 35 mm a cores da época — o grão, o desvio de cor e o desgaste do papel |
| Interiores húmidos | fotografia de interiores abandonados, **sem** a saturação habitual do género |

## 5. A regra 1986/2026 — proposta, e quase imposta pela mecânica

`GDD:133` conta com **«reutilização inteligente de locais entre 1986 e 2026»**, e nunca diz como. É a
decisão que faz o jogo caber num orçamento solo. Proposta:

> **A geometria nunca muda entre épocas. Muda a luz, o grading, o estado de um punhado de superfícies,
> e um pequeno conjunto de props-marcador.**

**Porque é quase imposta:** o jogador alinha uma fotografia de 1986 com uma divisão de 2026
(`GDD:376`). Para o alinhamento ser possível, **a geometria das duas épocas tem de ser a mesma até ao
caixilho**. Se a parede mudar de sítio, não há alinhamento — há um puzzle impossível. A mecânica do jogo
já decidiu esta parte; o que fica em aberto é o sabor.

| | Muda entre épocas | Não muda |
|---|---|---|
| Geometria e layout | ❌ nunca | ✅ idênticos |
| Luz e grading | ✅ é aqui que vive a diferença | |
| Estado de superfície | ✅ **3 a 5 por divisão** (tinta, vidro, vegetação, ferrugem) | as restantes |
| Props | ✅ **até 10 marcadores por interior** | a maioria fica |
| Som | ✅ a fábrica ligada, vozes ao longe | |

**1986** é quente e habitado: a fábrica trabalha, as janelas estão acesas, a tinta é recente, a
vegetação está cortada. **2026** é frio e encoberto: a fábrica está parada, as janelas cegas, a tinta
lascada, a floresta entrou pelos quintais. É a leitura que `Pine_Hollow_Historia.md:237-243` já dá em
prosa — «A fábrica está ativa.» (`:239`), «As casas estão habitadas.» (`:241`) — traduzida em regras.

**Custo:** ~1,3× por interior reutilizado, em vez de 2×. **Mas** — ver `Pine_Hollow_Producao.md` §1.8 —
distinguir épocas por luz obriga a **dois conjuntos de lightmaps** por interior, e é isso que decide se
cada época é uma cena aditiva própria. Essa decisão é da FASE 7/10, não desta task.

## 6. O que bloqueia, hoje

🔴 **Nada disto se vê no jogo até alguém ligar o pós-processamento.** A cena jogável
`Assets/Prototype_Player.unity` **não tem um único `Volume`**, e as duas câmaras têm
`m_RenderPostProcessing: 0` (`:235` e `:2069`). Escrever grading, vinheta ou grão hoje é escrever para
uma gaveta. Ligar isso é FASE 1, e é o primeiro passo depois desta decisão.

⚠️ O `PC_RPAsset` está em `m_ColorGradingMode: 0` (LDR) com LUT de 32. Qualquer direcção que queira
trabalhar a luz a sério quer **HDR** e LUT 32-64. Também FASE 1.

⚠️ O `DefaultVolumeProfile.asset` do template traz cinco componentes com script em falta
(`CopyPasteTestComponent` e afins). Vão aparecer como «missing script» a quem o abrir para configurar
grading. São lixo do pacote URP, não do projecto.

## 7. A paleta

Num jogo realista em espaço linear, «paleta» não é uma lista de cores de interface — é **disciplina de
albedo** mais **temperatura de luz**. Os materiais não são as cores que se vêem; são as cores que a
luz encontra.

### 7.1 Disciplina de albedo (a regra que evita o aspecto de plástico)

Nenhum albedo sai deste intervalo, e isto **não é gosto, é física**: superfícies reais não são nem
pretas nem brancas.

| | sRGB | Nota |
|---|---|---|
| Mínimo absoluto | **50** | asfalto molhado, carvão. Abaixo disto a superfície come luz e o sítio parece um buraco |
| Máximo absoluto | **240** | tinta branca fresca, neve. Acima disto estoira e perde-se a forma |
| A esmagadora maioria | 70-180 | é aqui que vive Pine Hollow |

### 7.2 As famílias (direcção C — a cor está na matéria)

| Família | sRGB | Onde |
|---|---|---|
| Névoa / céu encoberto | `#A8B2B5` | o tom que o jogo tem por omissão ao longe |
| Verde floresta profundo | `#2E3A2B` | copas, fundo de floresta |
| Verde musgo e rasteira | `#4A5A3A` | o verde que se vê de perto, mais claro que as copas |
| Madeira molhada | `#3E342C` | fachadas, alpendres, o pontão do lago |
| Betão manchado | `#7A7B76` | fábrica, passeios, túneis |
| Asfalto húmido | `#33363A` | estrada — e é o mais escuro que o jogo tem |
| Tinta lascada | `#C8C4B8` | casas da vila. Nunca branco puro |
| **Ferrugem** | `#7A3B22` | **o único vermelho do jogo** |
| Folha morta | `#8A5A2B` | o acento quente, sazonal, de Outono |
| Cobre oxidado | `#4E7A6A` | acento frio, raro — canalização, calhas, o que a água estragou |

**Regra de acento: um por plano, e é sempre matéria, nunca luz.** Num mundo cinzento-esverdeado, a
ferrugem ou uma folhada são o que o olho encontra — e é por isso que se usam para dizer *«olha para
aqui»* sem pôr um marcador no ecrã. Dois acentos no mesmo plano e nenhum aponta para nada.

**Humidade é a ferramenta de contraste.** Uma superfície molhada é mais escura e mais saturada do que
a mesma seca, sem mudar de cor. Numa paleta dessaturada é assim que se ganha variação sem gastar
assets nem sair da direcção.

### 7.3 Temperatura de luz — e é aqui que as épocas se separam

| | 2026 | 1986 |
|---|---|---|
| Céu (dominante) | **6500-7000 K**, encoberto e frio | 5500-6000 K, mais aberto |
| Direccional | fraca, 5500 K, sem direcção marcada | mais presente, mais baixa no céu |
| Luz praticável | poucas, **4000 K** (LED, o que sobrou) | muitas, **2700-3000 K** (tungsténio) |
| Iluminação pública | apagada ou partida | **sódio, ~2000 K** — o laranja da época |
| Janelas | cegas | acesas |

**É esta tabela a regra 1986/2026 na prática.** A geometria é a mesma (§5); o que muda é a coluna. Um
interior de 1986 é quente porque tem lâmpadas de tungsténio acesas; o mesmo interior em 2026 é frio
porque só lá entra o céu. Não é um filtro por cima — são luzes diferentes na mesma divisão.

### 7.4 O que isto obriga a FASE 1

O grading vive em **HDR**, não no `m_ColorGradingMode: 0` (LDR) que o `PC_RPAsset:79` tem hoje, com
LUT de 32 ou 64. Sem isso, as altas luzes das janelas de 1986 estoiram a branco e perde-se o único
sítio onde esta paleta tem calor.

---

## Em aberto — o que falta decidir

- [x] **A direcção** — A como regra de luz, C nos interiores importantes, B só em momentos (§3).
- [x] **Regra 1986/2026** — confirmada como está na §5: a geometria nunca muda.
- [x] **Paleta concreta** — §7: disciplina de albedo, dez famílias com valores, e a tabela de
      temperatura de luz que separa 1986 de 2026.
- [x] **Referências** — `Referencias/NOTAS.md`, cinco conjuntos com o que se rouba de cada um.
      As notas versionam-se; as imagens ficam de fora do repositório (`.gitignore`).
- [ ] **Grão e registo de lente do jogo** (não das fotografias): quanto, ou nenhum. Ver regra 3 da §2.
