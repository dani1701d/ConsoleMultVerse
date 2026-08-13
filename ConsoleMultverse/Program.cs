using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.Xml.Linq;

string command;

Console.WriteLine("Это консольная версия MultVerse. Чтобы узнать о этой программе больше, напишите 'help'.");

while (true)
{
    Console.Write("> ");
    command = Console.ReadLine();
    string[] arguments = command.Split(' ');

    switch (arguments[0])
    {
        case "exit":
            Environment.Exit(0);
            break;
        case "quit":
            Environment.Exit(0);
            break;
        case "help":
            DisplayHelp();
            break;
        case "about":
            DisplayAbout();
            break;
        case "clear":
            Console.Clear();
            Console.Write("\x1b[3J");
            break;
        case "get-recommend":
            await GetAnimsFromMainPage(0);
            await GetAnimsFromMainPage(1);
            await GetAnimsFromMainPage(2);
            await GetAnimsFromMainPage(3);
            break;
        case "get-newest":
            await GetAnimsFromMainPage(4);
            await GetAnimsFromMainPage(5);
            await GetAnimsFromMainPage(6);
            await GetAnimsFromMainPage(7);
            await GetAnimsFromMainPage(8);
            await GetAnimsFromMainPage(9);
            await GetAnimsFromMainPage(10);
            await GetAnimsFromMainPage(11);
            break;
        case "get-best":
            await GetAnimsFromMainPage(12);
            await GetAnimsFromMainPage(13);
            await GetAnimsFromMainPage(14);
            await GetAnimsFromMainPage(15);
            await GetAnimsFromMainPage(16);
            await GetAnimsFromMainPage(17);
            await GetAnimsFromMainPage(18);
            await GetAnimsFromMainPage(19);
            break;
        case "open":
            await DownloadAndOpen(arguments[1], await GetAnimFormat(arguments[1]));
            break;
        case "delete-anims":
            Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsoleMultVerse", "temp"), true);
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsoleMultVerse", "temp"));
            break;
        case "search":
            await GetAnimsFromSearch(arguments[1]);
            break;
        default:
            DisplayError($"Команды {arguments[0]} не существует.");
            break;
    }
}

void DisplayHelp()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Список команд:");
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("exit или quit - закрывает терминал");
    Console.WriteLine("help - выводит это меню");
    Console.WriteLine("about - информация об этой программе");
    Console.WriteLine("clear - очистить терминал");
    Console.WriteLine("get-recommend - выводит 4 рекомендованных анимаций");
    Console.WriteLine("get-newest - выводит 8 недавно вышедших анимаций");
    Console.WriteLine("get-best - выводит 8 лучших анимаций");
    Console.WriteLine("open [ID анимации] - скачивает анимацию в папку AppData/ConsoleMultVerse/temp и открывает её");
    Console.WriteLine("delete-anims - очищает все анимации в папке AppData/ConsoleMultVerse/temp");
    Console.WriteLine("search [запрос] - выводит все анимации по запросу");
    Console.ResetColor();
    Console.WriteLine();
}

void DisplayAbout()
{
    Console.WriteLine();
    Console.WriteLine("Цель этой программы - перенести главные возможности сайта MultVerse (и они будут улучшаться (наверное))");
    Console.WriteLine("А так надеюсь, что программа будет полезна.");
    Console.WriteLine();
}

void DisplayError(string error)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(error);
    Console.ResetColor();
}

async Task GetAnimsFromMainPage(int index)
{
    using (HttpClient client = new HttpClient())
    {
        try
        { 
            string mainPageHTML = await client.GetStringAsync("https://www.multverse.ru/index.php");

            var doc = new HtmlDocument();
            doc.LoadHtml(mainPageHTML);

            HtmlNodeCollection elements = doc.DocumentNode.SelectNodes("//td[contains(@style, 'width: 125px') and @valign='top']");

            if (elements != null)
            {
                foreach (HtmlNode elem in elements)
                {
                    if (elem != null)
                    {
                        string rawText = System.Net.WebUtility.HtmlDecode(elem.InnerText);
                        string[] lines = rawText.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(line => line.Trim())
                                         .Where(line => !string.IsNullOrEmpty(line))
                                         .ToArray();

                        string title = "";
                        string rating = "";
                        string author = "";

                        foreach (string line in lines)
                        {
                            if (line.StartsWith("Рейтинг:"))
                            {
                                rating = line;
                            }
                            else if (line.StartsWith("От:"))
                            {
                                author = line;
                            }
                            else if (string.IsNullOrEmpty(title))
                            {
                                title = line;
                            }
                        }

                        string animationId = "ID не найден";

                        // 1. Ищем тег <a>, у которого ссылка содержит "view.php?id="
                        HtmlNode linkNode = elem.SelectSingleNode(".//a[contains(@href, 'view.php?id=')]");

                        if (linkNode != null)
                        {
                            // Получаем значение атрибута href (например, "view.php?id=4635")
                            string hrefValue = linkNode.GetAttributeValue("href", "");

                            // 2. Извлекаем только цифры после "id="
                            int idIndex = hrefValue.IndexOf("id=");
                            if (idIndex != -1)
                            {
                                animationId = hrefValue.Substring(idIndex + 3); // Вырезаем всё, что после "id="

                                // Если в конце ссылки есть другие параметры (например, &page=2), очищаем их:
                                int ampersandIndex = animationId.IndexOf('&');
                                if (ampersandIndex != -1)
                                {
                                    animationId = animationId.Substring(0, ampersandIndex);
                                }
                            }
                        }

                        if (elements.IndexOf(elem) == index)
                        {
                            Console.WriteLine(title);
                            Console.WriteLine(rating);
                            Console.WriteLine(author);
                            Console.WriteLine($"ID: {animationId}");
                            Console.WriteLine();
                        }
                    }
                }
            }
        }
        catch (Exception e) { DisplayError($"Произошла ошибка: {e}"); }
    }
}

