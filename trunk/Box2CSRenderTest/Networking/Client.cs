﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Box2DSharpRenderTest.Networking
{
	public enum EClientDataPacketType
	{
		ConnectedPacket,
		DisconnectedPacket,
		ChatPacket,
		PlayerPacket
	}

	public class ClientDataPacket : DataPacket
	{
		public NetworkClient Client
		{
			get;
			set;
		}

		public ClientDataPacket(NetworkClient client, byte[] data, IPEndPoint endPoint) :
			base(data, endPoint)
		{
			Client = client;
		}

		public override void Parse()
		{
			while (Memory.BaseStream.Length != Memory.BaseStream.Position)
			{
				EClientDataPacketType type = (EClientDataPacketType)Memory.ReadByte();

				switch (type)
				{
				case EClientDataPacketType.ConnectedPacket:
					{
						Client.ConnectedIndex = (int)Memory.ReadByte();
						break;
					}
				case EClientDataPacketType.ChatPacket:
					{
						Program.MainForm.textBox1.Text += Memory.ReadString().Replace("\n", Environment.NewLine);
						break;
					}
				case EClientDataPacketType.DisconnectedPacket:
					{
						System.Windows.Forms.MessageBox.Show("Forcibly disconnected");
						System.Windows.Forms.Application.Exit();
						break;
					}
				case EClientDataPacketType.PlayerPacket:
					{
						Client.Transforms = new Box2CS.Transform[(int)BipedFixtureIndex.Max,2];

						for (int i = 0; i < (int)BipedFixtureIndex.Max; ++i)
						{
							Client.Transforms[i,0] = new Box2CS.Transform(new Box2CS.Vec2(Memory.ReadSingle(), Memory.ReadSingle()), new Box2CS.Mat22(Memory.ReadSingle()));
							Client.Transforms[i,1] = new Box2CS.Transform(new Box2CS.Vec2(Memory.ReadSingle(), Memory.ReadSingle()), new Box2CS.Mat22(Memory.ReadSingle()));
						}
						break;
					}
				}
			}
		}
	}

	public class NetworkClient
	{
		UdpClient _udpClient;

		public int ConnectedIndex
		{
			get;
			set;
		}

		public UdpClient Client
		{
			get { return _udpClient; }
		}

		public MemoryStream Memory
		{
			get;
			private set;
		}

		public BinaryWriter Stream
		{
			get;
			private set;
		}

		public IPEndPoint EndPoint
		{
			get;
			private set;
		}

		public Box2CS.Transform[,] Transforms
		{
			get;
			set;
		}

		public NetworkClient(IPAddress address, string name)
		{
			EndPoint = new IPEndPoint(address, NetworkSettings.Port);
			_udpClient = new UdpClient();
			_udpClient.Connect(EndPoint);

			Memory = new MemoryStream();
			Stream = new BinaryWriter(Memory);

			Stream.Write((byte)EServerDataPacketType.ConnectPacket);
			Stream.Write(name);
		}

		public void Close()
		{
			Memory.SetLength(0);
			Memory.Position = 0;
			Stream.Write((byte)EServerDataPacketType.DisconnectPacket);
			Check(true);

			_udpClient.Close();
		}

		public void Check(bool skipCheck = false)
		{
			if (!skipCheck && _udpClient.Available != 0)
			{
				IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
				var data = new ClientDataPacket(this, _udpClient.Receive(ref endPoint), endPoint);
				data.Parse();
			}

			if (Memory.Length != 0)
			{
				_udpClient.Send(Memory.GetBuffer(), (int)Memory.Length);
				Memory.SetLength(0);
				Memory.Position = 0;
			}
		}

		public void SendText(string str)
		{
			Stream.Write((byte)EServerDataPacketType.ChatPacket);
			Stream.Write((byte)ConnectedIndex);
			Stream.Write(str);
		}
	}
}
