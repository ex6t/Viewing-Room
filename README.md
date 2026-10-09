# Viewing Room

A Unity VR observatory for the Orbit capstone. The robot introduces learning models and their controls through a self-paced guided tour. Quest 3 standalone is the first target.

Open this project with **Unity 6000.5.10f1**, including Android Build Support, SDK/NDK and OpenJDK. Open `Assets/Scenes/SampleScene.unity` and enter Play Mode. The project uses URP 17.5, XR Interaction Toolkit 3.5.1 and OpenXR 1.17.1.

## Current opening: Home in Motion

Choose Guided Tour at the robot, read the journey preview, then continue to the central podium. The first model introduces Earth and the Sun in three dimensions. Change time and watch our home follow its orbit, read the short reflection, and preview the planet gallery. Revisit or return to the room whenever you choose. There is no quiz, score or fixed visit duration.

The tablet introduces one control at a time with a gentle colour pulse. Each cue retires after that control is used. Every instrument control remains available, and learners may continue without performing a checklist. The tablet sits lower while viewing the model so it leaves more of the exhibit visible.

| Action | Quest controller | Editor keyboard |
| --- | --- | --- |
| Reveal text / continue | A | Enter |
| Select welcome choice | X | X |
| Welcome choice | Left stick up/down | Up/down arrows |
| Change viewing speed | Left stick left/right | Left/right arrows |
| Pause / play the Earth model | X | X |
| Reset time / return to the model | B | B |
| Pause / choose Guided Tour or Free Roam | Y | M |
| Room teleport | Either trigger, aim and release | Requires XR input simulation |
| Snap turn in the room / during guided travel | Right stick | Requires XR input simulation |

The opening keeps Earth's orbital parameters fixed. Changing speed changes elapsed viewing time. The orbit, pause state and control introductions resume after the mode menu. Leaving for Free Roam suspends the model; revisiting resumes the saved interaction.

## Research calculations and visual scale

`EarthOrbitModel` calls the inherited **BergerSol.CalculateOrbitalParameters** for reference year **2000** and the unchanged **KeplerianSolver.keplerian_inverse** for true anomaly. The semi-major axis is **1.00000261 AU** and the prescribed sidereal period is **365.256363 days**, matching the inherited research example in `Program.cs`. This does not calculate the period from Kepler's third law.

The Sun is at a focus. The adapter uses the research radius-vector relationship and maps its XY orbital plane into Unity's horizontal XZ plane. Calculations remain in double precision until they become display coordinates. The display plane is tilted 30 degrees toward the viewing area for readability; this is a rigid viewing transform and does not change the computed orbit. Earth's eccentricity is about **0.01670366**, from the Berger calculation; the opening does not exaggerate it or let a controller change it.

This is a reference orbit with phase measured from perihelion, **not a live ephemeris or calendar-date position**. The display compresses orbital distance and time and enlarges the bodies independently for visibility. The globe is a visual marker for Earth; its surface orientation is illustrative. The model does not currently simulate axial rotation, surface insolation or other planets. Those belong to later validated chapters.

`Assets/Imported/OrbitResearch/bergers.cs` and `keplerian.cs`, including their metadata, are copied unchanged from the inherited project. Their attributions and references remain intact. The original research is [Kostadinov and Gilb, Earth Orbit v2.1](https://gmd.copernicus.org/articles/7/1051/2014/); the paper's supplement includes the MATLAB source.

The existing eccentricity experiment, its recording and `OrbitObservatory.prefab` remain at the wall for later Kepler-chapter integration. They show an illustrative experiment with deliberately adjustable eccentricity. That experiment is separate from the fixed Earth reference orbit in the opening.

## Room layout and next chapters

Keep the foyer focused on orientation and the opening. Use the docked Hub Station and dedicated inherited learning spaces for the larger chapters. The proposed route is **Home in Motion -> Planet Gallery -> Kepler's laws -> Earth and seasons -> Habitable zone -> Changing Sun -> Return/revisit**. Explain each destination before moving there.

| Anchor | Unity world position | Purpose |
| --- | --- | --- |
| Existing XR Origin | (-7, 0.5, 0) | Arrival and robot welcome |
| Existing robot start | (-2, 1.08, 0) | Tour choice and returning-learner invitation |
| Central model root / Sun focus | Root (0, 1.55, 0), Sun (0, 2.4, 0) | Earth–Sun opening above the existing podium |
| Guide arrival point | (0, 0, -1.8) | Guide stands south; learner follows with the existing 2.8 m separation |
| Existing wall exhibit | (0, 2.1, -9.7) | Retained orbital experiment |
| Proposed boarding staging | (11.3, 0, 0) | Existing east porch; path and landing checks required |
| Existing Hub Station root | (19, 0, 0) | Planet gallery and orbital-experiment destination |

Keep a 2 m circulation lane south of the central exhibit and toward the east porch. Reuse the Hub Station's existing EarthView and Eccentricity podiums as future anchors. Preserve the current room geometry, rig, central podium, wall assets and docked station.

**Boarding, the planet gallery and later chapters are previewed but are not connected in this version.** Their inherited content remains in [FALL2026-VR-Orbit-Simulation](https://github.com/ex6t/FALL2026-VR-Orbit-Simulation). Before connecting scenes, verify landing surfaces and navigation, preserve research units, and ensure exactly one active tracked rig and EventSystem.

## Code and content

Runtime scripts are small MonoBehaviours under `Assets/Scripts`. `ViewingRoomLesson` owns the opening sequence; `ViewingRoomLessonInput` reads controls; `EarthOrbitModel` presents the research calculation; the existing robot, tablet and locomotion scripts retain their roles. The reusable opening model is `Assets/Prefabs/HomeInMotion/HomeInMotion.prefab`.

The inherited project descends from [KellyLFrear/FALL2025-VR-Orbit-Simulation](https://github.com/KellyLFrear/FALL2025-VR-Orbit-Simulation). Imported art and packages retain their original ownership. Preserve Inspector references, metadata and the tracked XR rig. Keep changes small and understandable, and validate research units before adding quantitative date, season or sunlight controls.
