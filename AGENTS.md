# AGENTS.md

Instruções para agentes de código (Claude Code, Cursor, e outros que leiam `AGENTS.md`) neste repositório.

## What this is

**Pine Hollow** — a first-person investigation thriller in Unity 6 (`6000.6.2f1`), URP, realistic,
no combat, 2–4h target. The player buys the house of Ethan Cole, a photographer who vanished ten
years earlier, and finds photographs in which **Thomas Hale** appears across several decades
despite officially dying in 1986. Photography is the core mechanic *and* the narrative device:
photographs are physical objects you pick up, flip, zoom, compare and **align** — and certain
alignments drop you into playable flashbacks that change the present.

The central narrative rule, which the design docs repeat everywhere and which drives the systems:

> **Não podes observar o passado sem deixar uma marca nele.**

Scope: compact open world ~3 km², town of ~2.800 inhabitants, 8 chapters, ~7 flashbacks, two
endings. First technical objective is a **15–20 minute vertical slice** (arrival → house → studio →
first photographs → first alignment → first 1986 flashback → altered photograph), not the full town.

**Current build reality: a mechanics sandbox with a working photography layer.** 28 scripts, two
scenes, 157 EditMode tests. Walk, look, sprint, crouch, interact at short range (`E`) with a highlight on the focused
object, open a door, a drawer and a switch, read a document, pick an object up to rotate, zoom and
read its description, take screenshot-style photos. Since FASE 5: pick up **authored photographs**
from the world, flip them to read the canonical text on the back (`Q`), open the album (`Tab`),
compare two photographs side by side or overlaid, and align them — which flashes and reveals what
that pair hides. Footsteps and object-handling sounds play. No NPCs, no save, no era system, and
**no flashback**: aligning fires a trigger nothing consumes yet.
`bundleVersion 0.1.0`, `companyName: DefaultCompany`.

Docs, comments, `Debug.Log` strings and UI text are in **pt-PT**. Match that when editing. Commits
in pt-PT too.

**This is a git repository** (`github.com/Goncalves1307/PineHollow`, branch `main`, commits go
straight to `main`) with a 119-line `.gitignore` covering `Library/`, `Temp/`, `Logs/`,
`UserSettings/`, `*.csproj` and `*.slnx`. Everything else under `Assets/`, `Packages/` and
`ProjectSettings/` is source, **including every `.meta` file**. A `.cs` committed without its
`.meta` loses its GUID and every scene reference to it breaks — and a folder moved without its
`.meta` gets a fresh GUID, which breaks the same way.

## The design documents

Four markdown files at the repo root. They overlap heavily; read them in this order and know what
each one is actually good for:

| File | Use it for |
|---|---|
| `Pine_Hollow_GDD_v0.1.md` | The authoritative design reference. Pillars, world, map, timeline, systems, chapters, endings, vertical-slice contents. Start here. |
| `Pine_Hollow_Historia.md` | The full narrative, scene by scene, with the exact quoted lines and photograph back-texts. Use it whenever you need *canon*, not structure. |
| `Pine_Hollow_GAME_DESIGN.md` | An earlier, shorter cut of the GDD. Mostly redundant. Where it disagrees with the GDD, the GDD wins. |
| `Pine_Hollow_Plano_Completo_Desenvolvimento.md` | The 27-phase build plan with checkboxes, dependency order and milestones. **Its checkboxes are stale — see below.** |

They are design documents, not specifications of what exists. Nothing in them is a claim about the
code.

## Where the plan and the code disagree

Verified 2026-09-21 against the working tree. **When they conflict, the code is the fact.**

**The plan says we are at "Stage 2 — Player / próximo passo Stage 2.3 — Interação básica". We are
well past that.** Fases 2.3, 3 and 4 are entirely unchecked in the plan and entirely implemented in
`Assets/_Project/Scripts/`: camera raycast, `IInteractable`, interaction key, contextual prompt text, pick
up, inspection mode, rotation, zoom, exit. Fase 5 (photography) is unchecked and *partially*
implemented — see the caveat below. Do not re-implement a system because its box is empty; grep
`Assets/_Project/Scripts/` first.

**The folder structure the docs mandate is now the one on disk** (FASE 1, 2026-09-21).
`Assets/_Project/{Art,Audio,Materials,Models,Photography,Prefabs,Scenes,Scripts,Settings,UI}` plus
`Assets/Environment/` and `Assets/ThirdParty/` (which holds TextMesh Pro). The empty folders carry
a `.gitkeep`, because git does not store empty directories and without it they simply did not
appear in a fresh clone. Write new assets into `_Project/`; do not start a third layout.

