﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using log4net.Appender;
using System.Net.Sockets;
using System.Net;
using log4net.Core;

namespace Esilog.Gelf4net
{
	public abstract class GelfAppenderBase : AppenderSkeleton
	{
		public GelfAppenderBase()
		{
			Facility = c_defaultFacility;
			RemoteAddress = "127.0.0.1";
			RemotePort = 12201;
			Host = null;
		}

		public string RemoteAddress { get; set; }
		public int RemotePort { get; set; }
		public string Facility { get; set; }
		public string Host { get; set; }
		public string AdditionalFields { get; set; }

		public override void ActivateOptions()
		{
			base.ActivateOptions();

			m_additionalFieldsDictionary = AdditionalFields != null
				? AdditionalFields.Split(',').ToDictionary(it => it.Split(':')[0], it => it.Split(':')[1])
				: new Dictionary<string, string>();

			IPAddress remoteIPAddress;
			if (IPAddress.TryParse(RemoteAddress, out remoteIPAddress))
				m_remoteIPAddress = remoteIPAddress;
			else
				m_remoteIPAddress = GetIpAddressFromHostName(RemoteAddress);

			if (string.IsNullOrEmpty(Host))
				Host = GetLoggingHostName();
		}

		protected Dictionary<string, string> AdditionalFieldsDictionary
		{
			get { return m_additionalFieldsDictionary; }
		}

		protected IPAddress RemoteIPAddress
		{
			get { return m_remoteIPAddress; }
		}

		protected override sealed void Append(LoggingEvent loggingEvent)
		{
			String gelfJsonMessage = GenerateMessage(loggingEvent, Host, Facility, AdditionalFieldsDictionary);

			AppendCore(gelfJsonMessage);
		}

		protected abstract void AppendCore(string message);

		private string GenerateMessage(LoggingEvent loggingEvent, string hostName, string facility, Dictionary<string, string> globalAdditionalFields)
		{
			string renderedMessage = (Layout != null) ? RenderLoggingEvent(loggingEvent) : loggingEvent.RenderedMessage ?? loggingEvent.MessageObject.ToString();

			string fullMessage = loggingEvent.ExceptionObject != null
					   ? string.Format("{0} - {1}. {2}. {3}.", renderedMessage, loggingEvent.ExceptionObject.Source, loggingEvent.ExceptionObject.Message, loggingEvent.ExceptionObject.StackTrace)
					   : renderedMessage;

			string shortMessage = renderedMessage.Substring(0, Math.Min(renderedMessage.Length, c_maxShortMessageLength));

			var gelfMessage = new GelfMessage
			{
				Facility = (facility ?? c_defaultFacility),
				Host = hostName,
				Level = GetSyslogSeverity(loggingEvent.Level),
				ShortMessage = shortMessage,
				FullMesage = fullMessage,
				TimeStamp = loggingEvent.TimeStamp,
				Version = c_gelfVersion,
			};

			if (loggingEvent.LocationInformation != null)
			{
				gelfMessage.File = loggingEvent.LocationInformation.FileName;
				gelfMessage.Line = loggingEvent.LocationInformation.LineNumber;
			}

			string messageJson = JsonConvert.SerializeObject(gelfMessage);

			if ((globalAdditionalFields != null && globalAdditionalFields.Count != 0) || (loggingEvent.Properties != null && loggingEvent.Properties.Count != 0))
			{
				Dictionary<string, string> additionalFields = globalAdditionalFields ?? new Dictionary<string, string>();

				if (loggingEvent.Properties != null)
				{
					foreach (DictionaryEntry property in loggingEvent.Properties)
					{
						string key = property.Key as string;

						if (key == null)
							continue;

						string value = property.Value as string ?? (property.Value != null ? property.Value.ToString() : null);

						// allow individual messages to override global additional fields
						if (additionalFields.ContainsKey(key))
							additionalFields[key] = value;
						else
							additionalFields.Add(key, value);
					}
				}

				messageJson = AppendAdditionalFields(messageJson, additionalFields);
			}

			return messageJson;
		}

		private static string AppendAdditionalFields(string messageJson, Dictionary<string, string> additionalFields)
		{
			var messageJObject = JObject.Parse(messageJson);

			foreach (var field in additionalFields)
			{
				string key = field.Key;
				if (string.IsNullOrWhiteSpace(key))
					continue;

				if (!key.StartsWith("_"))
					key = string.Format("_{0}", key);

				// prevent sending submitting a field with the key '_id' per GELF spec: https://github.com/Graylog2/graylog2-docs/wiki/GELF
				if (key != "_id")
				{
					key = Regex.Replace(key, "[\\W]", "");
					messageJObject.Add(key, field.Value);
				}
			}

			return messageJObject.ToString();
		}

		private String GetLoggingHostName()
		{
			String ret = Host;
			if (ret == null)
			{
				try
				{
					ret = Dns.GetHostName();
				}
				catch (SocketException)
				{
					ret = c_unknownHost;
				}
			}
			return ret;
		}

		private IPAddress GetIpAddressFromHostName(string hostName)
		{
			IPAddress[] addresslist = Dns.GetHostAddresses(hostName);
			return addresslist.Length != 0 ? addresslist[0] : null;
		}

		private static int GetSyslogSeverity(Level level)
		{
			if (level == Level.Alert)
				return (int) LocalSyslogAppender.SyslogSeverity.Alert;

			if (level == Level.Critical || level == Level.Fatal)
				return (int) LocalSyslogAppender.SyslogSeverity.Critical;

			if (level == Level.Debug)
				return (int) LocalSyslogAppender.SyslogSeverity.Debug;

			if (level == Level.Emergency)
				return (int) LocalSyslogAppender.SyslogSeverity.Emergency;

			if (level == Level.Error)
				return (int) LocalSyslogAppender.SyslogSeverity.Error;

			if (level == Level.Fine
				|| level == Level.Finer
				|| level == Level.Finest
				|| level == Level.Info
				|| level == Level.Off)
				return (int) LocalSyslogAppender.SyslogSeverity.Informational;

			if (level == Level.Notice
				|| level == Level.Verbose
				|| level == Level.Trace)
				return (int) LocalSyslogAppender.SyslogSeverity.Notice;

			if (level == Level.Severe)
				return (int) LocalSyslogAppender.SyslogSeverity.Emergency;

			if (level == Level.Warn)
				return (int) LocalSyslogAppender.SyslogSeverity.Warning;

			return (int) LocalSyslogAppender.SyslogSeverity.Debug;
		}

		[JsonObject(MemberSerialization.OptIn)]
		private class GelfMessage
		{
			[JsonProperty("facility")]
			public string Facility { get; set; }

			[JsonProperty("file")]
			public string File { get; set; }

			[JsonProperty("full_message")]
			public string FullMesage { get; set; }

			[JsonProperty("host")]
			public string Host { get; set; }

			[JsonProperty("level")]
			public int Level { get; set; }

			[JsonProperty("line")]
			public string Line { get; set; }

			[JsonProperty("short_message")]
			public string ShortMessage { get; set; }

			[JsonProperty("timestamp")]
			public DateTime TimeStamp { get; set; }

			[JsonProperty("version")]
			public string Version { get; set; }
		}

		private Dictionary<string, string> m_additionalFieldsDictionary;
		private IPAddress m_remoteIPAddress;

		private const string c_unknownHost = "UNKNOWN_HOST";
		private const int c_maxShortMessageLength = 250;
		private const string c_gelfVersion = "1.0";
		private const string c_defaultFacility = "GELF";
	}
}
