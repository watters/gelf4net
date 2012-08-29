using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using log4net.Core;
using NUnit.Framework;
using log4net.Repository.Hierarchy;

namespace Gelf4netTest
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestFixture]
	public class Gelf4NetAppenderTest
	{
		private string graylogServerHost = "";

		[SetUpAttribute]
		public void Start()
		{
			graylogServerHost = "192.168.1.102";
		}

		[Test]
		public void AppendTest()
		{
			var gelfAppender = new TestGelf4NetAppenderWrapper();
			gelfAppender.RemoteAddress = graylogServerHost;
			gelfAppender.ActivateOptions();

			//def logEvent = new LoggingEvent(this.GetType().Name, new Category('catName'), System.currentTimeMillis(), Priority.WARN, "Some Short Message", new Exception('Exception Message'))
			var data = new LoggingEventData
			{
				Domain = GetType().Name,
				Level = Level.Debug,
				LoggerName = "Tester",
				Message = "GrayLog4Net!!!",
				TimeStamp = DateTime.Now,
				UserName = "ElTesto"
			};

			var logEvent = new LoggingEvent(data);
			gelfAppender.TestAppend(logEvent);
		}

		public void AppendTestChunkMessage()
		{
			var gelfAppender = new TestGelf4NetAppenderWrapper();
			gelfAppender.RemoteAddress = graylogServerHost;
			gelfAppender.AdditionalFields = "nombre:pedro,apellido:jimenez";
			gelfAppender.ActivateOptions();

			//def logEvent = new LoggingEvent(this.GetType().Name, new Category('catName'), System.currentTimeMillis(), Priority.WARN, "Some Short Message", new Exception('Exception Message'))
			var data = new LoggingEventData
			{
				Domain = this.GetType().Name,
				Level = Level.Debug,
				LoggerName = "Big Tester",
				Message = LoremIpsum.Text,
				TimeStamp = DateTime.Now,
				UserName = "ElTesto"
			};

			var logEvent = new LoggingEvent(data);
			logEvent.Properties["customProperty"] = "My Custom Property Woho";

			gelfAppender.TestAppend(logEvent);
		}

		[Test]
		public void AppendWithCustomPerMessageProperties()
		{
			var testAppender = new TestGelf4NetAppenderWrapper();
			testAppender.RemoteAddress = "127.0.0.1";
			testAppender.AdditionalFields = "global1:foo,global2:bar";
			testAppender.ActivateOptions();

			var data = new LoggingEventData
				{
					Domain = GetType().Name,
					Level = Level.Debug,
					LoggerName = "PerMessageProperties",
					TimeStamp = DateTime.UtcNow,
					UserName = "ElTesto"
				};

			var messageObject = new CustomMessage { ShortMessage = "This is a custom message to test per-message fields and overrides.", CustomFields = new Dictionary<string, string> { { "message1", "baz" }, { "message2", "baf" }, { "message3", "woohoo" } } };
			data.Message = messageObject.ToString();
			var logEvent = new LoggingEvent(GetType(), new Hierarchy(), data.LoggerName, data.Level, messageObject, new InvalidOperationException("This is a test exception!"));

			testAppender.TestAppend(logEvent);

			JObject gelfMessage = JObject.Parse(testAppender.LastMessage);


			Assert.AreEqual(gelfMessage["_global1"].ToString(), "foo");
			Assert.AreEqual(gelfMessage["_global2"].ToString(), "bar");
			Assert.AreEqual(gelfMessage["_message1"].ToString(), "baz");
			Assert.AreEqual(gelfMessage["_message2"].ToString(), "baf");
			Assert.AreEqual(gelfMessage["_message3"].ToString(), "woohoo");
			Assert.AreEqual(messageObject.ShortMessage, gelfMessage["short_message"].ToString());
		}

		[Test]
		public void TestSendMessageIteration()
		{
			var array = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			var max = 8;
			var iterations = array.Count / max + 1;

			for (int i = 0; i < iterations; i++)
			{
				array.Skip(i * max).Take(max).ToList<int>().ForEach(Console.WriteLine);
				Console.WriteLine("---");
			}
		}

		private class CustomMessage
		{
			public string ShortMessage { get; set; }
			public Dictionary<string, string> CustomFields { get; set; }

			public override string ToString()
			{
				return "Default Custom Message ToString()";
			}
		}
	}
}