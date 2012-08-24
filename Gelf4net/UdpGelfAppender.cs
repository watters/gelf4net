using System;
using Esilog.Gelf4net.Transport;

namespace Esilog.Gelf4net
{
	public class UdpGelfAppender : GelfAppenderBase
	{
		public UdpGelfAppender()
		{
			ChunkSize = c_defaultMaxChunkSize;
			m_transport = new UdpGelfTransport();
		}

		public int ChunkSize { get; set; }

		public override void ActivateOptions()
		{
			base.ActivateOptions();

			m_transport.ChunkSize = ChunkSize;
		}

		protected override void AppendCore(string message)
		{
			m_transport.Send(Host, RemoteIPAddress, RemotePort, message);
		}

		private const int c_defaultMaxChunkSize = 1024;
		readonly UdpGelfTransport m_transport;
	}
}