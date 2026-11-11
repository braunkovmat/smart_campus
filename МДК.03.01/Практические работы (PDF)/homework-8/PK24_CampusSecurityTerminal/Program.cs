using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CampusSecurityTerminal
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== МОДУЛЬ ИБ И АУДИТА АРМ БИБЛИОТЕКИ («УМНЫЙ КАМПУС») ===");

            var security = new SecurityService();

            // Сценарий 1: Регистрация и проверка пароля библиотекаря через BCrypt
            Console.WriteLine("\n[1] Проверка хэширования паролей (BCrypt)...");
            string operatorPassword = "SecureCampusPassword2026!";

            // Хэшируем пароль (эмуляция сохранения в БД)
            string passwordHash = security.HashPassword(operatorPassword);
            Console.WriteLine($"Сгенерированный BCrypt хэш: {passwordHash.Substring(0, 25)}...");

            // Проверяем правильный пароль
            bool ok = security.VerifyPassword("librarian1", "SecureCampusPassword2026!", passwordHash);
            Console.WriteLine($"Верный пароль: {(ok ? "доступ разрешён" : "отказ")}");

            // Проверяем неверный пароль
            bool bad = security.VerifyPassword("librarian1", "WrongPassword123", passwordHash);
            Console.WriteLine($"Неверный пароль: {(bad ? "доступ разрешён" : "отказ")}");

            // Сценарий 2: Аудит запросов к закрытым фондам
            Console.WriteLine("\n[2] Аудит запросов к закрытым фондам...");
            security.AuditFundRequest("librarian1", "Редкие книги", ok);
            security.AuditFundRequest("librarian1", "Архив диссертаций", bad);
            Console.WriteLine("События записаны в журнал аудита.");

            // Сценарий 3: Проверка клиентского сертификата (X.509)
            Console.WriteLine("\n[3] Проверка цифрового сертификата клиента (X.509)...");
            string certPath = Path.Combine("Certs", "operator_card.pfx");

            // Файла нет: метод вернёт false, а система зафиксирует это в журнале
            security.VerifyClientCertificate(certPath, "CertSecret123");

            // Для проверки срока действия создаём просроченный сертификат
            Directory.CreateDirectory("Certs");
            using (var rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest("CN=librarian1", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using var cert = request.CreateSelfSigned(DateTimeOffset.Now.AddYears(-2), DateTimeOffset.Now.AddYears(-1));
                File.WriteAllBytes(certPath, cert.Export(X509ContentType.Pfx, "CertSecret123"));
            }
            security.VerifyClientCertificate(certPath, "CertSecret123");

            Console.WriteLine("\nПроверка завершена. Журнал аудита сохранен в Logs\\security_audit.log");
        }
    }
}
