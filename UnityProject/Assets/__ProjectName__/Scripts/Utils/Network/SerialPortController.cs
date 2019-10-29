﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

/*
 * シリアル通信を行うクラス
 */

namespace GarageKit
{
	public class SerialPortController : MonoBehaviour
	{
		public bool autoOpen = true;

		/// <summary>
		/// シリアルポートのインスタンス
		/// </summary>
		private SerialPort port = null;
		
		/// <summary>
		/// シリアルポート設定
		/// </summary>
		public string portName = "COM1";
		public int baudRate = 9600;
		public Parity parity = Parity.None;
		public int dataBits = 8;
		public StopBits stopBits = StopBits.One;
		public Encoding encoding = Encoding.ASCII;
		public NewLineCode newLineCode = NewLineCode.LF;
		public Handshake handShake = Handshake.None;
		public bool dtrEnable = false;
		public bool rtsEnable = false;
		public int readTimeout = 50;
		public int writeTimeout = 50;

		public bool IsOpen
		{
			get{ return (port != null) ? port.IsOpen : false; }
		}
		
		// 一度に受け取るコマンドの文字バッファサイズ　固定長
		const int STR_LENGTH = 1024;
		
		// 改行コード設定
		public enum NewLineCode
		{
			LF = 0,
			CR,
			CRLF,
		}
		
		// コマンドを受信したときのデリゲート
		public Action<string> OnReceive;

		
		void Awake()
		{

		}
		
		void Start()
		{
			if(autoOpen)
				Open();
		}
		
		void OnApplicationQuit()
		{
			Close();
		}


		public void Open()
		{
			if(port == null)
			{
				port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
				port.Encoding = encoding;
				switch(newLineCode)
				{
					case NewLineCode.LF: port.NewLine = "\n"; break;
					case NewLineCode.CR: port.NewLine = "\r"; break;
					case NewLineCode.CRLF: port.NewLine = "\r\n"; break;
				}
				port.Handshake = handShake;
				port.DtrEnable = dtrEnable;
				port.RtsEnable = rtsEnable;
				port.ReadTimeout = readTimeout;
				port.WriteTimeout = writeTimeout;
				
				try
				{
					if(!port.IsOpen)
					{
						port.Open();
						
						// データ受信ルーチンの開始
						StartCoroutine("ReadData");
					}
				}
				catch (Exception e)
				{
					Debug.LogError(e.Message); 
				}
			}
		}

		public void Close()
		{
			StopCoroutine("ReadData");

			if(port != null)
			{
				port.Close();
				port.Dispose();
				port = null;
			}
		}
		
		// データ受信
		private IEnumerator ReadData()
		{
			while(port != null && port.IsOpen)
			{
				string dataStr = "";
				
				try
				{
					byte data = (byte)port.ReadByte();
					while(data != 255)
					{
						dataStr += (char)data;
						
						// 次データの読み取り
						data = (byte)port.ReadByte();
					}
				}
				catch(Exception)
				{
					// pass
				}
				
				if(dataStr != "")
				{
					if(OnReceive != null)
						OnReceive(dataStr);
				}
				
				yield return null;
			}
		}
		
		// コマンド文字列を送信
		public void SendCommand(string str)
		{
			if(port != null && port.IsOpen)
			{
				try
				{
					port.WriteLine(str);
				}
				catch(Exception e)
				{
					Debug.LogError(e.Message);
				}
			}
		}
		
		// コマンド文字列をバイトに変換して送信する
		public void SendCommandByte(string str)
		{
			List<byte> bytes = new List<byte>();
			for(int i = 0; i < str.Length; i++)
				bytes.Add((byte)str[i]);
			
			SendByte(bytes.ToArray());
		}
		
		// スペース区切りの数字のコマンド配列を16進数に変換してデータ送信
		public void SendCommandArrayHexByte(string str)
		{
			string[] hexs = str.Split(new string[] { " " }, StringSplitOptions.None);

			List<byte> bytes = new List<byte>();
			foreach (string hex in hexs)
				bytes.Add((byte)Convert.ToInt32(hex, 16));

			SendByte(bytes.ToArray());
		}

		// バイトデータを送信
		public void SendByte(byte[] strBytes)
		{
			if(port != null && port.IsOpen)
			{
				try
				{
					port.Write(strBytes, 0, strBytes.Length);
				}
				catch(Exception e)
				{
					Debug.LogError(e.Message);
				}
			}
		}
	}
}