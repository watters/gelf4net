using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace Esilog.Gelf4net.Transport
{
	internal sealed class UdpGelfTransport : GelfTransportBase
	{
		public UdpGelfTransport()
		{
			ChunkSize = c_defaultChunkSize;
		}

		public int ChunkSize { get; set; }

		public override void Send(string serverHostName, IPAddress ipAddress, int serverPort, string message)
		{
			IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, serverPort);

			using (UdpClient udpClient = new UdpClient())
			{
				var gzipMessage = GzipMessage(message);

				if (ChunkSize < gzipMessage.Length)
				{
					var chunkCount = GetChunkCount(gzipMessage.Length, ChunkSize);

					// the GELF spec prohibits messages with more than 128 chunks
					if (chunkCount > c_maxNumberOfChunks)
					{
						// attempt to truncate the message while preserving as much useful data as possible
						gzipMessage = TruncateMessage(message);
						chunkCount = GetChunkCount(gzipMessage.Length, ChunkSize);

						// if some moron sets a really tiny chunk size (e.g. less than 4 bytes), drop his message on the floor.
						if (chunkCount > c_maxNumberOfChunks)
							return;
					}

					var messageId = GenerateMessageId(serverHostName);

					for (int i = 0; i < chunkCount; i++)
					{
						var messageChunkPrefix = CreateChunkedMessagePart(messageId, i, chunkCount);
						var skip = i * ChunkSize;
						var messageChunkSuffix = gzipMessage.Skip(skip).Take(ChunkSize).ToArray();

						var messageChunkFull = new byte[messageChunkPrefix.Length + messageChunkSuffix.Length];
						messageChunkPrefix.CopyTo(messageChunkFull, 0);
						messageChunkSuffix.CopyTo(messageChunkFull, messageChunkPrefix.Length);

						udpClient.Send(messageChunkFull, messageChunkFull.Length, ipEndPoint);
					}
				}
				else
				{
					udpClient.Send(gzipMessage, gzipMessage.Length, ipEndPoint);
				}
			}
		}

		private byte[] TruncateMessage(string message)
		{
			// truncate the message by returning only core fields and a note that the original message was truncated;
			// this should limit the size of the message a few hundred bytes unless somebody is doing something silly

			var originalMessageObject = JObject.Parse(message);

			var newMessageObject = new JObject();
			newMessageObject.Add("version", originalMessageObject["version"]);
			newMessageObject.Add("host", originalMessageObject["host"]);
			newMessageObject.Add("short_message", originalMessageObject["short_message"]);
			newMessageObject.Add("timestamp", originalMessageObject["timestamp"]);
			newMessageObject.Add("level", originalMessageObject["level"]);
			newMessageObject.Add("facility", originalMessageObject["facility"]);
			newMessageObject.Add("full_message", string.Format("This message was truncated because the the number of chunks exceeded {0}. Configured MaxChunkSize: {1}", c_maxNumberOfChunks, ChunkSize));

			return GzipMessage(newMessageObject.ToString());
		}

		private static int GetChunkCount(int length, int chunkSize)
		{
			return (length / chunkSize) + 1;
		}

		private static byte[] CreateChunkedMessagePart(string messageId, int index, int chunkCount)
		{
			var result = new List<byte>();

			var gelfHeader = new byte[2] { Convert.ToByte(30), Convert.ToByte(15) };
			result.AddRange(gelfHeader);
			result.AddRange(Encoding.Default.GetBytes(messageId).ToArray());
			result.Add(Convert.ToByte(index));
			result.Add(Convert.ToByte(chunkCount));

			return result.ToArray<byte>();
		}

		private static string GenerateMessageId(string serverHostName)
		{
			var md5String = String.Join("", MD5.Create().ComputeHash(Encoding.Default.GetBytes(serverHostName)).Select(it => it.ToString("x2")).ToArray());
			var random = new Random((int) DateTime.Now.Ticks);
			var sb = new StringBuilder();
			var t = DateTime.Now.Ticks % 1000000000;
			var s = String.Format("{0}{1}", md5String.Substring(0, 10), md5String.Substring(20, 10));
			var r = random.Next(10000000).ToString("00000000");

			sb.Append(t);
			sb.Append(s);
			sb.Append(r);

			//Message ID: 8 bytes 
			return sb.ToString().Substring(0, c_maxHeaderSize);
		}

		private const int c_maxHeaderSize = 8;
		private const int c_maxNumberOfChunks = 128;
		private const int c_defaultChunkSize = 1024;
	}
}