**~~The photography that exists is not the photography the GDD describes~~ — done in FASE 5**
(`869f4yfg4`). `capturedPhotos` is a `List<PhotoData>`: date, location, people, metadata, front,
**back**, image, declared alignments and discoveries. Authored photographs are `PhotoAsset`
ScriptableObjects (the first in the project) and hand out a **copy** — revealing a discovery is
game state, and writing it into the asset dirties the file on disk. Player captures come from
`PhotoData.DeCaptura` and differ by one field, not by a second code path. The in-game camera is
still a prototype of one verb, and still 256×256.

**Nothing in the code anticipates two eras.** Fase 7 requires a World State with a 2026 state and a
1986 state, and Chapter 4 turns on a flashback *creating* a door that then exists in 2026. No
current script has any notion of era, and `DoorInteractable` holds its state in private fields with
no external reader. Any new interactable should expect to become era-aware and world-state-driven;
don't bury state where a save system and an era switch can't reach it.

## Commands

The editor is at `/home/diogo/dados/Unity/6000.6.2f1/Editor/Unity` (installed via Unity Hub, and
note the install lives *inside* `~/dados/Unity/`, sibling to the project — do not glob from there).

```bash
# compile + asset import, headless — the only automated check this project has
/home/diogo/dados/Unity/6000.6.2f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /home/diogo/dados/Unity/PineHollow \
  -logFile /tmp/unity-compile.log
grep -E "error CS|Compilation failed" /tmp/unity-compile.log   # empty == compiled
```

**That command fails while the editor has the project open** — `Aborting batchmode due to fatal
error: It looks like another Unity instance is running with this project open.` (hit it on
2026-09-21). Check with `pgrep -af 6000.6.2f1` and look for `Temp/UnityLockfile` before blaming the
code; close the editor, or just read the compile result off `Logs/Editor.log`:

```bash
grep -nE "error CS|warning CS" Logs/Editor.log | tail -20
```

`Logs/Editor.log` is append-only across sessions and currently ~10MB — always `tail`, never `cat`.

**There are eleven test files**, all EditMode, **157 tests**, in `Assets/_Project/Tests/Editor/`:
`PlayerStateMachineTests`, `InteractionSystemTests`, `InteractablesTests`, `InspectionSystemTests`,
`HighlightSystemTests`, `CrosshairSystemTests`, `PrototypePlayerSceneTests` (this one opens the
real scene and asserts the inspector wiring), plus the FASE 5 four: `PhotoDataTests`,
`PhotoComparisonSystemTests`, `PhotoAlbumSystemTests` and `PhotoVersoTests`.

**Tests that touch UI must wire the `RawImage`s.** The comparison tests did not, and because
`Desenhar()`/`AplicarTransformacao()` return on the first line without them, a bug that put the
alignment on the wrong origin passed 157 green tests. If a system draws, the test has to let it
draw.

```bash
# os testes -- e repara que NAO leva -quit: com ele o Unity sai antes de os correr
/home/diogo/dados/Unity/6000.6.2f1/Editor/Unity \
  -batchmode -nographics \
  -projectPath /home/diogo/dados/Unity/PineHollow \
  -runTests -testPlatform EditMode -testResults /tmp/unity-tests.xml -logFile /tmp/unity-tests.log
```

**`-quit` and `-runTests` do not go together.** With both, the log says `Batchmode quit
successfully invoked` and no results file is written at all — it looks like a crash and is not.

**There are still zero `.asmdef` files, and that is deliberate.** All game code compiles into the
default `Assembly-CSharp`, and an asmdef **cannot reference a predefined assembly** — so a test
asmdef would not see the game code at all. The way around it is the folder name: anything under an
`Editor/` folder with no asmdef compiles into `Assembly-CSharp-Editor`, which Unity rebuilds as
`Assembly-CSharp-Editor-testable` and which *does* see `Assembly-CSharp`. Put new EditMode tests
next to that file and they are picked up with no asmdef. Giving the game code its own asmdef would
work too, but it is a structural change — do not do it as a side effect of adding a test.

What does exist is a settings validator, `Assets/_Project/Scripts/Editor/Fase1Validacao.cs`. It
asserts the FASE 1 foundation — layers, collision matrix, folders, build settings, quality levels,
shadow distance, camera tags, culling masks and post-processing — and exits non-zero in batchmode:

