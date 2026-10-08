# CSEL Tamagotchi

A Tamagotchi-style virtual pet for phones (portrait 9:16), made in Unity. Look after a pixel-art
hamster by feeding it, giving it water, studying, sleeping and playing. It reacts with an animated
emote and a sound to every action. Let it go hungry and it gets **hangry**. Neglect it for too long
and it gets **sick**.

<p align="center"><img src="Docs/screenshot.png" alt="Game screen" width="320"></p>

<p align="center"><img src="Docs/states_overview.png" alt="Sleeping, hangry, playing and sick states" width="720"></p>

<p align="center"><img src="Docs/screenshot_landscape.png" alt="Wide window: UI stays a centered phone column" width="480"></p>

> Screenshots generated with **Tamagotchi → Capture Screenshot** / **Capture State Screenshots**
> (states left to right: sleeping, hangry, playing at the beach, sick) / **Capture Landscape Check**
> (a wide window: the UI stays a centered phone column). A GIF of the gameplay will be added later.

## Status

| Step | Feature | Status |
|------|---------|--------|
| 1 | Project setup (portrait, Canvas Scaler, safe area, art import) | ✅ Done |
| 2 | UI layout (stat bars, pet area, action buttons, Scene button, mute) | ✅ Done |
| 3 | Stats, hunger bar, hangry state, sick / game over + Restart | ✅ Done |
| 4 | Pet state machine and emotes (idle, eating, drinking, studying, sleeping, playing, happy, sad, crying, hangry, sick) | ✅ Done |
| 5 | Audio (AudioManager, CC0 sounds, mute) | ⏳ Planned |
| 6 | Save/load with offline decay | ⏳ Planned |
| 7 | Polish | ⏳ Planned |
| 8 | WebGL (GitHub Pages) and mobile builds | ⏳ Planned |

## Features

- **Six stats (0–100):** Hunger, Thirst, Happiness, Energy, Intelligence and Health. They decay every
  second. All numbers are in `Assets/Data/PetStatsConfig.asset` (see *Tuning the stats*).
- **Hunger bar** at the top that goes green → yellow → red. **Below 25%** the pet is **hangry**: the bar
  flashes red and pulses, and the mood line says so. *(The angry emote and hangry sound come in steps 4–5.)*
- **Health** drops while Hunger or Thirst is at 0, and slowly recovers while both are above 50%.
- **Sick / game over:** if Health reaches 0, **or Hunger stays at 0 for 30 seconds**, the pet gets
  sick. A game-over card with the crying hamster and a **Restart** button appears.
- **Sleep** restores Energy over time (4 per second) and slows the other stats. The pet wakes up by
  itself when Energy is full, or when you tap Sleep again or do anything else.
- **Full-screen scenes:** the background fills the whole screen behind the UI. The **Scene** button
  cycles through 6 pixel-art backgrounds (default: cozy home). The stats sit at the top and the
  buttons at the bottom, leaving the middle clear so nothing covers the pet.
- **Speech bubble** above the pet's head reacts to every action ("Yum yum!", "Too tired to study...").
  Messages fade after a few seconds; warnings like **"I'm HANGRY! Feed me!"** stay until fixed.
- **Animated pet with a state machine** (`PetController`). It reacts visibly to every button and to its stats:

  | State | When | Animation and effect |
  |-------|------|-------------|
  | Idle | Nothing special going on | `idle` frames, slow loop |
  | Eating | FEED (2 s) | `eating` frames + little hops |
  | Drinking | DRINK (2 s) | `eating` frames (the pack has no drinking frames) + little hops |
  | Studying | STUDY (2 s) | `studying` frames |
  | Playing → Happy | PLAY (2 s, then 1.2 s happy) | `playing` then `happy` frames + bouncing |
  | Sad | Happiness below 25%, or the pet refuses (too tired) | `sad` frames |
  | Crying | Health below 25% | `crying` frames |
  | Hangry | Hunger below 25% | `sad` frames + red pulse + angry shake |
  | Sleeping | SLEEP | `sleeping` frames; the screen dims and switches to the moonlit bedroom |
  | Sick | Health 0 / starving too long | `crying` frames + green tint; game-over card (animated) |

  Priority when several apply: Sick > Sleeping > Hangry > Crying > Sad > Idle. Button reactions play on
  top for a moment, except while sick or asleep.
