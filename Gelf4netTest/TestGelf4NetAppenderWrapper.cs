using System;
using Esilog.Gelf4net;

namespace Gelf4netTest
{
	class TestGelf4NetAppenderWrapper : GelfAppenderBase
	{
		public void TestAppend(log4net.Core.LoggingEvent loggingEvent)
		{
			Append(loggingEvent);
		}

		protected sealed override void AppendCore(string message)
		{
			Console.WriteLine(message);
			LastMessage = message;
		}

		public string LastMessage { get; set; }
	}
}
