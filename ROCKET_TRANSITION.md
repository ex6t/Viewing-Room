# Rocket transition prototype

The Earth reveal's right grip / V action now runs a cockpit ascent on every switch from Earth to space. Returning to Earth retains the short fade. The existing HubStation, live California monitor and scientific calculations remain the endpoint.

## Try it

Open Arrival or Observatory in Unity 6000.5.10f1. Follow the introduction, press A / Enter to reveal Earth, then press right grip / V to launch. Look around the cabin and through its open 3D window. The default journey takes 30 seconds.

- X: pause or resume the ascent. This is separate from the lesson's pause setting.
- B: cancel the ascent and return to the ground Earth reveal.
- A, time scrubbing and Sun-path/terrain controls are gated during flight.
- Another right grip / V after returning from space starts a fresh ascent.

The altitude dashboard shows metres below 1 km, kilometres above that, a vertical metric scale and the approximate atmosphere layer. Buildings, clouds, an aircraft and a curved Earth provide visual height cues. A quiet, generated cabin engine sound pauses with the ascent. Head tracking stays active; joystick locomotion and snap turning are paused until the flight ends.

## Implementation

`ObservatoryTour.SwapPerspective` owns entry, fades and handoff. It adds an `ObservatoryRocketAscent` at runtime when the scene does not have one, so the prototype requires no edits to the shared Observatory scene. The component builds and reuses `ObservatoryRocketCabin`, with original primitive scenery and existing included materials/font. No packages or downloaded models are required.

The cockpit is placed once at the initial tracked eye position with a level yaw orientation. The tracked camera and XR Origin are never animated by the ascent. Exterior scenery changes around the stable cockpit, with actual stereo rendering and head parallax through the window. The existing monocular sky monitor is not used as the rocket window.

One educational `AltitudeMetres` value drives scenery, sky colour, layer labels and the metric indicator. The presentation compresses distance and time; the existing enlarged Earth/Sun model is not used to measure altitude. Approximate layer thresholds are 12, 50 and 85 km, with the final marker at 100 km. Reaching that marker does not imply entering orbit. The science lesson's elapsed days and pause flag remain unchanged during the journey.

The prototype temporarily isolates its rendering on layer 30 (unused in the existing scene), then restores camera mask, clear flags, background colour, far clip and optional Skybox enabled state before the space display saves its ground baseline. Auxiliary monitor cameras are paused during flight. Path and terrain settings are captured, suppressed and restored on normal completion or B cancellation. Disabling the whole lesson keeps its existing behaviour of restoring scenery and hiding the path.

The existing Sun material's included unlit shader is reused with solid tinted material copies. No global lighting settings are changed. Cabin geometry has no retained colliders, and repeated launches reuse the same hierarchy. Owned materials, mesh copies and generated audio are released when the component is destroyed.

## Research and comfort

- NASA, [Earth's Atmospheric Layers](https://www.nasa.gov/image-article/earths-atmospheric-layers-3/): approximate atmosphere structure; real boundary heights vary.
- NASA, [Interesting Fact of the Month 2022](https://www.nasa.gov/space-science-and-astrobiology-at-ames/interesting-fact-of-the-month-current/interesting-fact-of-the-month-2022/): the conventional 100 km Karman line and the absence of a sharp physical edge.
- Meta, [Locomotion comfort and usability](https://developers.meta.com/vr/design/locomotion-comfort-usability/): predictable movement, visual references and reducing uncomfortable acceleration/optic flow.

The cabin has no scripted head rotation, roll or shake. Comfortable movement and text readability still require physical Quest testing with multiple users.

## Validation

Checked on November 3, 2026 in Unity 6000.5.10f1 with Meta XR Simulator:

- 330 Play Mode assertions passed across normal completion, B cancellation, all three path/terrain states and both lesson pause states.
- Real Input System keyboard events checked V launch, X pause/resume, B cancellation/return, held-button handling and blocked lesson controls during flight.
- Late cancellation and coordinator disable during the final fade both returned to Earth. Disabling/re-enabling the lesson, completing it and replaying it left no stale flight state.
- Camera settings, tracked pose, scientific time, lesson pause state, monitor visibility and scenery renderer states restored. Zero, seated and standing simulated camera heights were checked.
- HUD text fit its bounds, the metric marker stayed on its line, and the cabin added no retained colliders or cameras.
- A default-duration live ascent was captured at 250 m, 10 km and 85 km, followed by the existing space endpoint. Captures show the revised building faces, aircraft, ocean, land and blue horizon rim.

The full seven-scene Android IL2CPP ARM64 APK build succeeded with 0 errors and 27 warnings in 2 minutes 29 seconds. APK archive integrity and ARM64 native libraries were verified. The Editor still emits its existing graphics API environment-cache diagnostic during image captures; no gameplay exceptions were recorded. Physical Quest comfort, binocular readability, controller usability and device performance remain unverified.

Test captures, the validation source and logs are stored outside the repository under `Documents/OrbitBackups/RocketTransition-20261102/Evidence`. The run retained the user's existing local scene/settings edits; those edits are outside the prototype commit.
