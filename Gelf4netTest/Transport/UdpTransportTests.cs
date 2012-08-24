using NUnit.Framework;
using Esilog.Gelf4net.Transport;
using System.Reflection;

namespace Gelf4netTest.Transport
{
	[TestFixture]
	class UdpTransportTests
	{
		[Test()]
		public void TestMessageId()
		{
			// Arrange
			string hostName = "localhost";

			// Act; cheat an use reflection to access private method
			var generateMessageId = typeof(UdpGelfTransport).GetMethod("GenerateMessageId", BindingFlags.Static | BindingFlags.NonPublic);
			string actual = generateMessageId.Invoke(obj: null, parameters: new object[] { hostName }) as string;

			// Assert
			const int expectedLength = 8;
			Assert.AreEqual(actual.Length, expectedLength);
		}

		[Test()]
		public void CreateChunkedMessagePart_StartsWithCorrectHeader()
		{
			// Arrange
			string messageId = "A1B2C3D4";
			int index = 1;
			int chunkCount = 1;

			// Act
			byte[] result = CreateChunkedMessagePart(messageId, index, chunkCount);
			// Assert
			Assert.That(result[0], Is.EqualTo(30));
			Assert.That(result[1], Is.EqualTo(15));
		}

		[Test()]
		public void CreateChunkedMessagePart_ContainsMessageId()
		{
			// Arrange
			string messageId = "A1B2C3D4";
			int index = 1;
			int chunkCount = 1;

			// Act
			byte[] result = CreateChunkedMessagePart(messageId, index, chunkCount);

			// Assert
			Assert.That(result[2], Is.EqualTo((int) 'A'));
			Assert.That(result[3], Is.EqualTo((int) '1'));
			Assert.That(result[4], Is.EqualTo((int) 'B'));
			Assert.That(result[5], Is.EqualTo((int) '2'));
			Assert.That(result[6], Is.EqualTo((int) 'C'));
			Assert.That(result[7], Is.EqualTo((int) '3'));
			Assert.That(result[8], Is.EqualTo((int) 'D'));
			Assert.That(result[9], Is.EqualTo((int) '4'));
		}

		[Test()]
		public void CreateChunkedMessagePart_EndsWithIndexAndCount()
		{
			// Arrange
			string messageId = "A1B2C3D4";
			int index = 1;
			int chunkCount = 2;

			// Act
			byte[] result = CreateChunkedMessagePart(messageId, index, chunkCount);

			// Assert
			Assert.That(result[10], Is.EqualTo(index));
			Assert.That(result[11], Is.EqualTo(chunkCount));
		}

		private byte[] CreateChunkedMessagePart(string messageId, int index, int chunkCount)
		{
			var createChunkedMessagePart = typeof(UdpGelfTransport).GetMethod("CreateChunkedMessagePart", BindingFlags.Static | BindingFlags.NonPublic);
			return createChunkedMessagePart.Invoke(obj: null, parameters: new object[] { messageId, index, chunkCount }) as byte[];

		}
	}
}
