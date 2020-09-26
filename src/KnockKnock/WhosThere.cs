using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace KnockKnock
{
	public class WhosThere
	{
		/// <summary>
		/// A magic string. You may change this, but it has to be the same on all clients that want to know about each other.
		/// </summary>
		public const string WHOS_THERE_MAGIC = "Knock, knock! Who's there? Water. Water who? Water you doing?";

		/// <summary>
		/// Raised when this <see cref="WhosThere"/> receives notification of someone existing.
		/// </summary>
		public EventHandler<SomeonesHereEventArgs> SomeonesHere { get; set; }

		private readonly IPEndPoint broadcast;
		private readonly UdpClient udp;
		private readonly byte[] packet;
		private readonly byte[] magic;
		private readonly int port;
		private bool here;

		/// <summary>
		/// Initializes a new instance of <see cref="WhosThere"/>, ready to broadcast and receive on <paramref name="port"/>.
		/// </summary>
		/// <param name="name">Whatever you feel like calling this. May be null. Don't use for authorization.</param>
		/// <param name="port">The port to send and receive on.</param>
		public WhosThere(string name, int port)
		{
			if (name == null)
				name = string.Empty;
			else if (name.Length + WHOS_THERE_MAGIC.Length > 65534)
				throw new ArgumentOutOfRangeException(nameof(name), $"Name length cannot be greater than {65535 - WHOS_THERE_MAGIC.Length}");

			magic = Encoding.ASCII.GetBytes(WHOS_THERE_MAGIC);
			byte[] nameB = Encoding.ASCII.GetBytes(name);
			packet = new byte[magic.Length + nameB.Length];
			Buffer.BlockCopy(magic, 0, packet, 0, magic.Length);
			if (name.Length > 0)
				Buffer.BlockCopy(nameB, 0, packet, magic.Length, nameB.Length);

			this.port = port;
			broadcast = new IPEndPoint(IPAddress.Broadcast, port);
			udp = new UdpClient(port)
			{
				EnableBroadcast = true
			};
		}

		/// <summary>
		/// Stop listening for others.
		/// </summary>
		public void ImGone()
		{
			ImGoneAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		/// Stop listening for others.
		/// </summary>
		public async Task ImGoneAsync()
		{
			here = false;
			await udp.SendAsync(Array.Empty<byte>(), 0, new IPEndPoint(IPAddress.Loopback, port));
		}

		/// <summary>
		/// Notify a network that you are here
		/// </summary>
		public void ImHere()
		{
			ImHereAsync().GetAwaiter().GetResult();
		}

		/// <summary>
		/// Notify a network that you are here, and start listening for others.
		/// </summary>
		public async Task ImHereAsync()
		{
			await udp.SendAsync(packet, packet.Length, broadcast);

			if (here)
				return;

			udp.BeginReceive(EndReceive, new IPEndPoint(IPAddress.Broadcast, port));
			here = true;
		}

		/// <summary>
		/// The magic happens here. Don't call this manually.
		/// </summary>
		private void EndReceive(IAsyncResult ar)
		{
			IPEndPoint remote = (IPEndPoint)ar.AsyncState;
			byte[] datagram = udp.EndReceive(ar, ref remote);

			if (remote.Address.ToString().StartsWith("169.254."))
				goto again; // probably from self. notice how galaxy brain the subnet checking is.

			// signal to stop listening
			if (datagram.Length == 0 && !here)
				return;

			// not something we care about
			if (datagram.Length < magic.Length)
				goto again;

			for (int i = 0; i < magic.Length; i++)
			{
				if (datagram[i] != magic[i])
					goto again; // not something we care about
			}

			SomeonesHere?.Invoke(this, new SomeonesHereEventArgs(Encoding.ASCII.GetString(datagram, magic.Length, datagram.Length - magic.Length), remote));

		again:
			udp.BeginReceive(EndReceive, new IPEndPoint(IPAddress.Any, port));
		}
	}
}
