using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RegigigasMod.Modules.Components
{
    public class RegigigasShinyComponent : NetworkBehaviour
    {
        private CharacterBody characterBody;
        private ModelSkinController skinController;

        private void Awake()
        {
            this.characterBody = this.GetComponent<CharacterBody>();
            this.skinController = this.GetComponentInChildren<ModelSkinController>();

            this.Invoke("ShinyRoll", 0.1f);
        }

        private void ShinyRoll()
        {
            if (NetworkServer.active)
            {
                var isShiny = Util.CheckRoll(Modules.Config.shinySpawnRate, this.characterBody.master);
                if (isShiny)
                {
                    this.RpcApplyShiny();

                    if (this.characterBody.master)
                        this.characterBody.master.gameObject.AddComponent<RegigigasDropComponent>().itemDropDef = RoR2Content.Items.ShinyPearl;
                }
            }
        }

        [ClientRpc]
        private void RpcApplyShiny()
        {
            if (this.skinController)
            {
                if (this.characterBody)
                    this.characterBody.skinIndex = 1;

                this.skinController.ApplySkin(1);
            }
        }
    }
}