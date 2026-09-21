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

**Current build reality: a mechanics sandbox.** 11 scripts, one playable scene, ~840 lines. Walk,
look, sprint, interact at short range (`E`), pick an object up and rotate it in front of the
camera, take screenshot-style photos into an in-memory album. No narrative content, no NPCs, no
audio, no save, no era system. `bundleVersion 0.1.0`, `companyName: DefaultCompany`.

Docs, comments, `Debug.Log` strings and UI text are in **pt-PT**. Match that when editing. Commits
in pt-PT too.

**This directory is not a git repository** and has no `.gitignore`. If one is initialised, ignore
`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx` — everything else under
`Assets/`, `Packages/` and `ProjectSettings/` is source, **including every `.meta` file**. A `.cs`
committed without its `.meta` loses its GUID and every scene reference to it breaks.

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
`Assets/Scripts/`: camera raycast, `IInteractable`, interaction key, contextual prompt text, pick
up, inspection mode, rotation, zoom, exit. Fase 5 (photography) is unchecked and *partially*
implemented — see the caveat below. Do not re-implement a system because its box is empty; grep
`Assets/Scripts/` first.

**The folder structure in the docs was never adopted.** Both the GDD (§28) and the plan (Fase 1)
mandate `Assets/_Project/{Art,Audio,Materials,Models,Prefabs,Scenes,Scripts,Settings,UI}` plus
`Environment/` and `ThirdParty/`. What exists is those folders **flat under `Assets/`, all empty**
except `Prefabs/` (1 file), `Scenes/` (1 file) and `Scripts/`. There is no `_Project/`, no
`Environment/`, no `ThirdParty/`. The plan checks "Estrutura de pastas" as done; it is not.
Decide once whether to adopt `_Project/` or to bless the flat layout, then make the docs match —
do not quietly write to a third layout.

**`Bootstrap.unity` does not exist.** The plan checks "Cenas Bootstrap e Prototype_Player" as done.
`Prototype_Player.unity` exists but sits at `Assets/` root, not in `Assets/Scenes/`; there is no
Bootstrap scene anywhere.

**The photography that exists is not the photography the GDD describes.** `PhotographySystem` is an
*in-game camera*: it renders the view to a 256×256 RenderTexture and appends a bare `Texture2D` to
a `List`. The GDD's photography is about photographs as **data objects** — Fase 5 spells out the
shape: date, location, people, metadata, front, **back**, image; then compare, overlay, align,
trigger flashback. A raw `Texture2D` carries none of that and cannot. Before building comparison or
alignment on top, the list has to become a `PhotoData` type. Treat the current capture path as a
prototype of one verb ("tirar fotografia"), not as the foundation.

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

**There are no tests.** `com.unity.test-framework` 1.8.0 is installed, but there are zero test
files and **zero `.asmdef` files in the whole project** — all game code compiles into the default
`Assembly-CSharp`. Adding a first test means adding `Assets/Tests/` with its own asmdef referencing
`UnityEngine.TestRunner`; there is no existing pattern to copy. Until then, verification is manual:
enter play mode in `Assets/Prototype_Player.unity` and exercise the keys listed below.

## The scenes

There are two, and **the one in the build settings is not the one being worked on**:

- `Assets/Prototype_Player.unity` (70K) — the real scene. Player, door, inspectable object, the
  whole photography UI. **Not listed in `ProjectSettings/EditorBuildSettings.asset`.**
- `Assets/Scenes/SampleScene.unity` (11K) — the untouched URP template scene (Main Camera,
  Directional Light, Global Volume). **This is the only enabled scene in the build settings**, so a
  build today ships an empty room. Fix the build list before anyone tries to ship a player.

`Assets/Readme.asset` and `Assets/TutorialInfo/` are URP template leftovers, also untouched.

## Architecture

Eleven scripts in `Assets/Scripts/`, flat, one `MonoBehaviour` per file, no namespaces. All of them
hang off the `Player` GameObject or off the object they act on. Three clusters:

**Interaction** — [`IInteractable`](Assets/Scripts/IInteractable.cs) is the whole contract:
`Interact()` + `GetInteractionText()`. [`InteractionSystem`](Assets/Scripts/InteractionSystem.cs)
raycasts 3m from the player camera every frame, `GetComponent<IInteractable>()` on the hit, and
drives the `[E] <text>` prompt. Implementors: `DoorInteractable` (slerps a pivot 90°),
`InspectionInteractable` (hands the object to `InspectionSystem`), `TestInteractable` (a debug stub
— safe to delete once something real replaces it).

**Inspection** — [`InspectionSystem`](Assets/Scripts/InspectionSystem.cs) reparents the target to
the camera transform, caches `position`/`rotation`/`parent`, locks the player, and restores on
`Esc`. `InteractionSystem.Update` early-returns while `IsInspecting`, so it is the one place where
a system explicitly defers to another. This is the system the GDD's photo inspection (rotate, flip,
zoom, read the back) should grow out of.

**Photography** — [`PhotographySystem`](Assets/Scripts/PhotographySystem.cs) is the state machine
(`F` enters photo mode, LMB captures, `Esc` backs out one level). Capture copies the player
camera's transform+FOV onto `photoCaptureCamera`, calls `Render()` into
`Assets/Photography/PhotoRenderTexture.renderTexture`, and `ReadPixels` into a new `Texture2D`.
[`PhotoAlbumSystem`](Assets/Scripts/PhotoAlbumSystem.cs) rebuilds a thumbnail grid from that list;
[`PhotoThumbnail`](Assets/Scripts/PhotoThumbnail.cs) is an `IPointerClickHandler` that opens
[`PhotoViewerSystem`](Assets/Scripts/PhotoViewerSystem.cs).

