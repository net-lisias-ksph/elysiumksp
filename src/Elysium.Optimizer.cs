using KSP;
using UnityEngine;

namespace ElysiumKSP.Optimizer
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ElysiumVesselOptimizerModule : MonoBehaviour
    {
        public float targetFps = 60f;
        private float fps, dT, timer = 0f;

        void Update()
        {
            dT += (Time.deltaTime - dT) * 0.1f;
            fps = 1.0f / dT;

            timer += Time.deltaTime;
            if (timer >= 1.0f)
            {
                Optimize();
                timer = 0f;
            }
        }

        void Optimize()
        {
            if (fps < targetFps)
            {
                foreach (Vessel v in FlightGlobals.Vessels)
                {
                    if (v.parts.Count < 20) continue;
                    foreach (Part p in v.Parts)
                    {
                        if (p.Rigidbody != null)
                        {
                            p.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                            p.Rigidbody.interpolation = RigidbodyInterpolation.None;
                        }
                    }
                }

                ParticleSystem[] systems = FindObjectsOfType<ParticleSystem>();
                foreach (var ps in systems) if (ps.isPlaying) ps.maxParticles = Mathf.Max(ps.maxParticles / 2, 10);
                
                foreach (Vessel v in FlightGlobals.Vessels)
                {
                    foreach (Part p in v.Parts)
                    {
                        Renderer r = p.GetComponentInChildren<Renderer>();
                        float d = Vector3.Distance(Camera.main.transform.position, r.transform.position);
                        r.enabled = d <= 200f;
                    }
                }
            }
            timer = 0f;
        }
    }

    public class SceneOptimizer : MonoBehaviour
    {
        void FixedUpdate()
        {
            foreach (var v in FlightGlobals.Vessels)
            {
                foreach (var part in v.parts)
                {
                    if (!part.gameObject.activeInHierarchy) continue;
                    if (Vector3.Distance(part.transform.position, Camera.main.transform.position) > 1000f)
                    {
                        if (part.Rigidbody != null && part.Rigidbody.isKinematic == false) part.Rigidbody.isKinematic = false;
                    }
                    else 
                    {
                        if (part.Rigidbody != null && part.Rigidbody.isKinematic == false) part.Rigidbody.isKinematic = true;
                    }
                }
            }
        }
    }
}