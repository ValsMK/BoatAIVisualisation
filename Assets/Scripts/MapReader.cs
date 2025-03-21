using UnityEngine;
using System.Drawing;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;


/// <summary>
///     ����� ��������� ����������� ����� � �������� ��� ������� �������
/// </summary>
public class ColorMatch
{
    public UnityEngine.Color Color { get; set; }

    /// <summary>
    ///     ��������, ������� ������� � ������� �������
    /// </summary>
    public (int, int) Tuple { get; set; }

    /// <summary>
    ///     ������� (�������� �����) ��� ��������
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    ///     ��������� ������������� ���� (��� ������ � �������)
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"Color (R,G,B): ({Color.a}, {Color.g}, {Color.b}) = ({Tuple.Item1}, {Tuple.Item2}). Comment: {Comment}";
    }
}

/// <summary>
///     ����������� ����� (�� ���� ��������� ��������� ����� ������. � ���������� ���������� ����� ��� ������)
///     �������� ��������� ��� ������ � ������ �������
/// </summary>
public static class MapReader
{
    //����������� ����� �� ���������. ������� ����, ����� �� ��������� ������ ���
    private const string Separator = " ";

    /// <summary>
    ///     ��������� ���� ���� � ������� �����������, ���� ������� ���������� �������� ������������
    /// </summary>
    /// <param name="col">����, ������� ����</param>
    /// <param name="colorMap">������, � ������� ����</param>
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

        // ���� ������ �� �����
        return (-1, -1);
    }

    /// <summary>
    ///     ��������� ������� ������ ������� �� ������� ��������� ������ �����������
    /// </summary>
    /// <param name="bmp">�������� ��������</param>
    /// <param name="colorMap">������ ����������� ������</param>
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

        //������ ������� �� �������� � ���������� ������ ������� � ������� �������
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
    ///     ��������� ������� ������ ������������ �� ������� ����� ���� "(R,G,B) (int, int)" 
    /// </summary>
    public static ColorMatch[] GetColorMapFromString(string[] strings)
    {
        // ���������� ���������, ������� �������� 2 � ����� ������� �� ���� �����������
        Regex regex = new("[ ]{2,}");

        ColorMatch[] colorMap = new ColorMatch[strings.Length];

        for (int i = 0; i < strings.Length; i++)
        {
            // ������� ������ ������� � �������� ������ � ������� �������
            var str = regex.Replace(strings[i], Separator).Trim();
            var parts = str.Split(Separator);

            // ��������� ���� (R,G,B)
            var colorParts = parts[0].Trim('(', ')').Split(',');
            float r = int.Parse(colorParts[0]) / 255f;
            float g = int.Parse(colorParts[1]) / 255f;
            float b = int.Parse(colorParts[2]) / 255f;
            var color = new UnityEngine.Color(r, g, b, 1f); // �����-����� = 1 �� ���������

            // ��������� ���������� (int, int)
            var coordParts = parts[1].Trim('(', ')').Split(',');
            var coord = (int.Parse(coordParts[0]), int.Parse(coordParts[1]));

            // ������������ �����������
            var commentPart = (parts.Length > 2) ? parts[2].Trim() : string.Empty;

            // ��������� ������
            colorMap[i] = new ColorMatch { Color = color, Tuple = coord, Comment = commentPart };
        }

        return colorMap;
    }

    /// <summary>
    ///     ��������� ���������� ������ ������� � ����
    /// </summary>
    /// <param name="array">������ �������</param>
    /// <param name="filePath">����� � �����</param>
    public static void WriteArrayToFile(FlowMap flows, string filePath)
    {
        //using ������������, ����� � ����� ��������� �� �������������� � ��������� ����� � ������������� ����������� ������, ������� ��� ���� ��������
        //��� ��������, ���� ����� ������� ��������� ��������� IDisposabl�. ���������� ����� ���������� ������������, ��� ������ ������ ������ �����,
        //��� ���� ��������� ����������
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
