using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BaseCOMPort
{
    /// <summary>
    /// Сам Http сервер.
    /// </summary>
    public class HttpServer
    {

        protected int port;

        public HttpServer(int port)
        {
            this.port = port;
        }
        /// <summary>
        /// Инициализацияя и запуск сервера.
        /// </summary>
        /// <param name="prefixes">Параметры HttpListener</param>
        public static void StartHttp(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Не поддреживается \"HttpListener class\", ставь более свежую версию netframework.");
                return;
            }

            // Дефолтное значение параметра.
            if (prefixes == null || prefixes.Length == 0 || prefixes[0] == null)
                prefixes = new string[] {"http://127.0.0.1:8080/"};

            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                if(s != null)
                    listener.Prefixes.Add(s);
            }
            try
            {
                listener.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("\nОшибка при попытке открытия порта:\n{0}",e.ToString());
                Environment.Exit(1);
            }
            if (MyTables.isDebugMode)
                Console.WriteLine("Start...");
            // Note: The GetContext method blocks while waiting for a request.
            do
            {
                // Ожидаем http запроса.
                HttpListenerContext context = listener.GetContext();

                Req r = new Req(context);
                Thread th = new Thread(r.DataMove);
                th.Start();

                //r.DataMove();
            } while (MyTables.isWork);
            listener.Stop();

        }
        
    }

    /// <summary>
    /// Класс обработки сообщения принятого HttpListener.
    /// </summary>
    class Req
    {
        private HttpListenerContext context;

        public Req (HttpListenerContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Упрощенное преобразование строки в JSON формат.
        /// </summary>
        /// <param name="ar">Строка текста</param>
        /// <returns>строка в формате JSON</returns>
        public string ToJson(string ar)
        {
            string rez = "";
            char ch;

            for(int i = 0; i < ar.Length; i++)
            {
                ch = ar[i];
                rez += "\\u" + ((int)ch).ToString("X4");
            }
            return rez;
        }
        /// <summary>
        /// Собственно обработка запроса и выдача ответа.
        /// </summary>
        public void DataMove()
        {
            string text = "";
            int n = -1;
            bool error = false;

            // Анализ запроса.
            HttpListenerRequest request = context.Request; // Собственно запрос.
            System.Collections.Specialized.NameValueCollection qs = request.QueryString; // Параметры запроса.

            if (MyTables.isDebugMode)
            {
                // Вывод информации для отладки.
                Console.WriteLine("request.HttpMethod: {0}", request.HttpMethod); // Тип запроса (GET/POST).
                Console.WriteLine("request.RawUrl: {0}", request.RawUrl);
                Console.WriteLine("request.Url.AbsolutePath: {0}", request.Url.AbsolutePath);
                Console.WriteLine("request.Url.LocalPath: {0}", request.Url.LocalPath);
                Console.WriteLine("Ouery:");
                foreach (string s in qs)
                    Console.WriteLine("  [{0}] = {1}", s, qs[s]);
            }

            // Массив параметров по дефолту.
            Dictionary<string,string> parametrs = new Dictionary<string,string>();
            parametrs["port"]  = "COM1"; // Имя COM - порта.
            parametrs["speed"] = "9600"; // Скорость обмена.
            parametrs["data"]  = "Строка для передачи в устройство"; // Строка для вывода в порт.
            parametrs["name"]  = "Respons"; // Имя переменной для ответа (JavaScript).
            parametrs["id"]    = "0"; // Номер порта в таблице.

            // Перенесем полученные параметры в массив.
            // Заодно почистим от скриптов.
            foreach (string i1 in qs)
                parametrs[i1] = qs[i1].Replace('<', '_').Replace('>', '_').Replace(';', '_');

            if (MyTables.isDebugMode)
            {
                // Вывод параметров для отладки.
                Console.WriteLine("Параметры:");
                foreach(var i in parametrs)
                    Console.WriteLine("  [{0}]={1}", i.Key,i.Value);
            }

            // Получаем путь, который используем как команду.
            string command = request.Url.LocalPath.Replace('/',' ').Trim();
            if (MyTables.isDebugMode)
                Console.WriteLine("Команда: {0}",command);

            switch (command)
            {
                case "open":
                    // Открываем нужный COM - порт.
                    for (int i2 = 0; i2 < Const.comCount;i2++ )
                    {
                        // Ищем свободную ячейку.
                        if (MyTables.comTable[i2] == null)
                        {
                            n = i2;
                            break;
                        }
                    }
                    if (n == -1)
                    {
                        text = parametrs["name"] + "=\"Нет свободной ячейки\"";
                        error = true;
                        break;
                    }
                    // Инициализируем COM - порт. n - Номер ячейки в таблице.
                    try
                    {
                        MyTables.comTable[n] = new COM_Connect(parametrs["port"], Convert.ToInt32(parametrs["speed"]));
                    }catch(Exception e)
                    {
                        text = parametrs["name"] + "=\"Не возможно открыть порт\"";
                        error = true;
                        break;
                    }

                    text = parametrs["name"] + "=" + n;
                    break;
                case "get":
                    // Получаем данные из буфера порта.
                    n = Convert.ToInt16(parametrs["id"]);
                    if (n >= 0 && n < Const.comCount && MyTables.comTable[n] != null)
                    {
                        // Читаем строку из буфера.
                        try
                        {
                            text = parametrs["name"] + " = \"" + ToJson(MyTables.comTable[n].ReadLine()) + "\"";
                        } catch(Exception e)
                        {
                            error = true;
                            text = parametrs["name"] + " = \"Ошибка обращения к порту\"";
                            break;
                        }
                    } else
                    {
                        error = true;
                        text = parametrs["name"] + " = \"Порт не инициализирован\"";
                    }
                    break;
                case "put":
                    // Посылаем данные в порт.
                    n = Convert.ToInt16(parametrs["id"]);
                    if (n >= 0 && n < Const.comCount && MyTables.comTable[n] != null)
                    {
                        try
                        {
                            MyTables.comTable[n].Transmitt(parametrs["data"]);
                            text = parametrs["name"] + " = true";
                        } catch(Exception e)
                        {
                            error = true;
                            text = parametrs["name"] + " = \"Ошибка обращения к порту\"";
                            break;
                        }
                    } else
                    {
                        error = true;
                        text = parametrs["name"] + " = \"Порт не инициализирован\"";
                    }
                    
                    break;
                case "close":
                    // Закрываем порт.
                    n = Convert.ToInt16(parametrs["id"]);
                    if (n >= 0 && n < Const.comCount && MyTables.comTable[n] != null)
                    {
                        try
                        {
                            MyTables.comTable[n].Close();
                            MyTables.comTable[n] = null;
                            text = parametrs["name"] + " = true";
                        } catch(Exception e)
                        {
                            error = true;
                            text = parametrs["name"] + " = \"Ошибка обращения к порту\"";
                            break;
                        }
                    } else
                    {
                        error = true;
                        text = parametrs["name"] + " = \"Порт не инициализирован\"";
                    }

                    break;
                case "info":
                    // Получаем данные о компьюттере (uin).
                    Console.WriteLine("Get id!");
                    text = "info = \""+GetInfo.Info()+"\"";
                    break;

                default:
                    error = true;
                    text = parametrs["name"] + " = \"Ошибка: Не существующая команда.\"";
                    break;
            }

            // Формирование ответа.
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString;

            if(!error)
                responseString = text + ";\nerror = 0;\n";
            else
                responseString = text + ";\nerror = 1;\n";

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
        }
    }
}
