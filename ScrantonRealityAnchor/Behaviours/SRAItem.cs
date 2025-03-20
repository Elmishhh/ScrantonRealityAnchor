using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace ScrantonRealityAnchor.Behaviours
{
    public class SRAItem : GrabbableObject
    {
        public float radius = 11.25f;
        public bool itemActive;
        public bool itemDiscarded;
        public AudioSource SRA_AudioSource;
        public AudioSource SRA_LoopingAudioSource;

        //public RaycastHit[] objectsInRange;
        public Collider[] objectsInRange;
        public List<EnemyAI> enemyAIinRange;

        public Collider[] objectsNearRange;
        public List<EnemyAI> enemyAINearRange;

        public int SRALayerMask = 1084754248;

        public GameObject outerGlow;
        public GameObject innerGlow;

        public GameObject bottomSpinningObject;
        public GameObject topSpinningObject;

        public AudioClip activeSFX;

        public override void Start()
        {
            base.Start();
            insertedBattery = new Battery(false, 1);

            outerGlow = transform.Find("glow").gameObject;
            innerGlow = transform.Find("innerGlow").gameObject;
            bottomSpinningObject = transform.Find("bottom half model/spinning base").gameObject;
            topSpinningObject = transform.Find("top half model/spinning base").gameObject;

            SRA_LoopingAudioSource = innerGlow.GetComponent<AudioSource>();
            SRA_LoopingAudioSource.spatialize = true;

            InvertNormals(outerGlow.transform.GetChild(0).gameObject);
        }
        public override void GrabItem()
        {
            base.GrabItem();
            GrabItemServerRpc();
        }
        public override void DiscardItem()
        {
            base.DiscardItem();
            DropItemServerRpc();
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (playerHeldBy == null) { return; }

            if (buttonDown)
            {
                ActivateItemServerRpc(!itemActive);
            }
        }
        public void FixedUpdate()
        {
            if (itemActive)
            {
                bottomSpinningObject.transform.Rotate(0, 2, 0);
                topSpinningObject.transform.Rotate(0, 2, 0);
                if (itemDiscarded)
                {
                    bottomSpinningObject.transform.Rotate(0, 6, 0); // intentionally stacking the rotation speed
                    topSpinningObject.transform.Rotate(0, 6, 0);
                    objectsInRange = Physics.OverlapSphere(innerGlow.transform.position, radius, SRALayerMask);
                    enemyAIinRange = new List<EnemyAI>();
                    for (int i = 0; i < objectsInRange.Length - 1; i++)
                    {
                        EnemyAI enemy = objectsInRange[i].transform.GetComponent<EnemyAI>();
                        EnemyAICollisionDetect enemycolldetect = objectsInRange[i].transform.GetComponent<EnemyAICollisionDetect>();
                        if (enemy != null)
                        {
                            enemyAIinRange.Add(enemy);
                        }
                        else if (enemycolldetect != null && enemy != enemycolldetect)
                        {
                            enemyAIinRange.Add(enemycolldetect.mainScript);
                        }
                    }

                    objectsNearRange = Physics.OverlapSphere(innerGlow.transform.position, radius + 5, SRALayerMask);
                    enemyAINearRange = new List<EnemyAI>();
                    for (int i = 0; i < objectsNearRange.Length - 1; i++)
                    {
                        EnemyAI enemy = objectsNearRange[i].transform.GetComponent<EnemyAI>();
                        EnemyAICollisionDetect enemycolldetect = objectsNearRange[i].transform.GetComponent<EnemyAICollisionDetect>();
                        if (enemy != null)
                        {
                            enemyAINearRange.Add(enemy);
                        }
                        else if (enemycolldetect != null && enemy != enemycolldetect)
                        {
                            enemyAINearRange.Add(enemycolldetect.mainScript);
                        }
                    }
                }
            }
        }
        public override void Update()
        {
            base.Update();
            if (itemActive && itemDiscarded)
            {
                foreach (EnemyAI ai in enemyAINearRange)
                {
                    Plugin.Logger.LogMessage($"enemy {ai.name} moving at vel: {ai.agent.velocity} with desired vel: {ai.agent.desiredVelocity}");
                }
                foreach (EnemyAI ai in enemyAIinRange)
                {
                    Vector3 originalVelocity = ai.agent.desiredVelocity;
                    Vector3 nextVelocity = originalVelocity / 2f;
                    ai.agent.velocity = nextVelocity;
                    Plugin.Logger.LogMessage($"set velocity and speed of {ai.name}");
                }
            }
        }
        public void ToggleItem(bool toggle)
        {
            innerGlow.SetActive(toggle);
            itemActive = toggle;
            if (toggle) { SRA_LoopingAudioSource.Play(); }
            else { SRA_LoopingAudioSource.Stop(); }
        }
        public void OnDropItem()
        {
            itemDiscarded = true;
            if (itemActive)
            {
                outerGlow.SetActive(true);
                StartCoroutine(ExpandOuterGlow());
            }
        }
        public void OnGrabItem()
        {
            itemDiscarded = false;
            if (outerGlow.activeSelf)
            {
                outerGlow.SetActive(false);
            }
        }
        public IEnumerator ExpandOuterGlow()
        {
            for (int i = 0; i < 38; i++)
            {
                outerGlow.transform.localScale = Vector3.one + Vector3.one * i * 2;
                yield return new WaitForFixedUpdate();
                //yield return new WaitForFixedUpdate();
            }
        }
        public void InvertNormals(GameObject obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Mesh mesh = meshFilter.mesh;
                Vector3[] normals = mesh.normals;

                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = -normals[i];
                }
                mesh.normals = normals;

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    int[] triangles = mesh.GetTriangles(i);
                    for (int j = 0; j < triangles.Length; j += 3)
                    {
                        int temp = triangles[j];
                        triangles[j] = triangles[j + 1];
                        triangles[j + 1] = temp;
                    }
                    mesh.SetTriangles(triangles, i);
                }
            }
            else
            {
                Plugin.Logger.LogError($"MeshFilter not found on '{obj.name}'.");
            }
        }

        #region toggleRPCs
        [ServerRpc]
        public void ActivateItemServerRpc(bool toggle)
        {
            ActivateItemClientRpc(toggle);
        }
        [ClientRpc]
        public void ActivateItemClientRpc(bool toggle)
        {
            ToggleItem(toggle);
        }
        #endregion
        #region dropRPCs
        [ServerRpc]
        public void DropItemServerRpc()
        {
            DropItemClientRpc();
        }
        [ClientRpc]
        public void DropItemClientRpc()
        {
            OnDropItem();
        }
        #endregion
        #region grabRPCs
        [ServerRpc]
        public void GrabItemServerRpc()
        {
            GrabItemClientRpc();
        }
        [ClientRpc]
        public void GrabItemClientRpc()
        {
            OnGrabItem();
        }
        #endregion

    }
}
