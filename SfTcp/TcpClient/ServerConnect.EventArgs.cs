using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SfTcp.TcpClient
{

	public delegate void ConnectToServer(object sender, ServerConnectEventArgs e);
	public class ServerConnectEventArgs:EventArgs

	{
		private SocketAsyncOperation operation;

		public ServerConnectEventArgs(SocketAsyncOperation operation)
		{
			this.operation = operation;
		}

		public SocketAsyncOperation Operation { get => operation; set => operation = value; }
	}
}
