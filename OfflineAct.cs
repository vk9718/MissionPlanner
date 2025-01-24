using System;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using System.Linq;
using System.IO;
using Microsoft.Win32;

public class OfflineActivation
{
    private const string SecretKey = "Fej0MT9ezDa,Hc-2)5+a";
    private const string ActivationStorePath = "activation_store.dat";
    private const int MaxAllowedDriftDays = 30; // Moved to class level

    public class ActivationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    private class TimeValidator
    {
        private readonly string storagePath;

        public TimeValidator(string path)
        {
            storagePath = path;
            InitializeTimeStore();
        }

        private void InitializeTimeStore()
        {
            if (!File.Exists(storagePath))
            {
                SaveLastKnownTime(DateTime.UtcNow);
            }
        }

        public bool IsSystemTimeValid()
        {
            try
            {
                var systemTime = DateTime.UtcNow;
                var lastKnownTime = GetLastKnownTime();

                if (!lastKnownTime.HasValue)
                {
                    SaveLastKnownTime(systemTime);
                    return true;
                }

                if (systemTime < lastKnownTime.Value.AddDays(-MaxAllowedDriftDays))
                {
                    return false;
                }

                if (systemTime > lastKnownTime.Value)
                {
                    SaveLastKnownTime(systemTime);
                }

                return true;
            }
            catch
            {
                return true;
            }
        }
        private void SaveProductKey(string productKey)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\MyApp");
            key.SetValue("ProductKey", productKey);
            key.Close();
        }
        private string GetProductKey()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\MyApp");
            if (key != null)
            {
                return key.GetValue("ProductKey")?.ToString();
            }
            return null;
        }
        private void DeleteProductKey()
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\MyApp", true);
            if (key != null)
            {
                key.DeleteValue("ProductKey");
                key.Close();
            }
        }

        private DateTime? GetLastKnownTime()
        {
            try
            {
                if (File.Exists(storagePath))
                {
                    var encryptedTime = File.ReadAllText(storagePath);
                    var timeBytes = Convert.FromBase64String(encryptedTime);
                    var timeString = Encoding.UTF8.GetString(timeBytes);
                    return DateTime.ParseExact(timeString, "yyyy-MM-dd HH:mm:ss", null);
                }
            }
            catch { }
            return null;
        }

        private void SaveLastKnownTime(DateTime time)
        {
            try
            {
                var timeString = time.ToString("yyyy-MM-dd HH:mm:ss");
                var timeBytes = Encoding.UTF8.GetBytes(timeString);
                var encryptedTime = Convert.ToBase64String(timeBytes);
                File.WriteAllText(storagePath, encryptedTime);
            }
            catch { }
        }
    }

    private string GetSystemIdentifier()
    {
        try
        {
            var systemInfo = new StringBuilder();

            // Get CPU ID
            using (var mc = new ManagementClass("Win32_Processor"))
            using (var moc = mc.GetInstances())
            {
                foreach (var mo in moc)
                {
                    systemInfo.Append(mo.Properties["ProcessorId"].Value ?? "");
                    break;
                }
            }

            // Get Motherboard serial
            using (var mc = new ManagementClass("Win32_BaseBoard"))
            using (var moc = mc.GetInstances())
            {
                foreach (var mo in moc)
                {
                    systemInfo.Append(mo.Properties["SerialNumber"].Value ?? "");
                    break;
                }
            }

            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(systemInfo.ToString()));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 16);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to generate system identifier", ex);
        }
    }

    public string GenerateActivationKey(int validityDays)
    {
        var timeValidator = new TimeValidator(ActivationStorePath);
        if (!timeValidator.IsSystemTimeValid())
        {
            throw new Exception("System time appears to be invalid");
        }

        try
        {
            var systemId = GetSystemIdentifier();
            var currentTime = DateTime.UtcNow;
            var expirationDate = currentTime.AddDays(validityDays);

            var dataToEncrypt = $"{systemId}|{currentTime:yyyy-MM-dd HH:mm:ss}|{expirationDate:yyyy-MM-dd}";
            var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            var dataBytes = Encoding.UTF8.GetBytes(dataToEncrypt);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                var hash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);

                var activationKey = $"{systemId.Substring(0, 4)}-{systemId.Substring(4, 4)}-" +
                                  $"{hash.Substring(0, 4)}-{hash.Substring(4, 4)}";

                var encryptedDates = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        $"{currentTime:yyyy-MM-dd HH:mm:ss}|{expirationDate:yyyy-MM-dd}"));

                return $"{activationKey}_{encryptedDates}";
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error generating activation key", ex);
        }
    }

    public ActivationResult ValidateActivationKey(string activationKey)
    {
        var timeValidator = new TimeValidator(ActivationStorePath);
        if (!timeValidator.IsSystemTimeValid())
        {
            return new ActivationResult
            {
                IsValid = false,
                Message = "System time appears to be invalid"
            };
        }

        try
        {
            var currentSystemId = GetSystemIdentifier();
            var currentTime = DateTime.UtcNow;

            var parts = activationKey.Split('_');
            if (parts.Length != 2)
            {
                return new ActivationResult { IsValid = false, Message = "Invalid key format" };
            }

            var key = parts[0];
            var encryptedDates = parts[1];

            // Decode dates
            var dates = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedDates)).Split('|');
            var generationTime = DateTime.ParseExact(dates[0], "yyyy-MM-dd HH:mm:ss", null);
            var expirationDate = DateTime.ParseExact(dates[1], "yyyy-MM-dd", null);

            // More lenient time validation
            if (currentTime < generationTime.AddDays(-MaxAllowedDriftDays))
            {
                return new ActivationResult
                {
                    IsValid = false,
                    Message = "System time appears to be invalid",
                    ExpirationDate = expirationDate
                };
            }

            // Check expiration
            if (currentTime > expirationDate)
            {
                return new ActivationResult
                {
                    IsValid = false,
                    Message = "Activation key has expired",
                    ExpirationDate = expirationDate
                };
            }

            // Verify system ID
            var keySystemId = key.Split('-')[0] + key.Split('-')[1];
            if (!keySystemId.Equals(currentSystemId.Substring(0, 8),
                StringComparison.OrdinalIgnoreCase))
            {
                return new ActivationResult
                {
                    IsValid = false,
                    Message = "This activation key is not valid for this system"
                };
            }

            // Validate hash
            var dataToValidate = $"{currentSystemId}|{generationTime:yyyy-MM-dd HH:mm:ss}|{expirationDate:yyyy-MM-dd}";
            var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            var dataBytes = Encoding.UTF8.GetBytes(dataToValidate);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(dataBytes);
                var expectedHash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                var actualHash = key.Split('-')[2] + key.Split('-')[3];

                if (expectedHash.Substring(0, 8).Equals(actualHash,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return new ActivationResult
                    {
                        IsValid = true,
                        Message = "Activation key is valid",
                        ExpirationDate = expirationDate
                    };
                }
            }

            return new ActivationResult { IsValid = false, Message = "Invalid activation key" };
        }
        catch (Exception ex)
        {
            return new ActivationResult
            {
                IsValid = false,
                Message = $"Error validating key: {ex.Message}"
            };
        }
    }
}