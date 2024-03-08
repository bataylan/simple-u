using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SimpleU.Pattern.Bridge
{
    internal class BridgePattern
    {
        //split into two seperate hierarchy, 
        //abstraction and implementation
        private class BridgeVisualController
        {
            private IBridgeVisual _bridgeVisual;

            public void OnUpdate(int count)
            {
                _bridgeVisual.Update(count, CultureInfo.CurrentCulture);
            }
        }

        private interface IBridgeVisual
        {
            public void Update(int count, CultureInfo cultureInfo);
        }

        private class BridgeMoneyButton : IBridgeVisual
        {
            public TMPro.TMP_Text buttonText;

            public void Update(int money, CultureInfo cultureInfo)
            {
                buttonText.text = money.ToString("C", cultureInfo);
            } 
        }

        private class BridgeHourButton : IBridgeVisual
        {
            public TMPro.TMP_Text hourText;

            public void Update(int hours, CultureInfo cultureInfo)
            {
                var datetTime = new DateTime().AddHours(hours);
                hourText.text = datetTime.ToString(cultureInfo);
            } 
        }
    }
}