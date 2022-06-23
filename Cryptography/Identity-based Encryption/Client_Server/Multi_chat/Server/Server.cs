using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
            Connect();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            foreach (Socket item in clientList)
            {
                Send(item);
            }
            txbMessage.Clear();

        }
        public static class Cryptography
        {
            #region Settings

            private static int _iterations = 2;
            private static int _keySize = 256;

            private static string _hash = "SHA256";
            private static string _salt = "aselrias38490a32"; // Random
            private static string _vector = "8947az34awl34kjq"; // Random

            #endregion

            public static string Encrypt(string value, string password)
            {
                return Encrypt<AesManaged>(value, password);
            }
            public static string Encrypt<T>(string value, string password)
                    where T : SymmetricAlgorithm, new()
            {
                byte[] vectorBytes = ASCIIEncoding.ASCII.GetBytes(_vector);
                byte[] saltBytes = ASCIIEncoding.ASCII.GetBytes(_salt);
                byte[] valueBytes = ASCIIEncoding.ASCII.GetBytes(value);

                byte[] encrypted;
                using (T cipher = new T())
                {
                    PasswordDeriveBytes _passwordBytes =
                        new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                    byte[] keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                    cipher.Mode = CipherMode.CBC;

                    using (ICryptoTransform encryptor = cipher.CreateEncryptor(keyBytes, vectorBytes))
                    {
                        using (MemoryStream to = new MemoryStream())
                        {
                            using (CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write))
                            {
                                writer.Write(valueBytes, 0, valueBytes.Length);
                                writer.FlushFinalBlock();
                                encrypted = to.ToArray();
                            }
                        }
                    }
                    cipher.Clear();
                }
                return Convert.ToBase64String(encrypted);
            }

            public static string Decrypt(string value, string password)
            {
                return Decrypt<AesManaged>(value, password);
            }
            public static string Decrypt<T>(string value, string password) where T : SymmetricAlgorithm, new()
            {
                byte[] vectorBytes = ASCIIEncoding.ASCII.GetBytes(_vector);
                byte[] saltBytes = ASCIIEncoding.ASCII.GetBytes(_salt);
                byte[] valueBytes = Convert.FromBase64String(value);

                byte[] decrypted;
                int decryptedByteCount = 0;

                using (T cipher = new T())
                {
                    PasswordDeriveBytes _passwordBytes = new PasswordDeriveBytes(password, saltBytes, _hash, _iterations);
                    byte[] keyBytes = _passwordBytes.GetBytes(_keySize / 8);

                    cipher.Mode = CipherMode.CBC;

                    try
                    {
                        using (ICryptoTransform decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
                        {
                            using (MemoryStream from = new MemoryStream(valueBytes))
                            {
                                using (CryptoStream reader = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                                {
                                    decrypted = new byte[valueBytes.Length];
                                    decryptedByteCount = reader.Read(decrypted, 0, decrypted.Length);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return String.Empty;
                    }

                    cipher.Clear();
                }
                return Encoding.UTF8.GetString(decrypted, 0, decryptedByteCount);
            }
        }


        IPEndPoint IP, IPtemp;
        Socket server;
        List<Socket> clientList;

        int count = 0;
        void Connect()
        {
            clientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Any, 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            server.Bind(IP);

            Thread Listen = new Thread(() => {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        
                        listView1.Items.Add(client.RemoteEndPoint.ToString());

                        clientList.Add(client);

                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    IP = new IPEndPoint(IPAddress.Any, 9999);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

                }                
                
            });
            Listen.IsBackground = true;
            Listen.Start();
        }


        void Close()
        {
            server.Close();
        }

        void Send(Socket client)
        {
            if (client != null && txbMessage.Text != string.Empty)
                client.Send(Serialize(txbMessage.Text));
        }

        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);

                    string message = (string)Deserialize(data);
                    
                    foreach(Socket item in clientList)
                    {
                        if (item != null && item != client)
                            item.Send(Serialize(message));
                        
                    }    
                    AddMessage(message);
                }
            }
            catch
            {
                clientList.Remove(client);
                client.Close();
            }

        }

        void AddMessage(string s)
        {
            lsvMessage.Items.Add(new ListViewItem() { Text = s });
        }

        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        private void btnSendList_Click(object sender, EventArgs e)
        {
            foreach (Socket client in clientList)
            {
                foreach (Socket item in clientList)
                {
                    client.Send(Serialize(item.RemoteEndPoint.ToString()));
                }
            }
        }

        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }
    }
}
