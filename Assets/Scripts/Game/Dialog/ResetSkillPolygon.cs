using System.Threading.Tasks;

using App;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialog {
    /// <summary>
    /// Tab RESET BHERO SKILL. Quay lại toàn bộ ability của hero trong MỘT giao dịch — không còn
    /// luồng hai bước randomize/process như bản cũ.
    ///
    /// Giá luỹ tiến theo số lần con hero đó đã reset (counter nằm trên chain, client đọc từ
    /// details ở bit 175). Rarity Mega trở lên không hỗ trợ: contract trả giá 0 và revert nếu gọi,
    /// nên những hero đó bị loại khỏi picker luôn.
    /// </summary>
    public class ResetSkillPolygon : NativeHeroActionPolygon {
        // rarityStats[rarity].ability >= 7 thì phase 1 rút cạn pool -> reset không đổi gì.
        // Ngưỡng này khớp với guard require(ability < 7) bên contract.
        private const int FirstUnsupportedRarity = 6;

        [SerializeField]
        private HeroAbilitiesGroup abilitiesGroup;

        [SerializeField]
        private Text resetCountLbl;

        protected override string ActionName => "Reset Skill";

        protected override bool CanProcess(PlayerData hero) {
            return hero != null
                   && hero.AccountType == HeroAccountType.Nft
                   && hero.rare < FirstUnsupportedRarity;
        }

        protected override Task<string> ReadPriceWei() {
            return BlockchainManager.GetResetSkillNativePrice(RarityOf(Hero), Hero.randomAbilityCounter);
        }

        protected override Task<HeroActionResult> SendAction(string priceWei) {
            return BlockchainManager.ResetSkill(Hero.heroId.Id, priceWei);
        }

        // Hero đã đổi chỉ số nên ô chọn được xoá đi. Không tự mở DialogNewHero ở đây: push
        // SYNC_HERO_RESPONSE đưa hero vừa đổi vào NewIds, SyncHeroController sẽ tự bật dialog đó —
        // mở thêm ở đây là hiện hai lần.
        protected override bool ClearSlotAfterSuccess => true;

        protected override void OnHeroChanged() {
            if (abilitiesGroup) {
                abilitiesGroup.gameObject.SetActive(Hero != null);
                if (Hero != null) {
                    abilitiesGroup.Show(Hero.abilities);
                }
            }
            if (resetCountLbl) {
                resetCountLbl.text = Hero != null ? Hero.randomAbilityCounter.ToString() : "--";
            }
        }
    }
}