## Input

**The project is set to the new Input System *only*** (`activeInputHandler: 1`,
`ENABLE_INPUT_SYSTEM` defined and no `ENABLE_LEGACY_INPUT_MANAGER`). `UnityEngine.Input.GetKey*`
throws `InvalidOperationException` at runtime — it will compile fine and die in play mode. Use
`Keyboard.current` / `Mouse.current`, and keep the `!= null` guard every existing call site has.

**`Assets/InputSystem_Actions.inputactions` is wired as the project-wide actions asset**
(`com.unity.input.settings.actions` in `EditorBuildSettings.asset`) **but nothing reads it.** There
is no `PlayerInput` component in the scene and no generated C# wrapper — every script polls devices
directly. Do not assume a rebind there changes anything; either migrate the polling to actions, or
treat the asset as dead weight.

Current bindings, spread across four scripts: `WASD` + `LShift` move, mouse look, `E` interact,
`F` photo mode, `Tab` album (see below), LMB shutter / thumbnail click, `Esc` back out.

## Things that will bite you

**The album is unreachable.** `PhotoAlbumSystem.OpenAlbum()` ([PhotoAlbumSystem.cs:18](Assets/Scripts/PhotoAlbumSystem.cs#L18))
has **no caller anywhere** — not in code, not as a UnityEvent in the scene or prefab.
`Tab` calls [`PhotographySystem.TogglePhotoAlbum()`](Assets/Scripts/PhotographySystem.cs#L226),
which only contains the *already-open* branch and returns without doing anything when the album is
closed. So `Tab` is a no-op and the whole album/viewer path is dead in play mode. Anything you
"fix" downstream of it is unverifiable until this is wired.

**Nobody owns the cursor, and `PlayerController` wins.**
[`PlayerController.HandleCursor()`](Assets/Scripts/PlayerController.cs#L109) runs **unconditionally**
every frame — it is outside both the `IsMovementLocked` and `IsLookLocked` guards. It relocks and
hides the cursor on *any* left-click while the cursor is free. `PhotoAlbumSystem.OpenAlbum()` and
`PhotoViewerSystem.Open()` both free the cursor to let you click a thumbnail; the first such click
re-locks it. Five scripts write `Cursor.lockState` at eight sites with no arbiter. The GDD adds a
journal, an inventory, a photo comparison screen and an investigation board — all cursor screens.
Give the lock a single owner before adding the sixth writer.

**`Esc` is overloaded four ways and Update order is undefined.** `PlayerController` (unlock cursor),
`InspectionSystem` (exit inspection), `PhotographySystem` (close preview / exit photo mode) and
`PhotoViewerSystem` (close) all read `escapeKey.wasPressedThisFrame` in the same frame. Unity gives
no ordering guarantee between MonoBehaviours, so one keypress can collapse two UI layers at once.
A new `Esc` handler makes this worse — route through the existing state machine instead.

**`PhotoViewerSystem.Close()` leaves the cursor free** (`CursorLockMode.None`, visible — same as
`Open()`, [PhotoViewerSystem.cs:47](Assets/Scripts/PhotoViewerSystem.cs#L47)). Closing the viewer
does not hand control back to the player.

**Photos are 256×256 and live only in RAM.** The render texture is 256×256, and `capturedPhotos`
([PhotographySystem.cs:31](Assets/Scripts/PhotographySystem.cs#L31)) is a plain `List<Texture2D>`
that is never trimmed and whose textures are never `Destroy`ed — every shot leaks a texture, and a
scene reload loses the lot. Nothing writes to disk, which also means Fase 21 (save system,
"fotografias descobertas") has nothing to persist yet.

**`InspectionSystem.inspectionDistance` is a `[SerializeField]` that is overwritten at runtime.**
`Inspect()` resets it to a hard-coded `1.5f` ([InspectionSystem.cs:32](Assets/Scripts/InspectionSystem.cs#L32))
before using it, so whatever you set in the inspector is ignored. It doubles as the live zoom
state, which is why. Do not "fix" the inspector value — change the literal, or split the two roles.

**The scene says `NewMonoBehaviourScript`, and that is fine.** `Assets/Prototype_Player.unity:681`
carries `m_EditorClassIdentifier: Assembly-CSharp::NewMonoBehaviourScript`, but the `m_Script` GUID
is `InteractionSystem.cs`'s and the serialised fields are `InteractionSystem`'s. It is a stale
string left from renaming the file; Unity binds by GUID. Harmless — do not hand-edit the YAML to
"correct" it.

**`InspectionInteractable` calls `FindFirstObjectByType` in `Interact()`** — a per-interaction
scene-wide search, and the one compiler warning this project emits (CS0618, deprecated). Wire an
`[SerializeField] InspectionSystem` like every other script here does.

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
  `IsOpen`) and consumers early-return on them in `Update`. Player locking is the one write-across
  boundary (`IsMovementLocked` / `IsLookLocked`, public setters on `PlayerController`).
- Interaction prompt text is the verb only — `GetInteractionText()` returns "Abrir", "Fechar",
  "Examinar", "Interagir"; `InteractionSystem` prepends `[E] `. Keep the verb in the interactable.
- No coroutines, no `async`, no events — everything is polled in `Update`. C# 9, netstandard2.1.
- Formatting in `Assets/Scripts/` is inconsistent: `PhotographySystem.Update`,
  `PlayerController.Update` and `PhotoAlbumSystem.CloseAlbum` are indented at column 0 inside their
  class. Match the surrounding file rather than reformatting a whole file inside a feature change.
