﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CurrencyManager : Singleton<CurrencyManager>
{
    private Action<double> EventSendCurrencyValue;
    private Action<double> EventSendPassiveCurrencyGainValue;
    private Action<TimeSpan, double> EventGainedOfflineCurrency;
    private Action<int> EventSendPremiumCurrencyValue;
    private Action<double, Vector3> EventSendActiveCurrencyGainValue;

    public Sprite spriteCurrency;
    public Sprite spritePremiumCurrency;
    [Space]
    public double currency;
    public int premiumCurrency;
    public double activeCurrencyGain;
    public List<Ship> ships = new List<Ship>();

    public void Update()
    {
        if (IsPlayerTapping())
        {
            TapBehaviour();
        }
    }

    public void InitData()
    {
        CanvasMain canvasMain = FindObjectOfType<CanvasMain>();
        CanvasOfflineEarning canvasOfflineEarning = FindObjectOfType<CanvasOfflineEarning>();

        canvasMain.InitData();

        AddCurrency(SaveManager.GetData().currency);
        AddPremiumCurrency(SaveManager.GetData().premiumCurrency);
        AddPassiveCurrency();

        SubscribeToEventGainedPassiveCurrency(canvasOfflineEarning.ShowPanel);
    }

    public void SaveData()
    {
        SaveManager.GetData().currency = currency;
        SaveManager.GetData().premiumCurrency = premiumCurrency;
    }

    public List<Ship> GetShips()
    {
        return ships;
    }

    public void AddShip(Ship ship)
    {
        ships.Add(ship);
    }

    public void AddCurrency(double value)
    {
        currency += value;
        EventSendCurrencyValue?.Invoke(currency);
        //Debug.Log(currency.ToString());
    }

    public void AddPremiumCurrency(int value)
    {
        premiumCurrency += value;
        EventSendPremiumCurrencyValue?.Invoke(premiumCurrency);
    }

    public void RemoveCurrency(double value)
    {

        currency -= value;
        EventSendCurrencyValue?.Invoke(currency);
        //Debug.Log(currency.ToString());
    }

    public void RemovePremiumCurrency(int value)
    {
        premiumCurrency -= value;
        EventSendPremiumCurrencyValue?.Invoke(premiumCurrency);
    }

    public void CalculateOfflineGain(TimeSpan timeOffline)
    {
        double secondsOffline = timeOffline.TotalSeconds;
        double currencyGained = (GetTotalPassiveCurrencyGain() * secondsOffline) / 3;

        if(currencyGained > 0)
        {
            EventGainedOfflineCurrency?.Invoke(timeOffline, currencyGained);
        }    
    }

    public void SubscribeToEventSendCurrency(Action<double> method)
    {
        EventSendCurrencyValue += method;
    }
    public void SubscribeToEventSendPremiumCurrency(Action<int> method)
    {
        EventSendPremiumCurrencyValue += method;
    }
    public void SubscribeToEventSendPassiveCurrencyGain(Action<double> method)
    {
        EventSendPassiveCurrencyGainValue += method;
    }
    public void SubscribeToEventSendActiveCurrencyGain(Action<double, Vector3> method)
    {
        EventSendActiveCurrencyGainValue += method;
    }
    public void SubscribeToEventGainedPassiveCurrency(Action<TimeSpan, double> method)
    {
        EventGainedOfflineCurrency += method;
    }


    private void AddPassiveCurrency()
    {
        StartCoroutine(PassiveCurrencyGain());
    }

    private bool IsPlayerTapping()
    {
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0);
    }

    private double GetTotalPassiveCurrencyGain()
    {
        double currencyGain = 0f;

        foreach (Ship ship in ships)
        {
            currencyGain += ship.GetTotalCurrencyGain();
        }

        return currencyGain;
    }

    private double GetActiveCurrencyGain()
    {
        double activeGain = Math.Round(GetTotalPassiveCurrencyGain() / 3);
        if (activeGain == 0)
            activeGain = 5;

        return activeGain;
    }

    private void TapBehaviour()
    {
        // passing a value of 0 makes it work on mobile but not on pc, so I have to use both
        if (EventSystem.current.IsPointerOverGameObject(0) || EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        else
        {
            double activeGain = GetActiveCurrencyGain();
            AddCurrency(activeGain);
            EventSendActiveCurrencyGainValue?.Invoke(activeGain, Input.mousePosition);

            if (GameManager.Instance.isVibrationOn)
                Vibration.VibrateSoft();

            Debug.Log($"Collected {activeGain} by tapping");
        }
    }

    private IEnumerator PassiveCurrencyGain()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            double totalPassiveCurrency = GetTotalPassiveCurrencyGain();
            EventSendPassiveCurrencyGainValue?.Invoke(totalPassiveCurrency);
            AddCurrency(totalPassiveCurrency);

            yield return null;
        }
    }
}
