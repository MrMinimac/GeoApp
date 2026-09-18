using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoAppWpf.Helpers
{
    public class GaussKrugerConverter
    {
        // Параметры референц-эллипсоида ГСК-2011
        private const double a = 6378136.5;
        private const double invF = 298.2564151;

        /// <summary>
        /// Переводит координаты Гаусса-Крюгера (ГСК-2011) в геодезические координаты (Широта, Долгота).
        /// </summary>
        public static (double Latitude, double Longitude) GKToGeodetic(double x, double y)
        {
            double f = 1.0 / invF;
            double e2 = 2 * f - f * f; // Квадрат первого эксцентриситета
            double ep2 = e2 / (1 - e2); // Квадрат второго эксцентриситета

            // 1. Извлечение номера зоны и истинного значения Y
            int zone = (int)(y / 1000000);
            double trueY = y - (zone * 1000000) - 500000.0;

            // 2. Осевой меридиан зоны (в радианах)
            double lon0 = (zone * 6 - 3) * Math.PI / 180.0;

            // 3. Вычисление выпрямляющей широты (mu)
            double e1 = (1 - Math.Sqrt(1 - e2)) / (1 + Math.Sqrt(1 - e2));
            double M = x;
            double mu = M / (a * (1 - e2 / 4 - 3 * e2 * e2 / 64 - 5 * Math.Pow(e2, 3) / 256));

            // 4. Вычисление предварительной широты (phi1)
            double phi1 = mu
                        + (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32) * Math.Sin(2 * mu)
                        + (21 * e1 * e1 / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu)
                        + (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu)
                        + (1097 * Math.Pow(e1, 4) / 512) * Math.Sin(8 * mu);

            // Тригонометрические функции для phi1
            double sinPhi1 = Math.Sin(phi1);
            double cosPhi1 = Math.Cos(phi1);
            double tanPhi1 = Math.Tan(phi1);

            // Радиусы кривизны
            double N1 = a / Math.Sqrt(1 - e2 * sinPhi1 * sinPhi1);
            double T1 = tanPhi1 * tanPhi1;
            double C1 = ep2 * cosPhi1 * cosPhi1;
            double R1 = a * (1 - e2) / Math.Pow(1 - e2 * sinPhi1 * sinPhi1, 1.5);
            double D = trueY / N1;

            // 5. Итоговая широта (в радианах)
            double phi = phi1 - (N1 * tanPhi1 / R1) * (
                Math.Pow(D, 2) / 2.0
                - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * ep2) * Math.Pow(D, 4) / 24.0
                + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * ep2 - 3 * C1 * C1) * Math.Pow(D, 6) / 720.0
            );

            // 6. Итоговая долгота (в радианах)
            double lambda = lon0 + (
                D
                - (1 + 2 * T1 + C1) * Math.Pow(D, 3) / 6.0
                + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * ep2 + 24 * T1 * T1) * Math.Pow(D, 5) / 120.0
            ) / cosPhi1;

            // Перевод из радиан в десятичные градусы
            double latitudeDegree = phi * 180.0 / Math.PI;
            double longitudeDegree = lambda * 180.0 / Math.PI;

            return (latitudeDegree, longitudeDegree);
        }
    }
}
