using UnityEngine;

namespace OrbitSimulation
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Valve.VR.InteractionSystem.Sample", "Assembly-CSharp", "PodiumController")]
    public class PodiumController : MonoBehaviour
    {
        public GameObject masterControl;

        private SimulationController simuControl;

        private AudioSource lecturer;

        private bool lectureOn = false;
        private bool[] podiums = new bool[10];

        public float cooldown = 0f;

        private float speedStorage;

        public GameObject focusModel;
        public GameObject orbitTrail;

        public GameObject keplerCamera;
        public AudioClip kepler1VO;
        public AudioClip kepler2VO;
        public AudioClip kepler3VO;
        public GameObject closeEarth;

        public GameObject earthViewCam;

        [Header("Hub Movement")]
        [Tooltip("Station mover used by the podium's travel button.")]

        public HubStationMover hubMover;
        [Tooltip("Retained Earth-view target setting from the inherited project.")]
        public int hubTargetIndexForEarth = 0;

        public AudioClip eccentricityVO;
        public AudioClip obliquityVO;
        public AudioClip perihelionVO;

        private void Start()
        {
            simuControl = masterControl.GetComponent<SimulationController>();
            lecturer = GetComponent<AudioSource>();

            if (hubMover == null)
            {
                hubMover = FindObjectOfType<HubStationMover>();
            }
        }

        public void ToggleHubMover()
        {
            if (cooldown < Time.time)
            {
                cooldown = Time.time + 1f;
                if (hubMover == null)
                {
                    hubMover = FindObjectOfType<HubStationMover>();
                    if (hubMover == null)
                    {
                        Debug.LogWarning("[PodiumController] No HubStationMover found in scene.");
                        return;
                    }
                }

                // Each press moves the station to the next configured viewpoint.
                hubMover.MoveToNext();
            }
        }

        public void ToggleKepler1()
        {
            if (cooldown < Time.time)
            {
                cooldown = Time.time + 1f;
                if (!lectureOn)
                {
                    speedStorage = simuControl.simulationSpeed;
                    simuControl.simulationSpeed = 30f;

                    lecturer.clip = kepler1VO;
                    lecturer.Play();
                    keplerCamera.SetActive(true);
                    focusModel.SetActive(true);
                    orbitTrail.SetActive(true);
                    lectureOn = true;
                    podiums[1] = true;
                }

                else if (podiums[1])
                {
                    simuControl.simulationSpeed = speedStorage;
                    focusModel.SetActive(false);

                    lecturer.Stop();
                    keplerCamera.SetActive(false);
                    orbitTrail.SetActive(false);
                    lectureOn = false;
                    podiums[1] = false;
                }
            }
        }
        public void ToggleKepler2()
        {
            if (cooldown < Time.time)
            {
                cooldown = Time.time + 1f;
                if (!lectureOn)
                {
                    speedStorage = simuControl.simulationSpeed;
                    simuControl.simulationSpeed = 30f;

                    lecturer.clip = kepler2VO;
                    lecturer.Play();
                    keplerCamera.SetActive(true);
                    orbitTrail.SetActive(true);
                    lectureOn = true;
                    podiums[2] = true;
                }

                else if (podiums[2])
                {
                    simuControl.simulationSpeed = speedStorage;

                    lecturer.Stop();
                    keplerCamera.SetActive(false);
                    orbitTrail.SetActive(false);
                    lectureOn = false;
                    podiums[2] = false;
                }
            }
        }
        public void ToggleKepler3()
        {
            if (cooldown < Time.time)
            {
                cooldown = Time.time + 1f;
                if (!lectureOn)
                {
                    speedStorage = simuControl.simulationSpeed;
                    simuControl.simulationSpeed = 30f;

                    lecturer.clip = kepler3VO;
                    lecturer.Play();
                    closeEarth.SetActive(true);
                    keplerCamera.SetActive(true);
                    lectureOn = true;
                    podiums[3] = true;
                }

                else if (podiums[3])
                {
                    simuControl.simulationSpeed = speedStorage;

                    lecturer.Stop();
                    closeEarth.SetActive(false);
                    keplerCamera.SetActive(false);
                    lectureOn = false;
                    podiums[3] = false;
                }
            }
        }

        public void DefineEccentricity()
        {
            if (cooldown < Time.time)
            {
                cooldown = Time.time + 1f;
                lecturer.clip = eccentricityVO;
                lecturer.Play();
            }
        }
        public void DefineObliquity()
        {
            if (cooldown < Time.time)
            {
                cooldown = Time.time + 1f;
                lecturer.clip = obliquityVO;
                lecturer.Play();
            }
        }
        public void DefinePerihelion()
        {
            if (cooldown < Time.time)
            {
                cooldown = Time.time + 1f;
                lecturer.clip = perihelionVO;
                lecturer.Play();
            }
        }
    }
}
