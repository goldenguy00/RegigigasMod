using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using RegigigasMod.Modules.Components;
using System.Linq;
using static RoR2.CameraTargetParams;

namespace RegigigasMod.SkillStates.Regigigas
{
    public class Revenge : BaseRegiSkillState
    {
        public static float baseDuration = 8f;

        private float lastHealth;
        private float storedDamage;
        private float duration;
        private Animator modelAnimator;
        private GameObject chargeEffectInstance;
        private Transform areaIndicator;

        private CameraParamsOverrideHandle camParamsOverrideHandle;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = Revenge.baseDuration;// / this.attackSpeedStat;
            this.lastHealth = base.healthComponent.combinedHealth;
            this.modelAnimator = base.GetModelAnimator();
            this.camParamsOverrideHandle = Modules.CameraParams.OverrideCameraParams(base.cameraTargetParams, RegigigasCameraParams.CHARGE, 0.5f);

            base.PlayAnimation("FullBody, Override", "RevengeEntry", "Revenge.playbackRate", this.duration * 0.1f);

            if (NetworkServer.active) base.characterBody.AddBuff(Modules.Buffs.armorBuff);

            Transform modelTransform = base.GetModelTransform();
            if (modelTransform)
            {
                var temporaryOverlay = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
                temporaryOverlay.duration = this.duration;
                temporaryOverlay.animateShaderAlpha = true;
                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = Resources.Load<Material>("Materials/matDoppelganger");
                temporaryOverlay.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
            }

            if (this.modelAnimator) this.modelAnimator.SetFloat(AnimationParameters.aimWeight, 0f);

            this.chargeEffectInstance = GameObject.Instantiate(new EntityStates.ImpBossMonster.BlinkState().blinkDestinationPrefab, base.gameObject.transform);
            this.chargeEffectInstance.transform.position = base.characterBody.corePosition;
            this.chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = this.duration;
            this.areaIndicator = this.chargeEffectInstance.transform.Find("Particles").Find("AreaIndicator");

            this.chargeEffectInstance.GetComponentInChildren<PostProcessDuration>().maxDuration = this.duration;
        }

        private void UpdateRadius()
        {
            float healthPercentage = this.storedDamage / base.healthComponent.fullCombinedHealth;
            float radius = Util.Remap(healthPercentage, 0f, 1f, RevengeEnd.minRadius, RevengeEnd.maxRadius);
            this.areaIndicator.localScale = Vector3.one * radius;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            float diff = this.lastHealth - base.healthComponent.combinedHealth;
            if (diff > 0) this.storedDamage += diff;
            this.lastHealth = base.healthComponent.combinedHealth;

            if (this.areaIndicator) this.UpdateRadius();

            if (base.isAuthority)
            {
                if (base.fixedAge >= this.duration)
                {
                    this.NextState();
                }

                // only playable regi can cancel this early
                if (this.GetTeam() == TeamIndex.Player)
                {
                    if (base.IsKeyDownAuthority() && base.fixedAge >= 0.5f)
                    {
                        this.NextState();
                    }
                }
            }
        }

        private void NextState()
        {
            this.outer.SetNextState(new RevengeEnd()
            {
                storedDamage = 3f * this.storedDamage
            });
        }

        public override void OnExit()
        {
            base.OnExit();

            this.cameraTargetParams.RemoveParamsOverride(this.camParamsOverrideHandle, 0.5f);

            if (this.chargeEffectInstance) EntityState.Destroy(this.chargeEffectInstance);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}