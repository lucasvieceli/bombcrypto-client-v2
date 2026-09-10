using System;
using App;
using Cysharp.Threading.Tasks;
using Game.Dialog;
using Senspark;
using Share.Scripts.PrefabsManager;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.FarmingScene.Scripts {
    internal enum Smithy {
        RepairShield,
        UpgradeShield,
        Material,
        BuyMaterial,
        UpgradeHeroLevel,
        ResetSkill,
        ResetSkin
    }

    public class DialogSmithyPolygon : Dialog, IDialogRepairShield {
        [SerializeField]
        private GameObject[] items;
        
        [SerializeField]
        private Image[] shadow;

        private ISoundManager _soundManager;
        private IBHeroManager _playerStoreManager;
        private PlayerData _resetThisHero;
        private UserAccount _userAccount;
        private Smithy _smithy;

        public static UniTask<DialogSmithyPolygon> Create() {
            return ServiceLocator.Instance.Resolve<IPrefabLoaderManager>().Instantiate<DialogSmithyPolygon>();
        }

        protected override void Awake() {
            base.Awake();
            _soundManager = ServiceLocator.Instance.Resolve<ISoundManager>();
            _playerStoreManager = ServiceLocator.Instance.Resolve<IBHeroManager>();
            _userAccount = ServiceLocator.Instance.Resolve<IUserAccountManager>().GetRememberedAccount();
        }

        public void OpenRepairShieldDialog() {
            _soundManager.PlaySound(Audio.Tap);
            ShowRepairShieldDialog();
        }

        public void OpenMaterialDialog() {
            _soundManager.PlaySound(Audio.Tap);
            _smithy = Smithy.Material;
            ShowItems(items[(int) _smithy]);
            ShowButtons((int) _smithy);
            var material = items[(int) _smithy].GetComponent<MaterialPolygon>();
            material.SetInfo(DialogCanvas);
        }

        public void OpenUpgradeShieldDialog() {
            _soundManager.PlaySound(Audio.Tap);

            // if (_userAccount.network == NetworkType.Binance) {
            //     DialogOK.ShowInfo(DialogCanvas, "Coming soon");
            //     return;
            // }
            
            //Tạm khoá tính năng nâng cấp shield
            // DialogOK.ShowInfo(DialogCanvas, "Coming soon");
            // return;

            _smithy = Smithy.UpgradeShield;
            ShowItems(items[(int) _smithy]);
            ShowButtons((int) _smithy);
            var upgradeShield = items[(int) _smithy].GetComponent<UpgradeShieldPolygon>();
            upgradeShield.Init(_resetThisHero);
            upgradeShield.SetInfo(DialogCanvas, OnChooseHero);
        }

        /// <summary>
        /// Mở Smithy thẳng vào tab UPGRADE BHERO LEVEL với hero đã chọn sẵn. Dùng cho nút Upgrade
        /// trong DialogInventory — trước đây nút đó trỏ vào DialogUpgrade, prefab và script đều đã
        /// bị xoá.
        /// </summary>
        public void InitForUpgradeHeroLevel(HeroId idHero) {
            SetResetThisHero(_playerStoreManager.GetPlayerDataFromId(idHero));
            OnDidShow(OpenUpgradeHeroLevelDialog);
        }

        public void Init(HeroId idResetThisHero) {
            var resetThisHero = _playerStoreManager.GetPlayerDataFromId(idResetThisHero);
            SetResetThisHero(resetThisHero);
            OnDidShow(ShowRepairShieldDialog);
        }

        public Dialog OnDidHide(Action action) {
            base.OnDidHide(action);
            return this;
        }

        private void ShowItems(GameObject dialog) {
            for (var i = 0; i < items.Length; i++) {
                items[i].gameObject.SetActive(false);
                if (dialog == items[i]) {
                    items[i].gameObject.SetActive(true);
                }
            }
        }

        private void ShowButtons(int button) {
            for (var i = 0; i < shadow.Length; i++) {
                shadow[i].gameObject.SetActive(true);
                if (button == i) {
                    shadow[i].gameObject.SetActive(false);
                }
            }
        }

        private void ShowRepairShieldDialog() {
            _smithy = Smithy.RepairShield;
            ShowItems(items[(int) _smithy]);
            ShowButtons((int) _smithy);
            var repairShield = items[(int) _smithy].GetComponent<RepairShieldPolygon>();
            repairShield.Init(_resetThisHero);
            repairShield.SetInfo(DialogCanvas, OnChooseHero);
        }

        private void OnChooseHero(PlayerData resetThisHero) {
            SetResetThisHero(resetThisHero);
        }

        private void SetResetThisHero(PlayerData resetThisHero) {
            _resetThisHero = resetThisHero;
            if (_resetThisHero != null) {
                var newHero = _playerStoreManager.GetPlayerDataFromId(_resetThisHero.heroId);
                _resetThisHero = newHero;
            }
        }
        
        public void OpenUpgradeHeroLevelDialog() {
            OpenNativeActionTab<UpgradeHeroLevelPolygon>(Smithy.UpgradeHeroLevel);
        }

        public void OpenResetSkillDialog() {
            OpenNativeActionTab<ResetSkillPolygon>(Smithy.ResetSkill);
        }

        public void OpenResetSkinDialog() {
            OpenNativeActionTab<ResetSkinPolygon>(Smithy.ResetSkin);
        }

        // Ba tab trả bằng native dùng chung một hợp đồng (SetInfo + Init) nên mở giống hệt nhau.
        // Prefab iPad không còn được phát triển và không có ba tab này, nên bỏ qua trong im lặng
        // thay vì ném lỗi khi mảng items ngắn.
        private void OpenNativeActionTab<T>(Smithy tab) where T : NativeHeroActionPolygon {
            var index = (int) tab;
            if (index >= items.Length) {
                return;
            }
            _soundManager.PlaySound(Audio.Tap);
            _smithy = tab;
            ShowItems(items[index]);
            ShowButtons(index);
            var panel = items[index].GetComponent<T>();
            if (!panel) {
                return;
            }
            panel.SetInfo(DialogCanvas, OnChooseHero);
            panel.Init(_resetThisHero);
        }

        public void OpenBuyMaterialDialog() {
            _soundManager.PlaySound(Audio.Tap);
            _smithy = Smithy.BuyMaterial;
            ShowItems(items[(int) _smithy]);
            ShowButtons((int) _smithy);
            var buyRock = items[(int) _smithy].GetComponent<BuyRock>();
            buyRock.SetInfo(DialogCanvas);
        }
        
        protected override void OnYesClick() {
            // Do nothing
        }

        public void OnBackBtn() {
            _soundManager.PlaySound(Audio.Tap);
            Hide();
        }
    }
}