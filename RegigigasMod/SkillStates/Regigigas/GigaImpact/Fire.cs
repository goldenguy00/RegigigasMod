using UnityEngine;
using EntityStates;
using RoR2;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using static RoR2.CameraTargetParams;

namespace RegigigasMod.SkillStates.Regigigas.GigaImpact
{
    public class Fire : BaseRegiSkillState
    {
        public float duration = 1.75f;

        private float speedCoefficient = 0.25f;
        private float baseSpeed = 38f;
        private CharacterModel characterModel;
        private Transform modelTransform;
        private float originalHeight;
        private float originalRadius;
        private GameObject effectInstance;
        private BlastAttack rushBlastAttack;
        private CapsuleCollider capsuleCollider;
        private uint rushPlayID;

        private CameraParamsOverrideHandle camParamsOverrideHandle;

        public override void OnEnter()
        {
            base.OnEnter();
            this.modelTransform = this.GetModelTransform();
            this.capsuleCollider = this.characterMotor.capsuleCollider;
            this.characterModel = this.modelLocator.modelTransform.GetComponent<CharacterModel>();
            this.characterModel.invisibilityCount++;
            this.camParamsOverrideHandle = Modules.CameraParams.OverrideCameraParams(base.cameraTargetParams, RegigigasCameraParams.EMOTE, 0.5f);

            Util.PlaySound("sfx_regigigas_flame_rush", this.gameObject);
            this.rushPlayID = Util.PlaySound("sfx_regigigas_flame_loop", this.gameObject);

            if (NetworkServer.active) this.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);

            this.originalHeight = this.capsuleCollider.height;
            this.originalRadius = this.capsuleCollider.radius;

            this.capsuleCollider.height = 0.1f;
            this.capsuleCollider.radius = 0.1f;

            this.gameObject.layer = LayerIndex.fakeActor.intVal;
            this.characterMotor.Motor.RebuildCollidableLayers();
            this.characterMotor.Motor.ForceUnground();

            base.PlayAnimation("FullBody, Override", "BufferEmpty");

            this.effectInstance = GameObject.Instantiate(Modules.RegiAssets.gigaImpactRushEffect);
            this.effectInstance.transform.parent = this.FindModelChild("Chest");
            this.effectInstance.transform.localPosition = Vector3.zero;
            this.effectInstance.transform.localRotation = Quaternion.identity;

            this.rushBlastAttack = new BlastAttack
            {
                attacker = this.gameObject,
                inflictor = this.gameObject,
                teamIndex = this.GetTeam(),
                procCoefficient = 0f,
                radius = 48f,
                baseForce = -200,
                baseDamage = 0f,
                falloffModel = BlastAttack.FalloffModel.None,
                damageColorIndex = DamageColorIndex.Default,
                attackerFiltering = AttackerFiltering.NeverHitSelf,
                damageType = DamageType.IgniteOnHit | DamageTypeCombo.GenericSpecial
            };

            EffectData effectData = new EffectData
            {
                origin = this.characterBody.footPosition,
                rotation = Util.QuaternionSafeLookRotation(this.characterDirection.forward),
                scale = 8f
            };

            EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Grandparent/GrandparentSpawnImpact.prefab").WaitForCompletion(), effectData, false);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.characterBody.isSprinting = true;
            base.StartAimMode(0.5f);

            if (base.isAuthority)
            {
                Vector3 forwardForce = this.inputBank.GetAimRay().direction * (this.baseSpeed + (this.characterBody.moveSpeed * this.speedCoefficient));
                this.characterMotor.velocity = forwardForce;

                this.rushBlastAttack.position = this.characterBody.corePosition;
                this.rushBlastAttack.bonusForce = forwardForce * 0.8f;
                this.rushBlastAttack.Fire();

                if (this.inputBank.skill4.justPressed) this.outer.SetNextStateToMain();
            }

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            AkSoundEngine.StopPlayingID(this.rushPlayID);

            if (this.modelTransform)
            {
                var temporaryOverlay = TemporaryOverlayManager.AddOverlay(this.modelTransform.gameObject);
                temporaryOverlay.duration = 3f;
                temporaryOverlay.animateShaderAlpha = true;
                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 5f, 1f, 0f);
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = Resources.Load<Material>("Materials/matOnFire");
                temporaryOverlay.AddToCharacterModel(this.characterModel);

                var temporaryOverlay2 = TemporaryOverlayManager.AddOverlay(this.modelTransform.gameObject);
                temporaryOverlay2.duration = 5f;
                temporaryOverlay2.animateShaderAlpha = true;
                temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlay2.destroyComponentOnEnd = true;
                temporaryOverlay2.originalMaterial = Resources.Load<Material>("Materials/matOnFire");
                temporaryOverlay2.AddToCharacterModel(this.characterModel);
            }

            this.characterModel.invisibilityCount--;
            this.characterMotor.Motor.ForceUnground();
            this.characterMotor.velocity = new Vector3(0f, 20f, 0f);

            this.capsuleCollider.height = this.originalHeight;
            this.capsuleCollider.radius = this.originalRadius;

            this.gameObject.layer = LayerIndex.defaultLayer.intVal;
            this.characterMotor.Motor.RebuildCollidableLayers();

            if (NetworkServer.active) this.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);

            if (this.effectInstance) EntityState.Destroy(this.effectInstance);

            this.cameraTargetParams.RemoveParamsOverride(this.camParamsOverrideHandle, 0.5f);

            Util.PlaySound("sfx_regigigas_explode", this.gameObject);

            this.FireBlast();
        }

        private void FireBlast()
        {
            EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Grandparent/GrandParentSunSpawn.prefab").WaitForCompletion(), new EffectData
            {
                origin = this.transform.position + new Vector3(0f, 5f, 0f),
                rotation = Quaternion.identity,
                scale = GigaImpactOld.blastAttackRadius
            }, false);

            if (base.isAuthority)
            {
                BlastAttack blastAttack = new BlastAttack
                {
                    attacker = this.gameObject,
                    inflictor = this.gameObject,
                    teamIndex = this.GetTeam(),
                    position = this.characterBody.corePosition,
                    procCoefficient = 1f,
                    radius = GigaImpactOld.blastAttackRadius,
                    baseForce = GigaImpactOld.blastAttackForce,
                    bonusForce = Vector3.up * GigaImpactOld.blastAttackBonusForce,
                    baseDamage = GigaImpactOld.blastAttackDamageCoefficient * this.damageStat,
                    falloffModel = BlastAttack.FalloffModel.None,
                    damageColorIndex = DamageColorIndex.Default,
                    attackerFiltering = AttackerFiltering.NeverHitSelf,
                    damageType = DamageType.IgniteOnHit | DamageType.Stun1s | DamageTypeCombo.GenericSpecial
                };
                blastAttack.Fire();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death; // YOU CANT HURT ME JACK
        }
    }
}