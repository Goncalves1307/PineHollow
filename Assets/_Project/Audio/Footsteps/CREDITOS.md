# Passos — proveniência, licenças e o que falta

**Todos os ficheiros desta pasta são CC0 (domínio público).** Não há atribuição obrigatória;
este ficheiro existe para se saber de onde vieram e não se voltar a perguntar.

| Pasta | Ficheiros | Origem | Autor | Licença |
|---|---|---|---|---|
| `Wood/` | `Footstep_Wood_01..03.wav` | [Different steps on wood, stone, leaves, gravel and mud](https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud) (OpenGameArt) | kddekadenz, a partir de pdsounds.org | CC0 |
| `Dirt/` | `Footstep_Dirt_01..06.wav` | [Fantozzi's Footsteps (Grass/Sand & Stone)](https://opengameart.org/content/fantozzis-footsteps-grasssand-stone) (OpenGameArt) | Fantozzi, via freesound.org | CC0 |
| `Stone/` | — | vazia | | |

⚠️ **O pack do Fantozzi está publicado como «stone» e soa a terra.** Foi ouvido em play mode a
2026-09-21 e o veredicto foi esse. Por isso vive em `Dirt/` e não em `Stone/` — o nome de origem
mentiria a quem viesse aqui buscar pedra. **Não o voltes a descarregar à espera de som de pedra.**

Isto não faz dele um som provisório: pelos documentos do jogo, terra é o chão da **floresta** e do
**cemitério**, que são locais a sério e não cenários de teste.

## O que lhes foi feito

Os originais vinham em OGG estéreo com níveis muito diferentes — a madeira entre −25,8 e
−12,8 dBFS de pico, o outro pack a 0 dB. Sem tratamento, a madeira era inaudível e o resto
estourava.

Cada ficheiro foi:

1. convertido para **WAV PCM 16-bit, 44,1 kHz**;
2. passado a **mono** — são passos do próprio jogador, tocados a `spatialBlend = 0`; em estéreo
   davam uma imagem lateral dentro da cabeça que não corresponde a nada;
3. **normalizado a −3 dBFS de pico**, para que a variação de volume venha do sistema (por
   estado: 0,25 agachado, 0,5 a andar, 0,75 a correr) e não de acidentes de gravação.

A variação entre passos é feita em runtime pelo `FootstepSystem`: escolhe um clip ao acaso do
conjunto e varia o pitch ±8%.

## O que falta, por ordem de utilidade

Contado pelas menções nos documentos do jogo, não por gosto:

| Superfície | Para onde | Porquê é prioridade |
|---|---|---|
| **Betão** e **metal** | a **fábrica** | É de longe o local mais presente nos documentos. Grelhas, escadas e chão industrial não soam a nada do que está aqui. |
| **Folhas / ramos** | floresta | A terra cobre o chão batido, mas não o folhiço. |
| **Pedra** | igreja, cemitério (lajes), Chamber | A pasta `Stone/` está vazia à espera deste. |
| **Água rasa / lama** | lago | |
| **Asfalto / gravilha** | estrada, estação | |
| **Tapete** e **azulejo** | interior da casa (FASE 10) | Só faz sentido escolher quando houver divisões para os ouvir. |

⚠️ **Antes de acrescentar superfícies novas, acrescenta variações à madeira.** São só **3 clips**,
e o sistema dá um passo a cada 1,2 m: em linha recta ouve-se a repetição em poucos segundos. Seis
é o mínimo para não soar a *loop*, e a madeira é o chão que se pisa mais.

## Como acrescentar uma superfície

Não precisa de código. Cria a pasta, mete lá os clips, põe um `SurfaceAudio` no objecto que se
pisa e arrasta-os para o array. O `FootstepSystem` faz um raycast para baixo e encontra-o
sozinho; sem `SurfaceAudio`, cai nos clips por omissão (hoje, madeira).

## Uma fonte que foi rejeitada

[Red Library: Footsteps 1](https://archive.org/details/Red_Library_Footsteps_1) (archive.org,
CC0) tem gravações de chão de madeira, mas são digitalizações de fita de arquivo com hiss
constante — a análise da envolvente dá energia praticamente plana, sem transientes
distinguíveis. Meter esse ruído em cada passo era pior do que não ter som.
