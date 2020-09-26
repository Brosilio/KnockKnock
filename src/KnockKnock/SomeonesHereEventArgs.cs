using System;
using System.Net;

namespace KnockKnock
{
	public class SomeonesHereEventArgs : EventArgs
	{
		public IPEndPoint RemoteAddress { get; }
		public string Name { get; }

		public SomeonesHereEventArgs(string name, IPEndPoint remote)
		{
			Name = name;
			RemoteAddress = remote;
		}
	}
}