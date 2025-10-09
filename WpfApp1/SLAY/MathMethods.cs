using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.SLAY
{
    public class MathMethods
    {
        public double[] SolveByGauss(double[,] A, double[] B)
        {
            int n = B.Length;
            double[] x = new double[n];
            double[,] matrix = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = A[i, j];
                }
                matrix[i, n] = -B[i];
            }

            for (int k = 0; k < n; k++)
            {
                int maxRow = k;
                double maxVal = Math.Abs(matrix[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    if (Math.Abs(matrix[i, k]) > maxVal)
                    {
                        maxVal = Math.Abs(matrix[i, k]);
                        maxRow = i;
                    }
                }

                if (maxRow != k)
                {
                    for (int j = k; j < n + 1; j++)
                    {
                        (matrix[k, j], matrix[maxRow, j]) = (matrix[maxRow, j], matrix[k, j]);
                    }
                }

                for (int i = k + 1; i < n; i++)
                {
                    double factor = matrix[i, k] / matrix[k, k];
                    for (int j = k; j < n + 1; j++)
                    {
                        matrix[i, j] -= factor * matrix[k, j];
                    }
                }
            }

            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = matrix[i, n];
                for (int j = i + 1; j < n; j++)
                {
                    x[i] -= matrix[i, j] * x[j];
                }
                x[i] /= matrix[i, i];
            }

            return x;
        }

        public double[] SolveByJordanGauss(double[,] A, double[] B)
        {
            int n = B.Length;
            double[,] matrix = new double[n, n + 1];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i, j] = A[i, j];
                }
                matrix[i, n] = -B[i];
            }

            for (int k = 0; k < n; k++)
            {
                double divisor = matrix[k, k];
                for (int j = k; j < n + 1; j++)
                {
                    matrix[k, j] /= divisor;
                }

                for (int i = 0; i < n; i++)
                {
                    if (i != k)
                    {
                        double factor = matrix[i, k];
                        for (int j = k; j < n + 1; j++)
                        {
                            matrix[i, j] -= factor * matrix[k, j];
                        }
                    }
                }
            }

            double[] x = new double[n];
            for (int i = 0; i < n; i++)
            {
                x[i] = matrix[i, n];
            }

            return x;
        }

        public double[] SolveByCramer(double[,] A, double[] B)
        {
            int n = B.Length;

            double[] x = new double[n];
            double mainDet = Determinant(A);

            if (Math.Abs(mainDet) < 1e-12)
            {
                throw new Exception("Определитель матрицы A равен нулю. Метод Крамера не применим.");
            }

            for (int i = 0; i < n; i++)
            {
                double[,] tempMatrix = (double[,])A.Clone();
                for (int j = 0; j < n; j++)
                {
                    tempMatrix[j, i] = -B[j];
                }
                x[i] = Determinant(tempMatrix) / mainDet;
            }

            return x;
        }

        public double Determinant(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            double[,] tempMatrix = (double[,])matrix.Clone();
            double det = 1;

            for (int k = 0; k < n; k++)
            {
                int maxRow = k;
                double maxVal = Math.Abs(tempMatrix[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    if (Math.Abs(tempMatrix[i, k]) > maxVal)
                    {
                        maxVal = Math.Abs(tempMatrix[i, k]);
                        maxRow = i;
                    }
                }

                if (maxRow != k)
                {
                    for (int j = 0; j < n; j++)
                    {
                        (tempMatrix[k, j], tempMatrix[maxRow, j]) = (tempMatrix[maxRow, j], tempMatrix[k, j]);
                    }
                    det *= -1; 
                }

                if (Math.Abs(tempMatrix[k, k]) < 1e-12)
                { 
                    return 0;
                }

                det *= tempMatrix[k, k];

                for (int i = k + 1; i < n; i++)
                {
                    double factor = tempMatrix[i, k] / tempMatrix[k, k];
                    for (int j = k + 1; j < n; j++)
                    {
                        tempMatrix[i, j] -= factor * tempMatrix[k, j];
                    }
                }
            }

            return det;
        }
    }
}
