using System;
using System.Diagnostics;
using System.IO;



namespace GoodsForecast
{
    class Program
    {

        public static void Main(string[] args)
        {
            #region Для случая dt!=const
            Dictionary<double, double> observations;

            string dataPath = @"..\..\..\data.csv";
            string newDataPath = @"..\..\..\newdata.csv";


            #region генерация данных для проверки + запись в файл data.csv
            using (var writer = new StreamWriter(dataPath))
            {
                Random random = new Random();
                double t = 0;
                writer.WriteLine("Time,Value");
                while (t < 1000)
                {
                    writer.WriteLine($"{t.ToString().Replace(',', '.')},{(((random.NextDouble()>0.97)?random.Next(-20, 20):0) + Math.Log(t + 0.01) * Math.Log(t + 0.01)).ToString().Replace(',', '.')}");
                    t += random.NextDouble();
                }
            }
            #endregion



            #region считывание данных из файла data.csv
            observations = new Dictionary<double, double>();
            using (var reader = new StreamReader(dataPath))
            {
                Random random = new Random();
                if (!reader.EndOfStream)
                {
                    reader.ReadLine();
                }
                while (!reader.EndOfStream)
                {
                    try
                    {
                        var doubleString = reader.ReadLine().Split(',').Select(s => double.Parse(s.Replace('.', ',')));
                        observations.Add(doubleString.First(), doubleString.Last());
                    }
                    catch { Console.WriteLine("Data value can not be parsed or data duplicate was found"); }
                }
            }
            #endregion



            #region Применение медианного сглаживания
            var width = 10; //ширина окна
            var newObservation = MedianSmoothingAnalogTime(observations, width);
            using (var writer = new StreamWriter(newDataPath))
            {
                writer.WriteLine("Time,Value");
                foreach (var o in newObservation)
                {
                    writer.WriteLine($"{o.Key.ToString().Replace(',', '.')},{o.Value.ToString().Replace(',', '.')}");
                }
            }
            #endregion




            #endregion


            //<!-------->


            #region Для случая с интервалом dt=const
            string intDataPath = @"..\..\..\intdata.csv";
            string newIntDataPath = @"..\..\..\intnewdata.csv";
            Dictionary<int, double> observationsDiscrete = new();


            #region генерация данных для проверки + запись в файл intdata.csv
            using (var writer = new StreamWriter(intDataPath))
            {
                Random random = new Random();
                writer.WriteLine("Time,Value");
                for (int t = 0; t < 1000; t++)
                {
                    writer.WriteLine($"{t},{(((random.NextDouble() > 0.99) ? 50f : 0f) + Math.Log(t + 0.01) * Math.Log(t + 0.01)).ToString().Replace(',', '.')}");
                }
            }
            #endregion



            #region считывание данных из файла intdata.csv
            observationsDiscrete = new Dictionary<int, double>();
            using (var reader = new StreamReader(intDataPath))
            {
                if (!reader.EndOfStream)
                {
                    reader.ReadLine();
                }
                while (!reader.EndOfStream)
                {
                    try
                    {
                        var timeValuePair = reader.ReadLine().Split(',').Select(s => double.Parse(s.Replace('.', ',')));
                        observationsDiscrete.Add((int)timeValuePair.First(), timeValuePair.Last());
                    }
                    catch { Console.WriteLine("Data value can not be parsed or data duplicate was found"); }
                }
            }
            #endregion



            #region Применение медианного сглаживания
            width = 5; //ширина окна

            var newObservationDiscrete = MedianSmoothingDiscreteTime(observationsDiscrete.Values.ToArray(), width);
            using (var writer = new StreamWriter(newIntDataPath))
            {
                writer.WriteLine("Time,Value");
                for (int i = 0; i < newObservationDiscrete.Length; i++)
                {
                    writer.WriteLine($"{observationsDiscrete.Keys.ToArray()[i]},{newObservationDiscrete[i].ToString().Replace(',', '.')}");
                }
            }
            #endregion

            #endregion
        }


        //медианное сглаживание для ряда наблюдений с НЕфиксированным интервалом измерений
        static Dictionary<double, double> MedianSmoothingAnalogTime(Dictionary<double, double> oldData, double windowWidth)
        {
            Dictionary<double, double> newData = new Dictionary<double, double>();
            foreach (var s in oldData)
            {
                //ширина рассматриваемого окна
                var tempWidth = windowWidth;

                //минимальное расстояние до края по времени
                double distToEdge = Math.Min(s.Key - Enumerable.Min(oldData, k => k.Key),
                                            Enumerable.Max(oldData, k => k.Key) - s.Key);

                //при чрезмерной близости к краю - сужаем окно
                if (distToEdge < windowWidth)
                {
                    tempWidth = distToEdge;
                }

                //добавляем в реультат медианное значение
                newData.Add(s.Key, oldData.Where(t => Math.Abs(t.Key - s.Key) <= tempWidth).Average(t => t.Value));
            }
            return newData;
        }


        //медианное сглаживание для ряда наблюдений с фиксированным интервалом измерений
        static double[] MedianSmoothingDiscreteTime(double[] oldData, int windowWidth)
        {
            List<double> newData = new List<double>();

            //расширение окна справа
            newData.Add(oldData[0]);
            for (int i = 1; i < windowWidth + 1; i++)
            {
                newData.Add((newData[i - 1] * (2 * i - 1) + (oldData[2 * i - 1] + oldData[2 * i])) / (2 * i + 1));
            }
            //общий случай
            for (int i = windowWidth + 1; i < oldData.Length - windowWidth; i++)
            {
                newData.Add(newData[i - 1] + (oldData[i + windowWidth] - oldData[i - windowWidth - 1]) / (2 * windowWidth + 1));
            }

            //правое сужение окна
            for (int i = oldData.Length - windowWidth; i < oldData.Length; i++)
            {
                var reversei = oldData.Length - i;

                newData.Add((newData[i - 1] * (2 * reversei + 1) - oldData[2 * i - oldData.Length] - oldData[2 * i - oldData.Length - 1]) / (2 * reversei - 1));
            }
            return newData.ToArray();

        }
    }
}
