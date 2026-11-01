# Viewing Room

A Unity VR observatory for the Orbit capstone. Begin in a calm, enclosed entrance on Earth, then connect the sky overhead to Earth's rotation. Quest 3 standalone is the first target.

Open with **Unity 6000.5.10f1** and Android Build Support, SDK/NDK and OpenJDK. Open **`Assets/Scenes/Arrival.unity`** and enter Play Mode. The project uses URP 17.5, XR Interaction Toolkit 3.5.1 and OpenXR 1.17.1. Arrival is the first build scene, followed by Observatory, Seasons, OrbitalModel, S1View, S2View and S3View; the previous `SampleScene.unity` remains available but disabled in the build. Editor flow and scientific checks pass. The new scene connections still need physical Quest headset testing.

## Guided experience

The robot greets the learner with “Welcome, explorer! Let’s explore a winter day in California,” then introduces the planned journey: **Earth's day → seasons → other planets → the Sun's future**. It greets once, then rests with both arms lowered. The fixed welcome panel sits below its head so the face and upper body remain visible. The entrance has one action: **A to enter the observatory**. Controls are introduced as needed.

The transition opens the day observation. This scene has one guided path, with no mode menu or Free Roam option. The physical guide remains in the entrance; the original 2D robot face accompanies prompts in the observatory. Watch the Sun move, daylight fade and stars appear. Move time forwards or backwards, then press A to reveal a **square, starry Sun/Earth display low-right from the starting viewpoint**. Its frame stays fixed in the world as the learner looks around. A slim red triangle points to California, with its tip anchored exactly to the surface: Earth's rotation carries it into sunlight and back into night. Earth naturally hides the pointer when it is on the far side.

During the Earth reveal, **squeeze the right grip to swap sky and space**. The space view reuses the inherited OrbitalModel HubStation floor, rails and lamp. The learner stands near the forward edge of the station, with the original textured Sun ahead-left and a closer, larger Earth on the right. The inherited red nebula skybox surrounds the station. Only these two bodies are shown, with no orbit line or seasonal labels. Station lighting stays separate from California's daylight. California's matching sky appears on a smaller, tilted monitor down-left, leaving the main view clear. Earth's small orbital progress follows the same elapsed days as its rotation. Squeeze again to return to the observatory with Earth in the canvas. A short fade covers the change; the tracked camera stays in place. Both views retain the same time and pause state. B first leaves space; another B returns from the Earth reveal to the sky lesson. A continues the guided route from either viewpoint.

Slim rear arches, a low terrace, hills and inherited nature props leave the outdoor sky clear. Prompts and the clock remain fixed in the world. Interaction is optional, with no quiz or score.

| Action | Quest controller | Editor keyboard |
| --- | --- | --- |
| Enter / reveal Earth / continue | A | Enter |
| Move time forwards/backwards | Left stick right/left | Right/left arrows |
| Pause / play automatic time | X | X |
| Swap space / observatory during the Earth reveal | Right grip | V |
| Show / hide the daily Sun path | Y | P |
| Hide / restore terrain while the path is visible | Left grip | H |
| Reset the day / return to sky / replay | B | B |
| Snap turn | Right stick | XR input simulation |

Time moves gently on its own. The left stick moves time even while playback is paused. Y toggles the Sun path during observation; M does not open a menu. Travel stays paused during the guided chapter and its ending; A continues into Seasons and B replays the chapter. Tracked head movement and guided snap turning remain available. Opening Observatory directly in the Editor retains its introductory flow for development.

## Sun path

The Sun path starts hidden. Press Y (P in the Editor) to show a gold daily arc and a pale horizon guide. It uses the current California latitude and solar declination from `EarthObservationModel`, so the moving Sun stays on the arc as time changes. This is a full daily circle at the current declination, rather than a trail of previous positions. It follows the geometry in Kostadinov's [SolGeoVis v1.1, Section 2.2 and Figure 2](https://doi.org/10.5281/zenodo.16998081), also checked against the published `sun_path_v2.m`.

