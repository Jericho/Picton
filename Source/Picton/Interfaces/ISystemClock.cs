using System;

namespace Picton.Interfaces
{
	/// <summary>
	/// Defines an abstraction for retrieving the current local and Coordinated Universal Time (UTC) values.
	/// </summary>
	/// <remarks>Implementations of this interface provide a way to obtain the current time, which can be useful for
	/// testing or substituting system time in applications. This interface is commonly used to enable time-based logic to
	/// be tested or overridden.</remarks>
	public interface ISystemClock
	{
		/// <summary>Gets the current date and time.</summary>
		DateTime Now { get; }

		/// <summary>Gets the current date and time in Coordinated Universal Time (UTC).</summary>
		DateTime UtcNow { get; }
	}
}
