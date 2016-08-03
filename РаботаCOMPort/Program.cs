using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;


namespace BaseCOMPort
{
    /// <summary>
    /// Константы используемые в приложении.
    /// </summary>
    static class Const
    {
        /// <summary>
        /// Максимальное колличество задейсвованных COM-портов.
        /// </summary>
        public const int comCount = 4;
    }
    /// <summary>
    /// Класс содержит глобальные таблицы и переменные.
    /// </summary>
    static class MyTables
    {
        /// <summary>
        /// Таблица для хранения объектов COM -портов.
        /// </summary>
        public static COM_Connect[] comTable = new COM_Connect [Const.comCount];
        /// <summary>
        /// Количество задействованных COM - портов.
        /// </summary>
        public static int countComPotrs = 0;
        /// <summary>
        /// Глобальный признак работы программы.
        /// </summary>
        public static bool isWork = true;
        /// <summary>
        /// Признак режима отладки.
        /// </summary>
        public static bool isDebugMode = false;
    }
    /// <summary>
    /// Собственно программа.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string[] str = new string[3];
            // Разбор командной строки.
            int j = 0;
            while(j < args.Length)
            {
                switch (args[j++])
                {
                    case "/d":
                        // отладка 1/0
                        MyTables.isDebugMode = true;
                        break;
                    case "/a":
                        // Параметры HttpListener
                        for (int i = 0; i < 3; i++)
                            if (j < args.Length)
                                str[i] = args[j++];
                            else
                                break;
                        break;
                    case "/?":
                        Console.WriteLine("\nHttp сервер для общения с COM-портом.\nПараметры:\n  /? - это сообщение.\n  /d - режим отладки.\n  /a - аргументы для HttpListener (до трех штук).\nНа винде старше XP тербуется запуск от полноценного администратора.");
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("\nОшибка в параметрах.");
                        Environment.Exit(1);
                        break;
                }
            }
            // Запускаем Http сервер.
            HttpServer.StartHttp(str);
        }
    }
}
