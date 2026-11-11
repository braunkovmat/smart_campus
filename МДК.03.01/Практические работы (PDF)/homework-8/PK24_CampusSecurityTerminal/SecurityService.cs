using BCrypt.Net;
using Serilog;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace CampusSecurityTerminal
{
    public class SecurityService
    {
        public SecurityService()
        {
            // Инициализация Serilog для изоляции журнала безопасности
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(@"Logs\security_audit.log".Replace('\\', Path.DirectorySeparatorChar),
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
                .CreateLogger();
        }

        // 1. Хэширование пароля библиотекаря при регистрации (через BCrypt)
        public string HashPassword(string plainPassword)
        {
            // Work factor (сложность) по умолчанию равен 11
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            Log.Information("Создан хэш пароля для учетной записи.");
            return hashedPassword;
        }

        // 2. Верификация пароля при входе библиотекаря на АРМ
        public bool VerifyPassword(string user, string plainPassword, string hashedPassword)
        {
            bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);

            if (isValid)
            {
                Log.Information("УСПЕХ: Пароль пользователя {User} успешно верифицирован.", user);
            }
            else
            {
                Log.Warning("ОТКАЗ: Попытка входа {User} с неверным паролем!", user);
            }

            return isValid;
        }

        // 3. Аудит запросов к закрытым фондам библиотеки (вариант 3)
        public void AuditFundRequest(string user, string fund, bool allowed)
        {
            if (allowed)
                Log.Information("АУДИТ: {User} получил доступ к закрытому фонду '{Fund}'.", user, fund);
            else
                Log.Warning("АУДИТ: {User} получил отказ в доступе к закрытому фонду '{Fund}'!", user, fund);
        }

        // 4. Проверка клиентского цифрового сертификата (X.509)
        public bool VerifyClientCertificate(string certPath, string certPassword)
        {
            try
            {
                if (!File.Exists(certPath))
                {
                    Log.Error("Файл сертификата не найден по пути: {Path}", certPath);
                    return false;
                }

                // Загружаем сертификат контейнера (например, .pfx)
                var certificate = new X509Certificate2(certPath, certPassword);

                // Проверяем срок действия
                if (DateTime.Now < certificate.NotBefore || DateTime.Now > certificate.NotAfter)
                {
                    Log.Warning("Сертификат просрочен или еще не активен. Срок: {From} - {To}", certificate.NotBefore, certificate.NotAfter);
                    return false;
                }

                // Проверяем цепочку доверия
                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                bool isChainValid = chain.Build(certificate);

                if (!isChainValid)
                {
                    Log.Warning("Цепочка сертификатов не прошла проверку доверия или отзывов (CRL).");
                    return false;
                }

                Log.Information("УСПЕХ: Клиентский сертификат '{Subject}' успешно верифицирован.", certificate.Subject);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Критическая ошибка при проверке цифрового сертификата: {Message}", ex.Message);
                return false;
            }
        }
    }
}
