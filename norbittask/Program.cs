using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using MihaZupan;
using System.IO;
using System.Net;
using System.Xml;
using System.Web;
using System.Xml.Linq;
using System.Net.Mail;
using System.Xml.Serialization;

namespace norbittask
{
    class Program
    {
       
        class ResponseStatus
        {
            public int Code { get; set; }
            public string Message { get; set; }
            public object Exception { get; set; }
            public object PasswordChangeUrl { get; set; }
            public object RedirectUrl { get; set; }
        }

        
            // HTTP-адрес приложения.
            private const string baseUri = "https://047957-crm-bundle.bpmonline.com/";
            // Контейнер для Cookie аутентификации bpm'online. Необходимо использовать в последующих запросах.
            // Это самый важный результирующий объект, для формирования свойств которого разработана
            // вся остальная функциональность примера.
            public static CookieContainer AuthCookie = new CookieContainer();
            // Строка запроса к методу Login сервиса AuthService.svc.
            private const string authServiceUri = baseUri + @"/ServiceModel/AuthService.svc/Login";

        // Выполняет запрос на аутентификацию пользователя.
        public static bool TryLogin(string userName, string userPassword)//АУТЕНТИФИКАЦИЯ В CRM
        {
            // Создание экземпляра запроса к сервису аутентификации.
            var authRequest = HttpWebRequest.Create(authServiceUri) as HttpWebRequest;
            // Определение метода запроса.
            authRequest.Method = "POST";
            // Определение типа контента запроса.
            authRequest.ContentType = "application/json";
            // Включение использования cookie в запросе.
            authRequest.CookieContainer = AuthCookie;

            // Помещение в тело запроса учетной информации пользователя.
            using (var requestStream = authRequest.GetRequestStream())
            {
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(@"{
                    ""UserName"":""" + userName + @""",
                    ""UserPassword"":""" + userPassword + @"""
                    }");
                }
            }

            // Вспомогательный объект, в который будут десериализованы данные HTTP-ответа.
            ResponseStatus status = null;
            // Получение ответа от сервера. Если аутентификация проходит успешно, в свойство AuthCookie будут
            // помещены cookie, которые могут быть использованы для последующих запросов.
            using (var response = (HttpWebResponse)authRequest.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    // Десериализация HTTP-ответа во вспомогательный объект.
                    string responseText = reader.ReadToEnd();
                    status = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<ResponseStatus>(responseText);
                }

            }

