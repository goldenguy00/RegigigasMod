using UnityEngine;
using EntityStates;
using UnityEngine.Networking;
using RoR2;
using UnityEngine.AddressableAssets;

namespace RegigigasMod.SkillStates.Regigigas.GigaImpact
{
    public class Channel : BaseSkillState
    {
        public float baseDuration = 1.9f;

        private float duration;
        private GameObject chargeEffectInstance;

        private uint prepPlayId;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;

            this.PlayAnimation("FullBody, Override", "RevengeEntry", "Revenge.playbackRate", this.duration);

            if (NetworkServer.active) this.characterBody.AddBuff(Modules.Buffs.armorBuff);

            Transform modelTransform = base.GetModelTransform();
            if (modelTransform)
            {
                var temporaryOverlay = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
                temporaryOverlay.duration = this.duration * 0.5f;
                temporaryOverlay.animateShaderAlpha = true;
                temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                temporaryOverlay.destroyComponentOnEnd = true;
                temporaryOverlay.originalMaterial = Resources.Load<Material>("Materials/matOnFire");
                temporaryOverlay.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
            }

            this.chargeEffectInstance = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Grandparent/ChargeGrandParentSunHands.prefab").WaitForCompletion());
            this.chargeEffectInstance.transform.parent = this.FindModelChild("Chest");
            this.chargeEffectInstance.transform.localPosition = new Vector3(0f, 0f, 0f);
            this.chargeEffectInstance.transform.localRotation = Quaternion.identity;
            this.chargeEffectInstance.transform.localScale = Vector3.one * 2f;

            this.chargeEffectInstance.GetComponentInChildren<ObjectScaleCurve>().timeMax = this.duration * 0.5f;

            this.prepPlayId = Util.PlaySound("sfx_regigigas_flame_prep", this.gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.characterBody.isSprinting = false;

            this.characterMotor.velocity *= 0.25f;

            if (this.characterMotor.velocity.y <= 0f) this.characterMotor.velocity.y = 0f;

            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextState(new Fire());
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            AkSoundEngine.StopPlayingID(this.prepPlayId);

            if (NetworkServer.active) this.characterBody.RemoveBuff(Modules.Buffs.armorBuff);

            if (this.chargeEffectInstance) EntityState.Destroy(this.chargeEffectInstance);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death; // IM FUCKING INVINCIBLE
        }
    }
}