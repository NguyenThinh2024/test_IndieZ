using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Thinh.Base
{
    public class GameUtils 
    {
        public static string ConvertMoneyDotFormatted(Int64 money, string dot = ",")
        {
            var moneyStr = money.ToString();
            var reversedStr = new string(moneyStr.Reverse().ToArray());
            var result = string.Join(dot, Enumerable.Range(0, reversedStr.Length / 3 + (reversedStr.Length % 3 == 0 ? 0 : 1))
                                                     .Select(i => reversedStr.Substring(i * 3, Math.Min(3, reversedStr.Length - i * 3))));
            return new string(result.Reverse().ToArray());
        }

        public static string ConvertMoneyShortFormatted(Int64 money)
        {
            if (money >= 1_000_000_000)
            {
                return (money / 1_000_000_000D).ToString("0.##") + "B";
            }
            else if (money >= 1_000_000)
            {
                return (money / 1_000_000D).ToString("0.##") + "M";
            }
            else if (money >= 100_000)
            {
                return (money / 1_000D).ToString("0.##") + "K";
            }
            else
            {
                return ConvertMoneyDotFormatted(money);
            }
        }

        public static string ConvertRemainTimeMMSS(float remainTime)
        {
            //int hours = Mathf.FloorToInt(remainTime / 3600);
            int minutes = Mathf.FloorToInt((remainTime % 3600) / 60);
            int seconds = Mathf.FloorToInt(remainTime % 60);
            return $"{minutes:D2}:{seconds:D2}";
        }

        public static string ConvertRemainTimeHHMMSS(float remainTime)
        {
            int hours = Mathf.FloorToInt(remainTime / 3600);
            int minutes = Mathf.FloorToInt((remainTime % 3600) / 60);
            int seconds = Mathf.FloorToInt(remainTime % 60);
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        public static void GetCountryName(out string countryCode, out string countryName)
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            RegionInfo regionInfo = new RegionInfo(currentCulture.Name);
            countryCode = regionInfo.TwoLetterISORegionName; // ISO 3166-1 alpha-2 code (e.g., "US", "VN")
            countryName = regionInfo.EnglishName; // Full country name in English (e.g., "United States", "Vietnam")
            Debug.Log("Device Country Code: " + countryCode);
            Debug.Log("Device Country Name: " + countryName);
        }
        
        public static int CountZeroes(Int64 number)
        {
            int zeroCount = 0;
    
            // Đếm số 0 ở cuối
            while (number % 10 == 0 && number != 0)
            {
                zeroCount++;
                number /= 10;
            }
    
            // Trả về số chữ số 0 cuối trừ đi 1 (như yêu cầu)
            return zeroCount > 0 ? zeroCount - 1 : 0;
        } 
        
        public static Int64 GetGoodInt(Int64 input)
        {
           
            // Get the highest power of 10 less than or equal to input
            Int64 divisor = (Int64)Math.Pow(10, CountZeroes(input));

            // Multiply back to return the rounded down value
            return (input / divisor) * divisor;
        }
        
        public static Vector3 GetControlPoint(Vector3 pointA, Vector3 pointB, float offset)
        {
            // Tìm điểm giữa của A và B
            Vector3 middlePoint = (pointA + pointB) / 2f;

            // Vector từ A đến B
            Vector3 direction = (pointB - pointA).normalized;

            // Tính vector vuông góc (đối với 2D thì xoay 90 độ)
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);

            // Tạo điểm điều khiển, di chuyển điểm giữa theo vector vuông góc với khoảng cách offset
            Vector3 controlPoint = middlePoint + perpendicular * offset;

            return controlPoint;
        }

        public static Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePoint = Input.mousePosition;
            mousePoint.z = Camera.main.nearClipPlane;
            return Camera.main.ScreenToWorldPoint(mousePoint);
        }
    }
}

