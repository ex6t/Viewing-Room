# Observatory Sun path

## Research basis

T. S. Kostadinov (2025), *SolGeoVis v1.1: Solar Position Visualizations and Scientific Computing for Astronomy and Earth Science Education*, Section 2.2, Figure 2 (pages 6-8), and Equation 4 (page 10). Manuscript and MATLAB source: https://doi.org/10.5281/zenodo.16998081. The published `sun_path_v2.m` was inspected alongside the attached manuscript. The record identifies the release as not journal accepted and lists CC BY-NC-SA 4.0 for its source material. No MATLAB source or figures are copied into the game.

The paper plots a daily circle for a fixed latitude and solar declination. Our existing `EarthObservationModel` supplies the Los Angeles latitude and current solar declination from the inherited Berger, Kepler and rotation code. Those research files are unchanged. The sky shader draws that circle directly in the observer's east/up/north frame:

```
pole = (0, sin(latitude), cos(latitude))
asin(dot(skyDirection, pole)) = solarDeclination
```

This is the same small circle described by the altitude relation in Equation 4. It places the calculated Sun on the path without sampling or advancing the scene clock. The circle updates with the model's current declination as time moves. It is a daily reference circle, not an exact time-integrated 24-hour ephemeris with changing declination. The lesson still uses its reference-year 2000 perihelion day and apparent solar time. Civil time, refraction, terrain-dependent sunrise and seasonal comparisons are outside this feature.

## Scene and controls

`Assets/Scenes/Observatory.unity` has an `ObservatorySunPath` on Observation Tour, explicit scenery renderer references, and a small standing reference. `ObservatoryTour` owns availability and captions. `ViewingRoomLessonInput` owns fresh-press controller/keyboard input. The existing Observatory sky shader owns rendering, including stereo eye setup. No packages or research calculations are changed.

- Y / P: show or hide the path (starts hidden).
- Left grip / H: hide or restore scenery while the path is shown.
- Gold solid arc: above the mathematical horizon.
- Blue dashed arc: below the horizon, shown only in diagram view.
- Pale line: mathematical horizon through the observer.

Terrain stays on by default to retain the everyday view of sunrise and sunset. Diagram view keeps a small standing reference and all original colliders. Hiding the path, completing the lesson or disabling the component restores prior scenery visibility. The space scene remains intact; its monitor can show the same California path. Controls do not change time or pause state.

## Validation

Checked on October 27, 2026 in Unity 6000.5.10f1:

- 360 scripted Play Mode checks passed: default visibility, independent terrain control, time/pause/pose/collider preservation, exact restoration, repeated toggles, component disable/reenable, 12 model dates and 75 comparisons with the published altitude/azimuth equations.
- P/H keyboard events and simulated Quest Y/left grip events passed, including held-button handling. Text bounds were checked after enlarging the hint area.
- Editor captures show the upper arc, horizon and dashed lower arc. Controller meshes were temporarily hidden only for these captures, then restored.
- The Earth/space swap kept the same time and pause state; the California monitor showed the path while the space sky stayed unchanged. Completing the lesson restored terrain and hid the path.
- Original research scripts are byte-identical, all original scene object IDs remain, and pre-existing user scene/settings changes are excluded from the feature commit.
- The updated sky shader compiled for Android GLES3 and Vulkan. The full seven-scene Android IL2CPP ARM64 APK build succeeded with 0 errors and 27 warnings in 2 minutes 7 seconds. APK archive integrity and ARM64 native libraries were verified.

Validation used the local scene with the user's existing uncommitted edits retained; those edits are excluded from the commit. The clean Play Mode run captured no gameplay exceptions. Unity emitted graphics API environment-cache diagnostics at first entry; imported package/deprecation warnings remain. Physical Quest 3 testing remains a separate acceptance step for binocular readability, controller mapping, comfort and device performance.
