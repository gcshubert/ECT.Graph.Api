using System;

namespace ECT.Graph.Api.Domain.Math;

public class ScientificValue : IComparable<ScientificValue>
{
    public double Coefficient { get; set; }
    public double Exponent { get; set; }

    public ScientificValue(double coefficient, double exponent)
    {
        Coefficient = coefficient;
        Exponent = exponent;
        Normalize();
    }

    public ScientificValue(double value)
    {
        if (value == 0)
        {
            Coefficient = 0;
            Exponent = 0;
            return;
        }
        Exponent = System.Math.Floor(System.Math.Log10(System.Math.Abs(value)));
        Coefficient = value / System.Math.Pow(10, Exponent);
    }

    private void Normalize()
    {
        if (Coefficient == 0)
        {
            Exponent = 0;
            return;
        }
        double logCoeff = System.Math.Log10(System.Math.Abs(Coefficient));
        int shift = (int)System.Math.Floor(logCoeff);
        Coefficient /= System.Math.Pow(10, shift);
        Exponent += shift;
    }

    public double ToDouble()
    {
        return Coefficient * System.Math.Pow(10, Exponent);
    }

    public double ToLog10() => Coefficient == 0 ? double.NegativeInfinity : System.Math.Log10(System.Math.Abs(Coefficient)) + Exponent;

    public override string ToString() => $"{Coefficient} × 10^{Exponent}";

    public int CompareTo(ScientificValue? other)
    {
        if (other == null) return 1;
        return ToLog10().CompareTo(other.ToLog10());
    }

    public static ScientificValue operator +(ScientificValue a, ScientificValue b)
    {
        // Align exponents
        if (a.Exponent > b.Exponent)
        {
            double expDiff = a.Exponent - b.Exponent;
            double adjustedCoeffB = b.Coefficient / System.Math.Pow(10, expDiff);
            return new ScientificValue(a.Coefficient + adjustedCoeffB, a.Exponent);
        }
        else if (b.Exponent > a.Exponent)
        {
            double expDiff = b.Exponent - a.Exponent;
            double adjustedCoeffA = a.Coefficient / System.Math.Pow(10, expDiff);
            return new ScientificValue(adjustedCoeffA + b.Coefficient, b.Exponent);
        }
        else
        {
            return new ScientificValue(a.Coefficient + b.Coefficient, a.Exponent);
        }
    }

    public static ScientificValue operator -(ScientificValue a, ScientificValue b)
    {
        return a + (new ScientificValue(-b.Coefficient, b.Exponent));
    }

    public static ScientificValue operator *(ScientificValue a, ScientificValue b)
    {
        double newCoeff = a.Coefficient * b.Coefficient;
        double newExp = a.Exponent + b.Exponent;
        return new ScientificValue(newCoeff, newExp);
    }

    public static ScientificValue operator /(ScientificValue a, ScientificValue b)
    {
        double newCoeff = a.Coefficient / b.Coefficient;
        double newExp = a.Exponent - b.Exponent;
        return new ScientificValue(newCoeff, newExp);
    }

    public static ScientificValue Pow(ScientificValue a, double power)
    {
        if (a.Coefficient == 0) return new ScientificValue(0, 0);
        double logVal = a.ToLog10() * power;
        double fractionalPart = logVal - System.Math.Floor(logVal);
        double newCoeff = System.Math.Pow(10, fractionalPart);
        double newExp = System.Math.Floor(logVal);
        return new ScientificValue(newCoeff, newExp);
    }

    public static bool operator >(ScientificValue a, ScientificValue b) => a.ToLog10() > b.ToLog10();
    public static bool operator <(ScientificValue a, ScientificValue b) => a.ToLog10() < b.ToLog10();
    public static bool operator >=(ScientificValue a, ScientificValue b) => a.ToLog10() >= b.ToLog10();
    public static bool operator <=(ScientificValue a, ScientificValue b) => a.ToLog10() <= b.ToLog10();

    public static ScientificValue Max(ScientificValue a, ScientificValue b) => a > b ? a : b;
    public static ScientificValue operator *(ScientificValue a, double b)
    {
        return new ScientificValue(a.Coefficient * b, a.Exponent);
    }

    public static ScientificValue operator *(double a, ScientificValue b)
    {
        return b * a;
    }
}