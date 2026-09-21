# Pine Hollow — Direcção artística

> **Estado: quadro de decisão, não direcção fechada.** As secções 1, 2 e 4 são factos e restrições.
> A secção 3 são **três direcções em aberto** — escolhe-se uma e este cabeçalho muda.
> A secção 5 (regra 1986/2026) vai **proposta**, porque é a que a mecânica do jogo quase decide sozinha.
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

### Recomendação

**A como direcção do jogo, com B reservada para momentos narrativos**, e migrar para C nos interiores
importantes — a casa do Ethan, o estúdio, a fábrica — onde o jogador está perto e o custo de autorar
matéria se paga, porque é lá que ele olha de verdade. A e C não são incompatíveis: A é a regra de luz,
C é a regra de matéria. Com 0 € de orçamento, A é o único arranque honesto.

**Isto não está decidido.** É o que eu faria; a escolha é tua.

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

---

## Em aberto — o que falta decidir

- [ ] **A direcção**: A, B, C, ou a mistura recomendada.
- [ ] **Paleta concreta** — só depois de escolhida a direcção e vistas as referências.
- [ ] **Referências**: recolher, e decidir se `Referencias/` entra no repositório ou fica de fora.
- [ ] **Regra 1986/2026**: confirmar a proposta da §5, ou contestá-la.
- [ ] **Grão e registo de lente do jogo** (não das fotografias): quanto, ou nenhum. Ver regra 3 da §2.
