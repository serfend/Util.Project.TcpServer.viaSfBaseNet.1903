
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SfTcp.TcpServer
{
	public class TcpServer:IDisposable
	{
		private SfBaseTcp.Net.Sockets.TCPListener server;
		public ILog Log { set; get; }
		public int Port { get => port; set => port = value; }

		public TcpConnection this[string key] { get {
				lock (list)
				{
					list.TryGetValue(key,out TcpConnection result);
					return result;
				}
			} set => list[key] = value; }
		private int port;
		public event ClientConnect OnConnect;
		public event ClientDisconnect OnDisconnect;
		public event ClientMessage OnMessage;
		public event ClientHttpMessage OnHttpMessage;
		private ConcurrentDictionary<string, TcpConnection> list;
		//private ConcurrentDictionary<string, int> lastMessageStamp=new ConcurrentDictionary<string, int>();
		private bool isListening;
		/// <summary>
		/// 用于定时检查终端，并释放长时间无通讯的终端
		/// </summary>
		//private Thread checkClientAlive;
		public TcpServer(int port)
		{
			list = new ConcurrentDictionary<string, TcpConnection>();
			server = new SfBaseTcp.Net.Sockets.TCPListener() {
				Port=port
			};
			server.DisconnectCompleted += Server_DisconnectCompleted; ;
			server.ReceiveCompleted += Server_ReceiveCompleted; ;
			server.AcceptCompleted += Server_AcceptCompleted; ;

			//checkClientAlive = new Thread(CheckClientAlive) { IsBackground = true };
			//checkClientAlive.Start(); 
			server.Start();
		}

		private void Server_AcceptCompleted(object sender, SfBaseTcp.Net.Sockets.SocketEventArgs e)
		{
			lock (anyMessage)
			{
				TcpConnection connection;
				var ip = e.Socket.RemoteEndPoint.ToString();

				connection = new TcpConnection(e.Socket, "null");
				if (list == null)
				{
					throw new Exception("list is not init");
				}
				list.AddOrUpdate(ip, connection, (clientIp, clientConnect) => clientConnect);
				//lastMessageStamp.AddOrUpdate(connection.Ip, Environment.TickCount, (key, value) =>
				//{
				//	return Environment.TickCount;
				//});
				OnConnect?.Invoke(connection, new ClientConnectEventArgs());
			}
		}

		private void Server_DisconnectCompleted(object sender, SfBaseTcp.Net.Sockets.SocketEventArgs e)
		{
			RaiseOnDisconnect(e.Socket.RemoteEndPoint.ToString());
		}

		private void Server_ReceiveCompleted(object sender, SfBaseTcp.Net.Sockets.SocketEventArgs e)
		{
			lock (anyMessage)
			{
				TcpConnection connection = null;
				var msg = new ClientMessageEventArgs(e.Data);

				string ip = e.Socket.RemoteEndPoint.ToString();

				list.TryGetValue(ip, out connection);

				msg.AnalysisRaw();
				Console.WriteLine($"{ip}->{msg.RawString}");
				if (msg.Error)
				{
					var httpMsg = TcpHttpMessage.CheckTcpHttpMessage(msg.RawString);
					if (httpMsg != null)
					{
						if (connection != null && connection.Client.IsConnected) OnHttpMessage?.Invoke(sender, new ClientHttpMessageEventArgs(httpMsg, connection));
						return;
					}
					else
					{
						//throw new MsgInvalidException(msg.RawString);
						return;
					}
				}
				RaiseOnMessage(connection, msg);
			}
		}
		private object anyMessage=new object();

		private void RaiseOnDisconnect(string s)
		{
			lock (anyMessage)
			{
				TcpConnection connection;
				if (!list.ContainsKey(s))return;
				connection = this[s];
				list.TryRemove(s, out TcpConnection removeConnection);
				//lastMessageStamp.TryRemove(s, out int value);

				OnDisconnect?.Invoke(connection, new ClientDisconnectEventArgs());
			}
		}
		private void RaiseOnMessage(TcpConnection connection,ClientMessageEventArgs e)
		{
			int nowTime = Environment.TickCount;
			//Console.WriteLine($"message {connection.AliasName}->{d.RawString}");
			//lastMessageStamp.AddOrUpdate(connection.Ip, nowTime, (xx, x) => nowTime);
			OnMessage?.Invoke(connection, e);
		}
		#region checkAlive
		//private void CheckClientAlive()
		//{
		//	int count = 0;
		//	int nowLife = 100;
		//	while (true)
		//	{
		//		Thread.Sleep(100);
		//		if (count++ > nowLife)
		//		{
		//			TcpConnection thisTimeToDisconnect = null;
		//			count = 0;
		//			int nowTime = Environment.TickCount;
		//			lock (list)
		//			{
		//				foreach (var c in list)
		//				{
		//					var connection = c.Value;
		//					if (connection == null)
		//					{
		//						continue;
		//					}
		//					if (nowTime - lastMessageStamp[connection.Ip] > 20000)
		//					{
		//						thisTimeToDisconnect = connection;
		//						nowLife = (int)(nowLife * 0.5) + 1;
		//						//当上一行代码实现了关闭，则注释此行 RaiseOnDisconnect(connection.Ip);
		//						break;
		//					}
		//				}
		//			}
		//			if (thisTimeToDisconnect == null)
		//			{
		//				nowLife = nowLife + 5;
		//			}
		//			else
		//			{
		//				thisTimeToDisconnect.Client?.Disconnect();
		//			}
		//		}
		//	}
		//}
		#endregion
		private void Disconnect(TcpConnection client)
		{
			client.Disconnect();
		}




		public void StartListening()
		{
			isListening = true;
			server.Start();
		}
		public void StopListening()
		{
			if (!isListening) return;
			isListening = false;
			server.Stop();
		}

		#region IDisposable Support
		private bool disposedValue = false; // 要检测冗余调用

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (server != null)
					{
						StopListening();
						server.Dispose();
					}
				}
				server = null;
				disposedValue = true;
			}
		}


		public void Dispose()
		{
			// 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
			Dispose(true);
		}
		#endregion

	}

	[Serializable]
	public class MsgInvalidException : Exception
	{
		public MsgInvalidException() { }
		public MsgInvalidException(string message) : base(message) { }
		public MsgInvalidException(string message, Exception inner) : base(message, inner) { }
		protected MsgInvalidException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
