# Pine Hollow — Plano Completo de Desenvolvimento

**Versão:** 0.1  
**Engine:** Unity 6  
**Pipeline:** Universal Render Pipeline (URP)  
**Perspetiva:** First Person  
**Género:** Thriller / Mistério / Investigação  
**Duração alvo:** 2–4 horas  
**Vertical Slice:** 15–20 minutos  
**Escala:** Projeto solo / pequena equipa

---

## 1. Visão do projeto

Pine Hollow é um thriller/mistério em primeira pessoa passado numa pequena cidade montanhosa fictícia em 2026.

O jogador controla uma pessoa comum que compra uma casa anteriormente pertencente ao fotógrafo Ethan Cole. Ao explorar a casa e o antigo estúdio fotográfico, encontra fotografias aparentemente normais que começam a revelar algo impossível: Thomas Hale, oficialmente morto em 1986, aparece em fotografias de diferentes décadas.

A fotografia é o principal mecanismo narrativo e de gameplay. O jogador pode recolher, observar, virar, ampliar, rodar, comparar e alinhar fotografias. Certos alinhamentos permitem entrar em flashbacks jogáveis.

Regra narrativa central:

> Não podes observar o passado sem deixar uma marca nele.

O jogo não terá combate. A tensão será criada através de exploração, descoberta, som, iluminação, ambiente, NPCs, fotografias e alterações entre passado e presente.

---

## 2. Estado atual

### Concluído

- [x] Conceito geral
- [x] História principal
- [x] Estrutura de 8 capítulos
- [x] Personagens principais
- [x] Cidade e principais locais
- [x] Gameplay loop
- [x] Sistema de fotografia conceptualizado
- [x] Sistema de flashbacks
- [x] Finais
- [x] GDD inicial
- [x] Projeto Unity 6
- [x] Universal 3D / URP
- [x] Input System
- [x] Estrutura de pastas <!-- marcado antes de existir; só passou a ser verdade a 2026-09-21, na FASE 1 -->
- [x] Cenas Bootstrap e Prototype_Player <!-- idem: a Bootstrap.unity não existia quando isto foi marcado -->
- [x] Player
- [x] Character Controller
- [x] Camera em primeira pessoa
- [x] Ground temporário
- [x] WASD
- [x] Sprint
- [x] Gravidade
- [x] Mouse look
- [x] Cursor lock/unlock

### Estado atual

Estamos no **Stage 2 — Player**, depois do movimento base.

Próximo passo: **Stage 2.3 — Interação básica**.

---

## 3. Filosofia de desenvolvimento

O jogo será desenvolvido por sistemas pequenos e testáveis.

Não construir o mapa completo primeiro.

**Sistema → Teste isolado → Integração → Polimento → Conteúdo**

A prioridade será sempre ter uma versão jogável antes de aumentar a escala.

---

# FASE 0 — Pré-produção

- [x] Conceito
- [x] História
- [x] GDD
- [x] Core gameplay
- [x] Personagens
- [ ] Mapa conceptual — **reaberto a 21/09/2026**: o que existe é uma lista de locais
      (`Pine_Hollow_GDD_v0.1.md:150-169`), não um mapa. Sem adjacências, sem distâncias, sem planta.
      Bloqueia a FASE 9.
- [x] Estrutura narrativa
- [x] Referências visuais finais — `Referencias/NOTAS.md`: cinco conjuntos, com **o que se rouba de
      cada um** e o que explicitamente não se rouba. As notas versionam-se, as imagens não
- [x] Direção artística final — `Pine_Hollow_Direccao_Artistica.md`: **A** como regra de luz, **C**
      nos interiores importantes, **B** só em momentos; regra 1986/2026 confirmada (21/09)
- [x] Lista final de assets — `Pine_Hollow_Producao.md` §2
- [x] Limites técnicos — `Pine_Hollow_Producao.md` §1

# FASE 1 — Fundação técnica do Unity

- [x] Unity 6
- [x] URP
- [x] Input System
- [x] TextMeshPro
- [x] Definições gráficas — pós-processamento ligado, perfil global limpo do que deforma a imagem
- [x] Qualidade — um só nível (PC); o `Mobile` saiu com Android e iOS
- [x] Layers — `Interactable`, `Player`, `PhotoOnly`, `IgnorePhoto`
- [x] Tags — só a built-in `MainCamera`; nenhum script lê tags personalizadas
- [x] Physics settings — matriz de colisão revista; `m_QueriesHitTriggers` fica a `1` de propósito
- [x] Cenas de produção — `Bootstrap.unity` criada e a arrancar a build
- [x] Estrutura de pastas — `_Project/` adoptado, com `.gitkeep` nas vazias

