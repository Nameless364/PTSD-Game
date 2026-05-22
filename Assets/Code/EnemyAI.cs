using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    public class EnemyAI : MonoBehaviour
    {
        [Header("References")]
        public Transform player;
        public List<Transform> patrolWaypoints;
        public List<RoomArea> rooms;
        private NavMeshAgent agent;
        private Movement playerMovement;

        [Header("Footstep Audio")]
        [Tooltip("AudioClip เสียงฝีเท้าของผี (ใส่หลายเสียงสลับกันได้)")]
        public AudioClip[] footstepClips;
        [Tooltip("ระยะที่ได้ยินเสียงเต็ม (เมตร)")]
        public float footstepMinDistance = 5f;
        [Tooltip("ระยะที่ไม่ได้ยินเสียงเลย (เมตร)")]
        public float footstepMaxDistance = 20f;
        [Tooltip("ความดังสูงสุด (0-1)")]
        public float footstepMaxVolume = 0.85f;
        [Tooltip("ช่วงเวลาระหว่างก้าว (วินาที) ตอน patrol")]
        public float footstepIntervalPatrol = 0.55f;
        [Tooltip("ช่วงเวลาระหว่างก้าว (วินาที) ตอน chase")]
        public float footstepIntervalChase  = 0.32f;

        private AudioSource _footstepSource;
        private float _footstepTimer = 0f;
        private int   _footstepIndex = 0;

        [Header("Detection Settings")]
        public float viewDistance = 15f;
        public float viewAngle = 360f;
        public float catchDistance = 2f;
        public LayerMask obstacleMask;

        [Header("Detection Meter")]
        [Tooltip("เวลา (วินาที) ที่ใช้เติม meter จนเต็มตอนเห็นผู้เล่นต่อเนื่อง")]
        public float detectionFillTime   = 2.5f;
        [Tooltip("เวลา (วินาที) ที่ meter ลดจนหมดตอนไม่เห็น")]
        public float detectionDrainTime  = 4f;
        [Tooltip("% ที่ meter ถือว่าเข้าสู่ Suspicious (0-1)")]
        public float suspiciousThreshold = 0.3f;

        [Header("Sound Detection")]
        public float noiseDetectionRange = 15f;
        public float noiseSensitivity = 15f;
        private Vector3? lastNoisePosition = null;

        [Header("Movement Settings")]
        public float patrolSpeed = 2f;
        public float chaseSpeed = 4.5f;

        // ---- Detection Meter State ----
        private float _detectionMeter  = 0f;   // 0 = ไม่รู้ตัว, 1 = จับได้
        private bool  _isSuspicious    = false;
        private bool  isChasing = false;
        private bool  isInteractingWithDoor = false;
        private bool  isGameOver = false;
        private BaseDoor[] allDoors;

        public float DetectionMeter => _detectionMeter;
        public bool  IsSuspicious   => _isSuspicious;

        [System.Serializable]
        public class RoomArea
        {
            public string roomName;
            public BaseDoor door;
            public Transform entrancePoint;
            public List<Transform> insidePoints;
        }

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            if (player == null)
            {
                GameObject pObj = GameObject.FindGameObjectWithTag("Player");
                if (pObj != null) player = pObj.transform;
            }
            if (player != null) playerMovement = player.GetComponentInChildren<Movement>();

            allDoors = FindObjectsByType<BaseDoor>(FindObjectsSortMode.None);

            // ---- ตั้งค่า AudioSource สำหรับเสียงฝีเท้า ----
            _footstepSource = gameObject.AddComponent<AudioSource>();
            _footstepSource.spatialBlend  = 1f;   // 3D audio เต็ม
            _footstepSource.rolloffMode   = AudioRolloffMode.Custom;
            _footstepSource.minDistance   = footstepMinDistance;
            _footstepSource.maxDistance   = footstepMaxDistance;
            _footstepSource.playOnAwake   = false;
            _footstepSource.loop          = false;
            _footstepSource.volume        = footstepMaxVolume;

            Debug.Log("👻 [EnemyAI] เริ่มระบบลาดตระเวนแล้ว (พบประตู " + allDoors.Length + " บาน)");
            StartCoroutine(PatrolRoutine());
        }

        void Update()
        {
            if (player == null || playerMovement == null || isGameOver) return;

            bool canSeePlayer = CheckLineOfSight();
            bool playerHiding = playerMovement.IsHiding();

            UpdateDetectionMeter(canSeePlayer, playerHiding);

            // เมื่อ meter เต็ม → Game Over
            if (_detectionMeter >= 1f && !isGameOver)
            {
                Debug.Log("👁️ [EnemyAI] Detection เต็ม! Game Over!");
                StartCoroutine(GameOverSequence());
                return;
            }

            if (isChasing)
            {
                if (!_isSuspicious || playerHiding)
                {
                    Debug.Log("❓ [EnemyAI] คลาดสายตา กลับสู่โหมดลาดตระเวน");
                    isChasing = false;
                }
            }

            // เข้า Chase ตอน suspicious เพื่อไล่ตาม
            if (_isSuspicious && canSeePlayer && !playerHiding && !isInteractingWithDoor)
                isChasing = true;

            if (isChasing) ChasePlayer();
            else CheckForNoise();

            UpdateFootsteps();
        }

        void UpdateFootsteps()
        {
            if (footstepClips == null || footstepClips.Length == 0) return;
            if (_footstepSource == null) return;

            // เล่นเสียงเฉพาะตอนผีกำลังเดิน (velocity > threshold)
            bool isMoving = agent.velocity.magnitude > 0.3f && !agent.isStopped;
            if (!isMoving) { _footstepTimer = 0f; return; }

            float interval = isChasing ? footstepIntervalChase : footstepIntervalPatrol;
            _footstepTimer -= Time.deltaTime;

            if (_footstepTimer <= 0f)
            {
                _footstepTimer = interval;

                // คำนวณ volume ตามระยะ (linear inverse)
                float dist = Vector3.Distance(transform.position, player.position);
                float t = Mathf.InverseLerp(footstepMinDistance, footstepMaxDistance, dist);
                float vol = Mathf.Lerp(footstepMaxVolume, 0f, t);

                if (vol > 0.01f)
                {
                    // สลับ clip แต่ละก้าว
                    AudioClip clip = footstepClips[_footstepIndex % footstepClips.Length];
                    _footstepIndex++;
                    _footstepSource.PlayOneShot(clip, vol);
                }
            }
        }

        void UpdateDetectionMeter(bool canSee, bool hiding)
        {
            bool shouldFill = canSee && !hiding && !isInteractingWithDoor;

            if (shouldFill)
            {
                // เติม meter ตามเวลา
                _detectionMeter += Time.deltaTime / detectionFillTime;
            }
            else
            {
                // ลด meter ตามเวลา
                _detectionMeter -= Time.deltaTime / detectionDrainTime;
            }

            _detectionMeter = Mathf.Clamp01(_detectionMeter);
            _isSuspicious   = _detectionMeter >= suspiciousThreshold;

            // ส่งค่าให้ UI
            if (DetectionIndicatorUI.Instance != null)
                DetectionIndicatorUI.Instance.SetDetectionLevel(_detectionMeter);
        }

        void CheckForNoise()
        {
            if (NoiseManager.Instance == null || playerMovement.IsHiding() || isGameOver) return;

            float noise = NoiseManager.Instance.currentNoise;
            float dist = Vector3.Distance(transform.position, player.position);

            if (noise > noiseSensitivity && dist < noiseDetectionRange)
            {
                lastNoisePosition = player.position;
            }
        }

        bool IsNoiseSignificant(Vector3 currentDestination)
        {
            if (!lastNoisePosition.HasValue) return false;
            return Vector3.Distance(currentDestination, lastNoisePosition.Value) > 2f;
        }

        IEnumerator PatrolRoutine()
        {
            while (!isGameOver)
            {
                if (isChasing) { yield return new WaitUntil(() => !isChasing || isGameOver); }
                if (isGameOver) yield break;

                float decision = Random.value;
                if (lastNoisePosition.HasValue)
                {
                    Vector3 target = lastNoisePosition.Value;
                    lastNoisePosition = null;
                    Debug.Log("🔍 [EnemyAI] มุ่งหน้าไปตรวจสอบจุดที่เกิดเสียง");
                    yield return StartCoroutine(MoveToDestination(target));
                    if (!lastNoisePosition.HasValue && !isChasing && !isGameOver)
                        yield return StartCoroutine(LookAround());
                }
                else if (decision < 0.6f && rooms.Count > 0)
                {
                    RoomArea targetRoom = rooms[Random.Range(0, rooms.Count)];
                    Debug.Log("🏠 [EnemyAI] กำลังมุ่งหน้าไปสำรวจห้อง: " + targetRoom.roomName);
                    yield return StartCoroutine(ExploreRoom(targetRoom));
                }
                else if (patrolWaypoints.Count > 0)
                {
                    Transform wp = patrolWaypoints[Random.Range(0, patrolWaypoints.Count)];
                    int retry = 0;
                    while (Vector3.Distance(transform.position, wp.position) < 3f && retry < 5 && patrolWaypoints.Count > 1)
                    {
                        wp = patrolWaypoints[Random.Range(0, patrolWaypoints.Count)];
                        retry++;
                    }
                    Debug.Log("🛤️ [EnemyAI] กำลังเดินไปจุดลาดตระเวน: " + wp.name);
                    yield return StartCoroutine(MoveToDestination(wp.position));
                    yield return StartCoroutine(LookAround());
                }

                yield return null;
            }
        }

        IEnumerator LookAround()
        {
            Debug.Log("👀 [EnemyAI] กำลังกวาดสายตามองรอบๆ...");
            agent.isStopped = true;

            float startY = transform.eulerAngles.y;
            float[] angles = { 45, -45, 0 };

            foreach (float angle in angles)
            {
                float targetY = startY + angle;
                Quaternion targetRot = Quaternion.Euler(0, targetY, 0);
                float t = 0;
                while (t < 1f)
                {
                    if (isChasing) yield break;
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
                    t += Time.deltaTime * 2f;
                    yield return null;
                }
                yield return new WaitForSeconds(0.8f);
            }

            agent.isStopped = false;
        }

        IEnumerator ExploreRoom(RoomArea room)
        {
            if (room.door == null || room.entrancePoint == null) yield break;

            bool isLocked = room.door is LockedDoor && ((LockedDoor)room.door).isLocked;
            if (isLocked)
            {
                Debug.Log("🔒 [EnemyAI] ห้อง " + room.roomName + " ล็อกอยู่ ข้ามไปจุดอื่น");
                yield break;
            }

            yield return StartCoroutine(MoveToDestination(room.entrancePoint.position));
            yield return StartCoroutine(MoveToDestination(room.door.transform.position, 1.2f));

            if (!room.door.open) yield return StartCoroutine(HandleDoorSequence(room.door));

            foreach (var p in room.insidePoints)
            {
                if (isChasing || isGameOver) break;
                yield return StartCoroutine(MoveToDestination(p.position));
                yield return new WaitForSeconds(1.5f);
            }

            if (!isChasing && !isGameOver)
            {
                yield return StartCoroutine(MoveToDestination(room.door.transform.position, 1.2f));
                if (room.door.open) yield return StartCoroutine(HandleDoorSequence(room.door));
                yield return StartCoroutine(MoveToDestination(room.entrancePoint.position));
            }
        }

        IEnumerator MoveToDestination(Vector3 target, float stopDistance = 0.7f)
        {
            if (isChasing || isGameOver) yield break;

            agent.isStopped = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = stopDistance;
            agent.SetDestination(target);

            yield return new WaitUntil(() => !agent.pathPending || isGameOver);
            if (isGameOver) yield break;

            float timeout = 12f;
            while (agent.remainingDistance > stopDistance + 0.1f && !isChasing && !isGameOver && timeout > 0)
            {
                if (IsNoiseSignificant(target)) yield break;
                yield return StartCoroutine(CheckForDoorsInWay());
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator CheckForDoorsInWay()
        {
            if (isInteractingWithDoor) yield break;

            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out hit, 1.2f))
            {
                BaseDoor door = hit.collider.GetComponent<BaseDoor>();
                if (door == null) door = hit.collider.GetComponentInParent<BaseDoor>();
                if (door != null)
                    yield return StartCoroutine(HandleDoorSequence(door));
            }
        }

        IEnumerator HandleDoorSequence(BaseDoor door)
        {
            isInteractingWithDoor = true;

            if (!door.open)
            {
                Debug.Log("🚪 [EnemyAI] กำลังเปิดประตู...");
                agent.isStopped = true;
                Vector3 dirToDoor = (door.transform.position - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(new Vector3(dirToDoor.x, 0, dirToDoor.z));
                yield return new WaitForSeconds(0.3f);
                door.Interact(true);
                yield return new WaitForSeconds(1.0f);
                agent.isStopped = false;
            }

            Debug.Log("🚶 [EnemyAI] กำลังเดินผ่านประตู...");
            Vector3 passPoint = transform.position + transform.forward * 2.5f;
            agent.SetDestination(passPoint);

            float timer = 0f;
            while (Vector3.Distance(transform.position, passPoint) > 0.5f && timer < 2f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            Debug.Log("🚪 [EnemyAI] หันกลับมาปิดประตู...");
            agent.isStopped = true;
            Vector3 dirBackToDoor = (door.transform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(dirBackToDoor.x, 0, dirBackToDoor.z));
            yield return new WaitForSeconds(0.5f);

            if (door.open) door.Interact(true);
            yield return new WaitForSeconds(1.0f);

            agent.isStopped = false;
            isInteractingWithDoor = false;
        }

        bool CheckLineOfSight()
        {
            if (player == null || playerMovement.IsHiding() || isGameOver || isInteractingWithDoor) return false;

            Vector3 eyePos = transform.position + Vector3.up * 1.6f;
            Vector3 targetPos = player.position + Vector3.up * 0.8f;
            float distToPlayer = Vector3.Distance(eyePos, targetPos);
            Vector3 dirToPlayer = (targetPos - eyePos).normalized;

            if (distToPlayer > viewDistance) return false;

            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer);
            if (angleToPlayer > viewAngle / 2f) return false;

            Ray eyeRay = new Ray(eyePos, dirToPlayer);

            // ขั้นที่ 1: เช็คประตูปิดทุกบาน
            // ใช้ Collider.bounds.IntersectRay เพื่อความแม่นยำ ไม่ขึ้นกับตำแหน่ง Pivot
            foreach (BaseDoor door in allDoors)
            {
                if (door == null || door.open) continue;

                // หา Collider จากลูกๆ ของประตู
                Collider[] cols = door.GetComponentsInChildren<Collider>();
                bool blocked = false;

                foreach (Collider col in cols)
                {
                    if (col.isTrigger) continue; // ข้าม trigger
                    float enter;
                    if (col.bounds.IntersectRay(eyeRay, out enter) && enter >= 0f && enter < distToPlayer)
                    {
                        blocked = true;
                        Debug.DrawRay(eyePos, dirToPlayer * enter, Color.cyan);
                        Debug.Log($"🛡️ [LOS] ประตู '{door.name}' (col={col.name}) กั้นอยู่ที่ dist={enter:F2}");
                        break;
                    }
                }

                // Fallback: ถ้าไม่มี Collider ใช้ geometry perpDist
                if (!blocked && cols.Length == 0)
                {
                    Vector3 doorPos = door.transform.position + Vector3.up * 1.0f;
                    float doorDist = Vector3.Distance(eyePos, doorPos);
                    if (doorDist < distToPlayer)
                    {
                        float perpDist = Vector3.Cross(dirToPlayer, doorPos - eyePos).magnitude;
                        if (perpDist < 1.5f) blocked = true;
                    }
                }

                if (blocked) return false;
            }

            // ขั้นที่ 2: เช็คกำแพงใน obstacleMask
            RaycastHit wallHit;
            if (Physics.Raycast(eyePos, dirToPlayer, out wallHit, distToPlayer, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (wallHit.collider.GetComponentInParent<EnemyAI>() == null &&
                    wallHit.collider.GetComponentInParent<Movement>() == null)
                {
                    Debug.DrawRay(eyePos, dirToPlayer * wallHit.distance, Color.gray);
                    return false;
                }
            }

            Debug.DrawRay(eyePos, dirToPlayer * distToPlayer, Color.red);
            return true;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 0, 1, 0.2f);
            Gizmos.DrawSphere(transform.position, 0.2f);

            Vector3 leftRayDirection = Quaternion.AngleAxis(-viewAngle / 2, Vector3.up) * transform.forward;
            Vector3 rightRayDirection = Quaternion.AngleAxis(viewAngle / 2, Vector3.up) * transform.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position + Vector3.up, leftRayDirection * viewDistance);
            Gizmos.DrawRay(transform.position + Vector3.up, rightRayDirection * viewDistance);

            Gizmos.color = new Color(1, 0, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, noiseDetectionRange);
        }

        void ChasePlayer()
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0.5f;
            agent.SetDestination(player.position);
        }

        void CatchPlayer()
        {
            if (isGameOver) return;
            StartCoroutine(GameOverSequence());
        }

        IEnumerator GameOverSequence()
        {
            isGameOver = true;
            agent.isStopped = true;

            // ล็อค meter ที่ 1 และล็อค UI ไว้
            _detectionMeter = 1f;
            if (DetectionIndicatorUI.Instance != null)
                DetectionIndicatorUI.Instance.SetDetectionLevel(1f);

            if (playerMovement != null) playerMovement.enabled = false;

            Transform cam = playerMovement.playerCamera;
            Vector3 targetPos = transform.position + Vector3.up * 1.5f;

            float duration = 1.0f;
            float elapsed = 0f;
            Quaternion startRot = cam.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector3 dir = (targetPos - cam.position).normalized;
                Quaternion targetRot = Quaternion.LookRotation(dir);
                cam.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                yield return null;
            }

            yield return new WaitForSeconds(1.5f);

            Debug.Log("💀 [EnemyAI] โหลดฉากใหม่...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
