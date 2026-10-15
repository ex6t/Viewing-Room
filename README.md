# Viewing Room

A Unity VR observatory for the Orbit capstone. Begin in a calm, enclosed entrance on Earth, then connect the sky overhead to Earth's rotation. Quest 3 standalone is the first target.

Open with **Unity 6000.5.10f1** and Android Build Support, SDK/NDK and OpenJDK. Open **`Assets/Scenes/Arrival.unity`** and enter Play Mode. The project uses URP 17.5, XR Interaction Toolkit 3.5.1 and OpenXR 1.17.1. Arrival is the first build scene, followed by Observatory, Seasons, OrbitalModel, S1View, S2View and S3View; the previous `SampleScene.unity` remains available but disabled in the build. Editor flow and scientific checks pass. The new scene connections still need physical Quest headset testing.

## Guided experience

The robot greets the learner with “Welcome, explorer! Let’s explore a winter day in California,” then introduces the planned journey: **Earth's day → seasons → other planets → the Sun's future**. It greets once, then rests with both arms lowered. The fixed welcome panel sits below its head so the face and upper body remain visible. The entrance has one action: **A to enter the observatory**. Controls are introduced as needed.

The transition opens the day observation. This scene has one guided path, with no mode menu or Free Roam option. The physical guide remains in the entrance; the original 2D robot face accompanies prompts in the observatory. Watch the Sun move, daylight fade and stars appear. Move time forwards or backwards, then press A to reveal a **square, starry Sun/Earth display low-right from the starting viewpoint**. Its frame stays fixed in the world as the learner looks around. The exterior sky remains visible, and both views show **the same moment**. The cyan dot marks the California observer: Earth's rotation carries it into sunlight and back into night. B returns to the sky without changing time or the learner's tracked pose.

Slim rear arches, a low terrace, hills and inherited nature props leave the outdoor sky clear. Prompts and the clock remain fixed in the world. Interaction is optional, with no quiz or score.

| Action | Quest controller | Editor keyboard |
| --- | --- | --- |
| Enter / reveal Earth / continue | A | Enter |
| Move time forwards/backwards | Left stick right/left | Right/left arrows |
| Pause / play automatic time | X | X |
| Reset the day / return to sky / replay | B | B |
| Snap turn | Right stick | XR input simulation |

Time moves gently on its own. The left stick moves time even while playback is paused. Y and M do not open a menu. Travel stays paused during the guided chapter and its ending; A continues into Seasons and B replays the chapter. Tracked head movement and guided snap turning remain available. Opening Observatory directly in the Editor retains its introductory flow for development.

## Research and scope

The inherited Berger orbital calculation and Kepler solver use reference year **2000**, semi-major axis **1.00000261 AU** and sidereal period **365.256363 days**. The Sun remains at a focus; controls change time rather than orbital parameters. The California observer is Los Angeles, **34.052235 degrees latitude, -118.243683 degrees east-positive longitude**. The opening is **14:00 apparent solar time** at perihelion in the reference year 2000, during **Northern Hemisphere winter**. It is a simulated teaching day, rather than the current calendar date or a local civil clock. Daylight follows the computed Sun altitude; no fixed local sunrise/sunset schedule is asserted. The original research scripts remain unchanged; see [Kostadinov and Gilb, Earth Orbit v2.1](https://gmd.copernicus.org/articles/7/1051/2014/).

The globe, marker and exterior sky share one clock. A display copy calibrates Earth's geographic UVs while preserving the imported mesh and texture. Rotation remains west to east; the globe shader lights the hemisphere facing the model Sun. Distances and body sizes are adjusted for teaching, and ground brightness is artistic. Weather, refraction and physical lux are not simulated.

The prototype reuses the inherited Earth, star skybox, robot, nature props, XR Origin and locomotion setup. Star attribution remains in `Assets/Real Stars Skybox/Demo Scene/Documentation.txt`. Prerecorded narration plays locally.

**The connected route covers Arrival, day/night observation, the Earth reveal, the inherited Seasons preview and OrbitalModel (HubStation).** At observatory completion, release A and press it again to enter Seasons. Its first fresh A press opens the second tablet; its next fresh A press continues to HubStation. The station habitable-zone task opens S1View, then continues through the inherited S2View and S3View scenes before returning to HubStation.

Arrival and Observatory center the tracked view on their spawn point once the headset is ready. This moves the XR Origin, preserves headset height and local tracking, and leaves later head movement and locomotion free.

The imported scenes reuse the compatible XR rig and have materials adapted for URP. The HubStation floor model explicitly uses its textured floor and trim materials. The inherited Sun uses its original surface texture with a self-lit URP material so it stays bright independently of scene lights. Their inherited season timeline and illustrative scenery are retained; they are not yet reconciled with the observatory reference clock or validated as a SolGeoVis implementation. Sun-path comparisons, selectable locations, a refined seasons lesson, constellation selection and the changing-Sun chapter remain future work. The inherited project descends from [KellyLFrear/FALL2025-VR-Orbit-Simulation](https://github.com/KellyLFrear/FALL2025-VR-Orbit-Simulation); imported content retains its original ownership.
