//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Animator whose speed is set based on a linear mapping
//
//=============================================================================

using UnityEngine;

namespace OrbitSimulation
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "Valve.VR.InteractionSystem", "Assembly-CSharp", "PlanetHandler")]
    public class PlanetHandler : MonoBehaviour
    {
        public LinearMapping linearMapping;
        public GameObject masterControl;
        SimulationController simuControl;

        private float currentLinearMapping = float.NaN;

        void Awake()
        {
            if (simuControl == null)
            {
                simuControl = masterControl.GetComponent<SimulationController>();
            }

            if (linearMapping == null)
            {
                linearMapping = GetComponent<LinearMapping>();
            }

            if (linearMapping != null && linearMapping.value <= 0f)
            {
                linearMapping.value = 0.5f;
            }

            simuControl.simulationSpeed = 1f;
        }

        void Update()
        {
            if (linearMapping == null || simuControl == null)
                return;

            if (currentLinearMapping != linearMapping.value)
            {
                currentLinearMapping = linearMapping.value;

                if (currentLinearMapping != 0)
                {
                    simuControl.simulationSpeed = Mathf.Pow(500f, linearMapping.value - 0.5f);
                }

                else
                {
                    simuControl.simulationSpeed = 0;
                }
            }
        }
    }
}
