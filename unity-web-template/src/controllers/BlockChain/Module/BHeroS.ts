import GeneralContract from './Utils/GeneralContract.js';
import { getDoubleGasFeeOptionV2, waitForReceipt } from "./Utils/NetworkUtils.js";
import { ethers, Contract } from "ethers";
import CoinToken from "./CoinToken.ts";
import BHeroToken from "./BHero.ts";
import {toNumberOrZero} from "../../../utils/Number.ts";

export interface HeroActionResult {
    success: boolean;
    txHash: string;
    details: string;
}

export default class BHeroSToken extends GeneralContract {
    private _bheroToken: BHeroToken;
    private _bcoinToken: CoinToken;
    private _coinCost?: bigint;
    private _senCost?: bigint;

    constructor(bcoinToken: CoinToken, _sensparkToken: CoinToken, bheroToken: BHeroToken, address: string, abi: JSON) {
        super(address, abi);
        this._bheroToken = bheroToken;
        this._bcoinToken = bcoinToken;
    }

    async getDesignContract(): Promise<Contract> {
        return this._bheroToken.getDesignContract();
    }

    async getHeroPrice(): Promise<{ coin: string, sen: string }> {
        const coin = await this.getMintCost();
        const sen = await this.getSenMintCost();
        return { coin, sen };
    }

    async getHeroPriceFormatted(): Promise<{ coin: number, sen: number }> {
        const coin = await this.getMintCost();
        const sen = await this.getSenMintCost();
        return {
            coin: parseFloat(coin) / 10 ** 18,
            sen: parseFloat(sen) / 10 ** 18
        };
    }

    async getMintCost(): Promise<string> {
        if (this._coinCost) {
            return this._coinCost.toString();
        }
        const contract = await this.getDesignContract();
        const value = await contract.getMintCostHeroS();
        this._coinCost = value;
        return value.toString();
    }

    async getSenMintCost(): Promise<string> {
        if (this._senCost) {
            return this._senCost.toString();
        }
        const contract = await this.getDesignContract();
        const value = await contract.getSenMintCostHeroS();
        this._senCost = value;
        return value.toString();
    }

    async getAmountRock(walletAddress: string): Promise<number> {
        try {
            const contract = await this.getContract();
            const valuePromise = contract.getTotalRockByUser(walletAddress);
            const timeoutPromise = new Promise<number>((resolve) => {
                setTimeout(() => {
                    resolve(0); // Resolve with 0 after 5 seconds
                }, 5000);
            });

            const value = await Promise.race([valuePromise, timeoutPromise]);
            return toNumberOrZero(value);
        } catch (error) {
            console.error("Error in getAmountRock:", error);
            return 0;
        }
    }

    async mint(userAddress: string, count: number): Promise<boolean> {
        try {
            const cost = await this.getHeroPrice();
            const countBN = ethers.toBigInt(count * 2);
            const coinBN = ethers.toBigInt(cost.coin) * countBN;
            await this._bcoinToken.checkAllowance(userAddress, this._address, coinBN);

            const contract = await this.getContract();
            const estimateGas = await contract.mint.estimateGas(count);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.mint(count, options);
            await waitForReceipt(transaction);
            return true;
        } catch (ex) {
            console.error(`exception ${ex}`);
            return false;
        }
    }

    async burnFusion(heroIds: number[]): Promise<boolean> {
        try {
            const contract = await this.getContract();
            const estimateGas = await contract.burnListToken.estimateGas(heroIds);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.burnListToken(heroIds, options);
            await waitForReceipt(transaction);
            return true;
        } catch (ex) {
            console.error(`exception ${ex}`);
            return false;
        }
    }

    async fusion(mainMaterials: number[], buffMaterials: number[]): Promise<boolean> {
        try {
            const contract = await this.getContract();
            const estimateGas = await contract.fusion.estimateGas(mainMaterials, buffMaterials);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.fusion(mainMaterials, buffMaterials, options);
            await waitForReceipt(transaction);
            return true;
        } catch (ex) {
            console.error(`exception ${ex}`);
            return false;
        }
    }

    async burnRepairShield(idHeroS: number, listHeroIds: number[]): Promise<boolean> {
        try {
            const contract = await this.getContract();
            const estimateGas = await contract.burnResetShield.estimateGas(idHeroS, listHeroIds);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.burnResetShield(idHeroS, listHeroIds, options);
            await waitForReceipt(transaction);
            return true;
        } catch (ex) {
            console.error(`exception ${ex}`);
            return false;
        }
    }