Terrain stays visible when the path is enabled. Press left grip (H) to hide the landscape, deck and surrounding structures and reveal the dashed blue continuation below the mathematical horizon. The lower sky becomes a dark diagram view; its Sun and path do not represent a view through the physical Earth. A small standing reference remains visible. Only renderers are hidden: collisions, head tracking and locomotion settings are preserved. Press again to restore the exact previous renderer states. Hiding the path, ending the lesson or disabling the feature also restores terrain.

The same path can appear in California's monitor during the space view. It never draws over the decorative space sky. Path and terrain controls preserve the simulation time and pause state, and work while paused. See [SUN_PATH.md](SUN_PATH.md) for the calculation and validation notes.

## Research and scope

The inherited Berger orbital calculation and Kepler solver use reference year **2000**, semi-major axis **1.00000261 AU** and sidereal period **365.256363 days**. The Sun remains at a focus; controls change time rather than orbital parameters. The California observer is Los Angeles, **34.052235 degrees latitude, -118.243683 degrees east-positive longitude**. The opening is **14:00 apparent solar time** at perihelion in the reference year 2000, during **Northern Hemisphere winter**. It is a simulated teaching day, rather than the current calendar date or a local civil clock. Daylight follows the computed Sun altitude; no fixed local sunrise/sunset schedule is asserted. The original research scripts remain unchanged; see [Kostadinov and Gilb, Earth Orbit v2.1](https://gmd.copernicus.org/articles/7/1051/2014/).

The globe, marker and exterior sky share one clock. The station view changes the display scale to 70 metres per AU, while keeping the research orbit, winter phase, axial tilt and rotation. It uses the inherited OrbitalModel Sun's visual radius and views more of Earth's sunlit hemisphere. As the viewpoint moves farther around Earth, the globe is enlarged proportionally to preserve its apparent size for inspection. Both bodies remain visible. Body sizes are enlarged; this is not a physically scaled solar system. Returning restores the compact model exactly. The local sky keeps updating in a separate camera; it is not a prerecorded image. California's stars use the research orientation; the inherited red nebula around the station is decorative artwork. A display copy calibrates Earth's geographic UVs while preserving the imported mesh and texture. Rotation remains west to east; the globe shader lights the hemisphere facing the model Sun, and Earth naturally hides the California pointer on the far side. Ground brightness is artistic. Weather, refraction and physical lux are not simulated.

The station provides a familiar environment for a later seasons chapter. The current day lesson has no seasonal controls; A still continues to the inherited Seasons preview.

The prototype reuses the inherited Earth, star skybox, robot, nature props, XR Origin and locomotion setup. Star attribution remains in `Assets/Real Stars Skybox/Demo Scene/Documentation.txt`. Prerecorded narration plays locally.

**The connected route covers Arrival, day/night observation, the Earth reveal, the inherited Seasons preview and OrbitalModel (HubStation).** At observatory completion, release A and press it again to enter Seasons. Its first fresh A press opens the second tablet; its next fresh A press continues to HubStation. The station habitable-zone task opens S1View, then continues through the inherited S2View and S3View scenes before returning to HubStation.

Arrival and Observatory center the tracked view on their spawn point once the headset is ready. This moves the XR Origin, preserves headset height and local tracking, and leaves later head movement and locomotion free.

The imported scenes reuse the compatible XR rig and have materials adapted for URP. The HubStation floor model explicitly uses its textured floor and trim materials. The inherited Sun uses its original surface texture with a self-lit URP material so it stays bright independently of scene lights. Their inherited season timeline and illustrative scenery are retained; they are not yet reconciled with the observatory reference clock or validated as a SolGeoVis implementation. Seasonal Sun-path comparisons, selectable locations, a refined seasons lesson, constellation selection and the changing-Sun chapter remain future work. The inherited project descends from [KellyLFrear/FALL2025-VR-Orbit-Simulation](https://github.com/KellyLFrear/FALL2025-VR-Orbit-Simulation); imported content retains its original ownership.