```bash
/home/diogo/dados/Unity/6000.6.2f1/Editor/Unity \
  -batchmode -nographics -quit -projectPath /home/diogo/dados/Unity/PineHollow \
  -executeMethod Fase1Validacao.Correr -logFile -
```

It checks configuration, not behaviour. Gameplay verification is still manual: enter play mode in
`Assets/_Project/Scenes/Prototype_Player.unity` and exercise the keys listed below.

## The scenes

There are two, both in the build settings, in this order:

- `Assets/_Project/Scenes/Bootstrap.unity` — first in the build list. Holds `BootstrapLoader`,
  which `DontDestroyOnLoad`s itself and loads the game scene by name. This is where anything that
  must survive a scene change belongs — the era switch of Fase 7 swaps scenes underneath it.
- `Assets/_Project/Scenes/Prototype_Player.unity` — the real scene. Player, door, inspectable
  object, the whole photography UI, and a global `Volume` with `PineHollowVolumeProfile`.

The URP template leftovers (`SampleScene.unity`, `Readme.asset`, `TutorialInfo/`) were deleted in
FASE 1.

## Architecture

Scripts in `Assets/_Project/Scripts/`, flat, one `MonoBehaviour` per file, no namespaces (plus
three editor-only scripts under `Editor/`, and `IInteractable`, `PlayerState`, `PhotoData`,
`PhotoMetadata`, `PhotoDiscovery` and `PhotoAlignment`, which are not `MonoBehaviour`s —
`PhotoAsset` is a `ScriptableObject`). All the rest hang off the `Player` GameObject or off the
object they act on. Four clusters:

**Player state** — [`PlayerStateMachine`](Assets/_Project/Scripts/PlayerStateMachine.cs) owns the
mode stack, the cursor and the `Esc`; [`PlayerState`](Assets/_Project/Scripts/PlayerState.cs) is
the enum. Every other cluster defers to it. See "The cursor and `Esc` have a single owner" below
before touching any of them.

**Interaction** — [`IInteractable`](Assets/_Project/Scripts/IInteractable.cs) is the whole contract:
`Interact()` + `GetInteractionText()`. [`InteractionSystem`](Assets/_Project/Scripts/InteractionSystem.cs)
raycasts 3m from the player camera every frame, `GetComponent<IInteractable>()` on the hit, and
drives the `[E] <text>` prompt, and hands the focused object to `HighlightSystem`. Implementors:
`DoorInteractable` (slerps a pivot 90°), `DrawerInteractable` (slides a body), `SwitchInteractable`,
`DocumentInteractable` (hands title+body to `ReadingSystem`), `InspectionInteractable` (hands the
object and its description to `InspectionSystem`). Every one of them carries a read-only state
property and a `stateId`, which is the minimum FASE 7 needs to find them across the two eras.

**Inspection** — [`InspectionSystem`](Assets/_Project/Scripts/InspectionSystem.cs) reparents the target to
the camera transform, caches `position`/`rotation`/`parent`, pushes `Inspecting`, and restores on
`Esc`. `InteractionSystem.Update` defers on `OpenModeCount > 0`. This is the system the GDD's photo
inspection (rotate, flip, zoom, read the back) should grow out of.

Its public API is exactly `IsInspecting` and `Inspect(GameObject target, string descricao = null)`,
and **the `descricao` is optional on purpose**: the FASE 5 `PhotoInteractable` reuses this system
and a photograph needs a description *and* a back. Keep new parameters optional. Rotation and zoom
only run while `IsTopMode(Inspecting)` — without that, any layer opened on top leaves the mouse
spinning the object behind it. The description is its own line in the inspection HUD and **not** the
`ReadingSystem`: that panel is 760×520 and opaque, so it would cover the very object being
described. Pick-up and put-down sounds share the `Player`'s `AudioSource` with the footsteps —
a second one on the same GameObject would make `FootstepSystem`'s `GetComponent` fallback depend on
component order.

**Photography** — [`PhotographySystem`](Assets/_Project/Scripts/PhotographySystem.cs) drives the
photo flow (`F` enters photo mode, LMB captures, `Esc` backs out one level) but is **no longer the
state machine** — since FASE 2 it pushes `Photographing`/`PhotoPreview` onto `PlayerStateMachine`
and only enters when `OpenModeCount == 0`, so `F` can no longer open photo mode on top of an
inspection. Capture copies the player
camera's transform+FOV onto `photoCaptureCamera`, calls `Render()` into
`Assets/_Project/Photography/PhotoRenderTexture.renderTexture`, and `ReadPixels` into a new `Texture2D`.
[`PhotoAlbumSystem`](Assets/_Project/Scripts/PhotoAlbumSystem.cs) rebuilds a thumbnail grid from that list;
[`PhotoThumbnail`](Assets/_Project/Scripts/PhotoThumbnail.cs) is an `IPointerClickHandler` that opens
[`PhotoViewerSystem`](Assets/_Project/Scripts/PhotoViewerSystem.cs) or marks for comparison.