    async createRock(listHeroIds: number[]): Promise<string> {
        try {
            const contract = await this.getContract();
            const estimateGas = await contract.createRock.estimateGas(listHeroIds);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.createRock(listHeroIds, options);
            const receipt = await waitForReceipt(transaction);
            if(!receipt) {
                return "";
            }
            //FIXME: có thể sai (TransactionHash from v5 to hash v6)
            return receipt.hash.toString();
        } catch (ex) {
            console.error(`exception ${ex}`);
            return "";
        }
    }

    async resetShieldHeroS(idHero: number, amountRock: number): Promise<boolean> {
        try {
            const contract = await this.getContract();
            const estimateGas = await contract.resetShieldHeroS.estimateGas(idHero, amountRock);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.resetShieldHeroS(idHero, amountRock, options);
            await waitForReceipt(transaction);
            return true;
        } catch (ex) {
            console.error(`exception ${ex}`);
            return false;
        }
    }

    async upgradeShieldLevel(idHero: number, amountRock: number): Promise<boolean> {
        try {
            const contract = await this.getContract();
            const estimateGas = await contract.upgradeShieldLevel.estimateGas(idHero, amountRock);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.upgradeShieldLevel(idHero, amountRock, options);
            await waitForReceipt(transaction);
            return true;
        } catch (ex) {
            console.error(`exception ${ex}`);
            return false;
        }
    }

    // ── Native-paid BHero actions (upgrade / reset skill / reset skin) ────────────────────
    // All three are payable on BHeroS with a strict require(msg.value == price) and no refund of
    // excess, so priceWei is relayed verbatim as an exact wei string and must have been read
    // immediately before signing. The details word is re-read after the receipt so the caller can
    // render the result without waiting for the server sync.

    async getUpgradeNativePrice(rarity: number, level: number): Promise<string> {
        const contract = await this.getDesignContract();
        const value = await contract.getUpgradeNativePrice(rarity, level);
        return value.toString();
    }

    async getResetSkillNativePrice(rarity: number, times: number): Promise<string> {
        const contract = await this.getDesignContract();
        const value = await contract.getResetSkillNativePrice(rarity, times);
        return value.toString();
    }

    async getResetSkinNativePrice(rarity: number): Promise<string> {
        const contract = await this.getDesignContract();
        const value = await contract.getResetSkinNativePrice(rarity);
        return value.toString();
    }

    async getNativeRate(): Promise<string> {
        const contract = await this.getDesignContract();
        const value = await contract.getNativeRate();
        return value.toString();
    }

    async upgradeHero(baseId: number, materialId: number, priceWei: string): Promise<HeroActionResult> {
        return this.sendHeroAction(baseId, priceWei,
            (contract, value) => contract.upgradeHero.estimateGas(baseId, materialId, { value }),
            (contract, options) => contract.upgradeHero(baseId, materialId, options));
    }

    async resetSkill(heroId: number, priceWei: string): Promise<HeroActionResult> {
        return this.sendHeroAction(heroId, priceWei,
            (contract, value) => contract.resetSkill.estimateGas(heroId, { value }),
            (contract, options) => contract.resetSkill(heroId, options));
    }

    async resetSkin(heroId: number, priceWei: string): Promise<HeroActionResult> {
        return this.sendHeroAction(heroId, priceWei,
            (contract, value) => contract.resetSkin.estimateGas(heroId, { value }),
            (contract, options) => contract.resetSkin(heroId, options));
    }

    private async sendHeroAction(heroId: number, priceWei: string,
                                 estimate: (contract: Contract, value: bigint) => Promise<bigint>,
                                 send: (contract: Contract, options: object) => Promise<any>
    ): Promise<HeroActionResult> {
        try {
            const value = BigInt(priceWei);
            const contract = await this.getContract();
            const estimateGas = await estimate(contract, value);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await send(contract, { ...options, value });
            const txHash = transaction.hash ?? "";
            await waitForReceipt(transaction);
            const details = await this._bheroToken.getTokenDetail(heroId);
            return { success: true, txHash, details };
        } catch (ex) {
            console.error(`exception ${ex}`);
            return { success: false, txHash: "", details: "" };
        }
    }

    async upgradeShieldLevelV2(idHero: number, nonce: number, signature: string): Promise<boolean> {
        try {
            const contract = await this.getContract();
            const estimateGas = await contract.upgradeShieldLevel.estimateGas(idHero, nonce, signature);
            const options = await getDoubleGasFeeOptionV2(estimateGas);
            const transaction = await contract.upgradeShieldLevel(idHero, nonce, signature, options);
            await waitForReceipt(transaction);
            return true;
        } catch (ex) {
            console.error(`exception ${ex}`);
            return false;
        }
    }
}