using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace ConsoleApp1
{
    // Модель данных заказа
    public class Order
    {
        public int Id { get; set; }
        public string Client { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }

        public override string ToString()
        {
            return $"ID: {Id}, Клиент: {Client}, Дата: {OrderDate:yyyy-MM-dd}, Сумма: {Amount:C}, Статус: {Status}";
        }
    }

    // Класс для работы с файлами
    public static class FileHandler
    {
        // Обобщенный метод для чтения данных из файла
        public static T ReadFromFile<T>(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException($"Файл {fileName} не найден.");
                }

                string extension = Path.GetExtension(fileName).ToLower();
                string fileContent = File.ReadAllText(fileName);

                switch (extension)
                {
                    case ".json":
                        return JsonSerializer.Deserialize<T>(fileContent);
                    case ".xml":
                        return DeserializeXml<T>(fileContent);
                    case ".csv":
                        return DeserializeCsv<T>(fileContent);
                    default:
                        throw new NotSupportedException($"Формат файла {extension} не поддерживается.");
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }
        }

        // Обобщенный метод для записи данных в файл
        public static void WriteToFile<T>(T data, string fileName)
        {
            try
            {
                string extension = Path.GetExtension(fileName).ToLower();
                string content = string.Empty;

                switch (extension)
                {
                    case ".json":
                        content = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                        break;
                    case ".xml":
                        content = SerializeXml(data);
                        break;
                    case ".csv":
                        content = SerializeCsv(data as List<Order>);
                        break;
                    default:
                        throw new NotSupportedException($"Формат файла {extension} не поддерживается.");
                }

                File.WriteAllText(fileName, content);
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw;
            }
        }

        // Методы для работы с XML
        private static T DeserializeXml<T>(string xmlContent)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xmlContent);
            return (T)serializer.Deserialize(reader);
        }

        private static string SerializeXml<T>(T data)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new StringWriter();
            serializer.Serialize(writer, data);
            return writer.ToString();
        }

        // Методы для работы с CSV
        private static T DeserializeCsv<T>(string csvContent)
        {
            var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var orders = new List<Order>();

            foreach (var line in lines.Skip(1)) // Пропускаем заголовок
            {
                var values = line.Split(',');
                if (values.Length == 5)
                {
                    orders.Add(new Order
                    {
                        Id = int.Parse(values[0]),
                        Client = values[1],
                        OrderDate = DateTime.Parse(values[2]),
                        Amount = decimal.Parse(values[3]),
                        Status = values[4]
                    });
                }
            }

            // Явное приведение типа для обобщенного метода
            if (typeof(T) == typeof(List<Order>))
            {
                return (T)(object)orders;
            }

            throw new InvalidOperationException("Для CSV поддерживается только List<Order>");
        }

        private static string SerializeCsv(List<Order> orders)
        {
            if (orders == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Id,Client,OrderDate,Amount,Status");

            foreach (var order in orders)
            {
                sb.AppendLine($"{order.Id},{order.Client},{order.OrderDate:yyyy-MM-dd},{order.Amount},{order.Status}");
            }

            return sb.ToString();
        }

        // Логирование ошибок
        private static void LogError(Exception ex)
        {
            string logMessage = $"[{DateTime.Now}] Ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}\n";
            File.AppendAllText("error_log.txt", logMessage);
        }
    }

    // Класс для управления заказами
    public class OrderManager
    {
        private List<Order> orders = new List<Order>();

        public void Run()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Система управления заказами ===");
                Console.WriteLine("1. Загрузить заказы из файла");
                Console.WriteLine("2. Сохранить заказы в файл");
                Console.WriteLine("3. Показать все заказы");
                Console.WriteLine("4. Сортировать заказы");
                Console.WriteLine("5. Поиск заказов");
                Console.WriteLine("6. Добавить новый заказ");
                Console.WriteLine("7. Удалить заказ");
                Console.WriteLine("8. Редактировать заказ");
                Console.WriteLine("9. Выход");
                Console.Write("Выберите действие: ");

                if (!int.TryParse(Console.ReadLine(), out int choice))
                {
                    Console.WriteLine("Некорректный ввод. Пожалуйста, введите число.");
                    Console.ReadKey();
                    continue;
                }

                try
                {
                    switch (choice)
                    {
                        case 1:
                            LoadOrders();
                            break;
                        case 2:
                            SaveOrders();
                            break;
                        case 3:
                            DisplayOrders(orders);
                            break;
                        case 4:
                            SortOrders();
                            break;
                        case 5:
                            SearchOrders();
                            break;
                        case 6:
                            AddOrder();
                            break;
                        case 7:
                            RemoveOrder();
                            break;
                        case 8:
                            EditOrder();
                            break;
                        case 9:
                            return;
                        default:
                            Console.WriteLine("Некорректный выбор. Попробуйте снова.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }

                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }

        private void LoadOrders()
        {
            Console.Write("Введите путь к файлу: ");
            string filePath = Console.ReadLine();
            orders = FileHandler.ReadFromFile<List<Order>>(filePath);
            Console.WriteLine($"Успешно загружено {orders.Count} заказов.");
        }

        private void SaveOrders()
        {
            if (orders.Count == 0)
            {
                Console.WriteLine("Нет заказов для сохранения.");
                return;
            }

            Console.Write("Введите путь к файлу: ");
            string filePath = Console.ReadLine();
            FileHandler.WriteToFile(orders, filePath);
            Console.WriteLine("Заказы успешно сохранены.");
        }

        private void DisplayOrders(List<Order> ordersToDisplay)
        {
            if (ordersToDisplay.Count == 0)
            {
                Console.WriteLine("Нет заказов для отображения.");
                return;
            }

            foreach (var order in ordersToDisplay)
            {
                Console.WriteLine(order);
            }
        }

        private void SortOrders()
        {
            Console.WriteLine("Сортировать по:");
            Console.WriteLine("1. ID");
            Console.WriteLine("2. Клиенту");
            Console.WriteLine("3. Дате");
            Console.WriteLine("4. Сумме");
            Console.WriteLine("5. Статусу");
            Console.Write("Выберите вариант сортировки: ");

            if (!int.TryParse(Console.ReadLine(), out int sortChoice))
            {
                Console.WriteLine("Некорректный ввод.");
                return;
            }

            var sortedOrders = sortChoice switch
            {
                1 => orders.OrderBy(o => o.Id).ToList(),
                2 => orders.OrderBy(o => o.Client).ToList(),
                3 => orders.OrderBy(o => o.OrderDate).ToList(),
                4 => orders.OrderBy(o => o.Amount).ToList(),
                5 => orders.OrderBy(o => o.Status).ToList(),
                _ => orders
            };

            DisplayOrders(sortedOrders);
        }

        private void SearchOrders()
        {
            Console.Write("Введите поисковый запрос: ");
            string searchTerm = Console.ReadLine().ToLower();

            var results = orders.Where(o =>
                o.Id.ToString().Contains(searchTerm) ||
                o.Client.ToLower().Contains(searchTerm) ||
                o.OrderDate.ToString("yyyy-MM-dd").Contains(searchTerm) ||
                o.Amount.ToString().Contains(searchTerm) ||
                o.Status.ToLower().Contains(searchTerm)).ToList();

            DisplayOrders(results);
            Console.WriteLine($"Найдено {results.Count} заказов.");
        }

        private void AddOrder()
        {
            var order = new Order();

            Console.Write("Введите ID: ");
            order.Id = int.Parse(Console.ReadLine());

            Console.Write("Введите имя клиента: ");
            order.Client = Console.ReadLine();

            Console.Write("Введите дату заказа (гггг-ММ-дд): ");
            order.OrderDate = DateTime.Parse(Console.ReadLine());

            Console.Write("Введите сумму: ");
            order.Amount = decimal.Parse(Console.ReadLine());

            Console.Write("Введите статус: ");
            order.Status = Console.ReadLine();

            orders.Add(order);
            Console.WriteLine("Заказ успешно добавлен.");
        }

        private void RemoveOrder()
        {
            Console.Write("Введите ID заказа для удаления: ");
            int id = int.Parse(Console.ReadLine());

            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                orders.Remove(order);
                Console.WriteLine("Заказ успешно удален.");
            }
            else
            {
                Console.WriteLine("Заказ не найден.");
            }
        }

        private void EditOrder()
        {
            Console.Write("Введите ID заказа для редактирования: ");
            int id = int.Parse(Console.ReadLine());

            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                Console.WriteLine("Заказ не найден.");
                return;
            }

            Console.WriteLine("Редактирование заказа:");
            Console.WriteLine(order);
            Console.WriteLine("Оставьте поле пустым, чтобы сохранить текущее значение.");

            Console.Write($"Новое имя клиента ({order.Client}): ");
            var newClient = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newClient)) order.Client = newClient;

            Console.Write($"Новая дата заказа ({order.OrderDate:yyyy-MM-dd}): ");
            var newDate = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newDate)) order.OrderDate = DateTime.Parse(newDate);

            Console.Write($"Новая сумма ({order.Amount}): ");
            var newAmount = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newAmount)) order.Amount = decimal.Parse(newAmount);

            Console.Write($"Новый статус ({order.Status}): ");
            var newStatus = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newStatus)) order.Status = newStatus;

            Console.WriteLine("Заказ успешно обновлен.");
        }
    }

    // Главный класс программы
    class Program
    {
        static void Main(string[] args)
        {
            var manager = new OrderManager();
            manager.Run();
        }
    }
}