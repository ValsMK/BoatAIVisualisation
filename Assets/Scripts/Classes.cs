using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class State : MonoBehaviour
{
    public StrengthVector DistanceToEndPosition { get; set; }

    public StrengthVector CurrentFlow { get; set; }

    public Obstacles Obstacles { get; set; }

    public int Hash => GetHash();

    public override string ToString()
    {
        return $"({DistanceToEndPosition}, {CurrentFlow}, {Obstacles})";
    }

    /// <summary>
    ///     Метод считает hash из состояния среды
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    private int GetHash()
    {
        var hash = HashCode.Combine(DistanceToEndPosition.Strength);
        hash = HashCode.Combine(DistanceToEndPosition.Angle, hash);
        hash = HashCode.Combine(CurrentFlow.Strength, hash);
        hash = HashCode.Combine(CurrentFlow.Angle, hash);

        for (int x = 0; x < Obstacles.Len; x++)
        {
            for (int y = 0; y < Obstacles.Len; y++)
            {
                hash = HashCode.Combine(Obstacles.GetValue(x, y), hash);
            }
        }
        return hash;
    }
}


public class StrengthVector : IEquatable<StrengthVector>
{
    //TODO Задавать направление не целым числом, а перечислением 

    public StrengthVector(int strength, int angle)
    {
        Strength = strength;
        Angle = angle;
    }

    public int Strength { get; }

    public int Angle { get; }


    public override string ToString()
    {
        return $"({Strength}, {Angle})";
    }
    public bool Equals(StrengthVector? other)
    {
        if (other is null)
            return false;

        return Strength == other.Strength && Angle == other.Angle;
    }
}


public enum ObstaclesEnum { Free = 0, Obstacle = -1 };
public class Obstacles : IEquatable<Obstacles>
{
    private ObstaclesEnum[,] _obstacles;
    public Obstacles(int size)
    {
        _obstacles = new ObstaclesEnum[size, size];
    }

    public void SetValue(Point point, ObstaclesEnum value)
    {
        _obstacles[point.X, point.Y] = value;
    }

    public void SetValue(int x, int y, ObstaclesEnum value)
    {
        _obstacles[x, y] = value;
    }

    public ObstaclesEnum GetValue(Point point)
    {
        return _obstacles[point.X, point.Y];
    }

    public ObstaclesEnum GetValue(int x, int y) => GetValue(new Point(x, y));

    public int Len => _obstacles.GetLength(0);

    public override string ToString()
    {
        return $"({_obstacles.GetValue(0, 0)}, {_obstacles.GetValue(0, 1)}, {_obstacles.GetValue(0, 2)}, {_obstacles.GetValue(1, 0)}, {_obstacles.GetValue(1, 1)}, {_obstacles.GetValue(1, 2)}, {_obstacles.GetValue(2, 0)}, {_obstacles.GetValue(2, 1)}, {_obstacles.GetValue(2, 2)})";
    }

    public bool Equals(Obstacles? other)
    {
        if (other is null)
            return false;

        for (int i = 0; i < this.Len; i++)
        {
            for (int j = 0; j < this.Len; j++)
            {
                if (other.GetValue(i, j) != this.GetValue(i, j))
                    return false;
            }
        }
        return true;
    }
}


public class FlowMap
{
    private Point _endPoint;
    private readonly StrengthVector[,] _flows;

    private void SetEndPoint(Point point)
    {
        _endPoint = point;
        _flows[point.X, point.Y] = new StrengthVector(10, 10);
    }

    public FlowMap(int lenX, int lenY)
    {
        _flows = new StrengthVector[lenX, lenY];
    }

    /// <summary>
    ///     Размер карты по горизонтали
    /// </summary>
    public int LenX => _flows.GetLength(0);

    /// <summary>
    ///     Размер карты по вретикали
    /// </summary>
    public int LenY => _flows.GetLength(1);

    /// <summary>
    ///     Начальная точка
    /// </summary>
    public Point StartPoint { get; set; }

    /// <summary>
    ///     Конечная точка
    /// </summary>
    public Point EndPoint { get => _endPoint; set => SetEndPoint(value); }


    /// <summary>
    ///     Течение в точке (x,y)
    /// </summary>
    public StrengthVector GetFlow(int x, int y) => _flows[x, y];

    /// <summary>
    ///     Течение в точке (x,y)
    /// </summary>
    public StrengthVector GetFlow(Point point) => _flows[point.X, point.Y];

    /// <summary>
    ///     Метод задает течение в точке (x, y)
    /// </summary>
    /// <param name="strength">Сила течения</param>
    /// <param name="angle">Направление течения</param>
    public void SetFLow(int x, int y, int strength, int angle)
    {
        _flows[x, y] = new StrengthVector(strength, angle);
    }

    /// <summary>
    ///     Метод задает течение в точке (x, y)
    /// </summary>
    /// <param name="flowTuple">Пара (сила, направление)</param>
    public void SetFLow(int x, int y, (int, int) flowTuple)
    {
        _flows[x, y] = new StrengthVector(flowTuple.Item1, flowTuple.Item2);
    }
}