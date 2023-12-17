namespace Picton.Interfaces
{
	public interface IRandomGenerator
	{
		/// <summary>
		/// Generates a random integer between two values.
		/// </summary>
		/// <param name="minValueInclusive">The minimum value.</param>
		/// <param name="maxValueExclusive">The maximum value.</param>
		/// <returns>A random value.</returns>
		int GetInt32(int minValueInclusive, int maxValueExclusive);

		string GenerateString(int length, string allowableCharacters = "abcdefghijklmnopqrstuvwxyz0123456789");

		byte[] GenerateSalt(int length);

		string GenerateSaltString(int length);
	}
}
