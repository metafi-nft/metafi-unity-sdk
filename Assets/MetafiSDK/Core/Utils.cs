using System;
using System.Numerics;
using UnityEngine;

namespace Metafi.Unity {
    public static class Utils {
        public static string ConvertNumberToBigNumber(int value, int decimals) {
            Debug.Log("1, value: " + value.ToString());
            BigInteger res = BigInteger.Multiply(new BigInteger(value), BigInteger.Pow(10, decimals));
            return res.ToString();
        }

        public static string ConvertNumberToBigNumber(double value, int decimals) {
            Debug.Log("2, value: " + value.ToString());
            BigInteger res = new BigInteger(value * Math.Pow(10, decimals));
            return res.ToString();
        }

        public static string ConvertNumberToBigNumber(float value, int decimals) {
            Debug.Log("3, value: " + value.ToString());
            return ConvertNumberToBigNumber(Convert.ToDouble(value), decimals);
        }
    }
}
