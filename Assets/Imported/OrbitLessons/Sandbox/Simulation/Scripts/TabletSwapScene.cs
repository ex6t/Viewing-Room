using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class TabletSwapScene : MonoBehaviour
{

    [SerializeField] private PlanetNavigator PlanetNavigator; // reference to the PlanetNavigator script

    // FUNCTION TO LOAD SCENES
    public void LadScenebyName(string name)
    {
        SceneManager.LoadScene(name); // load scene by name
    }

    // FUNCTION TO GO TO THE CLOSEUP CAMERAS
    public void CloeupCameraScene()
    {
       PlanetNavigator.StartPlanetTask(); // call the static function from PlanetNavigator to start the closeup camera scene
    }
}

