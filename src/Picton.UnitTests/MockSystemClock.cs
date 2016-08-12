using Moq;
using System;

namespace Picton.UnitTests
{
	public class MockSystemClock : Mock<ISystemClock>
	{
		public MockSystemClock(DateTime currentDateTime)
			: base(MockBehavior.Strict)
		{
			SetupGet(m => m.Now).Returns(new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second, currentDateTime.Millisecond, DateTimeKind.Utc));
			SetupGet(m => m.UtcNow).Returns(new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second, currentDateTime.Millisecond, DateTimeKind.Utc));
		}
	}
}
