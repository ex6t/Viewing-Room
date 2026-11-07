# Seasons prototype

Open `Assets/Scenes/Seasons.unity` in Unity 6000.5.10f1 and enter Play Mode for a direct test. The regular route reaches Seasons when you continue after the Observatory's day lesson.

The player starts on the inherited HubStation deck facing the open edge. A small side panel introduces the ring and its March equinox, June solstice, September equinox and December solstice markers. Aim at Earth, hold the grip and move along the ring to select a date. In the Editor, drag with the mouse, press **1–4** to jump to a marker, or use the **left/right arrows** to move one day. The selection keeps the apparent solar time the same so you can compare the height of the Sun across the year.

- **A / Enter** opens the existing Observatory at that exact simulation timestamp, with playback paused for comparison.
- In the Observatory, the **left stick / arrow keys** scrub the time and **X** resumes or pauses playback. **Y / P** displays the Sun path.
- **A / Enter** reveals Earth. **Right grip / V** runs the rocket ascent and station arrival; use it again to switch back to the ground view.
- **B** returns to Seasons with the current observation timestamp, including any time changes. If the rocket is still moving, B first cancels the trip and returns to the ground view.
- **Y / N** from Seasons continues to the inherited HubStation and its planet lesson.

The display reuses the existing Earth orbit model, Sun and Earth assets and the inherited Seasons environment. The closer orbit is tilted as a whole, and its four colored arcs are thicker so they are easier to follow from the deck. The equinox and solstice boundaries are calculated from the same Berger orientation and Kepler orbit used in the Observatory. The date labels are approximate because the teaching clock starts near early January at perihelion. The scene handoff retains the exact model time.

The Seasons and Observatory views use the same model year. The key comparison is how Earth's tilt changes the Sun's apparent height and the length of daylight. See NASA's [Seeing Equinoxes and Solstices from Space](https://science.nasa.gov/resource/seeing-equinoxes-and-solstices-from-space/) and [Earth Orbit v2.1](https://gmd.copernicus.org/articles/7/1051/2014/) for the inherited orbital research.

For a first comparison, visit the June solstice at 14:00, show the Sun path, then return and repeat with the December solstice at the same time. Look for the lower arc and lower Sun over California in December.

In Unity 6000.5.10f1, 51 Play Mode checks passed with the Meta XR Simulator. They covered mouse drag, XRI ray selection and release, the original day lesson, exact time through sky and rocket arrival, time scrubbing, both returns to Seasons, prompt readability and the route onward to HubStation. Android player scripts compiled. This run did not package an APK or test on a physical Quest.
