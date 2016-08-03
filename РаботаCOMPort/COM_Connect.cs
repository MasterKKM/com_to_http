using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace BaseCOMPort
{
    /// <summary>
    /// Обмен текстовыми строками с устройстовм подключенным к COM-порту.
    /// Строка воспринимается по \n, который отбрасывается. Также отбрасываются
    /// все символы с кодом меньше 0x20.
    /// </summary>
    class COM_Connect
    {
        private string recData = ""; // Буфер для принимаемой строки.
        public string text    = ""; // Буфер полностью принятой строки.
        private SerialPort _serialPort;

        /// <summary>
        /// Начало соединения.
        /// </summary>
        /// <param name="port">Текстовое имя COM-порта (COM1,COM2...COMn)</param>
        /// <param name="speed">Скорость соединения (9600,19200...115200)</param>
        public COM_Connect(string port,Int32 speed)
        {
            recData = "";
            // Открываем COM - порт.
            _serialPort = new SerialPort(
                     port, // COM-порт
                     speed, // Скорость.
                     Parity.None,
                     8,
                     StopBits.One
                 );
            // Настраиваем COM - порт.
            _serialPort.Handshake = Handshake.None;
            _serialPort.RtsEnable = true;
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataRecived);
            // Откроем порт.
            Clear();
        }

        /// <summary>
        /// Передача текстовой строки подключенному устройству.
        /// </summary>
        /// <param name="text">Текс для передачи.</param>
        public void Transmitt(string text)
        {
            _serialPort.Write(text);
        }

        /// <summary>
        /// Открывает если порт закрыт и переинициализирует если открыт.
        /// </summary>
        void Clear()
        {
            // Переинициализируем порт.
            if (_serialPort.IsOpen)
            {
                // Закроем если открыт.
                _serialPort.Close();
            }
            // Откроем по новой.
            _serialPort.Open();
            // Очистим буфер.
            recData = "";
        }

        /// <summary>
        /// Колбек-функция вызываемая при приеме очередной порции данных.
        /// Накапливает принятые байты в буфере recData. При получении \n копирует данные в
        /// буфер text и очищает recData.
        /// </summary>
        /// <param name="sender">Текущий объект SerialPort</param>
        /// <param name="e"></param>
        private void sp_DataRecived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(100); // Задержка для окончания загрузки строки.

            SerialPort obj = (SerialPort)sender; // Обьект.

            do
            {
                int data = obj.ReadChar();
                char ch = (char)data;
                if (data == 13)
                {
                    text = recData;
                    recData = "";
                }
                if (data >= 0x20)
                {
                    recData += ch;
                }
            } while (obj.BytesToRead > 0); // obj.BytesToRead - Количество байт в буфере.
        }

        /// <summary>
        /// Читает строку из буфера text (последняя полностью принятая строка).
        /// </summary>
        /// <returns>Прочитанная строка.</returns>
        public string ReadLine()
        {
            string te = text;
            text = "";
            return te;
        }

        /// <summary>
        /// Завершает соединение, закрывает порт.
        /// </summary>
        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                // Закроем если открыт.
                _serialPort.Close();
            }
        }

        // Деструктор на всякий случай.
        ~COM_Connect()
        {
            Close();
        }
    }
}
