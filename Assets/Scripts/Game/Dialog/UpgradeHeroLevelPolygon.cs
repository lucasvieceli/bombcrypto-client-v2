using System;
using System.Linq;
using System.Threading.Tasks;

using App;

using Cysharp.Threading.Tasks;

using Game.UI;

using Scenes.FarmingScene.Scripts;

using Senspark;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialog {
    /// <summary>
    /// Tab UPGRADE BHERO LEVEL. Đốt một hero nguyên liệu CÙNG LEVEL với hero gốc để lên một bậc,
    /// tối đa level 5. Luật cũ không đổi, chỉ đổi cách trả tiền sang native.
    /// </summary>
    public class UpgradeHeroLevelPolygon : NativeHeroActionPolygon {
        private const int MaxLevel = 5;

        [SerializeField]
        private SmithyHeroSlot materialSlot;

        [SerializeField]
        private Button chooseMaterialBtn;

        [SerializeField]
        private Text levelLbl;

        [SerializeField]
        private Text nextLevelLbl;

        private PlayerData _material;

        protected override string ActionName => "Upgrade";

        protected override bool CanProcess(PlayerData hero) {
            return hero != null
                   && hero.AccountType == HeroAccountType.Nft
                   && hero.level < MaxLevel;
        }

        // Giá tính theo bậc SẮP lên, level truyền cho contract là index 0-based.
        protected override Task<string> ReadPriceWei() {
            return BlockchainManager.GetUpgradeNativePrice(RarityOf(Hero), Hero.level - 1);
        }

        protected override Task<HeroActionResult> SendAction(string priceWei) {
            return BlockchainManager.UpgradeHero(Hero.heroId.Id, _material.heroId.Id, priceWei);
        }

        // Nguyên liệu đang có stake BCOIN/SEN thì stake đó cháy theo hero. Cảnh báo trước khi ký,
        // cùng dialog mà luồng đốt hero ở MaterialPolygon đang dùng.
        protected override void RequestConfirmation(Action onConfirmed) {
            if (_material == null || !_material.HaveAnyStaked()) {
                onConfirmed();
                return;
            }
            UniTask.Void(async () => {
                var confirm = await DialogConfirmBurnOrFusion.Create();
                if (!this) {
                    return;
                }
                confirm.SetInfo(1, onConfirmed, () => { });
                confirm.Show(Canvas);
            });
        }

        // Hero nguyên liệu đã bị đốt on-chain và không có push nào báo việc đó, nên phải tự gỡ
        // khỏi store, khỏi map và trừ vào sức chứa — đúng bộ ba mà luồng đốt hero ở
        // MaterialPolygon đang làm. Chạy trước khi Init() clear _material.
        protected override void OnActionSucceeded(PlayerData updated) {
            if (_material == null) {
                return;
            }
            var burned = new[] { _material.heroId };
            PlayerStoreManager.RemoveBurnHeroes(burned);
            PlayerStoreManager.AdjustTotalHeroesSize(-1);
            // Forge mở được cả ngoài map, nên phải guard Instance.
            if (LevelScene.Instance) {
                LevelScene.Instance.RemoveHeroesFromMap(burned);
            }
        }

        protected override void OnHeroChanged() {
            // Đổi hero gốc thì nguyên liệu cũ không còn chắc cùng level nữa -> clear luôn.
            SetMaterial(null);
            if (levelLbl) {
                levelLbl.text = Hero != null ? Hero.level.ToString() : string.Empty;
            }
            if (nextLevelLbl) {
                nextLevelLbl.text = Hero != null ? (Hero.level + 1).ToString() : string.Empty;
            }
            // Ô nguyên liệu chỉ xuất hiện sau khi đã chọn hero gốc, vì nó lọc theo level hero gốc.
            ShowMaterialUi(Hero != null);
        }

        private void ShowMaterialUi(bool visible) {
            if (materialSlot) {
                materialSlot.gameObject.SetActive(visible);
            }
            if (chooseMaterialBtn) {
                chooseMaterialBtn.gameObject.SetActive(visible);
            }
        }

        public async void OnChooseMaterialBtnClicked() {
            if (Hero == null) {
                return;
            }
            SoundManager.PlaySound(Audio.Tap);
            var inventory = await DialogInventoryCreator.Create();
            if (!this) {
                return;
            }
            // Exclude list được áp TRƯỚC khi phân trang, khác với FilterHeroesSuitableToUpgrade()
            // vốn chỉ lọc trong phạm vi trang hiện tại. Vẫn dùng ChooseMode.Upgrade để InventoryItem
            // bật cảnh báo hero đang stake khi chọn — nguyên liệu sẽ bị đốt.
            var exclude = PlayerStoreManager.GetPlayerDataList(HeroAccountType.Nft)
                .Where(e => !IsUsableAsMaterial(e))
                .Select(e => e.heroId)
                .ToArray();
            inventory.SetChooseHeroForUpgrade(Hero.heroId, Hero.level, exclude, heroId => {
                SetMaterial(PlayerStoreManager.GetPlayerDataFromId(heroId));
            });
            inventory.Show(Canvas);
        }

        // Nguyên liệu phải là hero NFT khác, CÙNG level với hero gốc. Luật cũ không đổi.
        private bool IsUsableAsMaterial(PlayerData hero) {
            return hero != null
                   && hero.AccountType == HeroAccountType.Nft
                   && hero.heroId != Hero.heroId
                   && hero.level == Hero.level;
        }

        private void SetMaterial(PlayerData material) {
            _material = material;
            if (materialSlot) {
                materialSlot.Show(_material);
            }
            RefreshQuote();
        }

        // Chưa chọn nguyên liệu thì chưa cho bấm, dù giá đã đọc được.
        protected override void OnQuoteApplied(bool hasPrice) {
            if (hasPrice && _material == null) {
                SetInteractable(false);
            }
        }
    }
}