[`PhotoComparisonSystem`](Assets/_Project/Scripts/PhotoComparisonSystem.cs) holds two photographs
side by side or overlaid. **Its manipulation and detection are public methods with no input inside
them**, and `Update` only calls them: `Mouse.current` is null in EditMode, and the rule that
decides whether the player discovered the story could not be left without a test.

**In overlay, the fixed photograph goes to the centre and the movable one is placed at
`Posicao × largura`** — normalised by the width on *both* axes, so the space is isotropic and
rotating in normalised coordinates is rotating in pixels. Getting this wrong is not cosmetic: when
the fixed photograph stayed where the side-by-side view had left it, the game reported
`Alinhada = true` with the images more than half a width apart, and never fired where the player
actually overlapped them.

## Input

**The project is set to the new Input System *only*** (`activeInputHandler: 1`,
`ENABLE_INPUT_SYSTEM` defined and no `ENABLE_LEGACY_INPUT_MANAGER`). `UnityEngine.Input.GetKey*`
throws `InvalidOperationException` at runtime — it will compile fine and die in play mode. Use
`Keyboard.current` / `Mouse.current`, and keep the `!= null` guard every existing call site has.

**`Assets/_Project/Settings/InputSystem_Actions.inputactions` is wired as the project-wide actions asset**
(`com.unity.input.settings.actions` in `EditorBuildSettings.asset`) **but nothing reads it.** There
is no `PlayerInput` component in the scene and no generated C# wrapper — every script polls devices
directly. Do not assume a rebind there changes anything; either migrate the polling to actions, or
treat the asset as dead weight.

Current bindings: `WASD` + `LShift` sprint + `LeftCtrl` crouch (hold, like the sprint), mouse
look, `E` interact, `F` photo mode, `Tab` album, LMB shutter / thumbnail click, `Esc` back out.
FASE 5 added: **`Q` flips** a photograph (in hand and in the viewer), **right-click on a thumbnail**
marks it for comparison (two marks open it), and inside the comparison **`Space`** switches side by
side / overlay, **drag** moves, **right-drag** rotates and **scroll** scales — and those three do
nothing in side-by-side, on purpose. The `Esc` is read in `PlayerStateMachine` and nowhere else; everything else is polled by
the system that owns it.

## Layers and tags

Created in FASE 1; before that the project had **zero** custom layers and `tags: []`.

| Layer | For |
|---|---|
| 8 `Interactable` | anything the interaction raycast should hit — `Door`, `InspectionObject` |
| 9 `Player` | the player capsule, so the raycast can exclude the player's own colliders |
| 10 `PhotoOnly` | rendered by the capture camera only; the player camera does not see it |
| 11 `IgnorePhoto` | rendered by the player camera only — `CameraBody`, the held device, lives here so it does not appear in its own photograph |

World photographs (`PhotoInteractable`) sit on layer 8 like any other interactable.

`UI` (5) and `PhotoOnly` (10) are out of the collision matrix entirely: they are rendering
categories, not physics ones.

**There are still no custom tags, and that is deliberate** — nothing in the codebase reads one
(`CompareTag`, `FindWithTag`: zero hits). The player camera carries the built-in `MainCamera` tag,
which was missing and made `Camera.main` return `null`. Don't add tags speculatively; add a layer
or a `[SerializeField]` reference instead.

## Things that will bite you

**~~The interaction raycast has no layer mask~~ — done in FASE 3** (`869f4yb37`).
`InteractionSystem` now has a `raycastMask` and passes `QueryTriggerInteraction.Ignore` at the call
site; the global `m_QueriesHitTriggers` stays at `1` on purpose. **The mask is not "only the
`Interactable` layer", and must not become that:** the raycast has to *consider* world geometry too,
or the focus goes through walls and `E` opens the door on the other side of the partition. What the
mask excludes is only what must never stop the ray — the player's own colliders, the UI and the
render-only layers. Who *answers* is decided at the end, by having `IInteractable`.


