using Netick;
using Unary.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Unary.Recusant
{
    public class PlayerExploder : NetworkBehaviourExtended
    {
        [Networked]
        public NetworkBool Exploding { get; set; } = false;

        [SerializeField]
        private PrefabRef<DecalProjector> ExplosionDecalPrefab = default;

        [SerializeField]
        private AssetRef<GameObject> ExplosionVisualPrefab = default;

        public GameObject Head;

        public float ExplosionRadius = 4.0f;
        public LayerMask Layers;

        private LocalPrefabPool _explosionDecalPool;
        private LocalPrefabPool _explosionVisualPool;

        private Ray _ray = new();
        private readonly RaycastHit[] _hits = new RaycastHit[20];
        private Vector3 _position = new();

        public override void NetworkStart()
        {
            _explosionDecalPool = PoolManager.Instance.GetLocalPool(ExplosionDecalPrefab);
            _explosionVisualPool = PoolManager.Instance.GetLocalPool(ExplosionVisualPrefab);
        }

        public override void NetworkFixedUpdate()
        {
            bool shouldExplode = false;

            if (FetchInputServer(out PlayerNetworkInput serverInput))
            {
                Exploding = serverInput.Exploding;
                shouldExplode = Exploding;
            }
            else if (IsClient)
            {
                if (IsInputSource)
                {
                    if (GetInput(out PlayerNetworkInput clientInput))
                    {
                        shouldExplode = clientInput.Exploding;
                    }
                }
                else
                {
                    shouldExplode = Exploding;
                }
            }

            if (!shouldExplode)
            {
                return;
            }

            _ray.origin = Head.transform.position;
            _ray.direction = Head.transform.forward * 100.0f;

            if (Physics.RaycastNonAlloc(_ray, _hits) == 0)
            {
                return;
            }

            _position = _hits[0].point + _hits[0].normal;

            GameObject visual = _explosionVisualPool.GetOldest();
            visual.transform.position = _position;
            visual.SetActive(true);

            ParticleSystem system = visual.GetComponent<ParticleSystem>();
            system.Clear(true);
            system.Play(true);

            visual.GetComponent<AudioSource>().Play();

            PlaceExplosionDecals(_position);
        }

        public override void NetworkUpdate()
        {
            if (IsInputSource && GetInput(out PlayerNetworkInput input))
            {
                input.Exploding |= Input.GetMouseButtonDown(1);

                SetInput(input);
            }
        }

        void PlaceExplosionDecals(Vector3 explosionCenter)
        {
            // Cast multiple rays in a sphere around the explosion for coverage
            int rayCount = 20; // Number of rays for placement
            for (int i = 0; i < rayCount; i++)
            {
                // Generate random direction within sphere
                Vector3 direction = Random.onUnitSphere;
                Ray ray = new Ray(explosionCenter, direction);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, ExplosionRadius, Layers))
                {
                    InstantiateAdjustedDecal(hit.point, hit.normal);
                }
            }
        }

        void InstantiateAdjustedDecal(Vector3 position, Vector3 normal)
        {
            // Calculate slope angle
            float slopeAngle = Vector3.Angle(normal, Vector3.up);

            // Instantiate the decal
            GameObject decal = _explosionDecalPool.GetOldest();
            decal.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-normal));

            // Adjust scale based on slope to prevent distortion
            DecalProjector projector = decal.GetComponent<DecalProjector>();
            if (projector != null)
            {

                Vector3 baseSize = new Vector3(4f, 4f, 0.2f); // Default size (X/Y for width/height, Z for depth)

                if (slopeAngle >= 85f && slopeAngle <= 95f) // ~90° (vertical wall)
                {
                    // Standard: No scaling needed for vertical
                    projector.size = baseSize;
                }
                else if (slopeAngle >= 40f && slopeAngle <= 50f) // ~45°
                {
                    // Slightly elongate Y to fit the slope
                    projector.size = new Vector3(baseSize.x, baseSize.y * 1.2f, baseSize.z);
                }
                else if (slopeAngle >= 25f && slopeAngle <= 35f) // ~30°
                {
                    // More elongation for gentler slope
                    projector.size = new Vector3(baseSize.x, baseSize.y * 1.4f, baseSize.z);
                }
                else
                {
                    // Interpolate for other angles (linear scaling)
                    float scaleFactor = Mathf.Lerp(1.4f, 1.0f, slopeAngle / 90f);
                    projector.size = new Vector3(baseSize.x, baseSize.y * scaleFactor, baseSize.z);
                }

            }

            // Optional: Add fading/destruction after time
            //Destroy(decal, 10f); // Remove after 10 seconds]

            decal.SetActive(true);
        }
    }
}
