using System;
using Esilog.Gelf4net.Transport;

namespace Esilog.Gelf4net
{
	public sealed class AmqpGelfAppender : GelfAppenderBase
	{
		public AmqpGelfAppender()
		{
			m_transport = new AmqpGelfTransport();
		}

		public int GrayLogServerAmqpPort { get; set; }
		public string GrayLogServerAmqpUser { get; set; }
		public string GrayLogServerAmqpPassword { get; set; }
		public string GrayLogServerAmqpVirtualHost { get; set; }
		public string GrayLogServerAmqpQueue { get; set; }

		public override void ActivateOptions()
		{
			base.ActivateOptions();

			m_transport.User = GrayLogServerAmqpUser;
			m_transport.Password = GrayLogServerAmqpPassword;
			m_transport.Queue = GrayLogServerAmqpQueue;
			m_transport.VirtualHost = GrayLogServerAmqpVirtualHost;
		}

		protected override void AppendCore(string message)
		{
			m_transport.Send(Host, RemoteIPAddress, GrayLogServerAmqpPort, message);
		}

		private readonly AmqpGelfTransport m_transport;
	}
}