**~~The album is unreachable~~ — fixed in FASE 5.** `Tab` opens and closes it, and
`TogglePhotoAlbum()` is **public on purpose**: `Tab` is read in `Update`, `Keyboard.current` is
null in EditMode, and while the method was private the dead path could not be tested — which is
exactly how it stayed dead. The panel is activated **before** `OpenAlbum()`, because
`PhotoAlbumSystem` lives inside it and its `Update` does not run while it is off; the other order
left `ViewingAlbum` on the stack with nobody consuming `Esc`.

**Two things that only a screenshot catches.** A `Quad` has a single face: as the inspection's
photograph it faced away from the camera and vanished when flipped — world photographs are thin
**cubes**. And an object with the URP default material is white in a beige scene: it was correctly
positioned, visible and in front of the camera, and did not appear in the image. Run
`Fase5Capturas.Correr` (see below) before believing a UI change works.

**The scene is assembled by script, not by hand.** `Assets/_Project/Scripts/Editor/Fase5Montagem.cs`
builds the comparison panel, the viewer's caption and back lines, the world photographs and their
wiring, and re-links anything that came loose. It is idempotent and must stay that way — an early
`return` because the main piece already exists is what once left the flash and the photo assets
unwired. `Fase5Capturas.cs` drives play mode and writes one PNG per screen; it does **not** take
`-nographics`, and it registers its stepper before `EnterPlaymode` because this project enters
play mode **without a domain reload**, so `[InitializeOnLoadMethod]` never fires.

**The cursor and `Esc` have a single owner: [`PlayerStateMachine`](Assets/_Project/Scripts/PlayerStateMachine.cs).**
Fixed in FASE 2. It is the **only** class that writes `Cursor.lockState`/`Cursor.visible` and the
**only** one that reads `escapeKey` — do not add a second of either. It runs at
`[DefaultExecutionOrder(-100)]`, the one execution-order override in the project, so it registers
the `Esc` before any consumer's `Update`.

How to plug a new screen in — journal, inventory, photo comparison, investigation board:

1. Add a value to [`PlayerState`](Assets/_Project/Scripts/PlayerState.cs).
2. `PushMode(yours)` when it opens, `PopMode(yours)` when it closes.
3. In `Update`, close on `stateMachine.ConsumeBack(yours)`. It returns `true` **only** to the mode
   on top of the stack, once per `Esc` — that is what stops one keypress collapsing two layers.
4. If it is a mouse screen, add it to `FreesCursor`. Movement and look lock themselves: they are
   derived from the state, never written from outside.

`IsMovementLocked`/`IsLookLocked` used to be public setters on `PlayerController` that five
systems wrote across (18 cross-writes). They are now read-only and derived. A free cursor locks
both — that is what stopped the camera from spinning with the mouse loose.

*Historical note, since the wrong numbers spread:* the pre-FASE-2 state was **4 scripts writing
`Cursor.lockState` at 9 sites** and **7 `escapeKey` reads across 4 scripts**. This file and
`ESTADO.md` both said "five scripts at eight sites" and "five lines", and the ClickUp task
inherited it from here.

**~~`PhotoViewerSystem.Close()` leaves the cursor free~~ — fixed in FASE 2.** `Close()` now pops
`ViewingPhoto` off the mode stack and control goes back to the player. Listed here only so the
old note is not mistaken for current behaviour.

