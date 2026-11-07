# Rocket transition prototype

The Earth reveal's right grip / V action now runs a cockpit ascent on every switch from Earth to space. Returning to Earth retains the short fade. The existing HubStation, live California monitor and scientific calculations remain the endpoint.

## Try it

Open Arrival or Observatory in Unity 6000.5.10f1. Follow the introduction, press A / Enter to reveal Earth, then press right grip / V to launch. Look around the cabin and through its open 3D window. The ascent takes 24 seconds, followed by the hatch opening and a short automatic move to the observation ledge. The rocket stays parked behind the learner.

- X: pause or resume the ascent or guided exit. This is separate from the lesson's pause setting.
- A / Enter: skip the ascent and start the normal hatch exit.
- B: cancel the trip and return to the ground Earth reveal.
- Time scrubbing and Sun-path/terrain controls are gated during flight.
- Another right grip / V after returning from space starts a fresh ascent.

The altitude dashboard shows metres below 1 km, kilometres above that, a vertical metric scale and the approximate atmosphere layer. Buildings, clouds, an aircraft and a curved Earth provide visual height cues. A quiet, generated cabin engine sound pauses with the ascent. Head tracking stays active; joystick movement, teleporting and snap turning remain disabled during the trip and station observation.

## Implementation

`ObservatoryTour.SwapPerspective` owns entry, fades and handoff. It adds an `ObservatoryRocketAscent` at runtime when the scene does not have one, so the prototype requires no edits to the shared Observatory scene. The component builds and reuses `ObservatoryRocketCabin`, with original primitive scenery and existing included materials/font. No packages or downloaded models are required.

The cockpit is placed once at the initial tracked eye position with a level yaw orientation. The ascent never animates the tracked camera or XR Origin. Exterior scenery changes around the stable cockpit, with actual stereo rendering and head parallax through the window. The existing monocular sky monitor is not used as the rocket window.

The flight starts from a private copy of the live Observatory sky. Overhead blue darkens faster than the lower sky, and the atmosphere fades to black at 100 km. The aligned HubStation nebula fades in after the crossing. The flight Earth uses the Observatory globe texture and material on a refined copy of its mesh. The mesh stays disabled through 100 km. Its close horizon then enters the bottom of the window, and the globe recedes as the indicated altitude increases. The framed view is staged for the lesson. The distance-to-radius ratio follows the displayed altitude.

`ObservatoryRocketArrival` then places the parked `Resources/Observatory/Rocket Ship` prefab at the captured launch heading. Its front hatch opens for 0.85 seconds before the XR Origin moves forward 2.4 metres over 3.6 seconds. This is automatic, level translation with no forced rotation or bob; the tracked camera's local pose remains owned by tracking. The station, Sun/Earth display, sky monitor, captions and clock are positioned for the endpoint. Looking around during ascent does not turn the dock to match the user's head.

The arrival retains its exact owned translation after reaching the ledge. Returning to Earth, cancellation, lesson completion or teardown removes that offset under the view transition and hides the ship. Per-frame deltas preserve unrelated tracking/recentering changes. The same guided locomotion lock stays active throughout; no walking input or teleport destinations are introduced.

One educational `AltitudeMetres` value drives scenery, sky colour, layer labels and the metric indicator. NASA's approximate thresholds in this range are 12 km for the stratosphere, 50 km for the mesosphere and 80 km for the thermosphere. The conventional 100 km Kármán line falls inside the thermosphere. The cabin continues to 600 km before the guided arrival. The science lesson's elapsed days and pause flag stay fixed during the journey.

The prototype temporarily isolates its rendering on layer 30 (unused in the existing scene), then restores camera mask, clear flags, background colour, far clip and the local Skybox material and enabled state before the space display saves its ground baseline. Auxiliary monitor cameras are paused during flight. Path and terrain settings are captured, suppressed and restored on normal completion or B cancellation. Disabling the whole lesson keeps its existing behaviour of restoring scenery and hiding the path.

The existing Sun material's included unlit shader is reused with solid tinted material copies. No global lighting settings are changed. Cabin geometry has no retained colliders, and repeated launches reuse the same hierarchy. Owned materials, mesh copies and generated audio are released when the component is destroyed.

## Research and comfort

- NASA, [Earth’s Atmosphere: A Multi-layered Cake](https://science.nasa.gov/earth/earth-atmosphere/earths-atmosphere-a-multi-layered-cake/): approximate layer boundaries and the conventional 100 km Kármán line.
- Meta, [Locomotion comfort and usability](https://developers.meta.com/vr/design/locomotion-comfort-usability/): predictable movement, visual references and reducing uncomfortable acceleration/optic flow.

The cabin has no scripted head rotation, roll or shake. Comfortable movement and text readability still require physical Quest testing with multiple users.

## Validation

The combined ascent and arrival were checked on November 3, 2026 in Unity 6000.5.10f1 with Meta XR Simulator:

- 330 Play Mode assertions passed across normal completion, B cancellation, all three path/terrain states and both lesson pause states.
- Real Input System keyboard events checked V launch, X pause/resume, B cancellation/return, held-button handling and blocked lesson controls during flight.
- Late cancellation and coordinator disable during the final fade both returned to Earth. Disabling/re-enabling the lesson, completing it and replaying it left no stale flight state.
- Camera settings, tracked pose, scientific time, lesson pause state, monitor visibility and scenery renderer states restored. Zero, seated and standing simulated camera heights were checked.
- HUD text fit its bounds, the metric marker stayed on its line, and the cabin added no retained colliders or cameras.
- 127 arrival assertions passed, including the default 3.6-second walk, native mesh clearance at eye and torso height, a seated pose, head turn and lean, X pause/resume, B cancellation at the hatch and mid-walk, and cleanup after disable.
- Three complete return trips restored the original XR Origin position with no cumulative drift. The parked ship and station stayed fixed as the rig moved. All user locomotion providers and teleport interactors stayed disabled during the flight, walk and station observation.
- The launch eye pose held the berth and California camera steady through head turns and leans. The Sun, Earth, sky monitor, captions and clock retained their intended framing from the ledge.
- Live Editor captures show the ascent at 250 m, 10 km and 85 km, plus forward and look-back views from the ledge.

The Android IL2CPP ARM64 build succeeded from the local Editor checkout in 7 minutes 29 seconds, with 0 errors and 27 warnings (the same count as the ascent-only build). The 101 MiB APK passed archive and Android signature checks, and the build report includes the parked rocket prefab. The Editor emits its existing graphics API environment-cache diagnostic during image captures; no gameplay exceptions were recorded in the live acceptance runs. Physical Quest comfort, binocular readability, controller usability and device performance remain unverified.

Test captures, the validation source and logs are stored outside the repository under `Documents/OrbitBackups/RocketTransition-20261102/Evidence`. The run retained the user's existing local scene/settings edits; those edits are outside the prototype commit.

The November 4 window update passed 29 additional Unity Play Mode checks, including the shared Earth mesh and texture, matching cabin and station skies, and camera/monitor restoration after cancellation and return. Editor renders cover 35, 85 and 100 km. The check used a shortened flight to exercise the handoff and captured the exterior at exact altitudes. These results cover the updated Editor prototype, with no new APK or headset test.

The November 5 atmosphere fix passed 47 Unity Play Mode checks. Exact altitude captures checked the blue-to-black gradient, layer labels, and Earth visibility at the 100 km crossing. A separate, unpaused 24-second ride reached 600 km and completed the parked-rocket handoff. Cancel and return restored the original camera, sky and monitors.
