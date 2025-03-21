using UnityEngine;
using System.Drawing;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;


/// <summary>
///     Класс описывает соотвествие цвета и значения для матрицы течений
/// </summary>
public class ColorMatch
{
    public UnityEngine.Color Color { get; set; }

    /// <summary>
    ///     Значение, которое попадет в матрицу течений
    /// </summary>
    public (int, int) Tuple { get; set; }

    /// <summary>
    ///     Коммент (название цвета) для удобства
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    ///     Строковое представление пары (для вывода в консоль)
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"Color (R,G,B): ({Color.a}, {Color.g}, {Color.b}) = ({Tuple.Item1}, {Tuple.Item2}). Comment: {Comment}";
    }
}

/// <summary>
///     Статический класс (не надо создавать экзепляер этого класса. к процедурам обращаемся через имя класса)
///     Содержит процедуры для работы с картой течений
/// </summary>
public static class MapReader
{
    //Разделитель строк по умолчанию. Вынесен сюда, чтобы не объявлять каждый раз
    private const string Separator = " ";

    /// <summary>
    ///     Процедура ищет цвет в массиве соответсвий, если находит возвращает значение соответствия
    /// </summary>
    /// <param name="col">Цвет, который ищем</param>
    /// <param name="colorMap">Массив, в котором ищем</param>
    private static (int, int) ColorToTuple(UnityEngine.Color col, ColorMatch[] colorMap)
    {
        Color32 col32 = col;

        foreach (var entry in colorMap)
        {
            Color32 entryColor = entry.Color;

            if (col32.r == entryColor.r &&
                col32.g == entryColor.g &&
                col32.b == entryColor.b &&
                col32.a == entryColor.a)
            {
                return entry.Tuple;
            }
        }

        // Если ничего не нашли
        return (-1, -1);
    }

    /// <summary>
    ///     Процедура создает массив течений из битмапа используя массив соответсвий
    /// </summary>
    /// <param name="bmp">исходная картинка</param>
    /// <param name="colorMap">массив соответсуий цветов</param>
    /// <returns></returns>
    public static FlowMap GetArrayFromImage(Texture2D texture, ColorMatch[] colorMap)
    {
        System.Drawing.Color[] bmp = texture.GetPixels()
                                    .Select(c => System.Drawing.Color.FromArgb(
                                        (int)(c.a * 255),
                                        (int)(c.r * 255),
                                        (int)(c.g * 255),
                                        (int)(c.b * 255)))
                                    .ToArray();

        var flows = new FlowMap(texture.width, texture.height);

        //Циклом пройдем по картинке и переделаем каждый пискель в элемент массива
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                UnityEngine.Color pixelColor = texture.GetPixel(x, y);
                var flowTuple = ColorToTuple(pixelColor, colorMap);
                var yNew = texture.height - 1 - y;
                flows.SetFLow(x, yNew, flowTuple);
            }

        return flows;
    }

    /// <summary>
    ///     Процедура создает массив соответствий из массива строк вида "(R,G,B) (int, int)" 
    /// </summary>
    public static ColorMatch[] GetColorMapFromString(string[] strings)
    {
        // Регулярное выражение, которое заменяет 2 и более пробела на один разделитель
        Regex regex = new("[ ]{2,}");

        ColorMatch[] colorMap = new ColorMatch[strings.Length];

        for (int i = 0; i < strings.Length; i++)
        {
            // Убираем лишние пробелы и приводим строку к нужному формату
            var str = regex.Replace(strings[i], Separator).Trim();
            var parts = str.Split(Separator);

            // Разбираем цвет (R,G,B)
            var colorParts = parts[0].Trim('(', ')').Split(',');
            float r = int.Parse(colorParts[0]) / 255f;
            float g = int.Parse(colorParts[1]) / 255f;
            float b = int.Parse(colorParts[2]) / 255f;
            var color = new UnityEngine.Color(r, g, b, 1f); // Альфа-канал = 1 по умолчанию

            // Разбираем координаты (int, int)
            var coordParts = parts[1].Trim('(', ')').Split(',');
            var coord = (int.Parse(coordParts[0]), int.Parse(coordParts[1]));

            // Опциональный комментарий
            var commentPart = (parts.Length > 2) ? parts[2].Trim() : string.Empty;

            // Заполняем массив
            colorMap[i] = new ColorMatch { Color = color, Tuple = coord, Comment = commentPart };
        }

        return colorMap;
    }

    /// <summary>
    ///     Процедура записывает массив течений в файл
    /// </summary>
    /// <param name="array">массив течений</param>
    /// <param name="filePath">пусть к файлу</param>
    public static void WriteArrayToFile(FlowMap flows, string filePath)
    {
        //using используется, чтобы в конце процедуры не заморачиваться с закрытием файла и освобождением оперативной памяти, которая для него выделена
        //Это возможно, если класс объекта реализует интерфейс IDisposablе. Реализация этого интерфейса предполагает, что объект такого класса знает,
        //как себя правильно уничтожить
        using StreamWriter writer = new(filePath);
        for (int y = flows.LenY - 1; y >= 0; y--)
        {
            for (int x = 0; x < flows.LenX; x++)
            {
                writer.Write(flows.GetFlow(x, y));
                if (x < flows.LenX - 1)
                    writer.Write(Separator);
            }
            writer.WriteLine();
        }
    }
}
