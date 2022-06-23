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


namespace Client
{
    public partial class Client : Form
    {
        public Client()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();
        }

        private void Client_Load(object sender, EventArgs e)
        {

        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (txbName.Text.Trim().Length == 0)
            {
                MessageBox.Show("Vui lòng nhập tên!");
                return;
            }
            else
            {
                Send();
                AddMessage(txbName.Text + ": " + txbMessage.Text);
            }
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

        IPEndPoint IP;
        Socket client;
        void Connect()
        {
            IP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9999);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                client.Connect(IP);
            }
            catch
            {
                MessageBox.Show("Không thể kết nối server!", "Lỗi!");
                return;
            }
            
            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
        }

        void Close()
        {
            client.Close();
        }

        void Send()
        {
            string content = txbName.Text + ": " + txbMessage.Text + "\n";
            string encrypted = Cryptography.Encrypt(content, txbName.Text);
            string secondencrypted = Cryptography.Encrypt(encrypted, "vuvinhhien@gmail.com");
            if (txbMessage.Text != string.Empty)
                 client.Send(Serialize(secondencrypted));
        }



        void Receive()
        {   try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);

                    string message = (string)Deserialize(data);

                    if ((message[0] == '1') && (message[1] == '2') && (message[2] == '7'))
                    {
                        listView1.Items.Add(message);
                    }
                    else
                    {

                        string decrypted = Cryptography.Decrypt(message, "vuvinhhien@gmail.com");
                        string seconddecrypted = Cryptography.Decrypt(decrypted, "nguyenhoangtuan@gmail.com");
                        AddMessage(seconddecrypted);
                    }
                }
            }
            catch
            {
                Close();
            }
                
        }

        void AddMessage(string s)
        {
            lsvMessage.Items.Add(new ListViewItem() { Text = s });
            txbMessage.Clear();
        }

        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private void Client_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }
    }
}