            // Проверка статуса аутентификации.
            if (status != null)
            {
                // Успешная аутентификация.
                if (status.Code == 0)
                {
                     return true;
                }
                // Сообщение о неудачной аутентификации.
                Console.WriteLine(status.Message);
            }
            return false;
        }

		private static void SendContact(string phone, string email, string firstname,string lastname, string message)//ОТПРАВЛЯЕМ ЗАПРОС CRM ЧТО БЫ ОНА ДОБАВИЛА ИНФОРМАЦИЮ О НОВОМ ОТЗЫВЕ
        {
            string botServiceUri = "https://047957-crm-bundle.bpmonline.com/0/rest/UsrBotService/CreateContact";
            var requestParams = $"?phone={phone}&" +
            $"email={email}&" +
            $"firstName={firstname}&" +
            $"lastName={lastname}&" +
            $"message={message}";

            var createContactRequest = HttpWebRequest.Create(botServiceUri + requestParams) as HttpWebRequest;

            createContactRequest.Method = "GET";
            createContactRequest.ContentType = "application/json";
            createContactRequest.CookieContainer = AuthCookie;
            ResponseStatus status = null;
            var response = createContactRequest.GetResponse();
        }

        private static ITelegramBotClient Bot;
        static void Main(string[] args)
        {//https://046846-crm-bundle.bpmonline.com/0/rest/UsrBotService/CreateContact?phone=+79510890350&email=wtf@wtfpas.com&firstName=%C8%EC%FF&lastName=%D4%E0%EC%E8%EB%E8%FF
            Console.WriteLine("Успешна ли аутентификация?: {0}", TryLogin("Буканов Максим", "ktybyf9415"));

            //    CreateBpmEntityByOdataHttpExample("Абра Кадабра");
            Console.WriteLine("Для выхода нажмите ENTER...");
            Console.ReadLine();
            var proxy = new HttpToSocks5Proxy("145.239.81.69", 1080);//"204.42.255.254", 1080
            Bot = new TelegramBotClient("726032039:AAGiYCbMbj5CCfAYHv0irIJhtdaAVLk12-4"
                //, proxy
                )
            { Timeout = TimeSpan.FromSeconds(10) };
            var me = Bot.GetMeAsync().Result;
            Console.WriteLine(me.FirstName);
            Bot.OnMessage += Bot_OnMessage;
            Bot.StartReceiving();

            Console.ReadLine();
        }

		public class Forma//КЛАСС ПОЛЬЗОВАТЕЛЯ, КОТОРЫЙ СОБИРАЕТСЯ ОСТАВИТЬ ОТЗЫВ
        {
            public DateTime kogda;
            public string first;
            public string second;
            public string message;
            public string email;
            public string phone;
            public Forma()
            {
                this.email = "";
                this.first = "";
                this.second = "";
                this.message = "";
                this.phone = "";
                this.kogda = new DateTime(2000, 1, 1);
            }
        };

		private static async Task SendEmailAsync(Forma persona)//ОТПРАВКА ПИСЬМА С ДАННЫМИ О ПОЛЬЗОВАТЕЛЕ
        {
            MailAddress from = new MailAddress("t3am.lead@yandex.ru", "Pasha");
            MailAddress to = new MailAddress("maximbu@mail.ru");
            MailMessage m = new MailMessage(from, to);
            m.Subject = "Тест";
            m.Body = $"В итоге мы имеем {persona.email}   {persona.first}   {persona.second}   {persona.kogda}  {persona.message}   {persona.phone}";
            var smtp = new SmtpClient
            {
                Host = "smtp.yandex.ru",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(from.Address, "TeamLead313")
            };
            await smtp.SendMailAsync(m);
            Console.WriteLine("Письмо отправлено");
        }

		private static async void Bot_OnMessage(object sender, MessageEventArgs e)//ПРИНЯТИЕ СООБЩЕНИЙ ОТ БОТА
        {
            var text = e?.Message?.Text;
            if (text == null) return;
            int count = 0;
            string vrem = text;
            while (vrem.IndexOf("#") != -1)//ПРОС7МОТР ЕСЛИ СООБЩЕНИЕ ЯВЛЯЕТСЯ ОТЗЫВОМ
            {
                count++; vrem = vrem.Substring(vrem.IndexOf("#")+1);
            }
            switch (text)
            {
                case "/start"://СООБЩЕНИЕ ПРИ НАЧАЛЕ РАБОТЫ С БОТОМ
                    await Bot.SendTextMessageAsync(chatId: e.Message.Chat,
                    text: $"Привет! Я Бот компании Норбит! здесь вы можете оставить свой отзыв об опыте использования нашими товарами и услугами").ConfigureAwait(false);
                    await Bot.SendTextMessageAsync(chatId: e.Message.Chat,
                    text: $"Вот команды которые я могу выполнять:    \n\r/form").ConfigureAwait(false);
                    break;

                case "/form"://КОГДА ПОЛЬЗОВАТЕЛЬ РЕШИЛ ОТПРАВИТЬ ОТЗЫВ
                    await Bot.SendTextMessageAsync(chatId: e.Message.Chat,
                    text: $"Хорошо. Отзыв так отзыв.\n\r Для того что бы ваш отзыв был сохранен и отправлен, пожалуйста, заполните форму согласно примеру ниже.\n\r" +
                    $"---------------- Шаблон ------------------ " +
                    $"\n\r# Номер вашего телефона" +
                    $"\n\r# Адрес электронной почты" +
                    $"\n\r# Ваш отзыв" +
                    $"\n\r--------------- Пример ------------- " +
                    $"\n\r# 8123457890" +
                    $"\n\r# moyapochta@yandex.ru" +
                    $"\n\r# Норбит - превосходная IT компания, знающая толк в своем деле!").ConfigureAwait(false);
                    Console.WriteLine($"Полученный текст '{text}' в чате'{e.Message.Chat.Id}'");
                    break;

                default://проверка номера, проверка почты
                    if (count == 3 && text.IndexOf('@') != -1)//ЕСЛИ ОТПРАВИЛИ ОТЗЫВ, ТО ОБРАБАТЫВАЕМ ЕГО
                    {
                        Forma persona = new Forma();
                        text = text.Substring(1);
                        int position = text.IndexOf('#');
                        persona.phone = text.Substring(0, position - 1);
                        text = text.Substring(position);//-----
                        text = text.Substring(1);
                        position = text.IndexOf('#');
                        persona.email = text.Substring(0, position - 1);
                        text = text.Substring(position);//-----
                        text = text.Substring(1);
                        persona.message = text;
                        persona.first = e?.Message?.From?.FirstName.ToString();
                        persona.second = e?.Message?.From?.LastName.ToString();
                        persona.kogda = new DateTime(e.Message.Date.Year, e.Message.Date.Month, e.Message.Date.Day, e.Message.Date.Hour + 3, e.Message.Date.Minute, e.Message.Date.Second);
                        await Bot.SendTextMessageAsync(chatId: e.Message.Chat,
                        text: $"Спасибо за Ваш отзыв! Мы сделаем все, что в наших силах, что бы стать лучше!!!").ConfigureAwait(false);//ИНФОРМИРУЕМ ПОЬЗОВАТЕЛЯ О ТОМ ЧТО ОТЗЫВ ПРИНЯТ
                        //Console.WriteLine($"В итоге мы имеем {persona.email}   {persona.first}   {persona.second}   {persona.kogda}  {persona.message}   {persona.phone}");
                        // SendEmailAsync(persona).GetAwaiter();
                        SendContact(persona.phone,persona.email,persona.first,persona.second,persona.message);
                    }
                    else{
                        await Bot.SendTextMessageAsync(chatId: e.Message.Chat, text: $"Ненененене, я могу только поприветствовать тебя и отправить твой отзыв.\n\r" +
                            $"Не стесняйся и нажми сюда -> /form <- то бы заполнить форму и оставить отзыв.").ConfigureAwait(false);//В СЛУЧАЕ КАКОГО НИБУДЬ ДРУГОГО СООЩЕНИЯ(НЕ РАБОТАЕТ НА МЕДИА ФАЙЛЫ)

						//Console.WriteLine(e?.Message?.From?.Id);
					}
                    //await Bot.SendTextMessageAsync(chatId: e.Message.Chat,text: $"Ммм... это что то новенькое\n\rВы сказали {text}").ConfigureAwait(false);
                break;
            }
        }
    }
}