async Task DownloadAndOpen(string animID, string fileformat)
{
    string videoURL = "https://www.multverse.ru/videos/";
    string flashURL = "https://www.multverse.ru/swf/";

    string URL = "";
    if (fileformat == "flv")
    {
        URL = videoURL + animID + "." + fileformat;
    }
    if (fileformat == "swf")
    {
        URL = flashURL + animID + "." + fileformat;
    }

    try
    {
        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsoleMultVerse", "temp"));
        string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConsoleMultVerse", "temp", animID + "." + fileformat);

        if (await CheckWebFileAsync(URL))
        {
            using (HttpClient client = new HttpClient())
            {
                // Получаем файл как массив байт
                byte[] fileBytes = await client.GetByteArrayAsync(URL);

                // Записываем байты на диск
                await File.WriteAllBytesAsync(outputPath, fileBytes);
            }

            Console.WriteLine("Файл успешно скачан!");

            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
        }
        else { Console.WriteLine("Файл на сервере не найден."); }
    }
    catch (Exception e) { DisplayError($"Произошла ошибка: {e}"); }
}

async Task<bool> CheckWebFileAsync(string url)
{
    try
    {
        using (HttpClient client = new HttpClient())
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
    catch
    {
        return false;
    }
}

async Task<string> GetAnimFormat(string animID)
{
    string videoURL = "https://www.multverse.ru/videos/" + animID + ".flv";
    string flashURL = "https://www.multverse.ru/swf/" + animID + ".swf";
    if (await CheckWebFileAsync(videoURL))
    {
        return "flv";
    }
    if (await CheckWebFileAsync(flashURL))
    {
        return "swf";
    }
    return "";
}

async Task GetAnimsFromSearch(string query)
{
    using (HttpClient client = new HttpClient())
    {
        try
        {
            string mainPageHTML = await client.GetStringAsync("https://www.multverse.ru/search.php?q=" + query);

            var doc = new HtmlDocument();
            doc.LoadHtml(mainPageHTML);

            HtmlNodeCollection elements = doc.DocumentNode.SelectNodes("//td[contains(@style, 'width: 130px') and @valign='top']");

            if (elements != null)
            {
                foreach (HtmlNode elem in elements)
                {
                    if (elem != null)
                    {
                        string rawText = System.Net.WebUtility.HtmlDecode(elem.InnerText);
                        string[] lines = rawText.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(line => line.Trim())
                                         .Where(line => !string.IsNullOrEmpty(line))
                                         .ToArray();

                        string title = "";
                        string rating = "";
                        string author = "";

                        foreach (string line in lines)
                        {
                            if (line.StartsWith("Рейтинг:"))
                            {
                                rating = line;
                            }
                            else if (line.StartsWith("От:"))
                            {
                                author = line;
                            }
                            else if (string.IsNullOrEmpty(title))
                            {
                                title = line;
                            }
                        }

                        string animationId = "ID не найден";

                        // 1. Ищем тег <a>, у которого ссылка содержит "view.php?id="
                        HtmlNode linkNode = elem.SelectSingleNode(".//a[contains(@href, 'view.php?id=')]");

                        if (linkNode != null)
                        {
                            // Получаем значение атрибута href (например, "view.php?id=4635")
                            string hrefValue = linkNode.GetAttributeValue("href", "");

                            // 2. Извлекаем только цифры после "id="
                            int idIndex = hrefValue.IndexOf("id=");
                            if (idIndex != -1)
                            {
                                animationId = hrefValue.Substring(idIndex + 3); // Вырезаем всё, что после "id="

                                // Если в конце ссылки есть другие параметры (например, &page=2), очищаем их:
                                int ampersandIndex = animationId.IndexOf('&');
                                if (ampersandIndex != -1)
                                {
                                    animationId = animationId.Substring(0, ampersandIndex);
                                }
                            }
                        }

                        Console.WriteLine(title);
                        Console.WriteLine(rating);
                        Console.WriteLine(author);
                        Console.WriteLine($"ID: {animationId}");
                        Console.WriteLine();
                    }
                }
            }
        }
        catch (Exception e) { DisplayError($"Произошла ошибка: {e}"); }
    }
}