- *Planned:* a sound for every action and emote, a mute toggle, and save/load.

## Controls

| Button | Effect (default numbers) |
|--------|--------|
| Feed | Hunger +25, Happiness +3 |
| Drink | Thirst +25 |
| Study | Intelligence +12, Energy −10, Happiness −5 (refused if Energy < 10: "too tired") |
| Sleep | Falls asleep and Energy refills over time; tap again to wake up |
| Play | Happiness +20, Energy −8, Hunger −3, Thirst −3 (refused if Energy < 8) |
| Scene | Changes the background (cozy home → sunny garden → beach → forest stream → sunset rooftop → moonlit bedroom) |
| ♪ (top right) | Mute / unmute (works from step 5) |
| Restart | On the game-over card: new pet with starting stats |

## Tuning the stats

1. In Unity's **Project** window open `Assets > Data` and click **PetStatsConfig**.
2. The **Inspector** shows every number: starting values, decay per minute, sleep speed, health
   drain/regeneration, the hangry threshold, how long hunger can stay at 0, and each button's effect.
3. Change values. They apply the next time you press Play. Your edits are kept when the scene is
   regenerated.
4. **Demo mode:** set **Time Scale** to `10` (or `30`) to watch the hunger bar drain, go hangry and get
   sick in about a minute. Set it back to `1` for normal play.

## Screen layout

