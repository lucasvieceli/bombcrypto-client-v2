using Animation;

using App;

using UnityEngine;
using UnityEngine.UI;

namespace Game.Dialog {
    /// <summary>
    /// Một ô chọn hero trong Smithy: avatar + backlight theo rarity + id, và trạng thái rỗng
    /// (dấu cộng). Tách riêng vì tab Upgrade cần hai ô giống hệt nhau (hero gốc + nguyên liệu).
    /// </summary>
    public class SmithyHeroSlot : MonoBehaviour {
        [SerializeField]
        private Avatar avatar;

        [SerializeField]
        private Image backlight;

        [SerializeField]
        private Text heroIdLbl;

        [SerializeField]
        private GameObject groupPlus;

        [SerializeField]
        private GameObject avatarGroup;

        public async void Show(PlayerData hero) {
            if (hero == null) {
                Clear();
                return;
            }
            if (heroIdLbl) {
                heroIdLbl.text = hero.heroId.Id.ToString();
            }
            if (avatar) {
                avatar.ChangeImage(hero);
            }
            if (groupPlus) {
                groupPlus.SetActive(false);
            }
            if (avatarGroup) {
                avatarGroup.SetActive(true);
            }
            if (!backlight) {
                return;
            }
            var sprite = await AnimationResource.GetBacklightImageByRarity(hero.rare, true);
            // Người chơi có thể đã đổi hero khác trong lúc chờ sprite load.
            if (!this) {
                return;
            }
            backlight.sprite = sprite;
            backlight.enabled = true;
        }

        public void Clear() {
            if (heroIdLbl) {
                heroIdLbl.text = string.Empty;
            }
            if (avatar) {
                avatar.HideImage();
            }
            if (backlight) {
                backlight.sprite = null;
                backlight.enabled = false;
            }
            if (groupPlus) {
                groupPlus.SetActive(true);
            }
            if (avatarGroup) {
                avatarGroup.SetActive(false);
            }
        }
    }
}