> Fechada a 2026-09-21. A distância de sombras (80 m) é **derivada, não medida** — a primeira
> medição em play mode manda sobre ela. MSAA e VSync ficaram por decidir pela mesma razão.
> A layer mask do raycast de interação **não** é desta fase: é a `869f4yb37`, na FASE 3.

Estrutura:

```text
Assets/
├── _Project/
│   ├── Art/
│   ├── Audio/
│   ├── Materials/
│   ├── Models/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   ├── Settings/
│   └── UI/
├── Environment/
└── ThirdParty/
```

# FASE 2 — Player

## 2.1 Estrutura
- [x] Player GameObject
- [x] Character Controller
- [x] Camera
- [x] Ground de teste

## 2.2 Movimento
- [x] WASD
- [x] Movimento relativo
- [x] Gravidade
- [x] Sprint
- [x] Mouse look
- [x] Limite vertical
- [x] Cursor lock

## 2.3 Interação básica
- [ ] Raycast da câmera
- [ ] Identificar objetos interativos
- [ ] Interface `IInteractable`
- [ ] Tecla de interação
- [ ] Feedback visual
- [ ] Texto contextual

## 2.4 Sensação
- [ ] Velocidade
- [ ] Aceleração/desaceleração
- [ ] Sensibilidade
- [ ] Head bob subtil
- [ ] Movimento de câmera
- [ ] Footsteps
- [ ] Sons por superfície

## 2.5 Estados futuros
- [ ] Normal
- [ ] Sprint
- [ ] Crouch
- [ ] Interação
- [ ] Inspeção
- [ ] Fotografia
- [ ] Flashback

# FASE 3 — Sistema de interação

- [ ] Raycast
- [ ] Interface de interação
- [ ] Interactables
- [ ] Highlight subtil
- [ ] Prompt
- [ ] Distância máxima
- [ ] Prioridade

Primeiros objetos:
- [ ] Porta
- [ ] Gaveta
- [ ] Interruptor
- [ ] Documento
- [ ] Fotografia
- [ ] Objeto examinável

Arquitetura:

```text
Player
↓
Interaction System
↓
IInteractable
↓
Object
```

# FASE 4 — Sistema de inspeção

- [ ] Pick up
- [ ] Modo de inspeção
- [ ] Rotação
- [ ] Zoom
- [ ] Movimento controlado
- [ ] Sair da inspeção
- [ ] Descrições
- [ ] Sons

Aplicação inicial:
- [ ] Documentos
- [ ] Fotografias
- [ ] Pequenos objetos
- [ ] Pistas

# FASE 5 — Sistema de fotografia

## Fotografias
- [ ] Photo Data
- [ ] Data
- [ ] Local
- [ ] Personagens
- [ ] Metadados
- [ ] Frente
- [ ] Verso
- [ ] Imagem

## Inspeção
- [ ] Zoom
- [ ] Rodar
- [ ] Virar
- [ ] Examinar
- [ ] Ler verso

## Comparação
- [ ] Selecionar duas fotografias
- [ ] Lado a lado
- [ ] Sobreposição
- [ ] Escala
- [ ] Posição
- [ ] Rotação
- [ ] Alinhamento

## Descobertas
- [ ] Estruturas
- [ ] Pessoas
- [ ] Portas
- [ ] Sombras
- [ ] Objetos
- [ ] Símbolos
- [ ] Alterações arquitetónicas

# FASE 6 — Sistema de investigação

## Journal
- [ ] Pessoas
- [ ] Locais
- [ ] Datas
- [ ] Fotografias
- [ ] Documentos
- [ ] Descobertas

## Investigation Board

```text
Thomas Hale
     │
     ├── 1986
     ├── Fábrica
     ├── Fotografias
     └── Câmara subterrânea
```

Regras:
- [ ] Evitar setas constantes
- [ ] Evitar checklist excessivo
- [ ] Permitir ligações feitas pelo jogador

Progressão: **Conhecimento, não XP.**

# FASE 7 — Sistema de flashbacks

```text
2026
↓
Alinhamento
↓
Transição
↓
1986
↓
Gameplay
↓
Evento
↓
2026
```

- [ ] World State
- [ ] Estado 2026
- [ ] Estado 1986
- [ ] Transição visual
- [ ] Transição sonora
- [ ] Spawn
- [ ] NPCs históricos
- [ ] Objetos históricos
- [ ] Retorno ao presente

