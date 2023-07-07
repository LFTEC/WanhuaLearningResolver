using System.Security.Cryptography;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using System.Net.Http.Headers;

namespace WanhuaLearningResolver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var username = textBox1.Text.Trim();
            var password = textBox2.Text.Trim();

            HttpClient client = new HttpClient();
            var result = client.GetAsync("https://learning.whchem.com:8836/Api/ServiceInfo/GetEncryptKey").Result;
            if (!result.IsSuccessStatusCode)
            {
                MessageBox.Show(result.StatusCode.ToString());
                return;
            }

            var response = result.Content.ReadAsStringAsync().Result;
            var responseData = JObject.Parse(response);
            var privateKey = responseData["data"].ToString();

            privateKey = privateKey.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "").Replace("\r", "").Replace("\n", "");

            JObject login = new JObject();
            login["username"] = textBox1.Text.Trim();
            login["password"] = textBox2.Text.Trim();

            RSACryptoService cryptoService = new RSACryptoService(privateKey);
            var ciphertext = cryptoService.EncryptToBase64(login.ToString());

            client = new HttpClient();
            client.DefaultRequestHeaders.Connection.Add("keep-alive");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 13; 2206123SC Build/TKQ1.220829.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/114.0.5735.196 Mobile Safari/537.36 uni-app Html5Plus/1.0 (Immersed/31.636364)");
            HttpContent httpContent = new StringContent(ciphertext);

            result = client.PostAsync("https://learning.whchem.com:8836/Api/Login/ByPassword", httpContent).Result;
            if (result.IsSuccessStatusCode)
            {
                response = result.Content.ReadAsStringAsync().Result;
                responseData = JObject.Parse(response);
                textBox3.Text = responseData["data"]["loginResult"]["token"].ToString();
            }
            else
            {
                MessageBox.Show(result.StatusCode.ToString());
                return;
            }


        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(richTextBox1.Text.Trim())) return;

            if (String.IsNullOrEmpty(textBox3.Text.Trim())) return;

            var aes = System.Security.Cryptography.Aes.Create();

            string token = textBox3.Text.Trim();
            var date = dateTimePicker1.Value;
            var key = token + date.ToString("yyyyMd");

            var md5 = MD5.Create();
            aes.Key = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
            aes.IV = md5.ComputeHash(aes.Key);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            try
            {
                var cipherText = richTextBox1.Text.Trim();
                var cipherTextBytes = BinaryConverter.ToBinary(cipherText);
                var final = System.Text.Encoding.UTF8.GetString(aes.CreateDecryptor().TransformFinalBlock(cipherTextBytes, 0, cipherTextBytes.Length));

                var json = JObject.Parse(final);
                richTextBox2.Text = json.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(richTextBox2.Text.Trim())) return;

            if (String.IsNullOrEmpty(textBox3.Text.Trim())) return;

            var aes = System.Security.Cryptography.Aes.Create();

            string token = textBox3.Text.Trim();
            var key = token + DateTime.Now.ToString("yyyyMd");

            var md5 = MD5.Create();
            aes.Key = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
            aes.IV = md5.ComputeHash(aes.Key);
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            try
            {
                var text = richTextBox2.Text.Trim();
                var textBytes = Encoding.UTF8.GetBytes(text);
                var final = BinaryConverter.ToString(aes.CreateEncryptor().TransformFinalBlock(textBytes, 0, textBytes.Length));

                richTextBox1.Text = final;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}