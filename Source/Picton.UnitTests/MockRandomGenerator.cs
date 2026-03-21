using Picton.Interfaces;
using System;

namespace Picton.UnitTests
{
	internal class MockRandomGenerator : IRandomGenerator
	{
		public int GetInt32(int minValueInclusive, int maxValueExclusive)
		{
			return 1;
		}

		public string GenerateString(int length, string allowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789")
		{
			return new string((allowableCharacters ?? "a")[0], length);
		}

		public byte[] GenerateSalt(int length)
		{
			byte defaultValue = 0x61; // lower case A
			byte[] salt = new byte[length];
#if NET
			Array.Fill(salt, defaultValue);
#else
			for (int i = 0; i < salt.Length; i++)
			{
				salt[i] = defaultValue;
			}
#endif

			return salt;
		}

		public string GenerateSaltString(int length)
		{
			return new string('a', length);
		}
	}
}
