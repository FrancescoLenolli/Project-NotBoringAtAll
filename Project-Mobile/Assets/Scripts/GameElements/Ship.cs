﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Ship : Collectible
{
    private Action<ShipData> EventUnlockNewShip;
    private Action<ShipData> EventSpawnShipModel;
    private Action<ShipData> EventShowInfo;
    private Action<Ship> EventSpawnUpgrades;

    private ShipsManager shipsManager;
    private bool isNextShipUnlocked;
    private bool canAutoBuy;
    private bool isButtonHeld;
    private List<UpgradeInfo> upgradesInfo = new List<UpgradeInfo>();

    [SerializeField] private ShipButtonSound shipSound = null;
    [SerializeField] private TextMeshProUGUI textShipName = null;
    [SerializeField] private TextMeshProUGUI textShipCost = null;
    [SerializeField] private TextMeshProUGUI textShipTotalCurrencyGain = null;
    [SerializeField] private TextMeshProUGUI textShipUnitCurrencyGain = null;
    [SerializeField] private TextMeshProUGUI textShipQuantity = null;
    [SerializeField] private Image imageShipIcon = null;
    [SerializeField] private Button buttonBuy = null;

    [HideInInspector] public ShipData shipData;
    [HideInInspector] public List<UpgradeInfo> UpgradesInfo { get => upgradesInfo; }

    public void InitData(ShipInfo shipInfo, ShipsManager shipsManager, CanvasBottom canvasBottom)
    {
        shipData = shipInfo.shipData;
        Quantity = shipInfo.quantity;
        gameObject.name = shipData.name;
        SetUpgradesInfo(shipInfo.upgradesInfo);
        SetWeight(shipData.index + 1);
        SetBaseCost();
        SetBaseCurrencyGain();
        SetCost();
        SetTotalCurrencyGain();

        textShipName.text = shipData.name;
        textShipUnitCurrencyGain.text = $"+ {Formatter.FormatValue(GetUnitCurrencyGain())}/s";
        textShipTotalCurrencyGain.text = $"+ {Formatter.FormatValue(TotalCurrencyGain)}/s";
        imageShipIcon.sprite = shipData.icon;

        SetTextQuantity();

        // No need to subscribe to EventUnlockNewShip if new ship is already unlocked.
        if (!IsQuantityEnough())
        {
            isNextShipUnlocked = false;
            this.shipsManager = shipsManager;
            SubscribeToEventSendData(shipsManager.UnlockNewShip);
        }
        else
        {
            isNextShipUnlocked = true;
        }

        if (Quantity > 0)
        {
            SubscribeToEventSpawnShip(shipsManager.SpawnShipModel);
            EventSpawnShipModel?.Invoke(shipData);
            UnsubscribeToEventSpawnShip(shipsManager.SpawnShipModel);
        }
        else
        {
            SubscribeToEventSpawnShip(shipsManager.SpawnShipModel);
            SubscribeToEventSpawnShip(shipSound.PlayShipUnlockedSound);
        }

        SubscribeToEventSpawnUpgrades(canvasBottom.SpawnUpgrades);
        SubscribeToEventShowInfo(canvasBottom.PanelInfo.ShowInfo);
        CurrencyManager.Instance.SubscribeToEventSendCurrency(SetButtonBuyStatus);

        canAutoBuy = false;
        StartCoroutine(AutoBuy());
    }

    public override void Buy()
    {
        if (CanBuy())
        {
            if (!GameManager.Instance.isTesting)
                CurrencyManager.Instance.RemoveCurrency(cost);

            shipSound.PlaySoundDefault();

            ++Quantity;
            if (Quantity > 0 && !isNextShipUnlocked)
            {
                EventSpawnShipModel?.Invoke(shipData);
                UnsubscribeToEventSpawnShip(shipsManager.SpawnShipModel);
                UnsubscribeToEventSpawnShip(shipSound.PlayShipUnlockedSound);
            }
            if (Quantity == 1)
            {
                EventSpawnUpgrades?.Invoke(this);
            }

            SetCost();
            SetTotalCurrencyGain();
            SetTextQuantity();

            if (!isNextShipUnlocked && IsQuantityEnough())
            {
                EventUnlockNewShip?.Invoke(shipData);
                UnsubscribeToEventSendData(shipsManager.UnlockNewShip);
                isNextShipUnlocked = true;
                canAutoBuy = false;
            }

            Vibration.VibrateSoft();
        }
    }

    public void UpgradeBought(UpgradeData upgradeData)
    {
        for (int i = 0; i < upgradesInfo.Count; ++i)
        {
            if (upgradesInfo[i].upgradeData == upgradeData)
            {
                upgradesInfo[i] = new UpgradeInfo(upgradesInfo[i].index, upgradesInfo[i].upgradeData, true);
                SetTotalCurrencyGain();
                return;
            }
        }
    }

    public void StartAutoBuy()
    {
        isButtonHeld = true;
        StartCoroutine(AutoBuyStartingDelay());
    }

    public void StopAutoBuy()
    {
        canAutoBuy = false;
        isButtonHeld = false;
    }

    public void ShowInfo()
    {
        EventShowInfo?.Invoke(shipData);
    }


    private bool IsQuantityEnough()
    {
        return Quantity >= shipData.qtForNextShip;
    }

    protected override double GetUnitCurrencyGain()
    {
        double currencyGain = baseCurrencyGain;
        List<UpgradeData> upgrades = shipData.upgrades;
        float totalUpgradesPct = 0f;

        foreach (UpgradeInfo info in upgradesInfo.Where(x => x.isOwned))
        {
            totalUpgradesPct += info.upgradeData.upgradePercentage;
        }

        double newCurrencyGain = totalUpgradesPct == 0f ? currencyGain : currencyGain + MathUtils.Pct(totalUpgradesPct, currencyGain);

        return newCurrencyGain;
    }

    protected override void SetBaseCurrencyGain()
    {
        double pow = (shipData.index * 1.1f);
        double multiplier = Math.Pow(shipData.index + 1, pow);
        baseCurrencyGain = (CurrencyManager.Instance.data.baseShipCurrencyGain * multiplier) / 2.8;
    }

    protected override void SetTotalCurrencyGain()
    {
        double unitCurrencyGain = GetUnitCurrencyGain();
        TotalCurrencyGain = unitCurrencyGain * Quantity;

        textShipUnitCurrencyGain.text = $"+ {Formatter.FormatValue(GetUnitCurrencyGain())}/s";
        textShipTotalCurrencyGain.text = $"+ {Formatter.FormatValue(TotalCurrencyGain)}/s";
    }

    protected override void SetBaseCost()
    {
        double multiplier = Math.Pow(shipData.index + 1, shipData.index + 1);
        baseCost = CurrencyManager.Instance.data.baseShipCost * multiplier;
    }

    protected override void SetCost()
    {
        double multiplier = Math.Pow(shipData.costIncreaseMultiplier, Quantity);
        cost = (baseCost * multiplier) / 1.1;
        SetTextCost();
    }

    private void SetUpgradesInfo(List<UpgradeInfo> upgradesInfo)
    {
        if (upgradesInfo.Count == 0)
        {
            shipData.upgrades.ForEach(upgrade => this.upgradesInfo.Add(new UpgradeInfo(upgrade.index, upgrade, false)));
        }
        else
        {
            this.upgradesInfo = upgradesInfo;
        }
    }

    private void SetTextQuantity()
    {
        textShipQuantity.text = $"x{Quantity}";
    }

    private void SetTextCost()
    {
        textShipCost.text = Formatter.FormatValue(cost);
    }

    private void SetButtonBuyStatus(double totalCurrency)
    {
        buttonBuy.interactable = cost <= totalCurrency;
    }

    private IEnumerator AutoBuy()
    {
        while (true)
        {
            if (canAutoBuy)
            {
                yield return new WaitForSeconds(.05f);
                Buy();
                yield return null;
            }

            yield return null;
        }
    }

    private IEnumerator AutoBuyStartingDelay()
    {
        yield return new WaitForSeconds(1.0f);

        if (isButtonHeld)
            canAutoBuy = true;

        yield return null;
    }


    private void SubscribeToEventSendData(Action<ShipData> method)
    {
        EventUnlockNewShip += method;
    }

    private void UnsubscribeToEventSendData(Action<ShipData> method)
    {
        EventUnlockNewShip -= method;
    }

    private void SubscribeToEventSpawnShip(Action<ShipData> method)
    {
        EventSpawnShipModel += method;
    }

    private void UnsubscribeToEventSpawnShip(Action<ShipData> method)
    {
        EventSpawnShipModel -= method;
    }

    private void SubscribeToEventSpawnUpgrades(Action<Ship> method)
    {
        EventSpawnUpgrades += method;
    }

    private void SubscribeToEventShowInfo(Action<ShipData> method)
    {
        EventShowInfo += method;
    }
}
