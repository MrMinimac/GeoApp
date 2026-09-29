namespace GeoAppCore.Services
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

        /// <summary>
        /// Переводит геодезические координаты (Широта, Долгота в десятичных градусах) 
        /// в координаты Гаусса-Крюгера (ГСК-2011).
        /// </summary>
        public static (double X, double Y) GeodeticToGK(double latitude, double longitude)
        {
            double f = 1.0 / invF;
            double e2 = 2 * f - f * f; // Квадрат первого эксцентриситета
            double ep2 = e2 / (1 - e2); // Квадрат второго эксцентриситета

            // Перевод градусов в радианы
            double phi = latitude * Math.PI / 180.0;
            double lambda = longitude * Math.PI / 180.0;

            // 1. Определение номера зоны и осевого меридиана
            int zone = (int)Math.Floor(longitude / 6.0) + 1;
            double lon0 = (zone * 6 - 3) * Math.PI / 180.0;

            // Разница долгот (в радианах)
            double l = lambda - lon0;

            // Тригонометрические функции
            double sinPhi = Math.Sin(phi);
            double cosPhi = Math.Cos(phi);
            double tanPhi = Math.Tan(phi);

            // 2. Вычисление длины дуги меридиана (M)
            double A0 = 1 - e2 / 4.0 - 3 * e2 * e2 / 64.0 - 5 * Math.Pow(e2, 3) / 256.0;
            double A2 = (3.0 / 8.0) * e2 + (3.0 / 32.0) * e2 * e2 + (45.0 / 1024.0) * Math.Pow(e2, 3);
            double A4 = (15.0 / 256.0) * e2 * e2 + (45.0 / 1024.0) * Math.Pow(e2, 3);
            double A6 = (35.0 / 3072.0) * Math.Pow(e2, 3);

            double M = a * (A0 * phi - A2 * Math.Sin(2 * phi) + A4 * Math.Sin(4 * phi) - A6 * Math.Sin(6 * phi));

            // 3. Радиус кривизны и промежуточные переменные
            double N = a / Math.Sqrt(1 - e2 * sinPhi * sinPhi);
            double T = tanPhi * tanPhi;
            double C = ep2 * cosPhi * cosPhi;
            double A = l * cosPhi;

            // 4. Вычисление координаты X (Север)
            double x = M + N * tanPhi * (
                Math.Pow(A, 2) / 2.0
                + (5 - T + 9 * C + 4 * C * C) * Math.Pow(A, 4) / 24.0
                + (61 - 58 * T + T * T + 600 * C - 330 * ep2) * Math.Pow(A, 6) / 720.0
            );

            // 5. Вычисление истинного значения Y (Восток) от осевого меридиана
            double trueY = N * (
                A
                + (1 - T + C) * Math.Pow(A, 3) / 6.0
                + (5 - 18 * T + T * T + 72 * C - 58 * ep2) * Math.Pow(A, 5) / 120.0
            );

            // 6. Добавление смещения Y (номер зоны + условный сдвиг 500 км)
            double y = (zone * 1000000.0) + 500000.0 + trueY;

            return (x, y);
        }
    }
}
