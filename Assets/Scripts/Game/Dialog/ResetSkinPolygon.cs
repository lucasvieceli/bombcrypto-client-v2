using System;
using System.Threading.Tasks;

using Animation;

using App;

using Cysharp.Threading.Tasks;

using Senspark;

using UnityEngine;

namespace Game.Dialog {
    /// <summary>
    /// Tab RESET BHERO SKIN. Quay lại skin + color trong một giao dịch. Contract đảm bảo skin mới
    /// KHÁC skin cũ; color thì không đảm bảo khác, nên UI không hứa hẹn gì về màu.
    ///
    /// Cả 10 rarity đều hỗ trợ, giá phẳng theo rarity (không luỹ tiến theo số lần reset).
    /// </summary>
    public class ResetSkinPolygon : NativeHeroActionPolygon {
        protected override string ActionName => "Reset Skin";

        protected override bool CanProcess(PlayerData hero) {
            return hero != null && hero.AccountType == HeroAccountType.Nft;
        }

        protected override Task<string> ReadPriceWei() {
            return BlockchainManager.GetResetSkinNativePrice(RarityOf(Hero));
        }

        protected override Task<HeroActionResult> SendAction(string priceWei) {
            return BlockchainManager.ResetSkin(Hero.heroId.Id, priceWei);
        }

        // Hero đã đổi chỉ số nên ô chọn được xoá đi. Không tự mở DialogNewHero ở đây: push
        // SYNC_HERO_RESPONSE đưa hero vừa đổi vào NewIds, SyncHeroController sẽ tự bật dialog đó —
        // mở thêm ở đây là hiện hai lần.
        protected override bool ClearSlotAfterSuccess => true;

        // Preload sprite của cặp (skin, color) mới TRƯỚC khi UI vẽ lại, vì HeroAnimator đọc clip
        // đồng bộ từ cache: cache lạnh thì không ra gì.
        protected override void OnActionSucceeded(PlayerData updated) {
            // Preload chỉ là tối ưu hiển thị: FarmingScene có thể không đăng ký loader này (Resolve
            // ném exception chứ không trả null), và thiếu nó không được phép làm hỏng một giao dịch
            // đã thành công.
            try {
                var loader = ServiceLocator.Instance.Resolve<IHeroSpriteLoader>();
                loader.Preload(updated.playerType, updated.playercolor,
                    PlayerStoreManager.GetHeroRarity(updated)).Forget();
            } catch (Exception e) {
                Debug.LogWarning($"Reset Skin: cannot preload new skin sprites: {e.Message}");
            }
        }
    }
}