- Portrait 9:16, Canvas Scaler **Scale With Screen Size**, reference **1080×1920**, safe area respected.
- Screen Match Mode is **Expand** (not *Match 0.5*): the whole 1080×1920 layout always fits. Taller phones
  get extra room around the pet, and wide windows (desktop, browser, the Editor's *Free Aspect*) keep
  the UI in a centered 9:16 column (`PortraitFrame`) while the background fills the window. *Match 0.5*
  squashed the layout in wide windows and clipped the button row on extra-tall phones.

## Unity version and packages

- **Unity 6.3 LTS — `6000.3.24f1`**
- Render pipeline: **URP 2D** (Universal Render Pipeline)
- Packages (from `Packages/manifest.json`): Universal RP, Input System, uGUI (includes TextMeshPro),
  2D Sprite, 2D Animation, Test Framework.

## Project structure

```
Assets/
  Art/
    Pet/Hamster/      hamster animation frames (idle, eating, studying, sleeping, happy, playing, sad, crying)
    Backgrounds/      6 full scenes (cozy_home, moonlit_bedroom, sunny_garden, beach, forest_stream, sunset_rooftop)
    Props/            decorative props (Home, Garden, Beach, Forest)
    UI/               Buttons/, Dialogs/, Status/ sprites from the pack
      Icons/          round stat icons cut from the Status bars (by Tools/make_ui_sprites.py)
      Generated/      9-slice bar/button/panel frames drawn in the pack's palette
    Fonts/            Kenney Pixel font (CC0) + its TextMesh Pro font asset
  Editor/
    ProjectSetup.cs            menu "Tamagotchi > Setup Project": player settings + generates Main.unity
    MainSceneBuilder.cs        builds the whole UI layout and wires every reference
    ScreenshotCapture.cs       menu "Tamagotchi > Capture Screenshot" -> Docs/screenshot.png
    ArtImportPostprocessor.cs  imports Assets/Art as Point-filtered, uncompressed sprites (+ 9-slice borders)
  Scenes/Main.unity   the game scene (generated by ProjectSetup; don't edit by hand, see below)
  Scripts/
    UI/UIManager.cs   owns all UI: stat bars, buttons, mood text; raises ActionPressed / MutePressed
    UI/StatBar.cs     one 0-100 meter (fixed color or gradient)
    UI/PressBounce.cs squash-and-spring feedback on button press
    UI/SpeechBubble.cs  pop-in speech bubble above the pet (normal or sticky messages)
    UI/SafeArea.cs    keeps UI clear of notches and cut-outs
    UI/PortraitFrame.cs keeps the UI a centered 9:16 column in wide windows
    World/BackgroundSwitcher.cs  full-screen background: Scene button cycling + temporary override (used by Sleep)
    Pet/PetStats.cs        the six stats: decay, actions, sleep, health, hangry, sick
    Pet/PetStatsConfig.cs  ScriptableObject with all tuning numbers
    Pet/PetController.cs   state machine: Idle, Eating, Drinking, Studying, Sleeping, Playing, Happy, Sad, Crying, Hangry, Sick
    Pet/SpriteAnimator.cs  flip-book animation of sprite frames on a UI Image
    GameManager.cs         wires buttons -> stats + pet reactions, and stats -> UI (bars, hangry, game over)
  Data/PetStatsConfig.asset  the tuning values used by the game
  TextMesh Pro/       TextMesh Pro essential resources
  Tests/PlayMode/     smoke test, UI layout, stat rules, game-flow and pet state machine tests
Docs/                 screenshots
Tools/
  verify.sh           batch-mode check: compile, regenerate scene, run tests
  make_ui_sprites.py  cleans button sprites, cuts icons, draws frames (Python + Pillow)
```

## Audio credits

_No audio yet. Step 5 adds CC0 sounds from [Kenney.nl](https://kenney.nl/assets), and every file
will be listed here with its source and license._

## Art and font credits

| Asset | Source | License |
|-------|--------|---------|
| Hamster sprites, backgrounds, props, UI sprites | Supplied by the project owner (tamagotchi asset pack) | Project owner's own assets |
| `Assets/Art/UI/Icons/*`, `Assets/Art/UI/Generated/*` (incl. speech bubble) | Derived from / drawn to match the pack by `Tools/make_ui_sprites.py` | Same as above |
| `Assets/Art/UI/Buttons/star.png`, `music.png`, `sleep_z.png` | Re-cut from the pack's `Source/complete_generated_asset_sheet.png` (the pack's own copies are clipped) | Same as above |
| `Assets/Art/Fonts/KenneyPixel.ttf` | [Kenney Fonts](https://kenney.nl/assets/kenney-fonts) by Kenney | CC0 1.0 (`KenneyFonts-License.txt`) |
| `Assets/TextMesh Pro/*` (LiberationSans etc.) | Unity TextMesh Pro essential resources | Unity Companion License / SIL OFL (LiberationSans) |

---

## How to run

### A. Mac (Terminal)

1. Clone the repo:
   ```bash
   git clone https://github.com/Tinenen-cs/Tinenen-cs-csel-tamagotchi.git && cd Tinenen-cs-csel-tamagotchi
   ```
2. Install **Unity Hub** from <https://unity.com/download>. In Unity Hub, go to **Installs → Install
   Editor** and choose **6000.3.24f1**. If it isn't listed, use the
   [Unity download archive](https://unity.com/releases/editor/archive), select **Unity 6** and find
   `6000.3.24f1`. Add the **WebGL**, **Android** and/or **iOS** build support modules if you want to
   build for those platforms.
3. Open Unity Hub:
   ```bash
   open -a "Unity Hub"
   ```
   Then go to **Projects → Add → Add project from disk** and select the `Tinenen-cs-csel-tamagotchi` folder.
4. Open the project. In the Project window, double-click `Assets/Scenes/Main.unity` and press **Play**.
   Set the Game view resolution to **1080x1920 Portrait** (or any 9:16 size) so it looks like a phone.
5. Optional command-line check (compile + scene smoke test, no GUI):
   ```bash
   UNITY="/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity" Tools/verify.sh
   ```
   _A command-line build command is added in step 8._

### Test-run in the Unity Editor (Windows, step by step)

1. **Get the latest code** (Git Bash or PowerShell):
   ```bash
   cd path/to/Tinenen-cs-csel-tamagotchi
   git checkout main
   git pull
   ```
2. Open **Unity Hub** from the Start menu.
3. **First time only:** go to **Projects → Add → Add project from disk**, select the project folder
   (the one that contains `Assets`, `Packages` and `ProjectSettings`) and click **Add Project**.
4. Make sure the project row shows editor version **6000.3.24f1**. If not, click the version and pick it.
5. Click the project to open it. The first open takes a few minutes while Unity builds `Library/`.
6. In the **Project** window (bottom), go to `Assets > Scenes` and double-click **Main**.
7. Click the **Game** tab. In the resolution dropdown at the top left ("Free Aspect"), click **+**, enter
   Label `Phone`, Type *Fixed Resolution*, **1080 × 1920**, click **OK** and select it. Lower the
   **Scale** slider if it's too big.
8. Press **▶ Play** (top center).
9. Try it:
   - **FEED / DRINK / PLAY / STUDY:** the bars change and the speech bubble above the hamster reacts.
   - **SLEEP:** the hamster sleeps, the screen dims and turns to the moonlit bedroom while Energy refills;
     tap again to wake up.
   - **PLAY:** the hamster plays and bounces, then looks happy.
   - **SCENE:** the background changes.
   - To see **hangry** and **sick** quickly, use demo mode (see *Tuning the stats*).
10. Keep the **Console** tab open (Window → General → Console). It should show no red errors.
11. Press **▶** again to stop. Changes made while playing are discarded.
12. **Run the automated tests (optional):** **Window → General → Test Runner → PlayMode → Run All**.
    All tests should be green.

### B. Other devices

- **Windows / Linux:** clone the repo the same way (Git Bash, PowerShell or a terminal), then
  add the folder in Unity Hub and open `Assets/Scenes/Main.unity`.
- **Android:** _detailed steps added in step 8_ (File → Build Profiles → Android → enable Developer
  Mode and USB debugging on the phone → Build And Run).
- **iPhone:** _detailed steps added in step 8_ (File → Build Profiles → iOS → open the generated Xcode
  project → sign with your Apple ID → Run on device; needs a Mac).
- **Browser (any device):** _WebGL build on GitHub Pages added in step 8_. It will be at
  <https://tinenen-cs.github.io/Tinenen-cs-csel-tamagotchi/>.

## Troubleshooting

- **The UI looks squashed or the buttons overlap the room (Scene/Game view):** the Game view is on
  *Free Aspect* (a wide shape). Pick **Phone (1080×1920)** in the Game view's resolution dropdown (see
  *Test-run in the Unity Editor*). Since the portrait-frame fix, wide windows (desktop, browser) also
  keep the UI as a centered phone-shaped column with the room filling the rest of the window.

- **"Missing scene" or empty Hierarchy:** open `Assets/Scenes/Main.unity`, or run
  **Tamagotchi → Setup Project** from the menu bar to regenerate it.
- **I changed the layout in the Scene view and it got overwritten:** `Main.unity` is generated by
  `Assets/Editor/MainSceneBuilder.cs`. Make layout changes there, then run **Tamagotchi → Setup Project**.
- **TextMesh Pro asks to "Import TMP Essentials":** they are already in `Assets/TextMesh Pro`. Close the
  window; if text is missing, re-run **Tamagotchi → Setup Project**.
- **Sprites look blurry:** right-click `Assets/Art` and choose **Reimport**. The import postprocessor
  then re-applies Point filtering.
- **Unity asks to upgrade or downgrade the project:** use exactly `6000.3.24f1` (see
  `ProjectSettings/ProjectVersion.txt`).
- **Tools/verify.sh can't find Unity:** set the `UNITY` environment variable to your editor's path.
- **Weird errors after switching branches:** close Unity, delete the `Library/` folder and reopen.
  Unity rebuilds it.

## Contributing

```bash
git pull origin main                    # get the latest
git checkout -b feature/short-name      # new branch per change
# ...make changes in Unity, then:
Tools/verify.sh                          # make sure it compiles and the scene runs clean
git add -A && git commit -m "Describe the change"
git push -u origin feature/short-name
gh pr create --base main --fill          # or open the PR on github.com
```

Always commit the `.meta` files that Unity creates next to each asset. Never commit `Library/`,
`Temp/`, `Logs/` or `UserSettings/` (the `.gitignore` already excludes them).
