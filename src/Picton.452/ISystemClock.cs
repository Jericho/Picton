using System;

namespace Picton
{
	public interface ISystemClock
	{
		DateTime Now { get; }
		DateTime UtcNow { get; }
	}
}
