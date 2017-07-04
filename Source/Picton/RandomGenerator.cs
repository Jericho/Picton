﻿using System;
using System.Linq;
using System.Security.Cryptography;

namespace Picton
{
	public static class RandomGenerator
	{
		#region PUBLIC METHODS

		/// <summary>
		/// This method simulates a roll of the dice. The input parameter is the
		/// number of sides of the dice.
		/// </summary>
		/// <param name="numberSides">Number of sides of the dice</param>
		/// <returns></returns>
		/// <remarks>
		/// From: https://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider.aspx
		/// </remarks>
		public static byte RollDice(byte numberSides)
		{
			if (numberSides <= 0)
				throw new ArgumentOutOfRangeException(nameof(numberSides));

			// Create a byte array to hold the random value.
			byte[] randomNumber = new byte[1];
			using (var random = RandomNumberGenerator.Create())
			{
				do
				{
					// Fill the array with a random value.
					random.GetBytes(randomNumber);
				}
				while (!IsFairRoll(randomNumber[0], numberSides));
			}

			// Return the random number mod the number of sides.
			// The possible values are zero-based, so we add one.
			return (byte)((randomNumber[0] % numberSides) + 1);
		}

		public static string GenerateString(int length, string allowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789")
		{
			using (var random = RandomNumberGenerator.Create())
			{
				var data = new byte[length];

				// If allowableCharacters.Length isn't a power of 2 then there
				// is a bias if we simply use the modulus operator. The first
				// characters of chars will be more probable than the last ones.

				// buffer used if we encounter an unusable random byte. We will
				// regenerate it in this buffer
				byte[] smallBuffer = null;

				// Maximum random number that can be used without introducing a bias
				int maxRandom = byte.MaxValue - ((byte.MaxValue + 1) % allowableCharacters.Length);

				random.GetBytes(data);

				var result = new char[length];

				for (int i = 0; i < length; i++)
				{
					byte v = data[i];

					while (v > maxRandom)
					{
						if (smallBuffer == null)
						{
							smallBuffer = new byte[1];
						}

						random.GetBytes(smallBuffer);
						v = smallBuffer[0];
					}

					result[i] = allowableCharacters[v % allowableCharacters.Length];
				}

				return new string(result);
			}
		}

		public static byte[] GenerateSalt(int length)
		{
			var salt = new byte[length];

			using (var random = RandomNumberGenerator.Create())
			{
				random.GetBytes(salt);
			}

			return salt;
		}

		public static string GenerateSaltString(int length)
		{
			var saltBytes = GenerateSalt(length);
			return Convert.ToBase64String(saltBytes);
		}

		#endregion

		#region PRIVATE METHODS

		private static bool IsFairRoll(byte roll, byte numSides)
		{
			// There are MaxValue / numSides full sets of numbers that can come up
			// in a single byte.  For instance, if we have a 6 sided die, there are
			// 42 full sets of 1-6 that come up.  The 43rd set is incomplete.
			int fullSetsOfValues = Byte.MaxValue / numSides;

			// If the roll is within this range of fair values, then we let it continue.
			// In the 6 sided die case, a roll between 0 and 251 is allowed.  (We use
			// < rather than <= since the = portion allows through an extra 0 value).
			// 252 through 255 would provide an extra 0, 1, 2, 3 so they are not fair
			// to use.
			return roll < numSides * fullSetsOfValues;
		}

		#endregion
	}
}
