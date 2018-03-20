using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Telegram_bot_intensive
{
    class Program
    {
        static void Main(string[] args)
        {
            int update_id = 0;
            string messageFromId = "";
            string messageText = "";
            string first_name = "";
            string last_name = "";
            string token = "594904432:AAFPNbUe2fNeSs9Z7CJCMnwbdx-4d74DjEQ";

            WebClient webClient = new WebClient();

            string startUrl = $"https://api.telegram.org/bot{token}";

            string[,] cities = new string[,] { { "Москва", @"http://www.eurometeo.ru/russia/moskva/export/xml/data/" }, { "Челябинск", @"http://www.eurometeo.ru/russia/chelyabinskaya-oblast/chelyabinsk/export/xml/data/" }, { "Санкт-Петербург", @"http://www.eurometeo.ru/russia/sankt-peterburg/export/xml/data/" }, { "Екатеринбург", @"http://www.eurometeo.ru/russia/sverdlovskaya-oblast/ekaterinburg/export/xml/data/" }, {"Уфа", @"http://www.eurometeo.ru/russia/bashkortostan/ufa/export/xml/data/" }, {"Казань", @"http://www.eurometeo.ru/russia/tatarstan/kazan/export/xml/data/" } };

            while (true)
            {
                string url = $"{startUrl}/getUpdates?offset={update_id + 1}";
                string response = webClient.DownloadString(url);

                var array = JObject.Parse(response)["result"].ToArray();

                foreach (var msg in array)
                {
                    update_id = Convert.ToInt32(msg["update_id"]);
                    try
                    {
                        first_name = msg["message"]["from"]["first_name"].ToString();
                        last_name = msg["message"]["from"]["last_name"].ToString();
                        messageFromId = msg["message"]["from"]["id"].ToString();
                        messageText = msg["message"]["text"].ToString();

                        Console.WriteLine($"{first_name} {last_name} id={messageFromId} \"{messageText}\"");

                        if (messageText == @"/start")
                        {
                            url = $"{startUrl}/sendMessage?chat_id={messageFromId}&text=Здравствуйте, {first_name} {last_name}!\n\nДанный сервис предоставляет информацию о погоде на текущую дату.\n\nСписок моих команд:\n/start - запуск бота\n/city - список городов, для которых доступен прогноз\n Либо напишите название города, если уверенны в его наличии в нашем списке.";
                            webClient.DownloadString(url);
                        }
                        else if (messageText == @"/city")
                        {
                            url = $"{startUrl}/sendMessage?chat_id={messageFromId}&text={GetCitiesMenu(cities)}";
                            webClient.DownloadString(url);
                        }
                        else if (GetCityUrl(messageText, cities) != "")
                        {
                            url = $"{startUrl}/sendMessage?chat_id={messageFromId}&text={GetWeather(GetCityUrl(messageText, cities), messageText)}";
                            webClient.DownloadString(url);
                        }
                        else
                        {
                            url = $"{startUrl}/sendMessage?chat_id={messageFromId}&text=В моем списке нет города \"{messageText}\".\n{first_name}, пожалуйста, воспользуйтесь списком доступных городов, введя команду /city";
                            webClient.DownloadString(url);
                        }
                    }
                    catch { }
                }

                Thread.Sleep(250); //задержка 250мс
            }
        }

        private static string GetWeather(string cityUrl, string messageText)
        {
            string text = "";

            if (messageText.ToLower() == "москва")
            {
                 text += $"Погода в Москве:\n\n";
            }
            else if (messageText.ToLower() == "уфа")
            {
                text += $"Погода в Уфе:\n\n";
            }
            else if (messageText.ToLower() == "казань")
            {
                text += $"Погода в Казани:\n\n";
            }
            else
            {
                text += $"Погода в {messageText}е:\n\n";
            }

             string xmlData = new WebClient().DownloadString(cityUrl);

            var xmlDataItem = XDocument.Parse(xmlData)
                                       .Descendants("weather")
                                       .Descendants("city")
                                       .Descendants("step").ToArray();
            int step = 0;

            foreach( var item in xmlDataItem)
            {
                text += $@"Время: {item.Element("datetime").Value
                                                           .Replace("04:00:00", "Ночь")
                                                           .Replace("10:00:00", "Утро")
                                                           .Replace("16:00:00", "День")
                                                           .Replace("22:00:00", "Вечер")}
Давление: {item.Element("pressure").Value} мм.рт.ст.
Температура: {item.Element("temperature").Value} градусов
Влажность: {item.Element("humidity").Value}%
Облачность: {CloudConverter(item.Element("cloudcover").Value)}


";
                step++;
                if (step > 3) break; 
            }

            return text;

        }

        private static string CloudConverter(string value)
        {
            string cloudcover = "";
            double cloud = Convert.ToDouble(value);

            if (cloud < 20)
            {
                cloudcover = "Ясно";
            }
            else if (cloud >= 20 && cloud < 40)
            {
                cloudcover = "Малооблачно";
            }
            else if (cloud >= 40 && cloud < 70)
            {
                cloudcover = "Переменная облачность";
            }
            else
            {
                cloudcover = "Пасмурно";
            }
            return cloudcover;
        }

        private static string GetCityUrl(string messageText, string[,] cities)
        {
            string text = "";
            for (int i = 0; i < cities.GetLength(0); i++)
            {
                if (messageText.ToLower() == cities[i, 0].ToLower())
                {
                    text = $@"{cities[i, 1]}";
                }
            }
            return text;
        }
        

        private static string GetCitiesMenu(string[,] cities)
        {
            string text = "";
            for (int i = 0; i < cities.GetLength(0); i++)
            {
            text += $"{cities[i,0]}\n";
            }
            return text;
        }


    }
}
