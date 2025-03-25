using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


// Класс для загрузки данных из Json-файла
[Serializable]
public class ConfigData
{
    public string WorkingDir;
    public string MapPNGPath;
    public string TestMapPNGPath;
    public string MapARRAYPath;
    public string InputQArrayPath;
    public string StartPosition;
    public string EndPosition;
    public string TestStartPosition;
    public string TestEndPosition;
    public string[] ColorMap;
}


public class Agent : MonoBehaviour
{
    private FlowMap _flowMap;
    private Dictionary<int, double[]> _Q_dictionary = new();
    private StrengthVector[] _actions;

    private float nextTime = 0f;
    public float interval = 1f; // Интервал выозва Updaet (в секундах)

    private readonly int _obstacleLen = 3;


    // Start is called before the first frame update
    void Start()
    {

        // Загрузка json файла с параметрами
        string path = Path.Combine(Application.streamingAssetsPath, "appsettings.json");
        string jsonText = File.ReadAllText(path);
        ConfigData config = JsonUtility.FromJson<ConfigData>(jsonText);

        // Загрузка начальной и конечной координат из файла конфигурации
        //Point startPosition = (new Point( int.Parse(config.TestStartPosition.Trim('(', ')').Split(',')[0]), int.Parse(config.TestStartPosition.Trim('(', ')').Split(',')[1]) ) );
        //Point endPosition = (new Point(int.Parse(config.TestEndPosition.Trim('(', ')').Split(',')[0]), int.Parse(config.TestEndPosition.Trim('(', ')').Split(',')[1])));
        Point startPosition = new Point(5, 5);
        Point endPosition = new Point(95, 95);

        // Установка начального положения объекта на начальное положение из конфигурации
        transform.position = new Vector2(startPosition.X, startPosition.Y);

        //Задание списка возможных действий
        _actions = new StrengthVector[]
        {
            new(2, 0), new(2, 45), new(2, 90), new(2, 135), new(2, 180), new(2, 225), new(2, 270), new(2, 315)
        };


        // Парсинг Q-словаря из файла
        _Q_dictionary = ParseFile(Path.Combine(config.WorkingDir, config.InputQArrayPath));


        // Загрузка карты течений
        //Texture2D texture = Resources.Load<Texture2D>("map");
        byte[] fileData = File.ReadAllBytes(Path.Combine(config.WorkingDir, config.MapPNGPath));
        Texture2D texture = new(2, 2);
        texture.LoadImage(fileData);
        ColorMatch[] colorMap = MapReader.GetColorMapFromString(config.ColorMap);
        _flowMap = MapReader.GetArrayFromImage(texture, colorMap);


        Debug.Log($"Start position flow: {_flowMap.GetFlow(startPosition)}");
        Debug.Log($"End position flow: {_flowMap.GetFlow(endPosition)}");

        // Установка карте течкений начальной и конечной точек
        _flowMap.StartPoint = startPosition;
        _flowMap.EndPoint = endPosition;
    }



