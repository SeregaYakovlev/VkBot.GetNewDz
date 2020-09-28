using System;
using System.Collections.Generic;
using System.Text;

namespace Puppeteer
{
    public static class DayOfWeekExtention
    {
        private static string[] russianDaysWeek = new string[]
            {
                "Воскресенье",
                "Понедельник",
                "Вторник",
                "Среда",
                "Четверг",
                "Пятница",
                "Суббота"
            };
        public static string ToRussianString(DayOfWeek dayOfWeek)
        {
            return russianDaysWeek[(int)dayOfWeek];
        }
    }
}
