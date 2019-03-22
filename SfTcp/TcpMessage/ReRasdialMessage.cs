using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SfTcp.TcpMessage
{
	public class CmdReRasdialMessage : ITcpMessage
	{
		private string title = TcpMessageEnum.CmdReRasdial;

		public string Title { get => title; set => title = value; }
	}
	public class RpReRasdialMessage : ITcpMessage
	{
		private string title = TcpMessageEnum.RpReRasdial;
		private string reason;

		public RpReRasdialMessage(string reason)
		{
			this.reason = reason;
		}

		public string Title { get => title; set => title = value; }
		public string Reason { get => reason; set => reason = value; }
	}
}