    // Update is called once per frame
    void Update()
    {
        // Условие, чтобы Update выполнялся не каждый кадр, а раз в interval секунд 
        if (Time.time >= nextTime)
        {
            nextTime = Time.time + interval;

            // Переделывание текущих координат агента в состояние и хэш
            Point current_position = new((int)Math.Round(transform.position.x), (int)Math.Round(transform.position.y));
            State state = CoordsToNewState(current_position);
            int stateHash = state.Hash;

            // Проверка на терминальное состояние
            if (_flowMap.GetFlow(current_position).Equals(new StrengthVector(-1, -1)))
            {
                Debug.Log("Столкновение!");
                EditorApplication.isPlaying = false;
            }
            if (_flowMap.GetFlow(current_position).Equals(new StrengthVector(10, 10)))
            {
                Debug.Log("Маршрут пройден!");
                EditorApplication.isPlaying = false;
            }

            // Поиск лучшего действия в данном состоянии
            int best_action_index = 0;
            if (_Q_dictionary.ContainsKey(stateHash))
            {
                Debug.Log($"Key found: {state.ToString()}");

                for (int i = 0; i < _Q_dictionary[stateHash].Length; i++)
                {
                    if (_Q_dictionary[stateHash][i] > _Q_dictionary[stateHash][best_action_index])
                        best_action_index = i;
                }
            } else
            {
                Debug.Log($"Key NOT found: {state.ToString()}");
            }
            StrengthVector best_action = _actions[best_action_index];

            
            // Расчёт новых координат исходя из лучшего действия и течения
            int delta_x_boat = (int)Math.Round(Math.Cos((double)best_action.Angle * Math.PI / 180)) * best_action.Strength;
            int delta_y_boat = (int)Math.Round(Math.Sin((double)best_action.Angle * Math.PI / 180)) * best_action.Strength;
            int delta_x_flow = (int)Math.Round(Math.Cos((double)state.CurrentFlow.Angle * Math.PI / 180)) * state.CurrentFlow.Strength;
            int delta_y_flow = (int)Math.Round(Math.Sin((double)state.CurrentFlow.Angle * Math.PI / 180)) * state.CurrentFlow.Strength;
            int delta_x = delta_x_boat + delta_x_flow;
            int delta_y = delta_y_boat + delta_y_flow;


            var angle = Math.Atan2(delta_y, delta_x);
            transform.rotation = new Quaternion(0, 0, (float)angle, 0);
            // Обновление координат
            transform.position = new Vector2(transform.position.x + delta_x, transform.position.y + delta_y);
        }
    }




    static Dictionary<int, double[]> ParseFile(string path)
    {
        var result = new Dictionary<int, double[]>();

        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(':');
            if (parts.Length != 2) continue;

            if (int.TryParse(parts[0].Trim(), out int key))
            {
                double[] values = parts[1]
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries) // Удаляем пустые элементы
                    .Select(s =>
                    {
                        if (double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                            return num;
                        return double.NaN; // Если парсинг не удался, вставляем NaN
                    })
                    .Where(d => !double.IsNaN(d)) // Убираем невалидные значения
                    .ToArray();

                result[key] = values;
            }
        }

        return result;
    }


    public State CoordsToNewState(Point coords)
    {
        // Заполнение данных о территории вокруг агента
        Obstacles around = new(_obstacleLen);

        int halfLength = ((around.Len - 1) / 2);

        for (int x = -1 * halfLength; x <= halfLength; x++)
        {
            for (int y = -1 * halfLength; y <= halfLength; y++)
            {
                if (IndexOutOfBounds(new Point(coords.X + x, coords.Y + y)))
                {
                    around.SetValue(new Point(x + halfLength, y + halfLength), ObstaclesEnum.Obstacle);
                }
                else
                {
                    if (_flowMap.GetFlow(coords.X + x, coords.Y + y).Strength == new StrengthVector(-1, -1).Strength && _flowMap.GetFlow(coords.X + x, coords.Y + y).Angle == new StrengthVector(-1, -1).Angle)
                        around.SetValue(new Point(x + halfLength, y + halfLength), ObstaclesEnum.Obstacle);
                }
            }
        }


        // Расстояние и угол до цели
        double distance_x = _flowMap.EndPoint.X - coords.X;
        double distance_y = _flowMap.EndPoint.Y - coords.Y;

        int distance = (int)Math.Round(Math.Sqrt(distance_x * distance_x + distance_y * distance_y));
        double degree1 = Math.Acos(distance_x / distance) * 180 / Math.PI;

        // Округление расстояния и угла до цели

        int degree = (int)Math.Round(degree1 / 10) * 10;

        int lower = distance - (distance % 3);
        int upper = lower + 3;
        distance = (distance - lower < upper - distance) ? lower : upper;


        StrengthVector distanceToEndPosition = new(distance, degree);
        StrengthVector currentFlow = _flowMap.GetFlow(coords);

        State state = new()
        {
            DistanceToEndPosition = distanceToEndPosition,
            CurrentFlow = currentFlow,
            Obstacles = around
        };

        return state;
    }




    private bool IndexOutOfBounds(Point coords)
    {
        bool up = (coords.Y >= _flowMap.LenY);
        bool down = (coords.Y < 0);
        bool left = (coords.X < 0);
        bool right = (coords.X >= _flowMap.LenX);

        return (up || down || left || right);
    }
}