**Photos are 256×256 and live only in RAM.** The render texture is 256×256, and `capturedPhotos`
([PhotographySystem.cs:31](Assets/_Project/Scripts/PhotographySystem.cs#L31)) is a plain `List<Texture2D>`
that is never trimmed and whose textures are never `Destroy`ed — every shot leaks a texture, and a
scene reload loses the lot. Nothing writes to disk, which also means Fase 21 (save system,
"fotografias descobertas") has nothing to persist yet.

**~~`InspectionSystem.inspectionDistance` is overwritten at runtime~~ — fixed in FASE 2.** The two
roles are split: `inspectionDistance` is the `[SerializeField]` starting distance and is now
honoured, and a private `currentDistance` carries the live zoom state. The old note said "do not
fix the inspector value"; that instruction is void.

**The scene says `NewMonoBehaviourScript`, and that is fine.** `Assets/_Project/Scenes/Prototype_Player.unity:681`
carries `m_EditorClassIdentifier: Assembly-CSharp::NewMonoBehaviourScript`, but the `m_Script` GUID
is `InteractionSystem.cs`'s and the serialised fields are `InteractionSystem`'s. It is a stale
string left from renaming the file; Unity binds by GUID. Harmless — do not hand-edit the YAML to
"correct" it.

**~~`InspectionInteractable` calls `FindFirstObjectByType`~~ — done in FASE 4** (`869f4yb3p`). It
takes an `[SerializeField] InspectionSystem` like every other script here. **The project now
compiles with zero warnings**, which only became a usable signal once the other sixteen CS0618 in
`Fase1Validacao.cs` and `PrototypePlayerSceneTests.cs` went too. Keep it at zero: with a log full of
warnings, nobody notices the next one.

## Design constraints that bind the code

These are decisions, not preferences. Don't add systems that contradict them.

- **No combat. No XP, no levels, no skill trees.** Progression is *knowledge* — what the player has
  learned unlocks what they can do. Anything resembling a stat is out of scope.
- **No permanent HUD.** The UI is a crosshair plus a contextual prompt. The GDD's own example is
  literally `+` and `[E] Interagir`. Journal, inventory, photo viewer, comparison and investigation
  board are screens you open, not overlays that live on screen.
- **Minimal waypoints and markers.** Navigation and investigation are supposed to come from clues,
  not arrows. The investigation board must *not* auto-resolve connections for the player.
- **Exploration is non-linear from Chapter 2 on.** Don't hard-gate the town behind a mission order.
- **Ambiguity is deliberate.** The phenomenon is never explained. Don't add a system, log or
  codex entry that settles it.
- **Build small and playable first.** The stated rule is `Sistema → Teste isolado → Integração →
  Polimento → Conteúdo`, and "não construir Pine Hollow inteiro antes de validar o gameplay". The
  dependency order is Foundation → Player → Interaction → Inspection → Photography → Investigation
  → World State → Flashbacks → NPCs → Content → Audio/Art → Save → Optimisation.

## Canon you must not invent

When writing placeholder content, use the real names and dates — placeholder text has a habit of
surviving.

- **People:** Thomas Hale (factory maintenance, officially dead 1986), Elias Ward (photographer/
  technician, investigates with Thomas in 1986, then disappears), Ethan Cole (photographer, previous
  owner of the house, investigates in 2016, disappears), and the protagonist — **an ordinary person
  with no name, no profession and no special ability**. Not a detective, journalist or chosen one.
- **The photograph years:** 1946, 1958, 1971, 1986, 1994, 2001, 2011, 2016, 2026. 1986 is the core.
- **The night:** 17 July 1986. Its timestamps are exact: 23:31, 23:38, 23:42, 23:47, 23:49, 23:52,
  23:56, 00:03.
- **Back-of-photograph lines** are canon and quoted verbatim in `Pine_Hollow_Historia.md` — "Ele
  ainda não chegou.", "Agora és tu.", "Não reveles todas.", "Thomas não é o único.", "É aqui que
  começa." Copy them, don't paraphrase.

**Naming trap:** in the Portuguese docs, *câmara* means both **camera** (the photographic device,
also "máquina fotográfica") and **chamber** (the underground room) — Final A is titled both
"Destruir a Máquina" and "Destruir a Câmara" for the same beat. In C#, `Camera` is already Unity's
type. Name the underground room `Chamber` and the device `PhotoCamera`/`CameraDevice`; never
`Camera`.

## Conventions worth keeping

- Dependencies are `[SerializeField]` private fields grouped under `[Header("…")]`, assigned in the
  inspector. `FindFirstObjectByType` appears once and is the exception, not the pattern.
- Cross-system state is exposed as read-only properties (`IsInspecting`, `IsPhotographyMode`,
  `IsOpen`) and consumers early-return on them in `Update`. **There is no write-across boundary
  any more:** `IsMovementLocked`/`IsLookLocked` moved to `PlayerStateMachine` and are derived from
  the mode stack, not set from outside. Push a mode instead.
- Interaction prompt text is the verb only — `GetInteractionText()` returns "Abrir", "Fechar",
  "Examinar", "Interagir"; `InteractionSystem` prepends `[E] `. Keep the verb in the interactable.
- No coroutines, no `async`, no events — everything is polled in `Update`. C# 9, netstandard2.1.
- Formatting in `Assets/_Project/Scripts/` is inconsistent: `PhotographySystem.Update`,
  `PlayerController.Update` and `PhotoAlbumSystem.CloseAlbum` are indented at column 0 inside their
  class. Match the surrounding file rather than reformatting a whole file inside a feature change.
