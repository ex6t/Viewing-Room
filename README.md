# Viewing Room

A Unity VR observatory for the Orbit capstone. The robot introduces learning models and their controls through a self-paced guided tour. Quest 3 standalone is the first target.

Open this project with **Unity 6000.5.10f1**, including Android Build Support, SDK/NDK and OpenJDK. Open `Assets/Scenes/SampleScene.unity` and enter Play Mode. The project uses URP 17.5, XR Interaction Toolkit 3.5.1 and OpenXR 1.17.1.

## Current guided stop

Choose Guided Tour at the robot. Read the route preview, then choose when to travel to the Orbit Observatory. Try time controls, compare orbit shapes, read the explanation, and preview the planet gallery. Revisit the model or return to the room. There is no quiz, score or fixed visit duration.

| Action | Quest controller | Editor keyboard |
| --- | --- | --- |
| Reveal text / continue | A | Enter |
| Select welcome choice / operate instrument | X | X |
| Welcome choice | Left stick up/down | Up/down arrows |
| Playback speed / orbit shape | Left stick left/right | Left/right arrows |
| Reset model / previous explanation | B | B |
| Optional inherited narration | Right stick click | R |
| Pause / choose Guided Tour or Free Roam | Y | M |
| Room teleport | Either trigger, aim and release | Requires XR input simulation |
| Snap turn | Right stick | Requires XR input simulation |

The model shows an **illustrative orbit**, with exaggerated eccentricity and scaled body sizes and time. It does not show Earth's current position, a real date, or a calibrated Earth ephemeris. The Sun stays at one focus; the planet follows the inherited Kepler solver; small dots mark equal time intervals. Playback speed changes presentation time, not the orbit's physical parameters.

The planet gallery and later chapters are previewed but are not connected to this build yet. The full route is orbit controls, planet gallery, Kepler's laws, Earth and seasons, habitable zone, and the changing Sun. The inherited learning scenes remain in [FALL2026-VR-Orbit-Simulation](https://github.com/ex6t/FALL2026-VR-Orbit-Simulation).

## Code and content

Runtime scripts are small MonoBehaviours under `Assets/Scripts`. `ViewingRoomLesson` owns the stop sequence; `ViewingRoomLessonInput` reads controls; `OrbitLearningModel` owns the demonstration; the existing robot, tablet, display and locomotion scripts retain their roles. The reusable model is `Assets/Prefabs/OrbitObservatory/OrbitObservatory.prefab`.

`Assets/Imported/OrbitResearch/keplerian.cs` is copied unchanged, with its original metadata, from the inherited project. It retains Dr. T. S. Kostadinov's attribution and research references. The inherited project descends from [KellyLFrear/FALL2025-VR-Orbit-Simulation](https://github.com/KellyLFrear/FALL2025-VR-Orbit-Simulation). Imported art and packages retain their original ownership.

Preserve existing Inspector references, asset metadata and the tracked XR rig. Keep changes small and understandable. Research calculations and real-world units need separate validation before adding quantitative date, season or sunlight controls.
