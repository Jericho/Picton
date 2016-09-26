using System;

namespace Picton.Interfaces
{
	public interface ISystemClock
	{
		DateTime Now { get; }
		DateTime UtcNow { get; }
	}
}
