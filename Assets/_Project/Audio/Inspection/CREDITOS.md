# Inspecção — proveniência, licenças e o que falta

**Todos os ficheiros desta pasta são CC0 (domínio público).** Não há atribuição obrigatória;
este ficheiro existe para se saber de onde vieram e não se voltar a perguntar.

| Ficheiros | Origem | Autor | Licença |
|---|---|---|---|
| `Inspect_Pickup_01..04.wav` | [100 CC0 metal and wood SFX](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) (OpenGameArt) | rubberduck | CC0 |
| `Inspect_Putdown_01..04.wav` | [100 CC0 metal and wood SFX](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) (OpenGameArt) | rubberduck | CC0 |

Originais: `wood_misc_01/06/07/09` (pegar) e `wood_hit_01/03/04/08` (pousar).

**Estes clips foram escolhidos por medição e só depois ouvidos.** A selecção foi por análise da
forma de onda — ver o critério abaixo — e o Diogo ouviu-os em play mode a 2026-09-21, no fim da
FASE 4, e aceitou-os. Se algum vier a soar mal, a troca é trocar o ficheiro: o sistema escolhe ao
acaso do array e não sabe os nomes.

## O critério de selecção

O pack tem 100 sons e os nomes não chegam para decidir. Mediu-se duração, pico, tempo de ataque
(de 10% do pico até ao pico), tempo de decaimento e centroide espectral de todos os candidatos de
madeira, e escolheu-se por forma:

- **Pegar** — ataque **lento**, 130 a 155 ms. Levantar uma caixa é manuseamento: a energia sobe,
  não bate. Os `wood_misc` com ataque rápido foram postos de lado por soarem a impacto.
- **Pousar** — ataque **rápido**, ~10 ms, com decaimento curto. Pousar é um contacto único.
  Rejeitaram-se os `wood_slam` (violentos de mais para pousar uma caixa), os `wood_hit_05` e
  `wood_hit_07` (centroide ~1 kHz, som cavernoso que não é o de uma caixa pequena) e o
  `wood_hit_02` (5 kHz, agudo e seco de mais, soa a estalido).

## O que lhes foi feito

Os originais vinham em **OGG estéreo a 48 kHz** e, descodificados, **passavam de 0 dBFS** — entre
+2,0 e +3,1 dB de pico, ou seja, já vinham a cortar.

Cada ficheiro foi:

1. convertido para **WAV PCM 16-bit, 44,1 kHz**;
2. passado a **mono** — são sons do objecto que o jogador tem nas mãos, tocados a
   `spatialBlend = 0`, tal como os passos;
3. **normalizado a −3 dBFS de pico**, o mesmo alvo dos passos, para que o volume venha do sistema
   e não de acidentes de gravação.

Não foi preciso cortar silêncio inicial: todos tinham menos de 14 ms antes do primeiro
transiente, o que não se nota.

A variação entre repetições é feita em runtime pelo `InspectionSystem`: escolhe um clip ao acaso
do array e varia o pitch ±8%, exactamente como o `FootstepSystem`.

## O que falta

| Som | Porquê |
|---|---|
| **Rodar** | Ficou deliberadamente de fora. `PlayOneShot` serve eventos pontuais; um som de rotação é um *loop* com volume em função da velocidade angular, e isso é um padrão que o projecto ainda não tem. |
| **Papel** | A FASE 5 põe fotografias na mão. Uma fotografia não soa a caixa de madeira — quando o `PhotoInteractable` existir, precisa dos seus. |

## Como acrescentar

Não precisa de código: mete os clips na pasta e arrasta-os para `pickupClips` ou `putdownClips`
no `InspectionSystem` do `Player`. Sem clips, o sistema não toca nada e não estoira.
