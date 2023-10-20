using System;
using System.Numerics;
public static class BigIntegerExtensions
{
	private static readonly string[] units = new string[]
	{
		"", // 0 (단위 없음)
		"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
		"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
	};
	public static string WithUnit(this BigInteger value)
	{
		if (value < 0) throw new ArgumentOutOfRangeException();
		double v = BigInteger.Log10(value); // 로그 값
		v = Math.Round(v * 1000) / 1000;  // 결과의 부동소수점 오차 보정. 최대 3자리만 표현하므로 올려도 무방
		int n = (int)v, // 자릿수
			u = n / 3, // 단위
			d = 2 - (n % 3); // 남길 소수점 자릿수 (0 ~ 2)
		if (u < units.Length) // "" ~ "Z"
		{
			var r = Math.Pow(10, v - (u * 3)); // 단위에 맞게 자른 값
			return r.ToString($"F{d}") + units[u];
		}
		else // 표기할 단위 없음 -> 0.00E+00 형식으로 표기
		{
			var r = Math.Pow(10, v - n);
			return r.ToString("F2") + $"E+{n}";
		}
	}
}

public static class DoubleExtensions
{
	public static int RoundToInt(this double value) => (int)Math.Round(value);
}