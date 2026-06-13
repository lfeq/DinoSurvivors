# Dino Survivors

A *Vampire Survivors*-style auto-battler built with **MonoGame** (DesktopGL, .NET 9). All
gameplay logic lives in the engine-agnostic `DinoSurvivors.Core` library; `Game1.cs` is the
MonoGame rendering/input shell, and `DinoSurvivors.Tests` covers the simulation.

## Running the game

From the repository root:

```bash
dotnet run
```

This builds the project (including the Content pipeline) and launches the game window. To run
the test suite:

```bash
dotnet test
```

> The game currently renders everything procedurally from a single 1×1 white pixel
> (`Game1.cs`, `LoadContent`). There are no image/audio assets yet — see below to add them.

## Adding assets

Assets are handled by the **MonoGame Content Pipeline**. The pipeline is already wired up:
`DinoSurvivors.csproj` references `MonoGame.Content.Builder.Task`, `Game1.cs` sets
`Content.RootDirectory = "Content"`, and content builds automatically on `dotnet build` /
`dotnet run`.

### 1. Add the file to `Content/`

Drop your asset into the `Content/` folder, e.g. `Content/player.png`, `Content/music.ogg`,
or `Content/ui.spritefont`.

### 2. Register it in `Content/Content.mgcb`

The easiest way is the visual editor:

```bash
dotnet tool install -g dotnet-mgcb-editor      # once
export PATH="$PATH:$HOME/.dotnet/tools"          # add global tools to PATH if missing
mgcb-editor Content/Content.mgcb                 # add the file, then "Build"
```

Or edit `Content/Content.mgcb` by hand — append an entry under the `#--- Content ---#`
section. Example for a texture:

```
#begin player.png
/importer:TextureImporter
/processor:TextureProcessor
/build:player.png
```

Common importer/processor pairs:

| Asset            | Extension      | Importer                  | Loads as       |
|------------------|----------------|---------------------------|----------------|
| Sprite / texture | `.png` `.jpg`  | `TextureImporter`         | `Texture2D`    |
| Bitmap font      | `.spritefont`  | `FontDescriptionImporter` | `SpriteFont`   |
| Sound effect     | `.wav`         | `WavImporter`             | `SoundEffect`  |
| Music            | `.ogg` `.mp3`  | `Mp3Importer`/`OggImporter`| `Song`        |

### 3. Load it in `Game1.LoadContent()`

Reference assets by name **without the extension**:

```csharp
_playerTexture = Content.Load<Texture2D>("player");
_song          = Content.Load<Song>("music");
_font          = Content.Load<SpriteFont>("ui");
```

### Alternative: load a PNG at runtime (no pipeline)

For quick iteration you can skip the pipeline, mark the file to copy to the output directory,
and load it directly:

```csharp
using var stream = File.OpenRead("Content/player.png");
_playerTexture = Texture2D.FromStream(GraphicsDevice, stream);
```

This is simpler but forfeits pipeline benefits (texture compression, `SpriteFont` generation,
audio format conversion).

## Creating a level (stage)

Levels are called **stages**. A stage is just data: a spawn schedule describing which enemies
appear and how fast, over time. All of it lives in
`DinoSurvivors.Core/Interfaces.cs` → `DefaultContent.WaveSchedules`, a
`Dictionary<int, WavePhase[]>` keyed by stage number.

### Anatomy of a stage

```csharp
{ 2, new[] {
    new WavePhase { StartTime = 0f,   SpawnInterval = 1.3f, Weights = new[] { (EnemyType.Compy, 30), (EnemyType.Raptor, 70) } },
    new WavePhase { StartTime = 240f, SpawnInterval = 1.1f, Weights = new[] { (EnemyType.Compy, 20), (EnemyType.Raptor, 50), (EnemyType.Triceratops, 30) } }
}},
```

A stage is an array of **`WavePhase`** entries (`Interfaces.cs`, `WavePhase`):

- **`StartTime`** — seconds into the stage when this phase becomes active. Phases must be
  ordered by ascending `StartTime`; the latest one whose `StartTime` has passed is used
  (see `Simulation.cs`, the spawn-phase lookup).
- **`SpawnInterval`** — seconds between spawns (lower = more enemies).
- **`Weights`** — `(EnemyType, weight)` pairs. Each spawn picks a type by relative weight, so
  `(Compy, 70), (Raptor, 30)` ≈ 70% Compy. Available types are in `EnemyType`:
  `Compy`, `Raptor`, `Triceratops`.

### How stages flow

- Normal stages run until `StageExitRevealTime` (600s / 10 min, `Simulation.cs`), when the
  safehouse exit appears. Reaching it opens a **safehouse break** (pick a reward), then
  advances to the next stage.
- `LiveEnemyCap` (`Simulation.cs`) caps simultaneously-alive enemies per stage.
- Stage progression is currently hardcoded in `Simulation.cs`
  (`SelectSafehouseRewardOption`): stages `1 → 2 → 3`, then `3` jumps to the **Heliport** boss
  stage (`HeliportStageNumber = 4`), which spawns the **T-Rex** and ends the run on victory.

### Add a new normal stage

To insert a new stage (e.g. a stage 4 before the Heliport boss):

1. **Add its spawn schedule** to `DefaultContent.WaveSchedules` in `Interfaces.cs`:

   ```csharp
   { 4, new[] {
       new WavePhase { StartTime = 0f,   SpawnInterval = 1.0f, Weights = new[] { (EnemyType.Raptor, 60), (EnemyType.Triceratops, 40) } },
       new WavePhase { StartTime = 300f, SpawnInterval = 0.8f, Weights = new[] { (EnemyType.Triceratops, 100) } }
   }},
   ```

2. **Give it an enemy cap** in `LiveEnemyCap` (`Simulation.cs`):

   ```csharp
   public int LiveEnemyCap => StageNumber switch { 2 => 100, 3 => 120, 4 => 130, HeliportStageNumber => 60, _ => 80 };
   ```

3. **Wire it into progression** in `SelectSafehouseRewardOption` (`Simulation.cs`). The current
   logic uses `StageNumber < 3` and `== 3` to decide the next stage — bump those bounds so the
   run reaches your new stage before transitioning to `HeliportStageNumber`. (If you add stages
   beyond the boss, also revisit `HeliportStageNumber` and the boss-stage checks.)

4. **Add a test** in `DinoSurvivors.Tests` covering the new stage's spawn behaviour and
   progression, then `dotnet test`.

> Tip: because all stage content is plain data in `Core`, you can balance and unit-test levels
> without touching MonoGame at all.
