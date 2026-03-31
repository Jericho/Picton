using Picton.Interfaces;
using System;

namespace Picton
{
	/// <inheritdoc/>
	public class SystemClock : ISystemClock
	{
		#region FIELDS

		private static readonly Lazy<ISystemClock> _instance = new(() => new SystemClock(), true);

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the singleton instance of the system clock implementation.
		/// </summary>
		/// <remarks>Use this property to access a shared, thread-safe instance of the system clock throughout the
		/// application. This instance provides a consistent source of time information and is suitable for scenarios where a
		/// single, global clock is required.</remarks>
		public static ISystemClock Instance { get { return _instance.Value; } }

		/// <inheritdoc/>
		public DateTime Now { get { return DateTime.Now; } }

		/// <inheritdoc/>
		public DateTime UtcNow { get { return DateTime.UtcNow; } }

		#endregion

		#region CONSTRUCTOR

		private SystemClock() { }

		#endregion
	}
}
