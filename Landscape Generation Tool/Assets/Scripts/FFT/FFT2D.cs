using System;
using System.Numerics;

public class FFT2D : FFT
{
    public FFT2D(int size) : base(size)
    {
    }

    public Complex[][] ToComplex(float[,] input)
        {
            var result = new Complex[Size][];

            for (var i = 0; i < Size; i++)
            {
                result[i] = new Complex[Size];

                for (var j = 0; j < Size; j++)
                {
                    var pixel = new Complex(input[i, j], 0);

                    result[i][j] = pixel;
                }
            }
            return result;
        }

    public float[,] Inverse(Complex[][] inputComplex)
    {
        var p = new Complex[Size][];
        var f = new Complex[Size][];
        var t = new Complex[Size][];

        var floatImage = new float[Size, Size];

        //CALCULATE P
        for (var l = 0; l < Size; l++)
        {
            p[l] = Inverse(inputComplex[l]);
        }

        //TRANSPOSE AND COMPUTE
        for (var l = 0; l < Size; l++)
        {
            t[l] = new Complex[Size];

            for (var k = 0; k < Size; k++)
            {
                t[l][k] = p[k][l] / (Size * Size);
            }

            f[l] = Inverse(t[l]);
        }

        for (var k = 0; k < Size; k++)
        {
            for (var l = 0; l < Size; l++)
            {
                floatImage[k, l] = (float)Math.Abs(f[k][l].Real);
            }
        }

        return floatImage;
    }

    public Complex[][] Forward(float[,] input)
    {
        var p = new Complex[Size][];
        var f = new Complex[Size][];
        var t = new Complex[Size][];

        //CONVERT TO COMPLEX NUMBERS
        var complexInput = ToComplex(input);

        //CALCULATE P
        for (var l = 0; l < Size; l++)
        {
            p[l] = Forward(complexInput[l]);
        }

        //TANSPOSE AND COMPUTE
        for (var l = 0; l < Size; l++)
        {
            t[l] = new Complex[Size];

            for (var k = 0; k < Size; k++)
            {
                t[l][k] = p[k][l] ;
            }

            f[l] = Forward(t[l]);
        }

        return f ;
    }

}
