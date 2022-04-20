using System;

namespace Picton.Interfaces
{
	public interface IRandomGenerator
	{
		/// <summary>
		/// This method simulates a roll of the dice. The input parameter is the
		/// number of sides of the dice.
		/// </summary>
		/// <param name="numberSides">Number of sides of the dice.</param>
		/// <returns>A random value.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Number of sides must be greater than zero.</exception>
		/// <remarks>
		/// From RNGCryptoServiceProvider <a href="https://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider.aspx">documentation</a>.
		/// </remarks>
		byte RollDice(byte numberSides);

		string GenerateString(int length, string allowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789");

		byte[] GenerateSalt(int length);

		string GenerateSaltString(int length);
	}
}
