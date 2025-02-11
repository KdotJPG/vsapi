﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vintagestory.API.MathTools
{
    /// <summary>
    /// Represents a vector of 2 floats. Go bug Tyron of you need more utility methods in this class.
    /// </summary>
    public class Vec2f
    {
        public static readonly Vec2f Zero = new Vec2f(0, 0);


        public float X;
        public float Y;

        public Vec2f()
        {

        }

        public Vec2f(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public Vec2f(Size2i size)
        {
            this.X = size.Width;
            this.Y = size.Height;
        }


        #region Operators
        public static Vec2f operator -(Vec2f left, Vec2f right)
        {
            return new Vec2f(left.X - right.X, left.Y - right.Y);
        }

        public static Vec2f operator +(Vec2f left, Vec2f right)
        {
            return new Vec2f(left.X + right.X, left.Y + right.Y);
        }


        public static Vec2f operator -(Vec2f left, float right)
        {
            return new Vec2f(left.X - right, left.Y - right);
        }

        public static Vec2f operator -(float left, Vec2f right)
        {
            return new Vec2f(left - right.X, left - right.Y);
        }

        public static Vec2f operator +(Vec2f left, float right)
        {
            return new Vec2f(left.X + right, left.Y + right);
        }


        public static Vec2f operator *(Vec2f left, float right)
        {
            return new Vec2f(left.X * right, left.Y * right);
        }

        public static Vec2f operator *(float left, Vec2f right)
        {
            return new Vec2f(left * right.X, left * right.Y);
        }

        public static Vec2f operator *(Vec2f left, double right)
        {
            return new Vec2f(left.X * (float)right, left.Y * (float)right);
        }

        public static Vec2f operator *(double left, Vec2f right)
        {
            return new Vec2f((float)left * right.X, (float)left * right.Y);
        }


        public static double operator *(Vec2f left, Vec2f right)
        {
            return left.X * right.X + left.Y * right.Y;
        }

        public static Vec2f operator /(Vec2f left, float right)
        {
            return new Vec2f(left.X / right, left.Y / right);
        }

        #endregion


        public override string ToString()
        {
            return "X=" + X + ", Y=" + Y;
        }

        public float Length()
        {
            return GameMath.Sqrt(X * X + Y * Y);
        }

        public float DistanceTo(float x, float y)
        {
            float dx = X - x;
            float dy = Y - y;
            return GameMath.Sqrt(dx * dx + dy * dy);
        }


        public static float Distance(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;

            return GameMath.Sqrt(dx * dx + dy * dy);
        }

        public Vec2f Clone()
        {
            return new Vec2f(X, Y);
        }
    }



}
