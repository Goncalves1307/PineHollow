# Passos — proveniência e licenças

**Todos os ficheiros desta pasta são CC0 (domínio público).** Não há atribuição obrigatória;
este ficheiro existe para se saber de onde vieram e não se voltar a perguntar.

| Pasta | Ficheiros | Origem | Autor | Licença |
|---|---|---|---|---|
| `Wood/` | `Footstep_Wood_01..03.wav` | [Different steps on wood, stone, leaves, gravel and mud](https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud) (OpenGameArt) | kddekadenz, a partir de pdsounds.org | CC0 |
| `Stone/` | `Footstep_Stone_01..06.wav` | [Fantozzi's Footsteps (Grass/Sand & Stone)](https://opengameart.org/content/fantozzis-footsteps-grasssand-stone) (OpenGameArt) | Fantozzi, via freesound.org | CC0 |

## O que lhes foi feito

Os originais vinham em OGG estéreo com níveis muito diferentes — a madeira entre −25,8 e
−12,8 dBFS de pico, a pedra a 0 dB. Sem tratamento, a madeira era inaudível e a pedra estourava.

Cada ficheiro foi:

1. convertido para **WAV PCM 16-bit, 44,1 kHz**;
2. passado a **mono** — são passos do próprio jogador, tocados a `spatialBlend = 0`; em estéreo
   davam uma imagem lateral dentro da cabeça que não corresponde a nada;
3. **normalizado a −3 dBFS de pico**, para que a variação de volume venha do sistema (por
   estado: 0,25 agachado, 0,5 a andar, 0,75 a correr) e não de acidentes de gravação.

A variação entre passos é feita em runtime pelo `FootstepSystem`: escolhe um clip ao acaso do
conjunto e varia o pitch ±8%.

## Uma fonte que foi rejeitada

[Red Library: Footsteps 1](https://archive.org/details/Red_Library_Footsteps_1) (archive.org,
CC0) tem gravações de chão de madeira, mas são digitalizações de fita de arquivo com hiss
constante — a análise da envolvente dá energia praticamente plana, sem transientes
distinguíveis. Meter esse ruído em cada passo era pior do que não ter som.
