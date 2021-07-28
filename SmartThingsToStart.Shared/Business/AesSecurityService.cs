using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SmartThingsToStart.Server.Business
{
	public class AesSecurityService<T> : ISecurityService<T>
	{
		private readonly Aes _aes;

		public AesSecurityService(string password, string salt, int keySize = 256)
		{
			var rfc2898 = new Rfc2898DeriveBytes(password, Encoding.Unicode.GetBytes(salt), 100);

			var aes = Aes.Create();

			aes.Padding = PaddingMode.PKCS7;
			aes.KeySize = keySize;
			aes.Key = rfc2898.GetBytes(keySize / 8);
			aes.IV = rfc2898.GetBytes(16);

			_aes = aes;
		}

		public string Encrypt(T value)
		{
			var json = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new EntityWrapper<T>(value)));
			byte[] encryptedJson;

			using (var output = new MemoryStream())
			{
				using (var stream = new CryptoStream(output, _aes.CreateEncryptor(), CryptoStreamMode.Write))
				{
					stream.Write(json, 0, json.Length);
				}

				encryptedJson = output.ToArray();
			}

			return Convert.ToBase64String(encryptedJson);
		}

		public T Decrypt(string value)
		{
			var encryptedJson = Convert.FromBase64String(value);
			byte[] json;

			using (var output = new MemoryStream())
			{
				using (var stream = new CryptoStream(output, _aes.CreateDecryptor(), CryptoStreamMode.Write))
				{
					stream.Write(encryptedJson, 0, encryptedJson.Length);
				}

				json = output.ToArray();
			}

			return JsonConvert.DeserializeObject<EntityWrapper<T>>(Encoding.Unicode.GetString(json)).GetData();
		}

		private class EntityWrapper<T>
		{
			public EntityWrapper(T value)
			{
				Data = value;
				Id = Guid.NewGuid(); // Ensure to always have something different
			}

			public EntityWrapper()
			{
			}

			public Guid Id { get; set; }

			public T Data { get; set; }

			public T GetData()
			{
				if (Id == Guid.Empty)
				{
					throw new InvalidOperationException("Invalid data");
				}

				return Data;
			}
		}
	}
}