using Picton.Interfaces;
using System;
using System.Security.Cryptography;

namespace Picton
{
	/// <summary>
	/// Random generator.
	/// </summary>
	public class RandomGenerator : IRandomGenerator
	{
		#region FIELDS

		private static readonly Lazy<IRandomGenerator> _instance = new Lazy<IRandomGenerator>(() => new RandomGenerator(), true);

		#endregion

		#region PROPERTIES

		public static IRandomGenerator Instance { get { return _instance.Value; } }

		/// <inheritdoc/>
		public int GetInt32(int minValueInclusive, int maxValueExclusive)
		{
#if NET48
			if (maxValueExclusive < minValueInclusive) throw new ArgumentOutOfRangeException(nameof(maxValueExclusive), $"{nameof(maxValueExclusive)} must be greater than (or equal to) {nameof(minValueInclusive)}");

			uint range = (uint)maxValueExclusive - (uint)minValueInclusive;

			// If there is only one possible choice, nothing random will actually happen, so return the only possibility.
			if (range == 0) return minValueInclusive;

			uint randomNumber = 0;

			// Create a byte array to hold the random value.
			byte[] randomBytes = new byte[4];
			using (var random = RandomNumberGenerator.Create())
			{
				do
				{
					random.GetBytes(randomBytes);
					randomNumber = BitConverter.ToUInt32(randomBytes, 0);
				}
				while (!IsFairRoll(randomNumber, range));
			}

			return (int)(minValueInclusive + (randomNumber % range));
#else
			return RandomNumberGenerator.GetInt32(minValueInclusive, maxValueExclusive);
#endif
		}

		/// <inheritdoc/>
		public string GenerateString(int length, string allowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789")
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

		/// <inheritdoc/>
		public byte[] GenerateSalt(int length)
		{
			var salt = new byte[length];

			using (var random = RandomNumberGenerator.Create())
			{
				random.GetBytes(salt);
			}

			return salt;
		}

		/// <inheritdoc/>
		public string GenerateSaltString(int length)
		{
			var saltBytes = GenerateSalt(length);
			return Convert.ToBase64String(saltBytes);
		}

		#endregion

		#region CONSTRUCTOR

		private RandomGenerator() { }

		#endregion

		#region PRIVATE METHODS

#if NET48
		private static bool IsFairRoll(uint roll, uint numSides)
		{
			// There are MaxValue / numSides full sets of numbers that can come up
			// in a single byte. For instance, if we have a 6 sided die, there are
			// 42 full sets of 1-6 that come up. The 43rd set is incomplete.
			uint fullSetsOfValues = uint.MaxValue / numSides;

			// If the roll is within this range of fair values, then we let it continue.
			// In the 6 sided die case, a roll between 0 and 251 is allowed. (We use
			// < rather than <= since the = portion allows through an extra 0 value).
			// 252 through 255 would provide an extra 0, 1, 2, 3 so they are not fair
			// to use.
			return roll < numSides * fullSetsOfValues;
		}
#endif
		#endregion
	}
}
