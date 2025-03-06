using System;
using System.Data.SQLite;
using System.IO;

class Program
{
    static void Main()
    {
        string dbFilePath = "customer-list.db";
        string connectionString = $"Data Source={dbFilePath};Version=3;";

        if (!File.Exists(dbFilePath))
        {
            SQLiteConnection.CreateFile(dbFilePath);
            Console.WriteLine("Database oluşturuldu!");
        }

        using (var connection = new SQLiteConnection(connectionString))
        {
            connection.Open();
            Console.WriteLine("Database bağlantısı başarılı!");

            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SiraNo INTEGER,
                    Name TEXT,
                    BirthYear TEXT,
                    Country TEXT
                );
            ";
            using (var command = new SQLiteCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
            }

            while (true)
            {
                Console.WriteLine("\n*");
                Console.WriteLine("\n1- Listeyi gör.");
                Console.WriteLine("2- Üye ekle.");
                Console.WriteLine("3- Üye sil.");
                Console.WriteLine("4- Üye bilgilerini güncelle.");
                Console.WriteLine("5- Ekranı temizle.");
                Console.WriteLine("6- Çıkış");
                Console.Write("\nBir işlem seçin: ");
                string selection = Console.ReadLine();

                switch (selection)
                {
                    case "1":
                        ViewList(connection);
                        break;
                    case "2":
                        AddUser(connection);
                        break;
                    case "3":
                        DeleteUser(connection);
                        break;
                    case "4":
                        UpdateUser(connection);
                        break;
                    case "5":
                        Console.Clear();
                        break;
                    case "6":
                        Console.WriteLine("Programdan çıkılıyor...");
                        return;
                    default:
                        Console.WriteLine("\nGeçersiz seçim, tekrar deneyin.");
                        break;
                }
            }
        }
    }

    // Sıra numaralarını alfabetik sıraya göre güncelle
    static void UpdateSiraNo(SQLiteConnection connection)
    {
        string updateQuery = @"
            UPDATE Users
            SET SiraNo = (
                SELECT COUNT(*)
                FROM Users u2
                WHERE u2.Name <= Users.Name
            );
        ";
        using (var updateCommand = new SQLiteCommand(updateQuery, connection))
        {
            updateCommand.ExecuteNonQuery();
        }
    }

    // "Değiştirme" veya "silme" işlemleri için "sıra numarası" kontrolü yap.
    static bool CheckUserExists(SQLiteConnection connection, int siraNo)
    {
        string checkQuery = "SELECT COUNT(*) FROM Users WHERE SiraNo = @SiraNo;";
        using (var checkCommand = new SQLiteCommand(checkQuery, connection))
        {
            checkCommand.Parameters.AddWithValue("@SiraNo", siraNo);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());
            return count > 0;
        }
    }

    // Üye listesini göster.
    static void ViewList(SQLiteConnection connection)
    {
        UpdateSiraNo(connection);

        Console.WriteLine("\nÜyelerin listesi:");
        Console.WriteLine();
        string selectQuery = "SELECT * FROM Users ORDER BY Name;";
        using (var command = new SQLiteCommand(selectQuery, connection))
        using (var reader = command.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.WriteLine("Liste boş.");
                return;
            }
            while (reader.Read())
            {
                Console.WriteLine($"{reader["SiraNo"]}. {reader["Name"]} - {reader["BirthYear"]} - {reader["Country"]}");
            }
        }
    }

    // Üye ekle.
    static void AddUser(SQLiteConnection connection)
    {
        Console.Write("İsim (Çıkmak için Enter'a basın): ");
        string name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Geçersiz işlem. Menüye dönülüyor...");
            return;
        }

        string birthYear;
        int year;
        do
        {
            Console.Write("Doğum yılı (yyyy, 1900-2050): ");
            birthYear = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(birthYear))
            {
                Console.WriteLine("Geçersiz işlem. Menüye dönülüyor...");
                return;
            }
            if (birthYear.Length != 4 || !int.TryParse(birthYear, out year) || year < 1900 || year > 2050)
            {
                Console.WriteLine("Geçerli bir yıl girin (1900-2050 arası).");
            }
        } while (birthYear.Length != 4 || !int.TryParse(birthYear, out year) || year < 1900 || year > 2050);

        Console.Write("Ülke: ");
        string country = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(country))
        {
            Console.WriteLine("Geçersiz işlem. Menüye dönülüyor...");
            return;
        }

        string insertQuery = @"
        INSERT INTO Users (Name, BirthYear, Country)
        VALUES (@Name, @BirthYear, @Country);
    ";
        using (var command = new SQLiteCommand(insertQuery, connection))
        {
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@BirthYear", birthYear);
            command.Parameters.AddWithValue("@Country", country);
            command.ExecuteNonQuery();
        }

        Console.WriteLine($"Üye başarıyla eklendi. Listeyi görmek için 1'e basın.");
    }

    // Üye sil.
    static void DeleteUser(SQLiteConnection connection)
    {
        ViewList(connection);

        Console.Write("\nSilmek istediğiniz üyenin sıra numarasını girin: ");
        if (int.TryParse(Console.ReadLine(), out int rowNumber))
        {
            if (!CheckUserExists(connection, rowNumber))
            {
                Console.WriteLine("\nÜye bulunamadı.");
                return;
            }

            string deleteQuery = "DELETE FROM Users WHERE SiraNo = @SiraNo;";
            using (var command = new SQLiteCommand(deleteQuery, connection))
            {
                command.Parameters.AddWithValue("@SiraNo", rowNumber);
                command.ExecuteNonQuery();
            }

            Console.WriteLine("\nÜye başarıyla silindi.");
        }
        else
        {
            Console.WriteLine("\nGeçersiz sıra numarası.");
        }
    }

    // Üye bilgilerini güncelle.
    static void UpdateUser(SQLiteConnection connection)
    {
        ViewList(connection);

        Console.Write("\nBilgilerini güncellemek istediğiniz üyenin sıra numarası: ");
        if (int.TryParse(Console.ReadLine(), out int rowNumber))
        {
            if (!CheckUserExists(connection, rowNumber))
            {
                Console.WriteLine("\nÜye bulunamadı.");
                return;
            }

            Console.Write("Yeni isim: ");
            string name = Console.ReadLine() ?? "Bilinmiyor";

            string birthYear;
            int year;
            do
            {
                Console.Write("Yeni doğum yılı (yyyy, 1900-2050): ");
                birthYear = Console.ReadLine() ?? "1900";
                if (!int.TryParse(birthYear, out year) || birthYear.Length != 4 || year < 1900 || year > 2050)
                {
                    Console.WriteLine("Geçerli bir yıl girin (1900-2050 arası).");
                }
            } while (!int.TryParse(birthYear, out year) || birthYear.Length != 4 || year < 1900 || year > 2050);

            Console.Write("Yeni Ülke: ");
            string country = Console.ReadLine() ?? "Bilinmiyor";

            string updateQuery = @"
                UPDATE Users
                SET Name = @Name,
                    BirthYear = @BirthYear,
                    Country = @Country
                WHERE SiraNo = @SiraNo;
            ";
            using (var command = new SQLiteCommand(updateQuery, connection))
            {
                command.Parameters.AddWithValue("@SiraNo", rowNumber);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@BirthYear", birthYear);
                command.Parameters.AddWithValue("@Country", country);
                command.ExecuteNonQuery();
            }

            Console.WriteLine("Üye başarıyla güncellendi.");
        }
        else
        {
            Console.WriteLine("\nGeçersiz sıra numarası.");
        }
    }
}

// GKA