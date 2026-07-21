using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MimicSpace
{
    public class Mimic : MonoBehaviour
    {
        [Header("Animation")]
        public GameObject legPrefab;

        [Header("Audio")]
        public AudioClip[] legGenerationSounds;
        [Tooltip("Volume dos sons de geração das pernas")]
        [Range(0f, 1f)]
        public float legSoundVolume = 1f;

        [Range(2, 20)]
        public int numberOfLegs = 5;
        [Tooltip("The number of splines per leg")]
        [Range(1, 10)]
        public int partsPerLeg = 4;
        int maxLegs;

        public int legCount;
        public int deployedLegs;
        [Range(0, 19)]
        public int minimumAnchoredLegs = 2;
        public int minimumAnchoredParts;

        [Tooltip("Minimum duration before leg is replaced")]
        public float minLegLifetime = 5;
        [Tooltip("Maximum duration before leg is replaced")]
        public float maxLegLifetime = 15;

        public Vector3 legPlacerOrigin = Vector3.zero;
        [Tooltip("Leg placement radius offset")]
        public float newLegRadius = 3;

        public float minLegDistance = 4.5f;
        public float maxLegDistance = 6.3f;

        [Range(2, 50)]
        [Tooltip("Number of spline samples per legpart")]
        public int legResolution = 40;

        [Tooltip("Minimum lerp coeficient for leg growth smoothing")]
        public float minGrowCoef = 4.5f;
        [Tooltip("MAximum lerp coeficient for leg growth smoothing")]
        public float maxGrowCoef = 6.5f;

        [Tooltip("Minimum duration before a new leg can be placed")]
        public float newLegCooldown = 0.3f;

        bool canCreateLeg = true;

        List<GameObject> availableLegPool = new List<GameObject>();

        [Tooltip("This must be updates as the Mimin moves to assure great leg placement")]
        public Vector3 velocity;

        // ==================== DANO DE PERNAS (Sentinela) ====================
        [Header("Dano de Pernas (Sentinela)")]
        [Tooltip("Se true, as pernas causam dano ao player por proximidade")]
        [HideInInspector] public bool legsDealDamage = false;
        [HideInInspector] public int legDamageAmount = 10;
        [HideInInspector] public float legDamageCooldown = 0.5f;
        [HideInInspector] public float legDamageRadius = 1.0f;

        void Start()
        {
            ResetMimic();
        }

        private void OnValidate()
        {
            maxLegs = numberOfLegs * partsPerLeg;
            minimumAnchoredParts = minimumAnchoredLegs * partsPerLeg;
            maxLegDistance = newLegRadius * 2.1f;
        }

        private void ResetMimic()
        {
            foreach (Leg g in Object.FindObjectsByType<Leg>(FindObjectsSortMode.None))
            {
                Destroy(g.gameObject);
            }
            legCount = 0;
            deployedLegs = 0;

            maxLegs = numberOfLegs * partsPerLeg;
            float rot = 360f / maxLegs;
            Vector2 randV = Random.insideUnitCircle;
            velocity = new Vector3(randV.x, 0, randV.y);
            minimumAnchoredParts = minimumAnchoredLegs * partsPerLeg;
            maxLegDistance = newLegRadius * 2.1f;

        }

        IEnumerator NewLegCooldown()
        {
            canCreateLeg = false;
            yield return new WaitForSeconds(newLegCooldown);
            canCreateLeg = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!canCreateLeg)
                return;

            // === Projetar posição do corpo no chão para cálculo de pernas ===
            // Quando o corpo está elevado (ex: Sentinela a 5m), precisamos
            // calcular as posições das pernas no nível do chão.
            Collider[] colsForGround = GetComponentsInChildren<Collider>();
            foreach(var c in colsForGround) if (c != null) c.enabled = false;

            float groundY = transform.position.y; // fallback: usar Y do corpo
            RaycastHit groundCheck;
            if (Physics.Raycast(transform.position, Vector3.down, out groundCheck, 50f))
            {
                groundY = groundCheck.point.y;
            }

            foreach(var c in colsForGround) if (c != null) c.enabled = true;

            // Posição do corpo projetada no chão (apenas XZ do corpo, Y do chão)
            Vector3 groundBodyPos = new Vector3(transform.position.x, groundY, transform.position.z);

            // New leg origin is placed in front of the mimic, no nível do chão
            legPlacerOrigin = groundBodyPos + velocity.normalized * newLegRadius;

            if (legCount <= maxLegs - partsPerLeg)
            {
                // Offset The leg origin by a random vector
                Vector2 offset = Random.insideUnitCircle * newLegRadius;
                Vector3 newLegPosition = legPlacerOrigin + new Vector3(offset.x, 0, offset.y);

                // If the mimic is moving and the new leg position is behind it, mirror it to make
                // it reach in front of the mimic.
                if (velocity.magnitude > 1f)
                {
                    float newLegAngle = Vector3.Angle(velocity, newLegPosition - groundBodyPos);

                    if (Mathf.Abs(newLegAngle) > 90)
                    {
                        newLegPosition = groundBodyPos - (newLegPosition - groundBodyPos);
                    }
                }

                if (Vector3.Distance(new Vector3(groundBodyPos.x, 0, groundBodyPos.z), new Vector3(legPlacerOrigin.x, 0, legPlacerOrigin.z)) < minLegDistance)
                    newLegPosition = ((newLegPosition - groundBodyPos).normalized * minLegDistance) + groundBodyPos;

                // if the angle is too big, adjust the new leg position towards the velocity vector
                if (Vector3.Angle(velocity, newLegPosition - groundBodyPos) > 45)
                    newLegPosition = groundBodyPos + ((newLegPosition - groundBodyPos) + velocity.normalized * (newLegPosition - groundBodyPos).magnitude) / 2f;

                Collider[] cols = GetComponentsInChildren<Collider>();
                foreach(var c in cols) if (c != null) c.enabled = false;

                RaycastHit hit;
                Vector3 myHit;
                if (Physics.Raycast(newLegPosition + Vector3.up * 10f, -Vector3.up, out hit))
                {
                    myHit = hit.point;
                    if (Physics.Linecast(transform.position, hit.point, out hit))
                        myHit = hit.point;
                }
                else
                {
                    // Fallback: usar a posição no chão em vez de no ar
                    myHit = new Vector3(newLegPosition.x, groundY, newLegPosition.z);
                }

                foreach(var c in cols) if (c != null) c.enabled = true;

                float lifeTime = Random.Range(minLegLifetime, maxLegLifetime);

                StartCoroutine("NewLegCooldown");
                for (int i = 0; i < partsPerLeg; i++)
                {
                    RequestLeg(myHit, legResolution, maxLegDistance, Random.Range(minGrowCoef, maxGrowCoef), this, lifeTime);
                    if (legCount >= maxLegs)
                        return;
                }
            }
        }

        // object pooling to limit leg instantiation
        void RequestLeg(Vector3 footPosition, int legResolution, float maxLegDistance, float growCoef, Mimic myMimic, float lifeTime)
        {
            GameObject newLeg;
            if (availableLegPool.Count > 0)
            {
                newLeg = availableLegPool[availableLegPool.Count - 1];
                availableLegPool.RemoveAt(availableLegPool.Count - 1);
            }
            else
            {
                newLeg = Instantiate(legPrefab, transform.position, Quaternion.identity);
            }
            newLeg.SetActive(true);

            // Propaga configuração de dano para a perna (Sentinela)
            Leg legComponent = newLeg.GetComponent<Leg>();
            legComponent.dealsDamage = legsDealDamage;
            legComponent.legDamage = legDamageAmount;
            legComponent.legDamageCooldown = legDamageCooldown;
            legComponent.legDamageRadius = legDamageRadius;

            legComponent.Initialize(footPosition, legResolution, maxLegDistance, growCoef, myMimic, lifeTime);
            newLeg.transform.SetParent(myMimic.transform);
            
            PlayLegSound(footPosition);
        }

        void PlayLegSound(Vector3 position)
        {
            if (legGenerationSounds == null || legGenerationSounds.Length == 0)
                return;

            int randIndex = Random.Range(0, legGenerationSounds.Length);
            AudioClip clipToPlay = legGenerationSounds[randIndex];
            
            float pitch = Random.Range(0.9f, 1.1f);
            
            // Se for o som 3 (index 2) ou o nome contiver "3", acelera para ficar mais agudo
            if (randIndex == 2 || (clipToPlay != null && clipToPlay.name.Contains("3")))
            {
                pitch = Random.Range(1.4f, 1.6f);
            }

            if (clipToPlay != null)
            {
                PlayClipAtPointWithPitch(clipToPlay, position, pitch, legSoundVolume);
            }
        }

        void PlayClipAtPointWithPitch(AudioClip clip, Vector3 position, float pitch, float volume)
        {
            GameObject audioObj = new GameObject("TempLegAudio");
            audioObj.transform.position = position;
            AudioSource aSource = audioObj.AddComponent<AudioSource>();
            aSource.clip = clip;
            aSource.pitch = pitch;
            aSource.volume = volume;
            aSource.spatialBlend = 1f; // Som 3D
            aSource.minDistance = 3f;
            aSource.maxDistance = 20f;
            aSource.rolloffMode = AudioRolloffMode.Linear;
            aSource.Play();
            Destroy(audioObj, clip.length / Mathf.Abs(pitch));
        }

        public void RecycleLeg(GameObject leg)
        {
            if (!availableLegPool.Contains(leg))
            {
                availableLegPool.Add(leg);
            }
            leg.SetActive(false);
        }

        /// <summary>
        /// Ativa ou desativa a geração de pernas procedurais e limpa as pernas existentes se desativado.
        /// </summary>
        public void SetLegsActive(bool active)
        {
            if (active)
            {
                enabled = true;
            }
            else
            {
                enabled = false;
                
                // Encontra e recicla todas as pernas ativas
                Leg[] activeLegs = GetComponentsInChildren<Leg>(true);
                foreach (Leg leg in activeLegs)
                {
                    if (leg.gameObject.activeSelf)
                    {
                        RecycleLeg(leg.gameObject);
                    }
                }
                
                legCount = 0;
                deployedLegs = 0;
            }
        }

        /// <summary>
        /// Recalcula valores derivados após alteração de parâmetros em runtime.
        /// Chamado pelo Geobionte_AI após transformação/restauração.
        /// </summary>
        public void RecalculateParameters()
        {
            maxLegs = numberOfLegs * partsPerLeg;
            minimumAnchoredParts = minimumAnchoredLegs * partsPerLeg;
            maxLegDistance = newLegRadius * 2.1f;
        }
    }

}