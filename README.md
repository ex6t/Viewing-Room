# Viewing Room

A Unity VR observatory for the Orbit capstone. The guided experience starts with something familiar—watching a day pass—then reveals what Earth is doing. Quest 3 standalone is the first target.

Open with **Unity 6000.5.10f1** and Android Build Support, SDK/NDK and OpenJDK. Open **`Assets/Scenes/Observatory.unity`** and enter Play Mode. The project uses URP 17.5, XR Interaction Toolkit 3.5.1 and OpenXR 1.17.1. Observatory is the enabled build scene; the previous room remains in `Assets/Scenes/SampleScene.unity`.

## Current prototype

Choose Guided Tour at the robot. Watch the Sun move, daylight fade and stars appear. Move time forwards or backwards, then press A to reveal the Earth–Sun model at **the same moment**. The cyan dot represents the observation point: watch it turn into sunlight and out again. B returns to the ground view without changing time.

The learner stays in the same position during the reveal. Short robot narration accompanies compact captions beside the view. The time instrument sits below the model. The left-stick cue pulses until used; interaction is optional, with no quiz, score or required checklist.

| Action | Quest controller | Editor keyboard |
| --- | --- | --- |
| Choose a menu option | Left stick up/down | Up/down arrows |
| Start / reveal Earth / continue | A | Enter |
| Move time forwards/backwards | Left stick right/left | Right/left arrows |
| Pause / play automatic time | X | X |
| Reset the day / return to ground / replay | B | B |
| Guided Tour / Free Roam menu | Y | M |
| Teleport in Free Roam | Either trigger, aim and release | Requires XR input simulation |
| Snap turn | Right stick | Requires XR input simulation |

Time moves gently on its own. The left stick directly moves time, including while automatic playback is paused. Opening the menu suspends time and narration; resuming retains the view, time and pause state. Free Roam restores the existing locomotion controls and suspends the demonstration.

## Research and display conventions

`EarthOrbitModel` uses the inherited **BergerSol.CalculateOrbitalParameters** at reference year **2000** and **KeplerianSolver.keplerian_inverse**. The semi-major axis is **1.00000261 AU**, and the prescribed sidereal period is **365.256363 days**, matching the inherited `Program.cs` example. The Sun remains at a focus, with Earth's computed eccentricity approximately **0.01670366**. Controls change time, never these parameters.

`EarthObservationModel` connects that orbit to a geometric observer at **35 degrees north**. It derives the axial frame from inherited `generate_rot.cs`, accounting for the transpose in the original MATLAB source and the longitude conversion already present in the C# Berger port. Calculations use doubles until converted to Unity coordinates. The full observer clock continues across orbital wraps, so daily rotation does not jump after a year.

The opening begins at **14:00 apparent solar time**, with orbital phase zero at perihelion. Spin uses the explicit idealized mean-solar-day relation `1 + 1 / PeriodDays` sidereal turns per simulation day. This is a teaching reference, not a real town, civil clock, calendar date or live ephemeris. The original research scripts remain unchanged. The source is [Kostadinov and Gilb, Earth Orbit v2.1](https://gmd.copernicus.org/articles/7/1051/2014/).

The ground Sun, globe lighting and observation marker share the same calculated state. A rigid presentation rotation gives a view of Earth's northern hemisphere so the dot stays visible throughout the day; it changes the viewing angle only. Orbital distances are compressed, bodies and the marker enlarged independently, and sky colours approximate atmospheric appearance. Refraction, weather and quantitative irradiance are not simulated. The Earth texture's geographic longitude does not identify a real observation site.

## Reused content and next chapters

The prototype reuses the inherited Earth mesh and texture, star skybox, trees, robot, XR Origin, input actions and locomotion setup. The Earth surface has a lit material so the day/night boundary is visible. The star artwork retains its existing attribution in `Assets/Real Stars Skybox/Documentation.txt`. The three short narration clips are temporary robot voice lines generated with the installed macOS Samantha voice.

The intended journey is **Earth's day -> Earth and seasons -> other planets and orbits -> habitability -> the changing Sun**. Each new view should answer a question raised by the previous interaction. Larger chapters can introduce and reuse the inherited models without sending learners between a museum and hub for the opening.

**This build implements the day/night observation and Earth reveal only.** Seasons is previewed at the end. Constellation selection, seasons controls, other planets and the red giant chapter are not connected yet. Their existing assets and models remain available for later work.

Runtime scripts remain Inspector-configured MonoBehaviours under `Assets/Scripts`: `ObservatoryTour` owns the two views and narration, `EarthObservationModel` owns the shared clock and observer frame, and `ViewingRoomLessonInput` reads controls. The prior room and `ViewingRoomLesson` remain usable for comparison.

The inherited project descends from [KellyLFrear/FALL2025-VR-Orbit-Simulation](https://github.com/KellyLFrear/FALL2025-VR-Orbit-Simulation). Imported content retains its original ownership. Preserve metadata, serialized references and the tracked rig when extending the prototype.