Evolução:
- [ ] Flashbacks iniciais observacionais
- [ ] Personagens começam a reagir
- [ ] Personagens conseguem ver o jogador
- [ ] Ações deixam marcas no presente

# FASE 8 — Vertical Slice

Objetivo: 15–20 minutos completos.

- [ ] Estrada de chegada
- [ ] Entrada em Pine Hollow
- [ ] Casa
- [ ] Exploração
- [ ] Estúdio fotográfico
- [ ] Primeira fotografia
- [ ] Segunda fotografia
- [ ] Pista de Ethan
- [ ] Inspeção
- [ ] Comparação
- [ ] Primeiro alinhamento
- [ ] Flashback de 1986
- [ ] Thomas no lago
- [ ] Regresso a 2026
- [ ] Fotografia alterada
- [ ] Protagonista aparece na fotografia

Critério: transmitir atmosfera, exploração, mistério, fotografia, primeiro flashback e primeira alteração temporal.

# FASE 9 — Blockout de Pine Hollow

Locais:
- [ ] Casa
- [ ] Centro
- [ ] Café
- [ ] Mercearia
- [ ] Polícia
- [ ] Câmara municipal
- [ ] Biblioteca
- [ ] Igreja
- [ ] Motel
- [ ] Casas
- [ ] Fábrica
- [ ] Lago
- [ ] Rio
- [ ] Ponte
- [ ] Cemitério
- [ ] Floresta
- [ ] Cabana
- [ ] Trilhos
- [ ] Montanha
- [ ] Miradouro
- [ ] Túneis
- [ ] Câmara subterrânea

Escala alvo: aproximadamente **3 km²**.

# FASE 10 — Casa do protagonista

Exterior:
- [ ] Terreno
- [ ] Estrada
- [ ] Estacionamento
- [ ] Jardim
- [ ] Floresta
- [ ] Entrada

Interior:
- [ ] Hall
- [ ] Sala
- [ ] Cozinha
- [ ] Quarto
- [ ] Casa de banho
- [ ] Escritório
- [ ] Estúdio
- [ ] Arrecadação

Gameplay:
- [ ] Portas
- [ ] Gavetas
- [ ] Luzes
- [ ] Objetos
- [ ] Documentos
- [ ] Fotografias
- [ ] Pistas de Ethan

# FASE 11 — Ethan Cole

- [ ] Fotografias numeradas
- [ ] Mapas
- [ ] Recortes
- [ ] Notas
- [ ] Datas
- [ ] Equipamento fotográfico
- [ ] Câmara
- [ ] Cassetes
- [ ] Mochila
- [ ] Anotações

Descobertas:
- [ ] Thomas aparece em várias décadas
- [ ] 1946
- [ ] 1958
- [ ] 1971
- [ ] 1986
- [ ] 2016
- [ ] Ethan investigava a fábrica
- [ ] Ethan desapareceu

# FASE 12 — NPCs

Sistema:
- [ ] NPC base
- [ ] Rotinas
- [ ] Horários
- [ ] Localização
- [ ] Conversas
- [ ] Relações
- [ ] Conhecimento
- [ ] Estados

NPCs:
- [ ] Antigo chefe da polícia
- [ ] Dona do café
- [ ] Bibliotecária
- [ ] Funcionário da polícia
- [ ] Antigo trabalhador da fábrica
- [ ] Proprietário do motel
- [ ] Habitantes antigos

Cada NPC terá personalidade, rotina, conhecimento, relações, informação pública e segredos.

# FASE 13 — Fábrica

2026:
- [ ] Exterior abandonado
- [ ] Escritórios
- [ ] Área industrial
- [ ] Corredores
- [ ] Zona selada
- [ ] Planta antiga
- [ ] Entrada subterrânea

1986:
- [ ] Fábrica ativa
- [ ] Trabalhadores
- [ ] Máquinas
- [ ] Iluminação industrial
- [ ] Thomas
- [ ] Elias
- [ ] Porta metálica

# FASE 14 — Túneis

- [ ] Entrada
- [ ] Túneis estreitos
- [ ] Água
- [ ] Objetos de diferentes décadas
- [ ] Equipamento de Ethan
- [ ] Mochila
- [ ] Cassetes
- [ ] Fotografias
- [ ] Câmara subterrânea

# FASE 15 — Câmara subterrânea

- [ ] Centenas de fotografias
- [ ] Fotografias de várias décadas
- [ ] Datas
- [ ] Thomas
- [ ] Elias
- [ ] Ethan
- [ ] Protagonista
- [ ] Câmara
- [ ] Objetos de diferentes épocas

Sequências:
- [ ] 23:31
- [ ] 23:38
- [ ] 23:42
- [ ] 23:47
- [ ] 23:52
- [ ] 23:56
- [ ] 00:03

# FASE 16 — História e capítulos

## Capítulo 1 — Pine Hollow
- [ ] Chegada
- [ ] Casa
- [ ] Estúdio
- [ ] Fotografias
- [ ] Thomas
- [ ] Primeiro alinhamento
- [ ] Primeiro flashback
- [ ] Fotografia alterada

## Capítulo 2 — Quem é Thomas Hale?
- [ ] Biblioteca
- [ ] Café
- [ ] Cemitério
- [ ] Polícia
- [ ] Fábrica
- [ ] Registo de morte
- [ ] História de Thomas

## Capítulo 3 — Ethan Cole
- [ ] Materiais escondidos
- [ ] Fotografias
- [ ] Mapas
- [ ] Recortes
- [ ] Datas anteriores
- [ ] Fotografia impossível

## Capítulo 4 — A Fábrica
- [ ] Exploração
- [ ] Planta de 1986
- [ ] Área subterrânea
- [ ] Porta
- [ ] Flashback
- [ ] Porta aparece em 2026

## Capítulo 5 — Debaixo do Lago
- [ ] Túneis
- [ ] Mochila de Ethan
- [ ] Cassete
- [ ] Câmara subterrânea
- [ ] Fotografias de várias décadas
- [ ] Fotografia de 17 julho 1986

## Capítulo 6 — 23:47
- [ ] Sequência temporal
- [ ] Thomas
- [ ] Elias
- [ ] Fotografias do futuro
- [ ] Fotografia de Elias em 1946
- [ ] Thomas desaparece
- [ ] Elias desaparece

## Capítulo 7 — A Noite
- [ ] Reconstrução
- [ ] 23:47 → 00:03
- [ ] Thomas
- [ ] Elias
- [ ] Nova sala
- [ ] Fotografia da casa
- [ ] “Agora és tu.”

## Capítulo 8 — O Último Rolo
- [ ] Regresso a casa
- [ ] Alterações
- [ ] Último rolo
- [ ] Fotografia final
- [ ] Câmara
- [ ] Câmara subterrânea
- [ ] Escolha final

# FASE 17 — Áudio

Ambiente:
- [ ] Vento
- [ ] Chuva
- [ ] Floresta
- [ ] Rio
- [ ] Lago
- [ ] Casa
- [ ] Fábrica
- [ ] Túneis

Gameplay:
- [ ] Passos
- [ ] Portas
- [ ] Gavetas
- [ ] Objetos
- [ ] Câmara
- [ ] Fotografias
- [ ] Interruptores

Atmosfera:
- [ ] Silêncios
- [ ] Sons distantes
- [ ] Sons temporais
- [ ] Distortion
- [ ] Transições

Música:
- [ ] Tema principal
- [ ] Exploração
- [ ] Investigação
- [ ] Flashbacks
- [ ] Câmara subterrânea
- [ ] Final

# FASE 18 — Arte e ambiente

> Lista fechada, com quantidades e origem: `Pine_Hollow_Producao.md` §2.

Mundo:
- [ ] Vegetação
- [ ] Árvores
- [ ] Rochas
- [ ] Estradas
- [ ] Lama
- [ ] Água
- [ ] Montanha
- [ ] Construções

Props:
- [ ] Móveis
- [ ] Livros
- [ ] Computadores
- [ ] Telefones
- [ ] Ferramentas
- [ ] Equipamento fotográfico
- [ ] Objetos de 1986
- [ ] Objetos modernos

# FASE 19 — Iluminação e atmosfera

- [ ] Exterior
- [ ] Interior
- [ ] Noite
- [ ] Chuva
- [ ] Nevoeiro
- [ ] Volumetric lighting
- [ ] Sombras
- [ ] Interior/exterior
- [ ] Variações 2026/1986
- [ ] Câmara subterrânea

# FASE 20 — UI

- [ ] Crosshair opcional
- [ ] Interaction prompt
- [ ] Journal
- [ ] Inventory
- [ ] Photo viewer
- [ ] Photo comparison
- [ ] Investigation board
- [ ] Pause menu
- [ ] Settings

Evitar HUD permanente.

# FASE 21 — Save System

- [ ] Save slots
- [ ] Autosave
- [ ] Estado da história
- [ ] Fotografias descobertas
- [ ] Pistas
- [ ] NPC states
- [ ] World states
- [ ] Posição do jogador
- [ ] Progressão dos capítulos

# FASE 22 — Otimização

Só depois do jogo estar funcional.

- [ ] Profiler
- [ ] CPU
- [ ] GPU
- [ ] Memory
- [ ] Occlusion culling
- [ ] LOD
- [ ] Texture sizes
- [ ] Draw calls
- [ ] Batching
- [ ] Lighting
- [ ] Streaming
- [ ] Scene management

# FASE 23 — QA

Gameplay:
- [ ] Soft locks
- [ ] Hard locks
- [ ] Objetos inacessíveis
- [ ] Interações duplicadas
- [ ] Flashbacks incorretos
- [ ] Progressão quebrada

Narrativa:
- [ ] Pistas
- [ ] Fotografias
- [ ] Datas
- [ ] Diálogos
- [ ] NPC knowledge
- [ ] Finais

Técnico:
- [ ] Crashes
- [ ] FPS
- [ ] Memory leaks
- [ ] Audio bugs
- [ ] Lighting bugs
- [ ] Collision bugs
- [ ] Save/load bugs

# FASE 24 — Polimento

- [ ] Animações
- [ ] Sons
- [ ] VFX
- [ ] Partículas
- [ ] Camera effects
- [ ] Transições
- [ ] UI
- [ ] Textos
- [ ] Detalhes ambientais
- [ ] Easter eggs
- [ ] Micro-histórias ambientais

# FASE 25 — Testes externos

Playtests para observar:
- [ ] Onde ficam presos
- [ ] Onde não percebem o objetivo
- [ ] Que pistas ignoram
- [ ] Onde se perdem
- [ ] Reações aos flashbacks
- [ ] Reações às fotografias
- [ ] Ritmo
- [ ] Duração

# FASE 26 — Build final

- [ ] Windows build
- [ ] Linux build, se aplicável
- [ ] Qualidade gráfica
- [ ] Resolução
- [ ] Fullscreen/windowed
- [ ] Áudio
- [ ] Controlos
- [ ] Save directory
- [ ] Crash handling
- [ ] Build validation

# FASE 27 — Finais

## Final A — Destruir a câmara
- [ ] Destruição
- [ ] Câmara subterrânea colapsa
- [ ] Pine Hollow normal
- [ ] Fotografia de 1986 permanece

## Final B — Tirar a fotografia
- [ ] Fotografia final
- [ ] Flash
- [ ] Jogador acorda em 1986
- [ ] Thomas: “Demoraste 40 anos.”
- [ ] Último plano
- [ ] Final ambíguo

---

# Ordem técnica de dependências

```text
Project Foundation
        ↓
Player
        ↓
Interaction
        ↓
Inspection
        ↓
Photography
        ↓
Investigation
        ↓
World State
        ↓
Flashbacks
        ↓
NPCs
        ↓
Narrative Content
        ↓
Audio / Art / Atmosphere
        ↓
Save System
        ↓
Optimization
        ↓
QA
        ↓
Final Build
```

# Milestones

1. Player funcional
2. Player + interação
3. Objeto examinável
4. Primeira fotografia
5. Duas fotografias + comparação
6. Alinhamento
7. Primeiro flashback
8. Alteração da fotografia
9. Vertical Slice completo
10. Pine Hollow
11. Conteúdo narrativo completo
12. Polimento e lançamento

# Próximos passos imediatos

Estamos aqui:

**Stage 2 — Player / movimento base concluído**

Próximas tarefas:

1. [ ] Stage 2.3 — Interação básica
2. [ ] Stage 2.4 — Polimento do Player
3. [ ] Stage 3 — Sistema de interação
4. [ ] Stage 4 — Sistema de inspeção
5. [ ] Stage 5 — Primeira fotografia
6. [ ] Stage 6 — Comparação
7. [ ] Stage 7 — Alinhamento
8. [ ] Stage 8 — Primeiro flashback
9. [ ] Stage 9 — Vertical Slice

# Regra de desenvolvimento

**Construir → testar → corrigir → confirmar → avançar.**

O objetivo é construir Pine Hollow sem reescrever constantemente os sistemas anteriores.

# Objetivo final

```text
Explorar
   ↓
Encontrar
   ↓
Examinar
   ↓
Questionar
   ↓
Relacionar
   ↓
Fotografar
   ↓
Alinhar
   ↓
Entrar no passado
   ↓
Alterar o presente
   ↓
Descobrir
   ↓
Escolher o destino final
```

O mistério deverá permanecer parcialmente aberto à interpretação. O jogo não precisa explicar completamente a natureza do fenómeno, permitindo diferentes interpretações sobre Thomas, Elias, Ethan, a câmara e o ciclo temporal.
